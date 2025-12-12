from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
from typing import Dict, Any, Optional, List
import uvicorn
import threading
import asyncio
import json
import sys
import os
import argparse
import time
import secrets

# Import ROS client
from ros_client import PF400ROSClient
# Import Real Robot drivers
from pf400_driver import PF400Driver
from pf400_sxl_driver import PF400SXLDriver
from pf400_models import PF400Model, get_model_by_name, get_model_config
# Import MongoDB integration
import db as mongodb

app = FastAPI(title="PF400 Control API")

# Mount static files for STL meshes
mesh_dir = os.path.join(os.path.dirname(__file__), "../../models/pf400_urdf/meshes")
app.mount("/meshes", StaticFiles(directory=mesh_dir), name="meshes")

# Mount URDF directory
urdf_dir = os.path.join(os.path.dirname(__file__), "../../models/pf400_urdf")
app.mount("/urdf", StaticFiles(directory=urdf_dir), name="urdf")

# Mount Planar Motor GLTF models (served from Mac backend)
planar_motor_models_dir = os.path.join(os.path.dirname(__file__), "../../models/planar_motor")
if os.path.exists(planar_motor_models_dir):
    app.mount("/models/planar_motor", StaticFiles(directory=planar_motor_models_dir), name="planar_motor_models")
    print(f"Mounted Planar Motor models: {planar_motor_models_dir}")

# Allow CORS for frontend
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify the frontend URL
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Global client instance (ROS, Sim, or Real)
robot_client = None

# =========================
# Node Contract (Phase 1)
# =========================
#
# Implements the Tachyon Node HTTP contract described in:
#   `scheduler_framework/NODE_CONTRACT.md`
#
# We are intentionally layering this onto the existing PF400 GUI backend so you can
# demo tomorrow without introducing a new service deployment yet.

def _new_ulid_str() -> str:
    """
    Dependency-free ULID generator (26 chars).
    Matches the scheduler_framework format: 48-bit ms timestamp + 80-bit randomness.
    """
    alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"

    def enc(v: int, n: int) -> str:
        out = ["0"] * n
        for i in range(n - 1, -1, -1):
            out[i] = alphabet[v & 31]
            v >>= 5
        return "".join(out)

    ts_ms = int(time.time() * 1000) & ((1 << 48) - 1)
    rnd = int.from_bytes(secrets.token_bytes(10), "big")  # 80 bits
    return enc(ts_ms, 10) + enc(rnd, 16)


class NodeActionModel(BaseModel):
    name: str
    description: str = ""
    args_schema: Dict[str, Any] = {}


class NodeDefinitionModel(BaseModel):
    node_id: str
    name: str
    kind: str
    version: str = "0.1.0"
    actions: List[NodeActionModel] = []


class NodeActionRequestModel(BaseModel):
    request_id: str = ""
    action: str
    args: Dict[str, Any] = {}
    locations: Dict[str, Any] = {}


class NodeActionResponseModel(BaseModel):
    request_id: str
    execution_id: str = ""
    status: str = "succeeded"  # queued|running|succeeded|failed|cancelled
    success: bool = True
    result: Dict[str, Any] = {}
    error: Optional[str] = None


class _NodeJob:
    def __init__(self, request_id: str, action: str, args: Dict[str, Any], locations: Dict[str, Any]):
        self.request_id = request_id
        self.execution_id = _new_ulid_str()
        self.action = action
        self.args = args
        self.locations = locations
        self.status = "queued"
        self.success = True
        self.result: Dict[str, Any] = {}
        self.error: Optional[str] = None
        self.created_at = time.time()
        self.updated_at = time.time()


_node_jobs_lock = threading.Lock()
_node_jobs_by_execution_id: Dict[str, _NodeJob] = {}
_node_execution_by_request_id: Dict[str, str] = {}


def _node_supported_actions() -> List[NodeActionModel]:
    """
    Actions intentionally map onto existing endpoints/driver capabilities.
    Keep this list short for Phase 1; we can expand after the demo.
    """
    return [
        NodeActionModel(
            name="get_joints",
            description="Fetch joints + cartesian state (same as GET /joints).",
            args_schema={},
        ),
        NodeActionModel(
            name="initialize",
            description="Initialize robot to GPL Ready mode (same as POST /initialize).",
            args_schema={},
        ),
        NodeActionModel(
            name="jog",
            description="Jog by joint or cartesian axis (same as POST /jog). For rail use axis='rail' or joint=6 on SXL.",
            args_schema={
                "joint": {"type": "integer", "description": "Joint index (e.g. 1-6)"},
                "axis": {"type": "string", "description": "Cartesian axis (x,y,z,yaw,r,t,gripper,rail)"},
                "distance": {"type": "number", "description": "Meters or radians depending on axis/joint"},
                "speed_profile": {"type": "integer", "description": "Motion profile id"},
            },
        ),
        NodeActionModel(
            name="jog_rail",
            description="Jog rail by relative distance (SXL only).",
            args_schema={"distance_m": {"type": "number"}, "profile": {"type": "integer"}},
        ),
        NodeActionModel(
            name="move_rail",
            description="Move rail to absolute position (SXL only).",
            args_schema={"position_m": {"type": "number"}, "profile": {"type": "integer"}},
        ),
    ]


