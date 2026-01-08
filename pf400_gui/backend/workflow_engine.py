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
    LOOP = "loop"                 # Repeat steps
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
                self._notify(ctx.run_id, {
                    "type": "step_started",
                    "run_id": ctx.run_id,
                    "step_id": step_id,
                    "step_type": node.get("type"),
                })
                
                try:
                    # Execute the step
                    result = await self._execute_step(ctx, node)
                    ctx.step_results[step_id] = result
                    ctx.step_states[step_id] = StepState.COMPLETED.value
                    
                    self._notify(ctx.run_id, {
                        "type": "step_completed",
                        "run_id": ctx.run_id,
                        "step_id": step_id,
                        "result": result,
                    })
                    
                    # Determine next steps
                    next_steps = self._get_next_steps(node, result, outgoing.get(step_id, []))
                    pending.extend(next_steps)
                    
                except Exception as e:
                    ctx.step_states[step_id] = StepState.FAILED.value
                    ctx.step_results[step_id] = {"error": str(e)}
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
        
        else:
            return {"unknown_type": step_type}
    
    async def _execute_device_action(self, ctx: WorkflowContext, data: Dict[str, Any]) -> Dict[str, Any]:
        """Execute a device action step."""
        device_alias = data.get("device")
        action = data.get("action")
        params = data.get("params", {})
        
        # Resolve device from collection
        devices = ctx.device_collection.get("devices", [])
        device_info = None
        for d in devices:
            if d.get("alias") == device_alias or d.get("device_name") == device_alias:
                device_info = d
                break
        
        if not device_info:
            raise ValueError(f"Device '{device_alias}' not found in collection")
        
        device_name = device_info.get("device_name")
        
        if ctx.simulate:
            return {
                "simulated": True,
                "device": device_name,
                "action": action,
                "params": params,
            }
        
        # Get device connection info and call its API
        device_doc = mongodb.get_device_by_name(device_name)
        if not device_doc:
            raise ValueError(f"Device '{device_name}' not found in database")
        
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

