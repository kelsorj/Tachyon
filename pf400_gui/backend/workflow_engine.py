"""
Workflow Engine - State machine for executing workflows.

Workflow States:
    IDLE -> RUNNING -> COMPLETED
              |-> PAUSED -> RUNNING
              |-> ERROR -> RECOVERING -> RUNNING
              |-> CANCELLED

Step States:
    PENDING -> EXECUTING -> COMPLETED
                 |-> FAILED
                 |-> SKIPPED
"""

import asyncio
import time
import json
import subprocess
import tempfile
import os
from datetime import datetime
from typing import Dict, Any, Optional, List, Callable
from enum import Enum
from dataclasses import dataclass, field
import threading

import db as mongodb


class WorkflowState(str, Enum):
    IDLE = "idle"
    RUNNING = "running"
    PAUSED = "paused"
    ERROR = "error"
    RECOVERING = "recovering"
    COMPLETED = "completed"
    CANCELLED = "cancelled"
    SIMULATING = "simulating"


class StepState(str, Enum):
    PENDING = "pending"
    EXECUTING = "executing"
    COMPLETED = "completed"
    FAILED = "failed"
    SKIPPED = "skipped"


class StepType(str, Enum):
    TRIGGER = "trigger"           # Start node
    DEVICE_ACTION = "device_action"  # Call a device API
    CODE_MODULE = "code_module"   # Execute Python/JS/C# code
    CONDITIONAL = "conditional"   # Branch based on condition
    DELAY = "delay"               # Wait for time
    LOOP = "loop"                 # Repeat steps (legacy)
    LOOP_START = "loop_start"     # Start of a loop
    LOOP_END = "loop_end"         # End of a loop (returns to loop_start)
    PARALLEL = "parallel"         # Execute multiple branches
    END = "end"                   # End node


@dataclass
class WorkflowContext:
    """Runtime context for a workflow execution."""
    run_id: str
    workflow_id: str
    workflow: Dict[str, Any]
    device_collection: Dict[str, Any]
    variables: Dict[str, Any] = field(default_factory=dict)
    step_states: Dict[str, str] = field(default_factory=dict)
    step_results: Dict[str, Any] = field(default_factory=dict)
    current_step_id: Optional[str] = None
    state: WorkflowState = WorkflowState.IDLE
    simulate: bool = False
    error: Optional[str] = None
    started_at: Optional[datetime] = None
    completed_at: Optional[datetime] = None