def _node_call_action_sync(action: str, args: Dict[str, Any]) -> Dict[str, Any]:
    """
    Execute a Node action synchronously using the existing robot_client/driver methods.
    Returns a JSON-serializable dict result.
    """
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")

    if action == "get_joints":
        # Reuse existing function logic (fast + safe)
        # inline minimal copy to avoid awaiting inside non-async helper
        joints = {}
        cartesian = {}
        try:
            if hasattr(robot_client, "get_joint_positions"):
                joints = robot_client.get_joint_positions()
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"Error getting joints: {e}")
        try:
            if hasattr(robot_client, "driver") and hasattr(robot_client.driver, "get_cartesian_position"):
                cartesian = robot_client.driver.get_cartesian_position()
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"Error getting cartesian: {e}")
        return {"joints": joints, "cartesian": cartesian}

    if action == "initialize":
        if hasattr(robot_client, "driver") and hasattr(robot_client.driver, "initialize_robot"):
            ok = robot_client.driver.initialize_robot()
            if not ok:
                raise HTTPException(status_code=500, detail="Initialization failed")
            return {"status": "success"}
        raise HTTPException(status_code=501, detail="Initialize not supported")

    if action == "jog":
        req = JogRequest(**args)
        # Mirror POST /jog behavior
        success = False
        if req.axis:
            if req.axis.lower() == "rail":
                if isinstance(robot_client, RealClient) and isinstance(robot_client.driver, PF400SXLDriver):
                    success = robot_client.driver.jog_rail(req.distance, req.speed_profile)
                else:
                    raise HTTPException(status_code=501, detail="Rail jog only supported on PF400SXL")
            elif hasattr(robot_client, "jog_cartesian"):
                success = robot_client.jog_cartesian(req.axis, req.distance, req.speed_profile)
            else:
                raise HTTPException(status_code=501, detail="Cartesian jog not supported")
        elif req.joint is not None:
            # Special-case: SXL rail joint index is 6
            if int(req.joint) == 6:
                if isinstance(robot_client, RealClient) and isinstance(robot_client.driver, PF400SXLDriver):
                    success = robot_client.driver.jog_rail(req.distance, req.speed_profile)
                else:
                    raise HTTPException(status_code=501, detail="Joint 6 (rail) only supported on PF400SXL")
            else:
                if hasattr(robot_client, "jog"):
                    success = robot_client.jog(req.joint, req.distance, req.speed_profile)
                else:
                    raise HTTPException(status_code=501, detail="Jog not supported")
        else:
            raise HTTPException(status_code=400, detail="Must specify joint or axis")

        if not success:
            raise HTTPException(status_code=500, detail="Jog failed")
        return {"status": "success"}

    if action == "jog_rail":
        if not (isinstance(robot_client, RealClient) and isinstance(robot_client.driver, PF400SXLDriver)):
            raise HTTPException(status_code=400, detail="Rail jogging only available for PF400SXL models")
        distance_m = float(args.get("distance_m", 0.0))
        profile = int(args.get("profile", 1))
        ok = robot_client.driver.jog_rail(distance_m, profile)
        if not ok:
            raise HTTPException(status_code=500, detail="Rail jog failed")
        return {"status": "success"}

    if action == "move_rail":
        if not (isinstance(robot_client, RealClient) and isinstance(robot_client.driver, PF400SXLDriver)):
            raise HTTPException(status_code=400, detail="Rail movement only available for PF400SXL models")
        position_m = float(args.get("position_m", 0.0))
        profile = int(args.get("profile", 1))
        ok = robot_client.driver.move_rail(position_m, profile)
        if not ok:
            raise HTTPException(status_code=500, detail="Rail move failed")
        return {"status": "success"}

    raise HTTPException(status_code=404, detail=f"Unknown Node action '{action}'")


def _node_run_job(job: _NodeJob) -> None:
    with _node_jobs_lock:
        job.status = "running"
        job.updated_at = time.time()
    try:
        result = _node_call_action_sync(job.action, job.args)
        job.result = result if isinstance(result, dict) else {"result": result}
        job.status = "succeeded"
        job.success = True
        job.error = None
    except HTTPException as e:
        job.status = "failed"
        job.success = False
        job.error = str(e.detail)
    except Exception as e:
        job.status = "failed"
        job.success = False
        job.error = str(e)
    finally:
        with _node_jobs_lock:
            job.updated_at = time.time()


@app.get("/health")
async def node_health():
    healthy = robot_client is not None
    detail = "ok" if healthy else "robot client not initialized"
    # Try to reflect real connection state if available
    if healthy and hasattr(robot_client, "driver") and hasattr(robot_client.driver, "connected"):
        healthy = bool(robot_client.driver.connected)
        detail = "connected" if healthy else "driver not connected"
    return {"healthy": healthy, "detail": detail}


@app.get("/definition")
async def node_definition():
    model_name = None
    if hasattr(robot_client, "model"):
        try:
            model_name = robot_client.model.value
        except Exception:
            model_name = str(getattr(robot_client, "model"))
    return NodeDefinitionModel(
        node_id=f"pf400-{DEVICE_NAME}",
        name=DEVICE_NAME,
        kind="robot.pf400",
        version="0.1.0",
        actions=_node_supported_actions(),
    )


@app.post("/actions/{action}", response_model=NodeActionResponseModel)
async def node_action_sync(action: str, req: NodeActionRequestModel):
    request_id = req.request_id or _new_ulid_str()
    try:
        result = _node_call_action_sync(action, req.args or {})
        return NodeActionResponseModel(
            request_id=request_id,
            execution_id="",
            status="succeeded",
            success=True,
            result=result if isinstance(result, dict) else {"result": result},
            error=None,
        )
    except HTTPException as e:
        return NodeActionResponseModel(
            request_id=request_id,
            execution_id="",
            status="failed",
            success=False,
            result={},
            error=str(e.detail),
        )


@app.post("/actions/{action}/submit", response_model=NodeActionResponseModel)
async def node_action_submit(action: str, req: NodeActionRequestModel):
    request_id = req.request_id or _new_ulid_str()

    # Idempotency: if we already have a job for this request_id, return it.
    with _node_jobs_lock:
        existing_exec = _node_execution_by_request_id.get(request_id)
        if existing_exec and existing_exec in _node_jobs_by_execution_id:
            job = _node_jobs_by_execution_id[existing_exec]
            return NodeActionResponseModel(
                request_id=job.request_id,
                execution_id=job.execution_id,
                status=job.status,
                success=job.success,
                result=job.result if job.status == "succeeded" else {},
                error=job.error if job.status == "failed" else None,
            )

        job = _NodeJob(
            request_id=request_id,
            action=action,
            args=req.args or {},
            locations=req.locations or {},
        )
        _node_jobs_by_execution_id[job.execution_id] = job
        _node_execution_by_request_id[request_id] = job.execution_id

    t = threading.Thread(target=_node_run_job, args=(job,), daemon=True, name=f"nodejob-{job.execution_id}")
    t.start()

    return NodeActionResponseModel(
        request_id=job.request_id,
        execution_id=job.execution_id,
        status=job.status,  # queued
        success=True,
        result={},
        error=None,
    )


@app.get("/actions/status/{execution_id}", response_model=NodeActionResponseModel)
async def node_action_status(execution_id: str):
    with _node_jobs_lock:
        job = _node_jobs_by_execution_id.get(execution_id)
    if not job:
        raise HTTPException(status_code=404, detail="Unknown execution_id")
    return NodeActionResponseModel(
        request_id=job.request_id,
        execution_id=job.execution_id,
        status=job.status,
        success=job.success if job.status in ("succeeded", "failed", "cancelled") else True,
        result=job.result if job.status == "succeeded" else {},
        error=job.error if job.status == "failed" else None,
    )

class ActionRequest(BaseModel):
    action_handle: str
    vars: Dict[str, Any]

class JogRequest(BaseModel):
    joint: Optional[int] = None # 1-5
    axis: Optional[str] = None  # x, y, z, yaw, r, t, gripper
    distance: float # meters or radians
    speed_profile: int = 1

class TeachpointRequest(BaseModel):
    id: str
    name: str
    description: Optional[str] = ""
    type: str = "joints"  # "joints" or "cartesian"
    joints: Optional[List[float]] = None
    cartesian: Optional[Dict[str, float]] = None

class MoveToTeachpointRequest(BaseModel):
    teachpoint_id: str
    speed_profile: int = 1

class SpeedSettingsRequest(BaseModel):
    profile_id: int = 2  # Default to fast profile
    speed: int = 80      # 1-100 percentage
    accel: int = None    # Optional, defaults to speed
    decel: int = None    # Optional, defaults to accel


# Device name for this robot instance
DEVICE_NAME = os.environ.get("DEVICE_NAME", "PF400-015")

# Robot model (400SX or 400SXL)
ROBOT_MODEL = os.environ.get("ROBOT_MODEL", "400SX")

class SimClient:
    def __init__(self):
        # Add scripts to path to import driver
        current_dir = os.path.dirname(os.path.abspath(__file__))
        scripts_dir = os.path.join(current_dir, "../../scripts")
        sys.path.append(scripts_dir)
        
        from pf400_sim_driver import PF400Simulator
        urdf_path = os.path.join(current_dir, "../../models/pf400_urdf/pf400Complete.urdf")
        self.sim = PF400Simulator(urdf_path)
        self.state = "READY"
        print("Simulator Client Initialized")

    def get_state(self):
        return self.state

    async def send_action(self, action_handle, vars_json):
        print(f"Sim Action: {action_handle} with {vars_json}")
        try:
            params = json.loads(vars_json)
            if action_handle == "move_to_joints":
                self.state = "MOVING"
                self.sim.move_to_joints(params, duration=2.0)
                self.state = "READY"
                return {"status": "success", "response": 0, "message": "Move complete"}
            else:
                return {"status": "failure", "message": f"Unknown action: {action_handle}"}
        except Exception as e:
            print(f"Sim Error: {e}")
            self.state = "ERROR"
            return {"status": "error", "message": str(e)}

    async def get_description(self):
        return {"description": "PF400 Simulator Interface"}
        
    def get_joint_positions(self):
        return self.sim.get_joint_positions()
    
    def jog(self, joint, distance, profile):
        # TODO: Implement sim jogging if needed
        return False