class WorkflowEngine:
    """
    Executes workflows with state management and real-time updates.
    """
    
    def __init__(self):
        self._active_runs: Dict[str, WorkflowContext] = {}
        self._listeners: List[Callable[[str, Dict[str, Any]], None]] = []
        self._lock = threading.Lock()
    
    def add_listener(self, callback: Callable[[str, Dict[str, Any]], None]):
        """Add a listener for workflow state changes."""
        self._listeners.append(callback)
    
    def remove_listener(self, callback: Callable[[str, Dict[str, Any]], None]):
        """Remove a listener."""
        if callback in self._listeners:
            self._listeners.remove(callback)
    
    def _notify(self, run_id: str, event: Dict[str, Any]):
        """Notify all listeners of an event."""
        for listener in self._listeners:
            try:
                listener(run_id, event)
            except Exception as e:
                print(f"Error notifying listener: {e}")
    
    def _persist_run(self, ctx: WorkflowContext):
        """Persist the current run state to MongoDB."""
        mongodb.update_workflow_run(ctx.run_id, {
            "state": ctx.state.value,
            "current_step_id": ctx.current_step_id,
            "step_states": ctx.step_states,
            "step_results": ctx.step_results,
            "variables": ctx.variables,
            "error": ctx.error,
            "completed_at": ctx.completed_at,
        })
    
    def get_active_runs(self) -> List[Dict[str, Any]]:
        """Get all currently active workflow runs."""
        with self._lock:
            return [
                {
                    "run_id": ctx.run_id,
                    "workflow_id": ctx.workflow_id,
                    "state": ctx.state.value,
                    "current_step_id": ctx.current_step_id,
                    "simulate": ctx.simulate,
                }
                for ctx in self._active_runs.values()
            ]
    
    def get_run_status(self, run_id: str) -> Optional[Dict[str, Any]]:
        """Get the current status of a run."""
        with self._lock:
            ctx = self._active_runs.get(run_id)
            if ctx:
                return {
                    "run_id": ctx.run_id,
                    "workflow_id": ctx.workflow_id,
                    "state": ctx.state.value,
                    "current_step_id": ctx.current_step_id,
                    "step_states": ctx.step_states,
                    "step_results": ctx.step_results,
                    "variables": ctx.variables,
                    "error": ctx.error,
                    "simulate": ctx.simulate,
                }
        # Check MongoDB for completed runs
        return mongodb.get_workflow_run(run_id)
    
    async def start_workflow(
        self,
        workflow_id: str,
        collection_id: Optional[str] = None,
        variables: Optional[Dict[str, Any]] = None,
        simulate: bool = False,
    ) -> str:
        """
        Start a workflow execution.
        
        Args:
            workflow_id: The workflow to execute
            collection_id: Optional device collection override
            variables: Initial variables
            simulate: If True, skip actual device calls
            
        Returns:
            run_id: The ID of the new run
        """
        # Load workflow
        workflow = mongodb.get_workflow(workflow_id)
        if not workflow:
            raise ValueError(f"Workflow {workflow_id} not found")
        
        # Load device collection
        coll_id = collection_id or workflow.get("device_collection_id")
        device_collection = {}
        if coll_id:
            device_collection = mongodb.get_device_collection(coll_id) or {}
        
        # Create run record
        run_data = {
            "workflow_id": workflow_id,
            "device_collection_id": coll_id,
            "variables": variables or {},
            "simulate": simulate,
            "state": WorkflowState.RUNNING.value,
        }
        run = mongodb.create_workflow_run(run_data)
        if not run:
            raise RuntimeError("Failed to create workflow run")
        
        run_id = run["run_id"]
        
        # Create context
        ctx = WorkflowContext(
            run_id=run_id,
            workflow_id=workflow_id,
            workflow=workflow,
            device_collection=device_collection,
            variables=variables or {},
            state=WorkflowState.RUNNING if not simulate else WorkflowState.SIMULATING,
            simulate=simulate,
            started_at=datetime.utcnow(),
        )
        
        with self._lock:
            self._active_runs[run_id] = ctx
        
        # Notify listeners
        self._notify(run_id, {
            "type": "workflow_started",
            "run_id": run_id,
            "workflow_id": workflow_id,
            "simulate": simulate,
        })
        
        # Start execution in background
        asyncio.create_task(self._execute_workflow(ctx))
        
        return run_id
    
    async def pause_workflow(self, run_id: str) -> bool:
        """Pause a running workflow."""
        with self._lock:
            ctx = self._active_runs.get(run_id)
            if not ctx:
                return False
            if ctx.state not in (WorkflowState.RUNNING, WorkflowState.SIMULATING):
                return False
            ctx.state = WorkflowState.PAUSED
        
        self._persist_run(ctx)
        self._notify(run_id, {"type": "workflow_paused", "run_id": run_id})
        return True
    
    async def resume_workflow(self, run_id: str) -> bool:
        """Resume a paused workflow."""
        with self._lock:
            ctx = self._active_runs.get(run_id)
            if not ctx:
                return False
            if ctx.state != WorkflowState.PAUSED:
                return False
            ctx.state = WorkflowState.RUNNING if not ctx.simulate else WorkflowState.SIMULATING
        
        self._persist_run(ctx)
        self._notify(run_id, {"type": "workflow_resumed", "run_id": run_id})
        
        # Continue execution
        asyncio.create_task(self._execute_workflow(ctx))
        return True
    
    async def cancel_workflow(self, run_id: str) -> bool:
        """Cancel a workflow."""
        with self._lock:
            ctx = self._active_runs.get(run_id)
            if not ctx:
                return False
            ctx.state = WorkflowState.CANCELLED
            ctx.completed_at = datetime.utcnow()
        
        self._persist_run(ctx)
        self._notify(run_id, {"type": "workflow_cancelled", "run_id": run_id})
        
        with self._lock:
            self._active_runs.pop(run_id, None)
        return True
    
    async def _execute_workflow(self, ctx: WorkflowContext):
        """Main workflow execution loop."""
        try:
            nodes = ctx.workflow.get("nodes", [])
            edges = ctx.workflow.get("edges", [])
            
            # Build node lookup and edge graph
            node_map = {n["id"]: n for n in nodes}
            outgoing = {}  # node_id -> [(target_id, edge_data), ...]
            for edge in edges:
                src = edge.get("source")
                tgt = edge.get("target")
                if src and tgt:
                    outgoing.setdefault(src, []).append((tgt, edge))
            
            # Find start node (type=trigger or first node with no incoming edges)
            incoming_nodes = {e.get("target") for e in edges}
            start_nodes = [n for n in nodes if n.get("type") == StepType.TRIGGER.value]
            if not start_nodes:
                start_nodes = [n for n in nodes if n["id"] not in incoming_nodes]
            
            if not start_nodes:
                ctx.state = WorkflowState.COMPLETED
                ctx.completed_at = datetime.utcnow()
                self._persist_run(ctx)
                self._notify(ctx.run_id, {"type": "workflow_completed", "run_id": ctx.run_id})
                return
            
            # Execute from start nodes
            pending = [n["id"] for n in start_nodes]
            
            while pending and ctx.state in (WorkflowState.RUNNING, WorkflowState.SIMULATING):
                step_id = pending.pop(0)
                
                # Check for pause
                while ctx.state == WorkflowState.PAUSED:
                    await asyncio.sleep(0.1)
                
                if ctx.state not in (WorkflowState.RUNNING, WorkflowState.SIMULATING):
                    break
                
                node = node_map.get(step_id)
                if not node:
                    continue
                
                ctx.current_step_id = step_id
                ctx.step_states[step_id] = StepState.EXECUTING.value
                self._persist_run(ctx)
                # Track how many times each step has executed (for loop iterations)
                if not hasattr(ctx, '_step_exec_counts'):
                    ctx._step_exec_counts = {}
                ctx._step_exec_counts[step_id] = ctx._step_exec_counts.get(step_id, 0) + 1
                exec_count = ctx._step_exec_counts[step_id]
                
                # Get current loop iteration for logging
                loop_iteration = ctx.variables.get("loop", {}).get("iteration", None)
                
                self._notify(ctx.run_id, {
                    "type": "step_started",
                    "run_id": ctx.run_id,
                    "step_id": step_id,
                    "step_type": node.get("type"),
                    "loop_iteration": loop_iteration,
                })
                
                try:
                    # Execute the step
                    result = await self._execute_step(ctx, node)
                    
                    # Store results with unique key to preserve all iterations
                    result_key = f"{step_id}_{exec_count}" if exec_count > 1 else step_id
                    result_with_meta = {**result, "_loop_iteration": loop_iteration, "_exec_count": exec_count}
                    ctx.step_results[result_key] = result_with_meta
                    ctx.step_states[step_id] = StepState.COMPLETED.value
                    
                    self._notify(ctx.run_id, {
                        "type": "step_completed",
                        "run_id": ctx.run_id,
                        "step_id": step_id,
                        "result": result_with_meta,
                        "loop_iteration": loop_iteration,
                    })
                    
                    # Determine next steps
                    next_steps = self._get_next_steps(node, result, outgoing.get(step_id, []))
                    pending.extend(next_steps)
                    
                except Exception as e:
                    ctx.step_states[step_id] = StepState.FAILED.value
                    ctx.step_results[step_id] = {"error": str(e), "_loop_iteration": loop_iteration}
                    ctx.state = WorkflowState.ERROR
                    ctx.error = str(e)
                    
                    self._notify(ctx.run_id, {
                        "type": "step_failed",
                        "run_id": ctx.run_id,
                        "step_id": step_id,
                        "error": str(e),
                    })
                    break
            
            # Finalize
            if ctx.state in (WorkflowState.RUNNING, WorkflowState.SIMULATING):
                ctx.state = WorkflowState.COMPLETED
            ctx.completed_at = datetime.utcnow()
            ctx.current_step_id = None
            self._persist_run(ctx)
            
            self._notify(ctx.run_id, {
                "type": "workflow_completed" if ctx.state == WorkflowState.COMPLETED else "workflow_ended",
                "run_id": ctx.run_id,
                "final_state": ctx.state.value,
            })
            
        except Exception as e:
            ctx.state = WorkflowState.ERROR
            ctx.error = str(e)
            ctx.completed_at = datetime.utcnow()
            self._persist_run(ctx)
            self._notify(ctx.run_id, {
                "type": "workflow_error",
                "run_id": ctx.run_id,
                "error": str(e),
            })
        
        finally:
            with self._lock:
                self._active_runs.pop(ctx.run_id, None)
    
    async def _execute_step(self, ctx: WorkflowContext, node: Dict[str, Any]) -> Dict[str, Any]:
        """Execute a single workflow step."""
        step_type = node.get("type", "")
        data = node.get("data", {})
        
        if step_type == StepType.TRIGGER.value:
            # Start node - nothing to execute
            return {"triggered": True}
        
        elif step_type == StepType.END.value:
            # End node - nothing to execute
            return {"ended": True}
        
        elif step_type == StepType.DELAY.value:
            # Wait for specified time
            delay_ms = data.get("delay_ms", 1000)
            if not ctx.simulate:
                await asyncio.sleep(delay_ms / 1000.0)
            return {"delayed_ms": delay_ms}
        
        elif step_type == StepType.DEVICE_ACTION.value:
            return await self._execute_device_action(ctx, data)
        
        elif step_type == StepType.CODE_MODULE.value:
            return await self._execute_code_module(ctx, data)
        
        elif step_type == StepType.CONDITIONAL.value:
            return await self._execute_conditional(ctx, data)
        
        elif step_type == StepType.LOOP_START.value:
            return await self._execute_loop_start(ctx, node)
        
        elif step_type == StepType.LOOP_END.value:
            return await self._execute_loop_end(ctx, node)
        
        else:
            return {"unknown_type": step_type}
    
    async def _execute_device_action(self, ctx: WorkflowContext, data: Dict[str, Any]) -> Dict[str, Any]:
        """Execute a device action step."""
        action = data.get("action", "")
        
        # Handle pick-place action (new format from workflow UI)
        if action == "pick-place":
            return await self._execute_pick_place(ctx, data)
        elif action == "move-to":
            return await self._execute_move_to(ctx, data)
        elif action == "safe":
            return await self._execute_safe(ctx, data)
        elif action == "home":
            return await self._execute_home(ctx, data)
        
        # Legacy format: device + action + params
        device_alias = data.get("device") or data.get("robot")
        params = data.get("params", {})
        
        if not device_alias:
            raise ValueError("No device/robot specified for device action")
        
        # Try to find device directly in database (skip collection lookup)
        device_doc = mongodb.get_device_by_name(device_alias)
        
        # If not found directly, try collection lookup
        if not device_doc:
            devices = ctx.device_collection.get("devices", [])
            for d in devices:
                if d.get("alias") == device_alias or d.get("device_name") == device_alias:
                    device_doc = mongodb.get_device_by_name(d.get("device_name"))
                    break
        
        if not device_doc:
            raise ValueError(f"Device '{device_alias}' not found")
        
        device_name = device_doc.get("name")
        
        if ctx.simulate:
            print(f"[Workflow Sim] Device action: {device_name} -> {action}")
            return {
                "simulated": True,
                "device": device_name,
                "action": action,
                "params": params,
            }
        
        # Get device connection info and call its API
        connection = device_doc.get("connection", {})
        api_url = connection.get("api_url") or f"http://{connection.get('backend_host', 'localhost')}:{connection.get('api_port', 8091)}"
        
        # Make HTTP request to device API
        import aiohttp
        async with aiohttp.ClientSession() as session:
            endpoint = f"{api_url}/{action}"
            async with session.post(endpoint, json=params) as resp:
                if resp.status >= 400:
                    text = await resp.text()
                    raise RuntimeError(f"Device action failed: {resp.status} - {text}")
                result = await resp.json()
                return {"device": device_name, "action": action, "result": result}

    async def _execute_pick_place(self, ctx: WorkflowContext, data: Dict[str, Any]) -> Dict[str, Any]:
        """
        Execute a pick-and-place action.
        
        The robot is determined AUTOMATICALLY from the devices' robot_access configuration.
        Each device (e.g., PlatePadHandoff) has a robot_access array that specifies:
        - robot_name: which robot can reach this device (e.g., "PF400-021")
        - teachpoint_id: the teachpoint on that robot (e.g., "pf400021handoff")
        
        This function:
        1. Looks up both source and target devices
        2. Finds a COMMON robot that can reach BOTH devices
        3. Uses that robot's teachpoint_id for each device
        4. Calls the robot's pick-place API
        """
        source_device = data.get("source_device")
        target_device = data.get("target_device")
        labware_id = data.get("labware_id")
        robot_name_override = data.get("robot")  # Optional: can override auto-detection
        
        if not source_device:
            raise ValueError("No source device specified for pick-place")
        if not target_device:
            raise ValueError("No target device specified for pick-place")
        
        # ============================================================================
        # TEACHPOINT RESOLUTION HELPER
        # ============================================================================
        def normalize_device_name_to_teachpoint(device_name: str) -> str:
            """
            Convert a device name to a likely teachpoint ID.
            Example: "PlatePadRight-001" -> "platepadright"
            Example: "PlatePadHandoff" -> "platepadhandoff"
            
            ONLY used as a fallback when device has no robot_access configured!
            """
            import re
            normalized = device_name.lower()
            normalized = re.sub(r'[-_]\d+$', '', normalized)
            normalized = re.sub(r'[^a-z0-9]', '', normalized)
            return normalized

        # Get labware info from workflow
        labware_info = None
        for lw in ctx.workflow.get("labware", []):
            if lw.get("id") == labware_id:
                labware_info = lw
                break
        
        # ============================================================================
        # LOOK UP DEVICES AND THEIR ROBOT ACCESS CONFIGURATIONS
        # ============================================================================
        source_doc = mongodb.get_device_by_name(source_device)
        target_doc = mongodb.get_device_by_name(target_device)
        
        if not source_doc:
            raise ValueError(f"Source device '{source_device}' not found in database")
        if not target_doc:
            raise ValueError(f"Target device '{target_device}' not found in database")
        
        source_robot_access = source_doc.get("robot_access", [])
        target_robot_access = target_doc.get("robot_access", [])
        
        # ============================================================================
        # FIND A COMMON ROBOT THAT CAN REACH BOTH DEVICES
        # ============================================================================
        # Each device's robot_access is an array like:
        # [{"robot_name": "PF400-021", "teachpoint_id": "pf400021handoff", "access_type": "pick_place"}]
        #
        # We need to find a robot that appears in BOTH arrays.
        # ============================================================================
        
        robot_name = None
        pick_teachpoint = None
        place_teachpoint = None
        
        # Build lookup of robots that can reach source device
        source_robots = {}
        for access in source_robot_access:
            rname = access.get("robot_name")
            if rname:
                source_robots[rname] = access.get("teachpoint_id")
        
        # Find a robot that can also reach target device
        for access in target_robot_access:
            rname = access.get("robot_name")
            if rname and rname in source_robots:
                # Found a common robot!
                robot_name = rname
                pick_teachpoint = source_robots[rname]
                place_teachpoint = access.get("teachpoint_id")
                print(f"[Workflow] Auto-detected robot '{robot_name}' can reach both devices")
                break
        
        # Allow manual override if specified
        if robot_name_override:
            robot_name = robot_name_override
            # Re-lookup teachpoints for the overridden robot
            pick_teachpoint = None
            place_teachpoint = None
            for access in source_robot_access:
                if access.get("robot_name") == robot_name:
                    pick_teachpoint = access.get("teachpoint_id")
                    break
            for access in target_robot_access:
                if access.get("robot_name") == robot_name:
                    place_teachpoint = access.get("teachpoint_id")
                    break
        
        # ============================================================================
        # FALLBACK: If no robot_access configured, use normalized device names
        # ============================================================================
        if not robot_name:
            # No common robot found - use default robot and normalized names
            robot_name = "PF400-021"  # Default robot
            print(f"[Workflow] WARNING: No common robot found for {source_device} and {target_device}, using default: {robot_name}")
        
        if not pick_teachpoint:
            pick_teachpoint = normalize_device_name_to_teachpoint(source_device)
            print(f"[Workflow] WARNING: No robot_access teachpoint for {source_device}, using normalized: {pick_teachpoint}")
        
        if not place_teachpoint:
            place_teachpoint = normalize_device_name_to_teachpoint(target_device)
            print(f"[Workflow] WARNING: No robot_access teachpoint for {target_device}, using normalized: {place_teachpoint}")
        
        # IMPORTANT: If no teachpoint found in robot_access, normalize the device name
        if not place_teachpoint:
            place_teachpoint = normalize_device_name_to_teachpoint(target_device)
            print(f"[Workflow] WARNING: No robot_access found for {target_device}, using normalized name: {place_teachpoint}")
        
        print(f"[Workflow] Pick-Place: {robot_name} moves labware from {source_device} ({pick_teachpoint}) to {target_device} ({place_teachpoint})")
        
        if ctx.simulate:
            print(f"[Workflow Sim] Pick-Place: {source_device} → {target_device}")
            return {
                "simulated": True,
                "robot": robot_name,
                "source": source_device,
                "target": target_device,
                "pick_teachpoint": pick_teachpoint,
                "place_teachpoint": place_teachpoint,
                "labware": labware_info.get("name") if labware_info else None,
            }
        
        # Call the PF400 pick-place API
        import aiohttp
        api_url = "http://localhost:8091"  # Default PF400 backend
        
        # Get labware type - could be type_id or type (name)
        labware_type_id = None
        if labware_info:
            labware_type_id = labware_info.get("type_id")
            if not labware_type_id:
                # Look up by type name
                labware_type_name = labware_info.get("type")
                if labware_type_name:
                    # Query labware types to find the ID
                    labware_types = mongodb.get_all_labware_types()
                    for lt in labware_types:
                        if lt.get("name") == labware_type_name:
                            labware_type_id = lt.get("_id")
                            break
        
        if not labware_type_id:
            raise ValueError("No labware type specified for pick-place action")
        
        print(f"[Workflow] Using labware_type_id: {labware_type_id}")
        
        async with aiohttp.ClientSession() as session:
            # Call pick-place endpoint using resolved teachpoints
            payload = {
                "pick_teachpoint_id": pick_teachpoint,
                "place_teachpoint_id": place_teachpoint,
                "labware_type_id": labware_type_id,
                "orientation": "landscape",
                # Field names must match PF400PickPlaceRequest in main.py.
                # The previous keys (pick_speed_profile/place_speed_profile) were
                # silently dropped by Pydantic, leaving both phases at profile 1 (slow).
                # Profile 2 = vendor-max speed, standard ramps (safe with a plate).
                # Profile 3 = vendor-max speed, sharper ramps (used when empty).
                "speed_no_plate": 3,
                "speed_holding_plate": 2,
            }
            print(f"[Workflow] Pick-place payload: {payload}")
            
            endpoint = f"{api_url}/pf400/pick-place"
            async with session.post(endpoint, json=payload) as resp:
                if resp.status >= 400:
                    text = await resp.text()
                    raise RuntimeError(f"Pick-place failed: {resp.status} - {text}")
                result = await resp.json()
                
                # Update labware location in context
                if labware_info:
                    labware_info["current_location"] = target_device
                
                return {
                    "robot": robot_name,
                    "source": source_device,
                    "target": target_device,
                    "pick_teachpoint": pick_teachpoint,
                    "place_teachpoint": place_teachpoint,
                    "labware": labware_info.get("name") if labware_info else None,
                    "result": result,
                }

    async def _execute_move_to(self, ctx: WorkflowContext, data: Dict[str, Any]) -> Dict[str, Any]:
        """Execute a move-to action."""
        robot_name = data.get("robot")
        target_device = data.get("target_device")
        
        if not robot_name:
            raise ValueError("No robot specified for move-to")
        if not target_device:
            raise ValueError("No target specified for move-to")
        
        print(f"[Workflow] Move-To: {robot_name} → {target_device}")
        
        if ctx.simulate:
            return {"simulated": True, "robot": robot_name, "target": target_device}
        
        import aiohttp
        api_url = "http://localhost:8091"
        
        async with aiohttp.ClientSession() as session:
            endpoint = f"{api_url}/teachpoints/move/{target_device}"
            async with session.post(endpoint, params={"speed_profile": 2}) as resp:
                if resp.status >= 400:
                    text = await resp.text()
                    raise RuntimeError(f"Move-to failed: {resp.status} - {text}")
                result = await resp.json()
                return {"robot": robot_name, "target": target_device, "result": result}

    async def _execute_safe(self, ctx: WorkflowContext, data: Dict[str, Any]) -> Dict[str, Any]:
        """Execute a safe position command."""
        robot_name = data.get("robot")
        
        print(f"[Workflow] Safe: {robot_name}")
        
        if ctx.simulate:
            return {"simulated": True, "robot": robot_name, "action": "safe"}
        
        import aiohttp
        api_url = "http://localhost:8091"
        
        async with aiohttp.ClientSession() as session:
            endpoint = f"{api_url}/pf400/safe"
            async with session.post(endpoint, params={"speed_profile": 2}) as resp:
                if resp.status >= 400:
                    text = await resp.text()
                    raise RuntimeError(f"Safe failed: {resp.status} - {text}")
                result = await resp.json()
                return {"robot": robot_name, "action": "safe", "result": result}

    async def _execute_home(self, ctx: WorkflowContext, data: Dict[str, Any]) -> Dict[str, Any]:
        """Execute a home command."""
        robot_name = data.get("robot")
        
        print(f"[Workflow] Home: {robot_name}")
        
        if ctx.simulate:
            return {"simulated": True, "robot": robot_name, "action": "home"}
        
        import aiohttp
        api_url = "http://localhost:8091"
        
        async with aiohttp.ClientSession() as session:
            endpoint = f"{api_url}/pf400/home"
            async with session.post(endpoint) as resp:
                if resp.status >= 400:
                    text = await resp.text()
                    raise RuntimeError(f"Home failed: {resp.status} - {text}")
                result = await resp.json()
                return {"robot": robot_name, "action": "home", "result": result}
    
    async def _execute_code_module(self, ctx: WorkflowContext, data: Dict[str, Any]) -> Dict[str, Any]:
        """Execute a code module step."""
        module_id = data.get("module_id")
        inline_code = data.get("code")
        language = data.get("language", "python")
        params = data.get("params", {})
        
        # Get code - either from module or inline
        code = inline_code
        if module_id and not code:
            module = mongodb.get_code_module(module_id)
            if not module:
                raise ValueError(f"Code module '{module_id}' not found")
            code = module.get("code", "")
            language = module.get("language", language)
        
        if not code:
            return {"skipped": True, "reason": "no code"}
        
        if ctx.simulate:
            return {"simulated": True, "language": language, "code_length": len(code)}
        
        # Execute the code
        return await self._run_code(language, code, params, ctx.variables)
    
    async def _run_code(
        self,
        language: str,
        code: str,
        params: Dict[str, Any],
        variables: Dict[str, Any],
    ) -> Dict[str, Any]:
        """Run code in a subprocess."""
        
        # Prepare input data
        input_data = json.dumps({"params": params, "variables": variables})
        
        if language == "python":
            # Wrap code to read input and write output
            wrapper = f'''
import json
import sys

_input = json.loads(sys.stdin.read())
params = _input["params"]
variables = _input["variables"]
result = None

{code}

print(json.dumps({{"result": result, "variables": variables}}))
'''
            cmd = ["python3", "-c", wrapper]
            
        elif language == "javascript":
            wrapper = f'''
const readline = require('readline');
let inputData = '';

process.stdin.on('data', chunk => inputData += chunk);
process.stdin.on('end', async () => {{
  const _input = JSON.parse(inputData);
  const params = _input.params;
  let variables = _input.variables;
  let result = null;
  
  {code}
  
  console.log(JSON.stringify({{result, variables}}));
}});
'''
            cmd = ["node", "-e", wrapper]
            
        elif language == "csharp":
            # C# requires compilation - use dotnet script or CSX
            # For simplicity, use dotnet-script if available
            wrapper = f'''
using System;
using System.Text.Json;

var inputJson = Console.In.ReadToEnd();
var input = JsonSerializer.Deserialize<Dictionary<string, object>>(inputJson);
var paramsObj = input["params"];
var variables = input["variables"] as Dictionary<string, object>;
object result = null;

{code}

Console.WriteLine(JsonSerializer.Serialize(new {{ result, variables }}));
'''
            # Write to temp file
            with tempfile.NamedTemporaryFile(mode='w', suffix='.csx', delete=False) as f:
                f.write(wrapper)
                temp_file = f.name
            cmd = ["dotnet", "script", temp_file]
            
        else:
            raise ValueError(f"Unsupported language: {language}")
        
        # Run subprocess
        try:
            proc = await asyncio.create_subprocess_exec(
                *cmd,
                stdin=asyncio.subprocess.PIPE,
                stdout=asyncio.subprocess.PIPE,
                stderr=asyncio.subprocess.PIPE,
            )
            stdout, stderr = await asyncio.wait_for(
                proc.communicate(input=input_data.encode()),
                timeout=60.0,
            )
            
            if proc.returncode != 0:
                raise RuntimeError(f"Code execution failed: {stderr.decode()}")
            
            output = json.loads(stdout.decode())
            
            # Update variables from output
            if "variables" in output:
                variables.update(output["variables"])
            
            return {"result": output.get("result"), "language": language}
            
        except asyncio.TimeoutError:
            raise RuntimeError("Code execution timed out (60s)")
        finally:
            if language == "csharp" and 'temp_file' in locals():
                try:
                    os.unlink(temp_file)
                except:
                    pass
    
    async def _execute_conditional(self, ctx: WorkflowContext, data: Dict[str, Any]) -> Dict[str, Any]:
        """Execute a conditional step - evaluates condition and returns branch result."""
        condition = data.get("condition", "true")
        
        # Simple expression evaluation using variables
        try:
            # Create safe eval context with variables
            eval_context = dict(ctx.variables)
            eval_context["True"] = True
            eval_context["False"] = False
            eval_context["None"] = None
            
            result = eval(condition, {"__builtins__": {}}, eval_context)
            return {"condition": condition, "result": bool(result)}
        except Exception as e:
            return {"condition": condition, "result": False, "error": str(e)}

    async def _execute_loop_start(self, ctx: WorkflowContext, node: Dict[str, Any]) -> Dict[str, Any]:
        """Initialize or continue a loop."""
        node_id = node.get("id")
        data = node.get("data", {})
        
        loop_type = data.get("loop_type", "count")
        loop_variable = data.get("loop_variable", "i")
        iterations = data.get("iterations", 1)
        
        # Debug: log what we're reading from node data
        print(f"[Workflow] Loop Start data: iterations={iterations}, loop_type={loop_type}, raw_data={data}")
        
        # Initialize loop state if not exists
        loop_key = f"_loop_{node_id}"
        if loop_key not in ctx.variables:
            ctx.variables[loop_key] = {
                "iteration": 0,
                "max_iterations": iterations,
                "loop_type": loop_type,
                "condition": data.get("condition", "true"),
                "collection": data.get("loop_collection"),
            }
            print(f"[Workflow] Loop initialized with max_iterations={iterations}")
        
        loop_state = ctx.variables[loop_key]
        current_iteration = loop_state["iteration"]
        
        # Check if loop should continue
        should_continue = False
        
        if loop_type == "count":
            should_continue = current_iteration < loop_state["max_iterations"]
        elif loop_type == "while":
            # Evaluate condition
            try:
                eval_context = dict(ctx.variables)
                eval_context["loop"] = {loop_variable: current_iteration}
                eval_context["True"] = True
                eval_context["False"] = False
                should_continue = bool(eval(loop_state["condition"], {"__builtins__": {}}, eval_context))
            except:
                should_continue = False
        elif loop_type == "for_each":
            collection = loop_state.get("collection", "labware")
            if collection == "labware":
                items = ctx.workflow.get("labware", [])
            else:
                items = []
            should_continue = current_iteration < len(items)
        
        # Set loop variable for use in loop body
        ctx.variables["loop"] = {
            loop_variable: current_iteration,
            "index": current_iteration,
            "iteration": current_iteration + 1,  # 1-indexed for display
            "first": current_iteration == 0,
        }
        
        print(f"[Workflow] Loop Start: iteration {current_iteration + 1}, continue={should_continue}")
        
        return {
            "loop_id": node_id,
            "iteration": current_iteration,
            "should_continue": should_continue,
            "loop_variable": loop_variable,
            # Include workflow structure for exit path calculation
            "_nodes": ctx.workflow.get("nodes", []),
            "_edges": ctx.workflow.get("edges", []),
        }

    async def _execute_loop_end(self, ctx: WorkflowContext, node: Dict[str, Any]) -> Dict[str, Any]:
        """End of loop - increment counter and determine if we should loop back."""
        data = node.get("data", {})
        node_id = node.get("id")
        
        # Get the paired loop_start from node data (set via UI dropdown)
        loop_start_id = data.get("paired_loop_start")
        
        # Fallback: look for edge connection if no paired_loop_start
        if not loop_start_id:
            edges = ctx.workflow.get("edges", [])
            nodes = ctx.workflow.get("nodes", [])
            
            # Find outgoing edges from this loop_end
            outgoing = [e for e in edges if e.get("source") == node_id]
            
            # Find connected loop_start node
            for edge in outgoing:
                target_id = edge.get("target")
                target_node = next((n for n in nodes if n.get("id") == target_id), None)
                if target_node:
                    node_type = target_node.get("type") or target_node.get("data", {}).get("nodeType")
                    if node_type == StepType.LOOP_START.value:
                        loop_start_id = target_id
                        break
        
        if not loop_start_id:
            # No loop_start found - just continue normally
            print(f"[Workflow] Loop End: no paired Loop Start selected, continuing normally")
            return {"loop_complete": True, "reason": "no_loop_start_paired"}
        
        loop_key = f"_loop_{loop_start_id}"
        if loop_key not in ctx.variables:
            return {"loop_complete": True, "reason": "loop_not_initialized"}
        
        loop_state = ctx.variables[loop_key]
        loop_state["iteration"] += 1
        
        print(f"[Workflow] Loop End: incremented to iteration {loop_state['iteration']}")
        
        return {
            "loop_id": loop_start_id,
            "iteration": loop_state["iteration"],
            "loop_back": True,
        }
    
    def _get_next_steps(
        self,
        node: Dict[str, Any],
        result: Dict[str, Any],
        edges: List[tuple],
    ) -> List[str]:
        """Determine which nodes to execute next based on result and edges."""
        next_steps = []
        
        node_type = node.get("type", "")
        
        if node_type == StepType.CONDITIONAL.value:
            # For conditionals, follow edges based on result
            condition_result = result.get("result", False)
            for target_id, edge_data in edges:
                edge_label = edge_data.get("sourceHandle") or edge_data.get("label", "")
                if condition_result and edge_label in ("true", "yes", "on_true", ""):
                    next_steps.append(target_id)
                elif not condition_result and edge_label in ("false", "no", "on_false"):
                    next_steps.append(target_id)
        
        elif node_type == StepType.LOOP_START.value:
            # For loop_start: if should_continue, enter loop body; else exit loop
            should_continue = result.get("should_continue", False)
            
            if should_continue:
                # Enter the loop body - follow normal edges
                for target_id, edge_data in edges:
                    next_steps.append(target_id)
            else:
                # Loop is done - find the exit path (after loop_end)
                # Look for the paired loop_end and find what comes after it
                loop_start_id = node.get("id")
                all_nodes = result.get("_nodes", [])  # Passed from context
                all_edges_list = result.get("_edges", [])
                
                # Find loop_end that pairs with this loop_start
                loop_end_node = None
                for n in all_nodes:
                    n_data = n.get("data", {})
                    if n_data.get("nodeType") == StepType.LOOP_END.value:
                        if n_data.get("paired_loop_start") == loop_start_id:
                            loop_end_node = n
                            break
                
                if loop_end_node:
                    # Find edges from loop_end that go to non-loop_start nodes
                    loop_end_id = loop_end_node.get("id")
                    for e in all_edges_list:
                        if e.get("source") == loop_end_id and e.get("target") != loop_start_id:
                            next_steps.append(e.get("target"))
                
                # Fallback: if still no exit path found, try explicit exit edges
                if not next_steps:
                    for target_id, edge_data in edges:
                        edge_label = edge_data.get("sourceHandle") or edge_data.get("label", "")
                        if edge_label in ("exit", "done", "skip"):
                            next_steps.append(target_id)
                
                print(f"[Workflow] Loop complete, exiting to: {next_steps}")
        
        elif node_type == StepType.LOOP_END.value:
            # For loop_end: if loop_back, go back to loop_start; else continue
            loop_back = result.get("loop_back", False)
            loop_start_id = result.get("loop_id")
            
            if loop_back and loop_start_id:
                # Go back to the loop_start node
                next_steps.append(loop_start_id)
            else:
                # Loop is complete, follow normal edges
                for target_id, edge_data in edges:
                    # Don't go back to loop_start when loop is complete
                    if target_id != loop_start_id:
                        next_steps.append(target_id)
        
        else:
            # For other nodes, follow all outgoing edges
            for target_id, edge_data in edges:
                next_steps.append(target_id)
        
        return next_steps


# Global engine instance
_engine: Optional[WorkflowEngine] = None


def get_engine() -> WorkflowEngine:
    """Get the global workflow engine instance."""
    global _engine
    if _engine is None:
        _engine = WorkflowEngine()
    return _engine