class RealClient:
    def __init__(self, ip=None, port=10100, model: PF400Model = None):
        # Get IP from environment or use default
        if ip is None:
            ip = os.environ.get("PF400_IP", "192.168.0.20")
        # Get port from environment or use default
        port = int(os.environ.get("PF400_ROBOT_PORT", port))
        # Determine model
        if model is None:
            model = get_model_by_name(ROBOT_MODEL) or PF400Model.SX
        
        # Create appropriate driver based on model
        if model == PF400Model.SXL:
            self.driver = PF400SXLDriver(ip, port)
            print(f"Using PF400SXL driver (with rail support)")
        else:
            self.driver = PF400Driver(ip, port)
            print(f"Using PF400SX driver (standard)")
        
        self.model = model
        self.model_config = get_model_config(model)
        
        if self.driver.connect():
            print(f"Real Robot Client Initialized and Connected to {ip}:{port}")
            print(f"Model: {model.value} - {self.model_config.description}")
            self.state = "READY"
        else:
            print(f"Real Robot Client Initialized but Connection Failed to {ip}:{port}")
            self.state = "ERROR"
            
    def get_state(self):
        return self.state if self.driver.connected else "ERROR"

    async def send_action(self, action_handle, vars_json):
        # For now, just logging
        print(f"Real Action: {action_handle} with {vars_json}")
        if not self.driver.connected:
             return {"status": "error", "message": "Robot not connected"}
             
        if action_handle == "move_to_joints":
            try:
                params = json.loads(vars_json)
                # Extract values
                j1 = params.get("j1", 0)
                j2 = params.get("j2", 0)
                j3 = params.get("j3", 0)
                j4 = params.get("j4", 0)
                grp = params.get("gripper", 0)
                
                # For SXL, also get J6 (rail) if provided
                if isinstance(self.driver, PF400SXLDriver):
                    j6 = params.get("j6", None) or params.get("rail", None)
                    success = self.driver.move_to_joints(j1, j2, j3, j4, grp, j6)
                else:
                    success = self.driver.move_to_joints(j1, j2, j3, j4, grp)
                
                if success:
                    return {"status": "success", "message": "Move command sent"}
                else:
                    return {"status": "failure", "message": "Move command failed"}
            except Exception as e:
                return {"status": "error", "message": str(e)}
                
        return {"status": "failure", "message": "Action not implemented"}

    async def get_description(self):
        return {"description": "Real PF400 Robot Interface"}
        
    def get_joint_positions(self):
        if not self.driver.connected:
            # Try to reconnect with auto-initialization
            if not self.driver.connect(auto_initialize=True):
                return {}  # Return empty if can't connect
        try:
            return self.driver.get_joint_positions()
        except Exception as e:
            print(f"Exception in get_joint_positions: {e}")
            # If we get an exception, robot might be stuck - try to reinitialize
            try:
                if self.driver.connected:
                    print("Robot unresponsive, attempting re-initialization...")
                    self.driver.initialize_robot()
            except:
                pass
            return {}
        
    def jog(self, joint, distance, profile):
        try:
            if not self.driver.connected:
                # Try to reconnect with auto-initialization
                if not self.driver.connect(auto_initialize=True):
                    print("Jog: Robot not connected and reconnection failed")
                    return False
            
            # Update profile if needed
            if profile != self.driver.current_profile:
                self.driver.set_profile(profile)
            
            # Handle joint 6 (rail) specially for SXL models
            if joint == 6:
                if isinstance(self.driver, PF400SXLDriver):
                    print(f"Jog: Attempting rail jog with distance {distance}m, profile {profile}")
                    result = self.driver.jog_rail(distance, profile)
                    if not result:
                        print(f"Jog: jog_rail returned False for rail, distance {distance}")
                    return result
                else:
                    print(f"Jog: Joint 6 (rail) not supported on this model (driver type: {type(self.driver).__name__})")
                    return False
            
            # Handle joints 1-5
            result = self.driver.jog_joint(joint, distance)
            if not result:
                print(f"Jog: jog_joint returned False for joint {joint}, distance {distance}")
            return result
        except Exception as e:
            print(f"Error in jog: {e}")
            import traceback
            traceback.print_exc()
            # If jog fails, robot might be stuck - try to reinitialize
            try:
                if self.driver.connected:
                    print("Jog failed, attempting re-initialization...")
                    self.driver.initialize_robot()
            except:
                pass
            return False
        
    def jog_cartesian(self, axis, distance, profile):
        try:
            if not self.driver.connected:
                # Try to reconnect with auto-initialization
                if not self.driver.connect(auto_initialize=True):
                    print("Jog_cartesian: Robot not connected and reconnection failed")
                    return False

            if profile != self.driver.current_profile:
                self.driver.set_profile(profile)
            
            result = self.driver.jog_cartesian(axis, distance)
            if not result:
                print(f"Jog_cartesian: returned False for axis {axis}, distance {distance}")
            return result
        except Exception as e:
            print(f"Error in jog_cartesian: {e}")
            import traceback
            traceback.print_exc()
            # If jog fails, robot might be stuck - try to reinitialize
            try:
                if self.driver.connected:
                    print("Jog failed, attempting re-initialization...")
                    self.driver.initialize_robot()
            except:
                pass
            return False

# Parse command line args at module level so they're available everywhere
parser = argparse.ArgumentParser()
parser.add_argument("--sim", action="store_true", help="Run in simulator mode")
parser.add_argument("--real", action="store_true", help="Run in real robot mode")
parser.add_argument("--port", type=int, default=3061, help="Port to run server on (default: 3061)")
cli_args, _ = parser.parse_known_args()

@app.on_event("startup")
async def startup_event():
    global robot_client
    
    # Check for simulator mode via environment variable or command line args
    use_sim = os.environ.get('PF400_SIM_MODE', '').lower() in ('1', 'true', 'yes')
    use_real = os.environ.get('PF400_REAL_MODE', '').lower() in ('1', 'true', 'yes')
    
    if cli_args.sim: use_sim = True
    if cli_args.real: use_real = True

    if use_sim:
        print("Starting in SIMULATOR mode")
        try:
            robot_client = SimClient()
        except Exception as e:
            print(f"Failed to initialize Simulator client: {e}")
    elif use_real:
        print("Starting in REAL ROBOT mode")
        try:
            # Get model from environment or use default
            model_name = os.environ.get("ROBOT_MODEL", ROBOT_MODEL)
            model = get_model_by_name(model_name) or PF400Model.SX
            robot_client = RealClient(model=model)
        except Exception as e:
            print(f"Failed to initialize Real client: {e}")
    else:
        print("Starting in ROS mode (default)")
        try:
            robot_client = PF400ROSClient()
            # Start ROS spinning in a background thread
            ros_thread = threading.Thread(target=robot_client.spin, daemon=True)
            ros_thread.start()
        except Exception as e:
            print(f"Failed to initialize ROS client: {e}")

@app.get("/state")
async def get_state():
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    return {"state": robot_client.get_state()}

@app.post("/action/{action_name}")
async def execute_action(action_name: str, params: Dict[str, Any]):
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    # Construct the action request
    # Note: The ROS service expects a JSON string for 'vars'
    result = await robot_client.send_action(action_name, json.dumps(params))
    return result

@app.post("/jog")
async def jog_robot(req: JogRequest):
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    try:
        success = False
        if req.axis:
            # Special case for rail axis - route to jog_rail on SXL models
            if req.axis.lower() == "rail":
                if isinstance(robot_client, RealClient) and isinstance(robot_client.driver, PF400SXLDriver):
                    print(f"Jog: Rail jog via axis='rail', distance={req.distance}m, profile={req.speed_profile}")
                    success = robot_client.driver.jog_rail(req.distance, req.speed_profile)
                else:
                    raise HTTPException(status_code=501, detail="Rail jog only supported on PF400SXL")
            # Cartesian Jog
            elif hasattr(robot_client, "jog_cartesian"):
                success = robot_client.jog_cartesian(req.axis, req.distance, req.speed_profile)
            else:
                raise HTTPException(status_code=501, detail="Cartesian jog not supported")
        elif req.joint is not None:
            # Joint Jog
            if hasattr(robot_client, "jog"):
                success = robot_client.jog(req.joint, req.distance, req.speed_profile)
            else:
                raise HTTPException(status_code=501, detail="Jog not supported")
        else:
            raise HTTPException(status_code=400, detail="Must specify joint or axis")

        if success:
            return {"status": "success"}
        else:
            raise HTTPException(status_code=500, detail="Jog failed")
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error in jog endpoint: {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Jog error: {str(e)}")

@app.get("/description")
async def get_description():
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    return await robot_client.get_description()

@app.get("/urdf/pf400Complete.urdf")
async def get_urdf():
    from fastapi.responses import FileResponse
    urdf_path = os.path.join(os.path.dirname(__file__), "../../models/pf400_urdf/pf400Complete.urdf")
    return FileResponse(urdf_path, media_type="application/xml")

@app.post("/initialize")
async def initialize_robot():
    """Initialize robot to GPL Ready mode (hp 1, attach 1)"""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    if hasattr(robot_client, 'driver') and hasattr(robot_client.driver, 'initialize_robot'):
        success = robot_client.driver.initialize_robot()
        if success:
            return {"status": "success", "message": "Robot initialized to GPL Ready mode"}
        else:
            raise HTTPException(status_code=500, detail="Initialization failed")
    else:
        raise HTTPException(status_code=501, detail="Initialize not supported")

@app.post("/speed")
async def set_speed(req: SpeedSettingsRequest):
    """Set motion profile speed settings."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    if hasattr(robot_client, 'driver') and hasattr(robot_client.driver, 'set_speed'):
        success = robot_client.driver.set_speed(
            profile_id=req.profile_id,
            speed=req.speed,
            accel=req.accel,
            decel=req.decel
        )
        if success:
            return {
                "status": "success", 
                "message": f"Speed set to {req.speed}%",
                "profile": robot_client.driver.get_current_profile()
            }
        else:
            raise HTTPException(status_code=500, detail="Failed to set speed")
    else:
        raise HTTPException(status_code=501, detail="Speed control not supported")

@app.get("/speed")
async def get_speed():
    """Get current motion profile settings."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    if hasattr(robot_client, 'driver') and hasattr(robot_client.driver, 'get_current_profile'):
        return robot_client.driver.get_current_profile()
    else:
        return {"profile_id": 1, "settings": {}}

@app.get("/joints")
async def get_joints():
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    # Get joint positions
    joints = {}
    try:
        if hasattr(robot_client, 'get_joint_positions'):
            joints = robot_client.get_joint_positions()
        elif hasattr(robot_client, 'sim'):
            joints = robot_client.sim.get_joint_positions()
    except Exception as e:
        print(f"Error getting joints: {e}")
        joints = {}
    
    # Get cartesian position
    cartesian = {}
    try:
        # Check if robot_client is RealClient and has driver with get_cartesian_position
        if hasattr(robot_client, 'driver') and hasattr(robot_client.driver, 'get_cartesian_position'):
            cartesian = robot_client.driver.get_cartesian_position()
    except Exception as e:
        print(f"Error getting cartesian: {e}")
        cartesian = {}
    
    return {"joints": joints, "cartesian": cartesian}

# ============== Teachpoints API ==============

@app.get("/teachpoints")
async def get_teachpoints():
    """Get all teachpoints for this device."""
    try:
        teachpoints = mongodb.get_device_teachpoints(DEVICE_NAME)
        # Convert to list format for frontend
        result = []
        # Handle both dict and list formats (in case MongoDB returns a list)
        if isinstance(teachpoints, dict):
            for tp_id, tp_data in teachpoints.items():
                tp_entry = {"id": tp_id, **tp_data}
                result.append(tp_entry)
        elif isinstance(teachpoints, list):
            # If it's already a list, use it directly
            result = teachpoints
        return {"teachpoints": result, "device": DEVICE_NAME}
    except Exception as e:
        print(f"Error getting teachpoints: {e}")
        import traceback
        traceback.print_exc()
        # Return empty list instead of raising error if MongoDB is unavailable
        return {"teachpoints": [], "device": DEVICE_NAME, "error": str(e)}

@app.post("/teachpoints")
async def save_teachpoint(req: TeachpointRequest):
    """Save a new teachpoint or update existing one."""
    try:
        teachpoint_data = {
            "name": req.name,
            "description": req.description,
            "type": req.type,
        }
        
        if req.joints:
            teachpoint_data["joints"] = req.joints
        if req.cartesian:
            teachpoint_data["cartesian"] = req.cartesian
            
        success = mongodb.save_teachpoint(DEVICE_NAME, req.id, teachpoint_data)
        if success:
            return {"status": "success", "message": f"Saved teachpoint '{req.name}'"}
        else:
            raise HTTPException(status_code=500, detail="Failed to save teachpoint")
    except Exception as e:
        print(f"Error saving teachpoint: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/teachpoints/save-current")
async def save_current_position(name: str, description: str = "", id: str = None):
    """Save the current robot position as a teachpoint.
    
    If 'id' is provided, updates that existing teachpoint.
    Otherwise, creates a new one with ID generated from name.
    """
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    try:
        # Get current positions
        joints_dict = {}
        if hasattr(robot_client, 'get_joint_positions'):
            joints_dict = robot_client.get_joint_positions()
        
        cartesian_dict = {}
        if hasattr(robot_client, 'driver') and hasattr(robot_client.driver, 'get_cartesian_position'):
            cartesian_dict = robot_client.driver.get_cartesian_position()
        
        # Convert joints dict to list (robot units: mm and degrees)
        # j1 is in meters, convert to mm
        # j2, j3, j4 are in radians, convert to degrees
        # gripper is in meters, convert to mm
        # j6 (rail) is in meters, convert to mm
        import math
        j1_mm = joints_dict.get('j1', 0) * 1000
        j2_deg = joints_dict.get('j2', 0) * 180 / math.pi
        j3_deg = joints_dict.get('j3', 0) * 180 / math.pi
        j4_deg = joints_dict.get('j4', 0) * 180 / math.pi
        gripper_mm = joints_dict.get('gripper', 0) * 1000
        j6_mm = joints_dict.get('j6', 0) * 1000  # Rail position
        
        joints_list = [j1_mm, j2_deg, j3_deg, j4_deg, gripper_mm, j6_mm]
        
        # Use provided ID for updates, or generate new ID from name
        tp_id = id if id else name.lower().replace(" ", "_").replace("-", "_")

        # If updating existing teachpoint, preserve link data
        existing_links = {}
        if id:
            existing_teachpoints = mongodb.get_device_teachpoints(DEVICE_NAME)
            if tp_id in existing_teachpoints:
                existing_tp = existing_teachpoints[tp_id]
                # Preserve link information
                if "linked_to" in existing_tp:
                    existing_links["linked_to"] = existing_tp["linked_to"]
                if "linked_from" in existing_tp:
                    existing_links["linked_from"] = existing_tp["linked_from"]

        teachpoint_data = {
            "name": name,
            "description": description,
            "type": "joints",
            "joints": joints_list,
            "cartesian": cartesian_dict,
            **existing_links  # Preserve any link data
        }
        
        print(f"Calling mongodb.save_teachpoint for device={DEVICE_NAME}, tp_id={tp_id}")
        success = mongodb.save_teachpoint(DEVICE_NAME, tp_id, teachpoint_data)
        print(f"mongodb.save_teachpoint returned: {success}")
        if success:
            action = "Updated" if id else "Saved"
            return {"status": "success", "message": f"{action} teachpoint '{name}'", "id": tp_id}
        else:
            raise HTTPException(status_code=500, detail="Failed to save teachpoint")
            
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error saving current position: {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))

@app.delete("/teachpoints/{teachpoint_id}")
async def delete_teachpoint(teachpoint_id: str):
    """Delete a teachpoint."""
    try:
        success = mongodb.delete_teachpoint(DEVICE_NAME, teachpoint_id)
        if success:
            return {"status": "success", "message": f"Deleted teachpoint '{teachpoint_id}'"}
        else:
            raise HTTPException(status_code=404, detail="Teachpoint not found")
    except Exception as e:
        print(f"Error deleting teachpoint: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.patch("/teachpoints/{teachpoint_id}/rename")
async def rename_teachpoint(teachpoint_id: str, name: str, description: str = None):
    """Rename a teachpoint (update its name and optionally description)."""
    try:
        # Get existing teachpoint
        teachpoints = mongodb.get_device_teachpoints(DEVICE_NAME)
        if teachpoint_id not in teachpoints:
            raise HTTPException(status_code=404, detail=f"Teachpoint '{teachpoint_id}' not found")
        
        # Update the name (and description if provided)
        tp = teachpoints[teachpoint_id]
        tp["name"] = name
        if description is not None:
            tp["description"] = description
        
        # Save back with same ID
        success = mongodb.save_teachpoint(DEVICE_NAME, teachpoint_id, tp)
        if success:
            return {"status": "success", "message": f"Renamed to '{name}'", "id": teachpoint_id}
        else:
            raise HTTPException(status_code=500, detail="Failed to rename teachpoint")
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error renaming teachpoint: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/teachpoints/move/{teachpoint_id}")
async def move_to_teachpoint(teachpoint_id: str, speed_profile: int = 1):
    """Move the robot to a saved teachpoint."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    try:
        teachpoints = mongodb.get_device_teachpoints(DEVICE_NAME)
        if teachpoint_id not in teachpoints:
            raise HTTPException(status_code=404, detail=f"Teachpoint '{teachpoint_id}' not found")
        
        tp = teachpoints[teachpoint_id]
        
        if tp.get("type") == "joints" and tp.get("joints"):
            joints = tp["joints"]
            # joints is [j1_mm, j2_deg, j3_deg, j4_deg, gripper_mm, j6_mm/rail]
            # Use move_to_joints_raw which expects robot native units (mm/deg)
            if hasattr(robot_client, 'driver'):
                j6_mm = joints[5] if len(joints) > 5 else None
                success = robot_client.driver.move_to_joints_raw(
                    j1_mm=joints[0],
                    j2_deg=joints[1],
                    j3_deg=joints[2],
                    j4_deg=joints[3],
                    gripper_mm=joints[4],
                    j6_mm=j6_mm,
                    profile=speed_profile
                )
                if success:
                    return {"status": "success", "message": f"Moved to '{tp.get('name', teachpoint_id)}'"}
                else:
                    raise HTTPException(status_code=500, detail="Move command failed")
            else:
                raise HTTPException(status_code=501, detail="Move not supported by current client")
        else:
            raise HTTPException(status_code=400, detail="Teachpoint has no joint data")
            
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error moving to teachpoint: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/device")
async def get_device_info():
    """Get device information from MongoDB."""
    try:
        device = mongodb.get_device_by_name(DEVICE_NAME)
        if device:
            return {"device": device}
        else:
            return {"device": None, "message": f"Device '{DEVICE_NAME}' not found in database"}
    except Exception as e:
        print(f"Error getting device info: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/devices")
async def get_all_devices():
    """Get all devices from MongoDB."""
    try:
        devices = mongodb.get_all_devices()
        return {"devices": devices}
    except Exception as e:
        print(f"Error getting all devices: {e}")
        raise HTTPException(status_code=500, detail=str(e))


# ============== Diagnostics API ==============

@app.get("/diagnostics")
async def get_diagnostics():
    """Get comprehensive diagnostics information for the robot."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    # Check if driver supports diagnostics
    if hasattr(robot_client, 'driver') and hasattr(robot_client.driver, 'get_diagnostics'):
        try:
            diagnostics = robot_client.driver.get_diagnostics()
            # Add model info
            if hasattr(robot_client, 'model'):
                diagnostics["model"] = robot_client.model.value
                diagnostics["model_config"] = robot_client.model_config.to_dict()
            return diagnostics
        except Exception as e:
            print(f"Error getting diagnostics: {e}")
            raise HTTPException(status_code=500, detail=str(e))
    else:
        # Basic diagnostics for non-diagnostics-capable drivers
        return {
            "model": "unknown",
            "connected": hasattr(robot_client, 'driver') and robot_client.driver.connected if hasattr(robot_client, 'driver') else False,
            "state": robot_client.get_state(),
            "message": "Full diagnostics not available for this driver"
        }


@app.get("/diagnostics/system-state")
async def get_system_state():
    """Get current system state."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    if hasattr(robot_client, 'driver') and hasattr(robot_client.driver, 'get_system_state'):
        try:
            return robot_client.driver.get_system_state()
        except Exception as e:
            print(f"Error getting system state: {e}")
            raise HTTPException(status_code=500, detail=str(e))
    else:
        return {
            "state": robot_client.get_state(),
            "connected": hasattr(robot_client, 'driver') and robot_client.driver.connected if hasattr(robot_client, 'driver') else False
        }


@app.get("/diagnostics/joints")
async def get_joint_states():
    """Get detailed joint state information."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    if hasattr(robot_client, 'driver') and hasattr(robot_client.driver, 'get_joint_states'):
        try:
            return robot_client.driver.get_joint_states()
        except Exception as e:
            print(f"Error getting joint states: {e}")
            raise HTTPException(status_code=500, detail=str(e))
    else:
        # Fallback to basic joint positions
        try:
            joints = robot_client.get_joint_positions()
            return {"joints": joints}
        except Exception as e:
            print(f"Error getting joints: {e}")
            raise HTTPException(status_code=500, detail=str(e))


@app.get("/diagnostics/rail")
async def get_rail_status():
    """Get rail (J6) status for SXL models."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    if not isinstance(robot_client, RealClient) or not isinstance(robot_client.driver, PF400SXLDriver):
        raise HTTPException(status_code=400, detail="Rail diagnostics only available for PF400SXL models")
    
    try:
        joints = robot_client.driver.get_joint_positions()
        rail_pos = joints.get("j6") or joints.get("rail", 0)
        
        return {
            "rail_enabled": True,
            "position_m": rail_pos,
            "position_mm": rail_pos * 1000.0,
            "position_percent": (rail_pos * 1000.0 / robot_client.driver.rail_length_mm) * 100.0,
            "rail_length_mm": robot_client.driver.rail_length_mm,
            "rail_length_m": robot_client.driver.rail_length_mm / 1000.0,
            "limits": {
                "min_mm": 0.0,
                "max_mm": robot_client.driver.rail_length_mm,
                "min_m": 0.0,
                "max_m": robot_client.driver.rail_length_mm / 1000.0
            }
        }
    except Exception as e:
        print(f"Error getting rail status: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/diagnostics/jog-rail")
async def jog_rail(distance_m: float, profile: int = 1):
    """Jog the rail (J6) by a relative distance (SXL only)."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    if not isinstance(robot_client, RealClient) or not isinstance(robot_client.driver, PF400SXLDriver):
        raise HTTPException(status_code=400, detail="Rail jogging only available for PF400SXL models")
    
    try:
        success = robot_client.driver.jog_rail(distance_m, profile)
        if success:
            return {"status": "success", "message": f"Rail jogged by {distance_m}m"}
        else:
            raise HTTPException(status_code=500, detail="Rail jog failed")
    except Exception as e:
        print(f"Error jogging rail: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/diagnostics/move-rail")
async def move_rail(position_m: float, profile: int = 1):
    """Move rail (J6) to absolute position (SXL only)."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    
    if not isinstance(robot_client, RealClient) or not isinstance(robot_client.driver, PF400SXLDriver):
        raise HTTPException(status_code=400, detail="Rail movement only available for PF400SXL models")
    
    try:
        success = robot_client.driver.move_rail(position_m, profile)
        if success:
            return {"status": "success", "message": f"Rail moved to {position_m}m"}
        else:
            raise HTTPException(status_code=500, detail="Rail move failed")
    except Exception as e:
        print(f"Error moving rail: {e}")
        raise HTTPException(status_code=500, detail=str(e))


# ============== Generic Device Teachpoints API ==============
# These endpoints work with any device by name (used by Planar Motor, etc.)

class DeviceTeachpointRequest(BaseModel):
    device_name: str
    id: str
    name: str
    description: str = ""
    position: Dict[str, float] = {}  # x, y, z, rx, ry, rz in meters/radians
    xbot_id: int = 1

@app.get("/devices/{device_name}/teachpoints")
async def get_device_teachpoints(device_name: str):
    """Get all teachpoints for a specific device."""
    try:
        teachpoints = mongodb.get_device_teachpoints(device_name)
        result = []
        if isinstance(teachpoints, dict):
            for tp_id, tp_data in teachpoints.items():
                tp_entry = {"id": tp_id, **tp_data}
                result.append(tp_entry)
        elif isinstance(teachpoints, list):
            result = teachpoints
        return {"teachpoints": result, "device": device_name}
    except Exception as e:
        print(f"Error getting teachpoints for {device_name}: {e}")
        return {"teachpoints": [], "device": device_name, "error": str(e)}

@app.post("/devices/{device_name}/teachpoints")
async def save_device_teachpoint(device_name: str, req: DeviceTeachpointRequest):
    """Save a teachpoint for a specific device."""
    try:
        # If updating existing teachpoint, preserve link data
        existing_links = {}
        existing_teachpoints = mongodb.get_device_teachpoints(device_name)
        if req.id in existing_teachpoints:
            existing_tp = existing_teachpoints[req.id]
            # Preserve link information
            if "linked_to" in existing_tp:
                existing_links["linked_to"] = existing_tp["linked_to"]
            if "linked_from" in existing_tp:
                existing_links["linked_from"] = existing_tp["linked_from"]

        teachpoint_data = {
            "name": req.name,
            "description": req.description,
            "position": req.position,
            "xbot_id": req.xbot_id,
            **existing_links  # Preserve any link data
        }

        success = mongodb.save_teachpoint(device_name, req.id, teachpoint_data)
        if success:
            return {"status": "success", "message": f"Saved teachpoint '{req.name}'"}
        else:
            raise HTTPException(status_code=500, detail="Failed to save teachpoint")
    except Exception as e:
        print(f"Error saving teachpoint for {device_name}: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.delete("/devices/{device_name}/teachpoints/{teachpoint_id}")
async def delete_device_teachpoint(device_name: str, teachpoint_id: str):
    """Delete a teachpoint from a specific device."""
    try:
        success = mongodb.delete_teachpoint(device_name, teachpoint_id)
        if success:
            return {"status": "success", "message": f"Deleted teachpoint '{teachpoint_id}'"}
        else:
            raise HTTPException(status_code=500, detail="Failed to delete teachpoint")
    except Exception as e:
        print(f"Error deleting teachpoint for {device_name}: {e}")
        raise HTTPException(status_code=500, detail=str(e))


# ============== Device Reachability & Teachpoint Linking API ==============

@app.get("/devices/{device_name}/reachable")
async def get_reachable_devices(device_name: str):
    """Get list of devices that this device can physically reach."""
    try:
        reachable = mongodb.get_reachable_devices(device_name)
        return {"device": device_name, "reachable_devices": reachable}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


class ReachableDeviceRequest(BaseModel):
    target_device: str
    access_type: str = "handoff"  # "handoff", "dropoff_only", "pickup_only"
    description: str = ""


@app.post("/devices/{device_name}/reachable")
async def add_reachable_device(device_name: str, req: ReachableDeviceRequest):
    """Add a device to the reachable devices list."""
    try:
        success = mongodb.add_reachable_device(
            device_name, 
            req.target_device, 
            req.access_type, 
            req.description
        )
        if success:
            return {"status": "success", "message": f"Added {req.target_device} to reachable devices"}
        else:
            raise HTTPException(status_code=500, detail="Failed to add reachable device")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.delete("/devices/{device_name}/reachable/{target_device}")
async def remove_reachable_device(device_name: str, target_device: str):
    """Remove a device from the reachable devices list."""
    try:
        success = mongodb.remove_reachable_device(device_name, target_device)
        if success:
            return {"status": "success", "message": f"Removed {target_device} from reachable devices"}
        else:
            raise HTTPException(status_code=500, detail="Failed to remove reachable device")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


class LinkTeachpointsRequest(BaseModel):
    source_teachpoint_id: str
    target_device: str
    target_teachpoint_id: str
    transfer_type: str = "dropoff"  # "dropoff" = source drops plate here


@app.post("/devices/{device_name}/teachpoints/link")
async def link_teachpoints(device_name: str, req: LinkTeachpointsRequest):
    """Link a teachpoint on this device to a teachpoint on another device."""
    try:
        success = mongodb.link_teachpoints(
            device_name,
            req.source_teachpoint_id,
            req.target_device,
            req.target_teachpoint_id,
            req.transfer_type
        )
        if success:
            return {
                "status": "success", 
                "message": f"Linked {device_name}:{req.source_teachpoint_id} → {req.target_device}:{req.target_teachpoint_id}"
            }
        else:
            raise HTTPException(status_code=500, detail="Failed to link teachpoints")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.delete("/devices/{device_name}/teachpoints/{teachpoint_id}/link")
async def unlink_teachpoint(device_name: str, teachpoint_id: str):
    """Remove the link from a teachpoint."""
    try:
        success = mongodb.unlink_teachpoints(device_name, teachpoint_id)
        if success:
            return {"status": "success", "message": f"Unlinked {teachpoint_id}"}
        else:
            raise HTTPException(status_code=500, detail="Failed to unlink teachpoint")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/devices/{device_name}/teachpoints/linked")
async def get_linked_teachpoints(device_name: str):
    """Get all teachpoints on this device that have links to other devices."""
    try:
        linked = mongodb.get_linked_teachpoints(device_name)
        return {"device": device_name, "linked_teachpoints": linked}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


if __name__ == "__main__":
    print(f"Starting server on port {cli_args.port}")
    uvicorn.run(app, host="0.0.0.0", port=cli_args.port)
