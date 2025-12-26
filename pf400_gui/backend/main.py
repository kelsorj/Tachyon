from fastapi import FastAPI, HTTPException, UploadFile, File
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
import shutil

# Import ROS client
from ros_client import PF400ROSClient
# Import Real Robot drivers
from pf400_driver import PF400Driver
from pf400_sxl_driver import PF400SXLDriver
from pf400_models import PF400Model, get_model_by_name, get_model_config
# Import MongoDB integration
import db as mongodb

app = FastAPI(title="PF400 Control API")

@app.get("/version")
async def get_version():
    """Return backend version metadata (useful to confirm which server the GUI is talking to)."""
    commit = None
    try:
        import subprocess
        commit = (
            subprocess.check_output(["git", "rev-parse", "--short", "HEAD"], cwd=os.path.dirname(__file__))
            .decode("utf-8")
            .strip()
        )
    except Exception:
        commit = None
    return {"version": "0.1.0", "git_commit": commit}

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

# Mount Labware library (VWorks definitions / future 3D models)
labware_models_dir = os.path.join(os.path.dirname(__file__), "../../models/labware")
if os.path.exists(labware_models_dir):
    app.mount("/models/labware", StaticFiles(directory=labware_models_dir), name="labware_models")
    print(f"Mounted labware models: {labware_models_dir}")

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
    features: Optional[Dict[str, Any]] = None


class TeachpointFeaturesUpdateRequest(BaseModel):
    # Orientation Features
    regrip_station: Optional[bool] = None
    grip_orientation: Optional[str] = None  # "landscape" | "portrait"
    access: Optional[str] = None  # "vertical" | "horizontal"
    # Approach values (mm)
    tangent_approach_mm: Optional[float] = None  # renamed from "Y Clearance"
    z_above_mm: Optional[float] = None  # used for vertical access
    # Z grasp offset (mm) range; math will be done later, but we store the intent now
    z_grasp_offset_min_mm: Optional[float] = None
    z_grasp_offset_max_mm: Optional[float] = None
    # Optional obstacle-avoidance path (ordered list of intermediate waypoints)
    # Stored verbatim under teachpoint.features.path
    path: Optional[Dict[str, Any]] = None

class MoveToTeachpointRequest(BaseModel):
    teachpoint_id: str
    speed_profile: int = 1


class TeachpointPathPointCreateRequest(BaseModel):
    name: Optional[str] = None


class TeachpointPathPointMoveRequest(BaseModel):
    speed_profile: int = 1

class SpeedSettingsRequest(BaseModel):
    profile_id: int = 2  # Default to fast profile
    speed: int = 80      # 1-100 percentage
    accel: int = None    # Optional, defaults to speed
    decel: int = None    # Optional, defaults to accel


class GripperSetRequest(BaseModel):
    gripper_mm: float
    speed_profile: int = 1


class GripperCloseUntilContactRequest(BaseModel):
    target_closed_mm: float
    speed_profile: int = 1
    step_mm: float = 1.0
    min_motion_mm: float = 0.2
    settle_seconds: float = 0.15
    max_steps: int = 200


class PF400GripperRatedCurrentRequest(BaseModel):
    rated_current_amps: float
    unit: int = 1
    array_index: int = 5  # "5th field" per manual; may need adjustment if controller uses 0-based indexing


class PF400GripperTorqueLimitsRequest(BaseModel):
    # Asymmetric method torque clamps in tcnts (PID torque only)
    tcnts_pos_10351: Optional[int] = None
    tcnts_neg_10352: Optional[int] = None
    # If provided, write to a specific unit (robot number); if omitted, controller-global
    unit: Optional[int] = None


class PF400PickPlaceRequest(BaseModel):
    labware_type_id: str
    pick_teachpoint_id: str
    place_teachpoint_id: str
    orientation: str = "landscape"  # landscape|portrait
    speed_no_plate: int = 1
    speed_holding_plate: int = 1
    pause_seconds: float = 0.35  # pause between steps so gripper motion is visible and joints poll catches it


class PF400SafeRequest(BaseModel):
    speed_profile: int = 1


# =========================
# Labware (Types)
# =========================

class Labware3DModel(BaseModel):
    url: str
    format: str = "stl"  # stl|gltf|glb|obj|step|stp|etc


class LabwareImageModel(BaseModel):
    url: str
    content_type: str = "image/png"


class PF400GripperModel(BaseModel):
    # Mirrors legacy "BenchBot Gripper" panel in pf400.PNG
    landscape_gripping_ranges_mm: Optional[str] = None  # e.g. "2-6" or "1-3,2.5,4-8"
    landscape_open_width_mm: Optional[float] = None
    landscape_closed_width_mm: Optional[float] = None
    landscape_tolerance_mm: Optional[float] = None

    portrait_gripping_ranges_mm: Optional[str] = None
    portrait_open_width_mm: Optional[float] = None
    portrait_closed_width_mm: Optional[float] = None
    portrait_tolerance_mm: Optional[float] = None

    grip_torque_percent: Optional[float] = None  # 0-100


class PlanarMotorModel(BaseModel):
    # Basic motion limits for Planar Motor handling profiles (units: SI)
    max_velocity_m_per_s: Optional[float] = None
    max_acceleration_m_per_s2: Optional[float] = None


class PlateDimensionsMM(BaseModel):
    length_mm: float
    width_mm: float
    height_mm: float


class PlatePropertiesModel(BaseModel):
    # Mirrors `plate-props.PNG` (legacy GUI) but stored in a modern nested object.
    robot_gripper_offset_mm: Optional[float] = None
    empty_check_offset_mm: Optional[float] = None  # legacy GUI label: "Robot gripper/HD Stack 1 empty check offset"

    thickness_mm: Optional[float] = None
    stacking_thickness_mm: Optional[float] = None
    shim_thickness_mm: Optional[float] = None  # legacy GUI: "Shim/nesting thickness/HD Stack 1 hold position"

    can_be_sealed: Optional[bool] = None
    sealed_thickness_mm: Optional[float] = None
    sealed_stacking_thickness_mm: Optional[float] = None

    can_have_lid: Optional[bool] = None
    lidded_thickness_mm: Optional[float] = None
    lidded_stacking_thickness_mm: Optional[float] = None
    lid_resting_height_mm: Optional[float] = None
    lid_departure_height_mm: Optional[float] = None

    # Plate handling
    lower_plate_at_labeler: Optional[bool] = None  # legacy GUI: "Lower plate at Microplate Labeler"
    can_mount: Optional[bool] = None
    can_be_mounted: Optional[bool] = None
    max_robot_handling_speed: Optional[str] = None  # slow|medium|fast

    # Misc
    filter_tip_pin_tool_length_mm: Optional[float] = None
    filter_channel_resting_depth_mm: Optional[float] = None
    requires_insert: Optional[str] = None  # e.g. "None"


class WellDimensionsMM(BaseModel):
    # Keep intentionally flexible; different plates specify different params
    diameter_mm: Optional[float] = None
    depth_mm: Optional[float] = None
    volume_ul: Optional[float] = None
    spacing_x_mm: Optional[float] = None
    spacing_y_mm: Optional[float] = None
    offset_x_mm: Optional[float] = None
    offset_y_mm: Optional[float] = None
    rows: Optional[int] = None
    cols: Optional[int] = None
    well_geometry: Optional[int] = None
    well_bottom_shape: Optional[int] = None

    # Tip parameters (used by plate-def.PNG)
    tip_source: Optional[str] = None  # agilent|third_party
    disposable_tip_capacity_ul: Optional[float] = None
    disposable_tip_length_mm: Optional[float] = None


class LabwareTypeCreateRequest(BaseModel):
    kind: str  # sbs_plate|tube|vial
    name: str
    vendor: Optional[str] = ""
    catalog_number: Optional[str] = ""
    description: Optional[str] = ""
    base_class: Optional[str] = ""  # microplate|filter_plate|reservoir|tip_box|lid|etc
    labware_class_ids: Optional[List[str]] = None
    plate_properties: Optional[PlatePropertiesModel] = None

    # SBS plate metadata
    wells: Optional[int] = None  # 6/24/48/96/384/1536
    well_type: Optional[str] = ""  # e.g. round, square, u_bottom, v_bottom, flat, etc
    plate_dimensions_mm: Optional[PlateDimensionsMM] = None
    well_dimensions_mm: Optional[WellDimensionsMM] = None

    # Optional 3D model reference (for visualization/clearance)
    model_3d: Optional[Labware3DModel] = None
    image_2d: Optional[LabwareImageModel] = None
    pf400: Optional[PF400GripperModel] = None
    planar_motor: Optional[PlanarMotorModel] = None
    notes: Optional[str] = ""


class LabwareTypeUpdateRequest(BaseModel):
    name: Optional[str] = None
    vendor: Optional[str] = None
    catalog_number: Optional[str] = None
    description: Optional[str] = None
    base_class: Optional[str] = None
    labware_class_ids: Optional[List[str]] = None
    plate_properties: Optional[PlatePropertiesModel] = None
    wells: Optional[int] = None
    well_type: Optional[str] = None
    plate_dimensions_mm: Optional[PlateDimensionsMM] = None
    well_dimensions_mm: Optional[WellDimensionsMM] = None
    model_3d: Optional[Labware3DModel] = None
    image_2d: Optional[LabwareImageModel] = None
    pf400: Optional[PF400GripperModel] = None
    planar_motor: Optional[PlanarMotorModel] = None
    notes: Optional[str] = None


class LabwareClassCreateRequest(BaseModel):
    name: str
    description: Optional[str] = ""


class LabwareClassUpdateRequest(BaseModel):
    name: Optional[str] = None
    description: Optional[str] = None


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


@app.on_event("shutdown")
async def shutdown_event():
    """Best-effort cleanup so PM2 restarts release robot sockets cleanly."""
    global robot_client
    try:
        if robot_client and hasattr(robot_client, "driver") and hasattr(robot_client.driver, "disconnect"):
            try:
                robot_client.driver.disconnect()
            except Exception:
                pass
    finally:
        pass


@app.post("/pf400/reconnect")
async def pf400_reconnect():
    """
    Force-close any existing PF400 sockets and reconnect.
    This is a lighter-weight recovery than restarting PM2.
    """
    if not robot_client or not hasattr(robot_client, "driver"):
        raise HTTPException(status_code=503, detail="Robot client not initialized")

    d = robot_client.driver
    ip = getattr(d, "ip", None)
    port = getattr(d, "port", None)

    try:
        if hasattr(d, "disconnect"):
            d.disconnect()
    except Exception:
        pass

    import time
    last_err = None
    for _ in range(4):
        try:
            ok = bool(d.connect(auto_initialize=True))
            if ok and getattr(d, "connected", False):
                return {"status": "success", "ip": ip, "port": port, "connected": True}
        except Exception as e:
            last_err = str(e)
        time.sleep(0.35)

    raise HTTPException(status_code=503, detail=f"Reconnect failed (ip={ip}, port={port}): {last_err or 'unknown error'}")

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


@app.post("/gripper/set")
async def set_gripper(req: GripperSetRequest):
    """Set PF400 gripper opening to an absolute value in mm (keeps all other joints the same)."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    if not hasattr(robot_client, "driver"):
        raise HTTPException(status_code=501, detail="Gripper set not supported by current client")

    try:
        joints_dict = robot_client.get_joint_positions() if hasattr(robot_client, "get_joint_positions") else {}
        import math
        j1_mm = float(joints_dict.get("j1", 0)) * 1000.0
        j2_deg = float(joints_dict.get("j2", 0)) * 180.0 / math.pi
        j3_deg = float(joints_dict.get("j3", 0)) * 180.0 / math.pi
        j4_deg = float(joints_dict.get("j4", 0)) * 180.0 / math.pi
        j6_mm = float(joints_dict.get("j6", 0)) * 1000.0 if "j6" in joints_dict else None

        success = robot_client.driver.move_to_joints_raw(
            j1_mm=j1_mm,
            j2_deg=j2_deg,
            j3_deg=j3_deg,
            j4_deg=j4_deg,
            gripper_mm=req.gripper_mm,
            j6_mm=j6_mm,
            profile=req.speed_profile,
        )
        if not success:
            raise HTTPException(status_code=500, detail="Failed to set gripper")
        return {"status": "success", "gripper_mm": req.gripper_mm}
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Gripper set error: {str(e)}")


@app.post("/gripper/close-until-contact")
async def gripper_close_until_contact(req: GripperCloseUntilContactRequest):
    """
    Close the gripper in small steps until it stalls (contact) or reaches target.
    Note: This is a proxy for "force sensing" based on position stall detection.
    """
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    if not hasattr(robot_client, "driver"):
        raise HTTPException(status_code=501, detail="Gripper close-until-contact not supported by current client")

    drv = robot_client.driver
    if not hasattr(drv, "close_gripper_until_contact"):
        raise HTTPException(status_code=501, detail="Driver does not support close-until-contact")

    try:
        result = drv.close_gripper_until_contact(
            target_closed_mm=req.target_closed_mm,
            profile=req.speed_profile,
            step_mm=req.step_mm,
            min_motion_mm=req.min_motion_mm,
            settle_seconds=req.settle_seconds,
            max_steps=req.max_steps,
        )
        if result.get("status") != "success":
            raise HTTPException(status_code=500, detail=result.get("reason") or "close-until-contact failed")
        return result
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"close-until-contact error: {str(e)}")


@app.get("/gripper/squeeze/simple")
async def gripper_get_squeeze_simple(unit: int = 1, array_index: int = 5):
    """
    Read PF400 gripper rated current (simple method) from PDB #10611 field index,
    and return estimated squeeze/opening force per the manual.
    """
    if not robot_client or not hasattr(robot_client, "driver"):
        raise HTTPException(status_code=503, detail="Robot driver not available")
    drv = robot_client.driver
    if not hasattr(drv, "gripper_get_rated_current_simple"):
        raise HTTPException(status_code=501, detail="Driver does not support gripper squeeze settings")
    try:
        return drv.gripper_get_rated_current_simple(unit=unit, array_index=array_index)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/gripper/squeeze/simple")
async def gripper_set_squeeze_simple(req: PF400GripperRatedCurrentRequest):
    """
    Set PF400 gripper rated current (simple method) by writing PDB #10611 field index,
    and return estimated squeeze/opening force per the manual.
    """
    if not robot_client or not hasattr(robot_client, "driver"):
        raise HTTPException(status_code=503, detail="Robot driver not available")
    drv = robot_client.driver
    if not hasattr(drv, "gripper_set_rated_current_simple"):
        raise HTTPException(status_code=501, detail="Driver does not support gripper squeeze settings")
    try:
        return drv.gripper_set_rated_current_simple(
            rated_current_amps=req.rated_current_amps,
            unit=req.unit,
            array_index=req.array_index,
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/gripper/squeeze/asymmetric")
async def gripper_set_squeeze_asymmetric(req: PF400GripperTorqueLimitsRequest):
    """
    Set asymmetric squeeze limits using PDB 10351/10352 (tcnts).
    This limits PID torque only (feedforward/spring compensation is not limited).
    """
    if not robot_client or not hasattr(robot_client, "driver"):
        raise HTTPException(status_code=503, detail="Robot driver not available")
    drv = robot_client.driver
    if not hasattr(drv, "gripper_set_torque_limits_asymmetric"):
        raise HTTPException(status_code=501, detail="Driver does not support gripper torque limit settings")
    try:
        return drv.gripper_set_torque_limits_asymmetric(
            tcnts_pos_10351=req.tcnts_pos_10351,
            tcnts_neg_10352=req.tcnts_neg_10352,
            unit=req.unit,
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

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
        if req.features is not None:
            teachpoint_data["features"] = req.features
            
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

        # If updating existing teachpoint, preserve link + features data
        existing_preserve: Dict[str, Any] = {}
        if id:
            existing_teachpoints = mongodb.get_device_teachpoints(DEVICE_NAME)
            if tp_id in existing_teachpoints:
                existing_tp = existing_teachpoints[tp_id]
                # Preserve link information
                if "linked_to" in existing_tp:
                    existing_preserve["linked_to"] = existing_tp["linked_to"]
                if "linked_from" in existing_tp:
                    existing_preserve["linked_from"] = existing_tp["linked_from"]
                if "features" in existing_tp:
                    existing_preserve["features"] = existing_tp["features"]

        teachpoint_data = {
            "name": name,
            "description": description,
            "type": "joints",
            "joints": joints_list,
            "cartesian": cartesian_dict,
            **existing_preserve  # Preserve links + features
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
async def move_to_teachpoint(teachpoint_id: str, speed_profile: int = 1, keep_gripper: bool = False):
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
            # If a path is defined, always go: safe tuck -> path points -> teachpoint.
            features = tp.get("features") if isinstance(tp.get("features"), dict) else {}
            path = features.get("path") if isinstance(features.get("path"), dict) else {}
            pts = path.get("points") if isinstance(path.get("points"), list) else []
            # joints is [j1_mm, j2_deg, j3_deg, j4_deg, gripper_mm, j6_mm/rail]
            # Use move_to_joints_raw which expects robot native units (mm/deg)
            if hasattr(robot_client, 'driver'):
                if pts:
                    # Safe tuck first (keeps J1/J6/gripper)
                    try:
                        if hasattr(robot_client.driver, "safe_tuck"):
                            robot_client.driver.safe_tuck(profile=int(speed_profile))
                        else:
                            # fall back to tuck posture via move_joint if needed
                            cur = robot_client.driver.get_joint_states()
                            if cur and len(cur) >= 5:
                                target = list(cur)
                                target[3] = -188.0
                                target[1] = 4.0
                                target[2] = 179.0
                                robot_client.driver.move_joint(target, profile=int(speed_profile))
                    except Exception:
                        pass
                    # Execute each path point while tucked.
                    # Use await_inrange with tight tolerances for smooth blending between waypoints.
                    blend_mm = float(path.get("blend_mm") or 2.0)
                    blend_deg = float(path.get("blend_deg") or 1.0)
                    poll_s = float(path.get("blend_poll_s") or 0.05)
                    timeout_s = float(path.get("blend_timeout_s") or 20.0)

                    for i, pt in enumerate(pts):
                        if not isinstance(pt, dict):
                            continue
                        pj = pt.get("joints")
                        if not isinstance(pj, list) or len(pj) < 5:
                            continue
                        try:
                            pj = [float(x) for x in pj]
                        except Exception:
                            continue
                        # Preserve gripper during path replay
                        try:
                            rr = robot_client.driver.send_command("wherej")
                            cur = [float(x) for x in str(rr).split()[1:]]
                            if len(cur) > 4 and len(pj) > 4:
                                pj[4] = cur[4]
                        except Exception:
                            pass
                        is_last = (i == len(pts) - 1)
                        if hasattr(robot_client.driver, "movej_raw"):
                            robot_client.driver.movej_raw(list(pj), profile=int(speed_profile), wait=is_last)
                        else:
                            robot_client.driver.move_joint(list(pj), profile=int(speed_profile), wait=is_last)
                        if not is_last and hasattr(robot_client.driver, "await_inrange"):
                            robot_client.driver.await_inrange(list(pj), tol_mm=blend_mm, tol_deg=blend_deg, poll_s=poll_s, timeout_s=timeout_s)
                j6_mm = joints[5] if len(joints) > 5 else None
                gripper_mm = joints[4]
                if keep_gripper:
                    # Keep current gripper opening rather than overwriting from teachpoint
                    try:
                        joints_dict = robot_client.get_joint_positions() if hasattr(robot_client, "get_joint_positions") else {}
                        gripper_mm = float(joints_dict.get("gripper", 0)) * 1000.0
                    except Exception:
                        pass
                success = robot_client.driver.move_to_joints_raw(
                    j1_mm=joints[0],
                    j2_deg=joints[1],
                    j3_deg=joints[2],
                    j4_deg=joints[3],
                    gripper_mm=gripper_mm,
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


@app.patch("/teachpoints/{teachpoint_id}/features")
async def patch_teachpoint_features(teachpoint_id: str, req: TeachpointFeaturesUpdateRequest):
    """Update teachpoint Orientation/Approach Features (no motion math yet; just persistence)."""
    try:
        teachpoints = mongodb.get_device_teachpoints(DEVICE_NAME)
        if teachpoint_id not in teachpoints:
            raise HTTPException(status_code=404, detail=f"Teachpoint '{teachpoint_id}' not found")

        tp = teachpoints[teachpoint_id]
        features = dict(tp.get("features") or {})

        # Apply updates
        for k, v in req.model_dump(exclude_unset=True).items():
            features[k] = v

        tp["features"] = features
        ok = mongodb.save_teachpoint(DEVICE_NAME, teachpoint_id, tp)
        if not ok:
            raise HTTPException(status_code=500, detail="Failed to update teachpoint features")
        return {"status": "success", "teachpoint_id": teachpoint_id, "features": features}
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


def _tp_path_points(tp: Dict[str, Any]) -> List[Dict[str, Any]]:
    """Get a mutable list of path points from a teachpoint features dict."""
    features = tp.get("features") if isinstance(tp.get("features"), dict) else {}
    path = features.get("path") if isinstance(features.get("path"), dict) else {}
    pts = path.get("points")
    return list(pts) if isinstance(pts, list) else []


def _tp_set_path_points(tp: Dict[str, Any], points: List[Dict[str, Any]]) -> None:
    """Set path points on a teachpoint features dict."""
    features = dict(tp.get("features") or {})
    path = dict(features.get("path") or {})
    path["points"] = list(points)
    features["path"] = path
    tp["features"] = features


def _capture_current_path_point(name: Optional[str] = None) -> Dict[str, Any]:
    """Capture current robot state (joints + cartesian) as a path point."""
    if not robot_client or not hasattr(robot_client, "driver"):
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    # Joints in robot-native units (mm/deg) are required for deterministic replay.
    # IMPORTANT: PF400SXLDriver.get_joint_states() returns a diagnostics dict (not a list),
    # so always read raw `wherej` and parse the numeric joint list.
    try:
        resp = robot_client.driver.send_command("wherej")
        parts = str(resp).strip().split()
        joints_raw = [float(x) for x in parts[1:]]  # skip status
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Failed to read current joints (wherej): {e}")
    if not joints_raw or len(joints_raw) < 5:
        raise HTTPException(status_code=500, detail="Failed to read current joints (empty wherej)")
    cart = None
    try:
        if hasattr(robot_client.driver, "get_cartesian_position"):
            cart = robot_client.driver.get_cartesian_position()
    except Exception:
        cart = None
    import time as _time
    return {
        "name": name,
        "captured_at": _time.time(),
        "joints": list(joints_raw),
        "cartesian": cart if isinstance(cart, dict) else None,
    }


@app.post("/teachpoints/{teachpoint_id}/path/points")
async def path_add_point(teachpoint_id: str, req: TeachpointPathPointCreateRequest):
    """Append a new path point captured from the robot's current position."""
    teachpoints = mongodb.get_device_teachpoints(DEVICE_NAME) or {}
    if teachpoint_id not in teachpoints:
        raise HTTPException(status_code=404, detail=f"Teachpoint '{teachpoint_id}' not found")
    tp = teachpoints[teachpoint_id]
    pts = _tp_path_points(tp)
    # Default name: P1, P2, ...
    nm = (req.name or "").strip() or f"P{len(pts)+1}"
    pts.append(_capture_current_path_point(nm))
    _tp_set_path_points(tp, pts)
    ok = mongodb.save_teachpoint(DEVICE_NAME, teachpoint_id, tp)
    if not ok:
        raise HTTPException(status_code=500, detail="Failed to update teachpoint path points")
    return {"status": "success", "teachpoint_id": teachpoint_id, "path": tp["features"].get("path")}


@app.patch("/teachpoints/{teachpoint_id}/path/points/{index}")
async def path_update_point(teachpoint_id: str, index: int, req: TeachpointPathPointCreateRequest):
    """Overwrite an existing path point with the robot's current position (keeps name unless provided)."""
    teachpoints = mongodb.get_device_teachpoints(DEVICE_NAME) or {}
    if teachpoint_id not in teachpoints:
        raise HTTPException(status_code=404, detail=f"Teachpoint '{teachpoint_id}' not found")
    tp = teachpoints[teachpoint_id]
    pts = _tp_path_points(tp)
    if index < 0 or index >= len(pts):
        raise HTTPException(status_code=404, detail="Path point index out of range")
    old = pts[index] if isinstance(pts[index], dict) else {}
    nm = (req.name or "").strip() or (old.get("name") if isinstance(old, dict) else None) or f"P{index+1}"
    pts[index] = _capture_current_path_point(nm)
    _tp_set_path_points(tp, pts)
    ok = mongodb.save_teachpoint(DEVICE_NAME, teachpoint_id, tp)
    if not ok:
        raise HTTPException(status_code=500, detail="Failed to update teachpoint path points")
    return {"status": "success", "teachpoint_id": teachpoint_id, "path": tp["features"].get("path")}


@app.delete("/teachpoints/{teachpoint_id}/path/points/{index}")
async def path_delete_point(teachpoint_id: str, index: int):
    """Delete a path point by index."""
    teachpoints = mongodb.get_device_teachpoints(DEVICE_NAME) or {}
    if teachpoint_id not in teachpoints:
        raise HTTPException(status_code=404, detail=f"Teachpoint '{teachpoint_id}' not found")
    tp = teachpoints[teachpoint_id]
    pts = _tp_path_points(tp)
    if index < 0 or index >= len(pts):
        raise HTTPException(status_code=404, detail="Path point index out of range")
    pts.pop(index)
    _tp_set_path_points(tp, pts)
    ok = mongodb.save_teachpoint(DEVICE_NAME, teachpoint_id, tp)
    if not ok:
        raise HTTPException(status_code=500, detail="Failed to update teachpoint path points")
    return {"status": "success", "teachpoint_id": teachpoint_id, "path": tp["features"].get("path")}


@app.post("/teachpoints/{teachpoint_id}/path/points/{index}/move")
async def path_move_to_point(teachpoint_id: str, index: int, req: TeachpointPathPointMoveRequest):
    """Move the robot to a specific path point (joint-space) for obstacle avoidance debugging."""
    if not robot_client or not hasattr(robot_client, "driver"):
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    teachpoints = mongodb.get_device_teachpoints(DEVICE_NAME) or {}
    if teachpoint_id not in teachpoints:
        raise HTTPException(status_code=404, detail=f"Teachpoint '{teachpoint_id}' not found")
    tp = teachpoints[teachpoint_id]
    pts = _tp_path_points(tp)
    if index < 0 or index >= len(pts):
        raise HTTPException(status_code=404, detail="Path point index out of range")
    pt = pts[index] if isinstance(pts[index], dict) else {}
    joints = pt.get("joints")
    if not joints or not isinstance(joints, list) or len(joints) < 5:
        raise HTTPException(status_code=400, detail="Path point has no joint data")
    # Validate numeric joints; older buggy captures may have stored dict keys (strings) here.
    try:
        joints = [float(x) for x in joints]
    except Exception:
        raise HTTPException(status_code=400, detail="Path point joint data is invalid; re-Add or Update this waypoint")
    resp = robot_client.driver.move_joint(list(joints), profile=int(req.speed_profile))
    if resp is None or str(resp).strip().startswith("-"):
        raise HTTPException(status_code=500, detail=f"Move to path point failed: {resp}")
    return {"status": "success", "teachpoint_id": teachpoint_id, "index": index, "name": pt.get("name")}

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


# ============== Labware API ==============

@app.get("/labware/types")
async def get_labware_types():
    """Get all labware types from MongoDB."""
    try:
        def _infer_pf400_from_vworks_raw(vworks_raw: Dict[str, Any]) -> Optional[Dict[str, Any]]:
            if not isinstance(vworks_raw, dict):
                return None
            # Prefer BENCHBOT_* keys (legacy naming) if present
            def _f(key: str) -> Optional[float]:
                v = vworks_raw.get(key)
                if v is None:
                    return None
                try:
                    return float(str(v).strip())
                except Exception:
                    return None
            def _s(key: str) -> Optional[str]:
                v = vworks_raw.get(key)
                if v is None:
                    return None
                s = str(v).strip()
                return s or None

            has_any = any(k in vworks_raw for k in (
                "BENCHBOT_LANDSCAPE_GRIPPER_OPEN_WIDTH",
                "BENCHBOT_LANDSCAPE_GRIPPER_CLOSED_WIDTH",
                "BENCHBOT_PORTRAIT_GRIPPER_OPEN_WIDTH",
                "BENCHBOT_PORTRAIT_GRIPPER_CLOSED_WIDTH",
            ))
            if not has_any:
                return None

            return {
                "landscape_gripping_ranges_mm": _s("BENCHBOT_LANDSCAPE_GRIPPER_OFFSET_RANGES"),
                "landscape_open_width_mm": _f("BENCHBOT_LANDSCAPE_GRIPPER_OPEN_WIDTH"),
                "landscape_closed_width_mm": _f("BENCHBOT_LANDSCAPE_GRIPPER_CLOSED_WIDTH"),
                "landscape_tolerance_mm": _f("BENCHBOT_LANDSCAPE_GRIPPER_TOLERANCE"),
                "portrait_gripping_ranges_mm": _s("BENCHBOT_PORTRAIT_GRIPPER_OFFSET_RANGES"),
                "portrait_open_width_mm": _f("BENCHBOT_PORTRAIT_GRIPPER_OPEN_WIDTH"),
                "portrait_closed_width_mm": _f("BENCHBOT_PORTRAIT_GRIPPER_CLOSED_WIDTH"),
                "portrait_tolerance_mm": _f("BENCHBOT_PORTRAIT_GRIPPER_TOLERANCE"),
                "grip_torque_percent": _f("BENCHBOT_GRIP_TORQUE_PERCENTAGE"),
            }

        labware_types = mongodb.get_all_labware_types()
        # Backfill pf400 from vworks_raw if missing for the API response.
        # IMPORTANT: do NOT persist here; users may have edited PF400 values and we must not overwrite them.
        for lt in labware_types:
            if lt.get("pf400") is not None:
                continue
            inferred = _infer_pf400_from_vworks_raw(lt.get("vworks_raw") or {})
            if inferred:
                lt["pf400"] = inferred
        return {"labware_types": labware_types}
    except Exception as e:
        print(f"Error getting labware types: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/labware/types")
async def create_labware_type(req: LabwareTypeCreateRequest):
    """Create a labware type (SBS plate, tube, vial)."""
    try:
        kind = (req.kind or "").strip()
        if kind not in ("sbs_plate", "tube", "vial"):
            raise HTTPException(status_code=400, detail="kind must be one of: sbs_plate, tube, vial")

        name = (req.name or "").strip()
        if not name:
            raise HTTPException(status_code=400, detail="name is required")

        if kind == "sbs_plate":
            if req.wells not in (6, 24, 48, 96, 384, 1536):
                raise HTTPException(status_code=400, detail="For sbs_plate, wells must be one of: 6,24,48,96,384,1536")
            if req.plate_dimensions_mm is None:
                raise HTTPException(status_code=400, detail="For sbs_plate, plate_dimensions_mm is required")

        labware_type_id = _new_ulid_str()
        created = mongodb.create_labware_type({
            "labware_type_id": labware_type_id,
            "kind": kind,
            "name": name,
            "vendor": (req.vendor or "").strip(),
            "catalog_number": (req.catalog_number or "").strip(),
            "description": (req.description or "").strip(),
            "base_class": (req.base_class or "").strip(),
            "labware_class_ids": req.labware_class_ids or [],
            "plate_properties": req.plate_properties.model_dump() if req.plate_properties else None,
            "wells": req.wells,
            "well_type": (req.well_type or "").strip(),
            "plate_dimensions_mm": req.plate_dimensions_mm.model_dump() if req.plate_dimensions_mm else None,
            "well_dimensions_mm": req.well_dimensions_mm.model_dump() if req.well_dimensions_mm else None,
            "model_3d": req.model_3d.model_dump() if req.model_3d else None,
            "image_2d": req.image_2d.model_dump() if req.image_2d else None,
            "pf400": req.pf400.model_dump() if req.pf400 else None,
            "planar_motor": req.planar_motor.model_dump() if req.planar_motor else None,
            "notes": req.notes or "",
        })
        if not created:
            raise HTTPException(status_code=500, detail="Failed to create labware type")
        return {"labware_type": created}
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error creating labware type: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.delete("/labware/types/{labware_type_id}")
async def delete_labware_type(labware_type_id: str):
    """Delete a labware type by ID."""
    try:
        ok = mongodb.delete_labware_type(labware_type_id)
        if not ok:
            raise HTTPException(status_code=404, detail="Labware type not found")
        return {"deleted": True, "labware_type_id": labware_type_id}
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error deleting labware type: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/labware/types/{labware_type_id}")
async def get_labware_type(labware_type_id: str):
    """Get one labware type by id."""
    try:
        doc = mongodb.get_labware_type_by_id(labware_type_id)
        if not doc:
            raise HTTPException(status_code=404, detail="Labware type not found")
        if doc.get("pf400") is None and isinstance(doc.get("vworks_raw"), dict):
            vr = doc["vworks_raw"]
            has_any = any(k in vr for k in (
                "BENCHBOT_LANDSCAPE_GRIPPER_OPEN_WIDTH",
                "BENCHBOT_LANDSCAPE_GRIPPER_CLOSED_WIDTH",
                "BENCHBOT_PORTRAIT_GRIPPER_OPEN_WIDTH",
                "BENCHBOT_PORTRAIT_GRIPPER_CLOSED_WIDTH",
            ))
            if has_any:
                def _f(key: str) -> Optional[float]:
                    v = vr.get(key)
                    if v is None:
                        return None
                    try:
                        return float(str(v).strip())
                    except Exception:
                        return None
                def _s(key: str) -> Optional[str]:
                    v = vr.get(key)
                    if v is None:
                        return None
                    s = str(v).strip()
                    return s or None
                inferred = {
                    "landscape_gripping_ranges_mm": _s("BENCHBOT_LANDSCAPE_GRIPPER_OFFSET_RANGES"),
                    "landscape_open_width_mm": _f("BENCHBOT_LANDSCAPE_GRIPPER_OPEN_WIDTH"),
                    "landscape_closed_width_mm": _f("BENCHBOT_LANDSCAPE_GRIPPER_CLOSED_WIDTH"),
                    "landscape_tolerance_mm": _f("BENCHBOT_LANDSCAPE_GRIPPER_TOLERANCE"),
                    "portrait_gripping_ranges_mm": _s("BENCHBOT_PORTRAIT_GRIPPER_OFFSET_RANGES"),
                    "portrait_open_width_mm": _f("BENCHBOT_PORTRAIT_GRIPPER_OPEN_WIDTH"),
                    "portrait_closed_width_mm": _f("BENCHBOT_PORTRAIT_GRIPPER_CLOSED_WIDTH"),
                    "portrait_tolerance_mm": _f("BENCHBOT_PORTRAIT_GRIPPER_TOLERANCE"),
                    "grip_torque_percent": _f("BENCHBOT_GRIP_TORQUE_PERCENTAGE"),
                }
                doc["pf400"] = inferred
        return {"labware_type": doc}
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error getting labware type: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/pf400/pick-place")
async def pf400_pick_place(req: PF400PickPlaceRequest):
    """Minimal labware-aware pick-and-place using teachpoints + labware PF400 gripper widths."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    if not hasattr(robot_client, "driver"):
        raise HTTPException(status_code=501, detail="Pick/place not supported by current client")

    # Fetch labware
    labware = mongodb.get_labware_type_by_id(req.labware_type_id)
    if not labware:
        raise HTTPException(status_code=404, detail="Labware type not found")

    def _infer_pf400_from_vworks_raw(vworks_raw: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        if not isinstance(vworks_raw, dict):
            return None
        def _f(key: str) -> Optional[float]:
            v = vworks_raw.get(key)
            if v is None:
                return None
            try:
                return float(str(v).strip())
            except Exception:
                return None
        def _s(key: str) -> Optional[str]:
            v = vworks_raw.get(key)
            if v is None:
                return None
            s = str(v).strip()
            return s or None
        has_any = any(k in vworks_raw for k in (
            "BENCHBOT_LANDSCAPE_GRIPPER_OPEN_WIDTH",
            "BENCHBOT_LANDSCAPE_GRIPPER_CLOSED_WIDTH",
            "BENCHBOT_PORTRAIT_GRIPPER_OPEN_WIDTH",
            "BENCHBOT_PORTRAIT_GRIPPER_CLOSED_WIDTH",
        ))
        if not has_any:
            return None
        return {
            "landscape_gripping_ranges_mm": _s("BENCHBOT_LANDSCAPE_GRIPPER_OFFSET_RANGES"),
            "landscape_open_width_mm": _f("BENCHBOT_LANDSCAPE_GRIPPER_OPEN_WIDTH"),
            "landscape_closed_width_mm": _f("BENCHBOT_LANDSCAPE_GRIPPER_CLOSED_WIDTH"),
            "landscape_tolerance_mm": _f("BENCHBOT_LANDSCAPE_GRIPPER_TOLERANCE"),
            "portrait_gripping_ranges_mm": _s("BENCHBOT_PORTRAIT_GRIPPER_OFFSET_RANGES"),
            "portrait_open_width_mm": _f("BENCHBOT_PORTRAIT_GRIPPER_OPEN_WIDTH"),
            "portrait_closed_width_mm": _f("BENCHBOT_PORTRAIT_GRIPPER_CLOSED_WIDTH"),
            "portrait_tolerance_mm": _f("BENCHBOT_PORTRAIT_GRIPPER_TOLERANCE"),
            "grip_torque_percent": _f("BENCHBOT_GRIP_TORQUE_PERCENTAGE"),
        }

    pf = (labware.get("pf400") or {})
    orient = (req.orientation or "landscape").lower().strip()
    if orient not in ("landscape", "portrait"):
        raise HTTPException(status_code=400, detail="orientation must be 'landscape' or 'portrait'")

    open_mm = pf.get(f"{orient}_open_width_mm")
    closed_mm = pf.get(f"{orient}_closed_width_mm")
    tolerance_mm = pf.get(f"{orient}_tolerance_mm")
    if (open_mm is None or closed_mm is None) and isinstance(labware.get("vworks_raw"), dict):
        inferred = _infer_pf400_from_vworks_raw(labware.get("vworks_raw") or {})
        if inferred:
            open_mm = inferred.get(f"{orient}_open_width_mm")
            closed_mm = inferred.get(f"{orient}_closed_width_mm")
            if tolerance_mm is None:
                tolerance_mm = inferred.get(f"{orient}_tolerance_mm")
    if open_mm is None or closed_mm is None:
        raise HTTPException(status_code=400, detail=f"Labware PF400 {orient} open/closed widths are not set")

    def _normalize_mm(val: Any) -> float:
        # If user entered meters (e.g. 0.132), auto-convert to mm.
        v = float(val)
        if 0 < abs(v) < 1.0:
            return v * 1000.0
        return v

    open_mm = _normalize_mm(open_mm)
    closed_mm = _normalize_mm(closed_mm)
    # Use labware-defined tolerance for BOTH open and close settle checks.
    # IMPORTANT: tolerance is already in millimeters; do NOT apply the meters→mm heuristic.
    # If missing or invalid, fall back to a conservative default.
    try:
        grip_tol_mm = abs(float(tolerance_mm)) if tolerance_mm is not None else 0.6
    except Exception:
        grip_tol_mm = 0.6
    if grip_tol_mm <= 0:
        grip_tol_mm = 0.6
    if abs(open_mm) < 0.1 or abs(closed_mm) < 0.1:
        # Driver treats <0.1mm as placeholder and will ignore gripper movement.
        raise HTTPException(
            status_code=400,
            detail="PF400 open/closed widths look too small; expected millimeters (e.g. 132.5), not meters or 0.1 placeholders.",
        )

    # Fetch teachpoints
    tps = mongodb.get_device_teachpoints(DEVICE_NAME) or {}
    if req.pick_teachpoint_id not in tps:
        raise HTTPException(status_code=404, detail="Pick teachpoint not found")
    if req.place_teachpoint_id not in tps:
        raise HTTPException(status_code=404, detail="Place teachpoint not found")

    def _tp_features(tp: Dict[str, Any]) -> Dict[str, Any]:
        f = tp.get("features")
        return f if isinstance(f, dict) else {}

    def _tp_z_above_mm(tp: Dict[str, Any]) -> float:
        z = _tp_features(tp).get("z_above_mm")
        if z is None:
            return 0.0
        try:
            return float(_normalize_mm(z))
        except Exception:
            return 0.0

    def _tp_tangent_mm(tp: Dict[str, Any]) -> float:
        """
        Tangent approach distance in mm.
        Convention:
          - Positive value approaches from +Y (global)
          - Negative value approaches from -Y (global)
        """
        v = _tp_features(tp).get("tangent_approach_mm")
        if v is None:
            return 0.0
        try:
            f = float(v)
            f = float(_normalize_mm(f))
            return 0.0 if abs(f) < 0.1 else f
        except Exception:
            return 0.0

    def _tp_cartesian(tp: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        c = tp.get("cartesian")
        if not isinstance(c, dict):
            return None
        try:
            x = float(c.get("x"))
            y = float(c.get("y"))
            z = float(c.get("z"))
            yaw = float(c.get("yaw"))
            pitch = float(c.get("pitch"))
            roll = float(c.get("roll"))
            out: Dict[str, Any] = {"x": x, "y": y, "z": z, "yaw": yaw, "pitch": pitch, "roll": roll}
            cfg = c.get("config")
            if cfg is not None:
                try:
                    out["config"] = int(float(cfg))
                except Exception:
                    pass
            return out
        except Exception:
            return None

    def _ensure_rail_mm(j6_mm: Optional[float], profile: int):
        """
        Best-effort: make sure rail is at requested position before cartesian moves.
        (MoveC does not explicitly command extra axes.)
        """
        if j6_mm is None:
            return
        if not hasattr(robot_client, "driver"):
            return
        try:
            # Prefer dedicated rail move to avoid full movej sequencing edge-cases.
            if hasattr(robot_client.driver, "move_rail_mm"):
                resp = robot_client.driver.move_rail_mm(float(j6_mm), profile=int(profile))
            else:
                # Fallback: use MoveOneAxis directly if available
                cmd = f"MoveOneAxis 6 {float(j6_mm)} {int(profile)}"
                resp = robot_client.driver.send_command(cmd)
                robot_client.driver.await_movement_completion()

            if isinstance(resp, str) and resp.strip().startswith("-"):
                raise HTTPException(status_code=500, detail=f"Rail pre-position failed: {resp}")
        except HTTPException:
            raise
        except Exception:
            # non-fatal: we'll try MoveC anyway
            return

    def _ensure_rail_and_z_mm(j6_mm: Optional[float], j1_mm: Optional[float], profile: int):
        """
        When the arm is tucked/safe, it's safe (and desirable) to move J6 (rail) and J1 (vertical) together.

        This avoids: "J6 finishes, then J1 executes".
        """
        if not hasattr(robot_client, "driver"):
            return
        if j6_mm is None and j1_mm is None:
            return
        try:
            cur = robot_client.driver.get_joint_states()
            if not cur or len(cur) < 5:
                return

            tgt = list(cur)
            # Keep/force tuck joints
            if len(tgt) > 3:
                tgt[1] = 4.0
                tgt[2] = 179.0
                tgt[3] = -188.0
            if j1_mm is not None and len(tgt) > 0:
                tgt[0] = float(j1_mm)
            if j6_mm is not None and len(tgt) > 5:
                tgt[5] = float(j6_mm)

            _move_joints_raw(tgt, profile=int(profile), gripper_mm=None)
        except Exception:
            # non-fatal: we'll try MoveC anyway
            return

    def _move_joints_raw(joints: List[float], profile: int, gripper_mm: Optional[float] = None):
        if not joints or len(joints) < 4:
            raise HTTPException(status_code=400, detail="Teachpoint has no joint data")
        j6_mm = joints[5] if len(joints) > 5 else None
        ok = robot_client.driver.move_to_joints_raw(
            j1_mm=joints[0],
            j2_deg=joints[1],
            j3_deg=joints[2],
            j4_deg=joints[3],
            gripper_mm=gripper_mm,
            j6_mm=j6_mm,
            profile=profile,
        )
        if not ok:
            raise HTTPException(status_code=500, detail="Move failed")

    def _run_teachpoint_path(tp_id: str, profile: int, steps: List[Dict[str, Any]], step_prefix: str) -> bool:
        """
        If teachpoint has features.path.points, execute them in order (joint-space) while tucked.
        Returns True if any points were executed.

        Use await_inrange with tight tolerances for smooth blending between waypoints.
        """
        tp = tps.get(tp_id) or {}
        features = tp.get("features") if isinstance(tp.get("features"), dict) else {}
        path = features.get("path") if isinstance(features.get("path"), dict) else {}
        pts = path.get("points") if isinstance(path.get("points"), list) else []
        if not pts:
            return False

        # Tight tolerances for smooth blending
        blend_mm = float(path.get("blend_mm") or 2.0)
        blend_deg = float(path.get("blend_deg") or 1.0)
        poll_s = float(path.get("blend_poll_s") or 0.05)
        timeout_s = float(path.get("blend_timeout_s") or 20.0)

        executed_any = False
        for i, pt in enumerate(pts):
            if not isinstance(pt, dict):
                continue
            joints = pt.get("joints")
            if not isinstance(joints, list) or len(joints) < 5:
                continue
            try:
                joints = [float(x) for x in joints]
            except Exception:
                raise HTTPException(status_code=400, detail=f"Teachpoint '{tp_id}' has invalid path point #{i+1}; re-Add or Update it")

            # Preserve current gripper value (path points are for obstacle avoidance, not gripping).
            try:
                wr = robot_client.driver.send_command("wherej")
                cur = [float(x) for x in str(wr).split()[1:]]
                if len(cur) > 4 and len(joints) > 4:
                    joints[4] = cur[4]
            except Exception:
                pass

            steps.append({
                "step": f"{step_prefix}_path_point",
                "teachpoint_id": tp_id,
                "index": i,
                "name": pt.get("name") or f"P{i+1}",
            })

            is_last = (i == len(pts) - 1)
            if hasattr(robot_client.driver, "movej_raw"):
                resp = robot_client.driver.movej_raw(list(joints), profile=int(profile), wait=is_last)
                if resp is None or str(resp).strip().startswith("-"):
                    raise HTTPException(status_code=500, detail=f"Path move failed during execution: {resp}")
            elif hasattr(robot_client.driver, "move_joint"):
                resp = robot_client.driver.move_joint(list(joints), profile=int(profile), wait=is_last)
                if resp is None or str(resp).strip().startswith("-"):
                    raise HTTPException(status_code=500, detail=f"Path move failed during execution: {resp}")
            else:
                _move_joints_raw(list(joints), int(profile), gripper_mm=None)

            executed_any = True
            if not is_last and hasattr(robot_client.driver, "await_inrange"):
                robot_client.driver.await_inrange(list(joints), tol_mm=blend_mm, tol_deg=blend_deg, poll_s=poll_s, timeout_s=timeout_s)

        return executed_any

    def _move_tp(tp_id: str, profile: int):
        tp = tps[tp_id]
        joints = tp.get("joints") or None
        if not joints or len(joints) < 4:
            raise HTTPException(status_code=400, detail=f"Teachpoint '{tp_id}' has no joint data")
        _move_joints_raw(joints, profile, gripper_mm=None)

    def _set_grip(mm: float, profile: int):
        # Reuse absolute gripper set logic by calling driver with current joints.
        joints_dict = robot_client.get_joint_positions() if hasattr(robot_client, "get_joint_positions") else {}
        import math
        j1_mm = float(joints_dict.get("j1", 0)) * 1000.0
        j2_deg = float(joints_dict.get("j2", 0)) * 180.0 / math.pi
        j3_deg = float(joints_dict.get("j3", 0)) * 180.0 / math.pi
        j4_deg = float(joints_dict.get("j4", 0)) * 180.0 / math.pi
        j6_mm = float(joints_dict.get("j6", 0)) * 1000.0 if "j6" in joints_dict else None
        ok = robot_client.driver.move_to_joints_raw(
            j1_mm=j1_mm,
            j2_deg=j2_deg,
            j3_deg=j3_deg,
            j4_deg=j4_deg,
            gripper_mm=float(mm),
            j6_mm=j6_mm,
            profile=profile,
        )
        if not ok:
            raise HTTPException(status_code=500, detail=f"Failed to set gripper to {mm}mm")

    def _get_gripper_mm() -> Optional[float]:
        try:
            joints_dict = robot_client.get_joint_positions() if hasattr(robot_client, "get_joint_positions") else {}
            if "gripper" in joints_dict:
                return float(joints_dict.get("gripper", 0)) * 1000.0
        except Exception:
            pass
        return None

    def _wait_for_gripper_settle(
        *,
        target_mm: float,
        tol_mm: float = 0.6,
        stable_reads: int = 3,
        poll_s: float = 0.08,
        timeout_s: float = 1.5,
    ) -> Dict[str, Any]:
        """
        Wait until the gripper is "settled" at a target opening.
        Uses position feedback (wherej) via `_get_gripper_mm()` and requires the reading to be within
        tolerance for N consecutive samples.
        """
        import time as _time

        t0 = _time.time()
        last_mm: Optional[float] = None
        stable = 0
        reads = 0

        while True:
            reads += 1
            cur = _get_gripper_mm()
            last_mm = cur if cur is not None else last_mm
            if cur is not None:
                if abs(float(cur) - float(target_mm)) <= float(tol_mm):
                    stable += 1
                else:
                    stable = 0

                if stable >= int(stable_reads):
                    return {
                        "ok": True,
                        "target_mm": float(target_mm),
                        "tol_mm": float(tol_mm),
                        "reads": reads,
                        "final_mm": float(cur),
                        "elapsed_s": float(_time.time() - t0),
                    }

            if (_time.time() - t0) >= float(timeout_s):
                return {
                    "ok": False,
                    "target_mm": float(target_mm),
                    "tol_mm": float(tol_mm),
                    "reads": reads,
                    "final_mm": float(last_mm) if last_mm is not None else None,
                    "elapsed_s": float(_time.time() - t0),
                    "reason": "timeout",
                }

            _time.sleep(float(poll_s))

    def _snapshot_state() -> Dict[str, Any]:
        """
        Debug snapshot of current robot state (best effort).
        - joints are returned in robot-native units (mm/deg) via wherej
        - cart_config is returned via wherec when available
        """
        snap: Dict[str, Any] = {}
        try:
            if hasattr(robot_client, "driver") and hasattr(robot_client.driver, "get_joint_states"):
                js = robot_client.driver.get_joint_states()
                if js and len(js) >= 4:
                    snap["j1_mm"] = js[0]
                    snap["j2_deg"] = js[1]
                    snap["j3_deg"] = js[2]
                    snap["j4_deg"] = js[3]
                    if len(js) > 5:
                        snap["j6_mm"] = js[5]
        except Exception:
            pass
        try:
            if hasattr(robot_client, "driver") and hasattr(robot_client.driver, "get_cartesian_position"):
                pos = robot_client.driver.get_cartesian_position() or {}
                if isinstance(pos, dict) and pos.get("config") is not None:
                    snap["cart_config"] = pos.get("config")
        except Exception:
            pass
        return snap

    def _tangent_magnitudes(base_mm: float) -> List[float]:
        """
        Return a descending list of tangent magnitudes to try (mm).
        This is used to recover from reachability errors (-1040/-1012) by trying a smaller tangent.
        """
        base = abs(float(base_mm or 0.0))
        if base <= 0:
            return [0.0]
        # Common-safe descending candidates (only keep <= base, always include base first)
        candidates = [base, 100.0, 80.0, 60.0, 50.0, 40.0, 30.0, 20.0, 10.0]
        out: List[float] = []
        for m in candidates:
            if m <= base and m not in out:
                out.append(m)
        return out

    def _move_safe(profile: int):
        """
        Return to a known safe 'tuck' posture while keeping J1 (vertical), J6 (rail), and gripper unchanged.
        Safe tuck angles (deg): J4=-188, J2=4, J3=179
        """
        def _is_error_resp(resp: Any) -> bool:
            if resp is None:
                return True
            s = str(resp).strip()
            if not s:
                return True
            return s.startswith("-")

        try:
            # Prefer the driver's `safe_tuck()` if available; it encapsulates the correct
            # sequencing and avoids format mismatches across SX vs SXL driver variants.
            if hasattr(robot_client.driver, "safe_tuck"):
                resp = robot_client.driver.safe_tuck(profile=int(profile))
            else:
                joints_raw = robot_client.driver.get_joint_states()
                # Some drivers (e.g., SXL diagnostics) may return a dict; for safe moves we need the raw list.
                if isinstance(joints_raw, dict):
                    # Parse from controller directly (status + joints)
                    raw = str(robot_client.driver.send_command("wherej") or "").split()[1:]
                    joints_raw = [float(x) for x in raw] if raw else []
                if not joints_raw or len(joints_raw) < 5:
                    raise HTTPException(status_code=500, detail="Failed to read current joints for safe move")
                target = list(joints_raw)
                # indices: 0=J1(mm), 1=J2(deg), 2=J3(deg), 3=J4(deg), 4=J5(mm), 5=J6(mm)
                target[3] = -188.0
                target[1] = 4.0
                target[2] = 179.0
                resp = robot_client.driver.move_joint(target, profile=int(profile))
            if _is_error_resp(resp):
                raise HTTPException(status_code=500, detail=f"Safe move failed: {resp}")
        except HTTPException:
            raise
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"Safe move error: {e}")

    import time
    # `pause_seconds` is intended as a settle delay for gripper operations (open/close),
    # not a general "dead stop" between motion segments.
    pause_gripper = max(0.0, float(req.pause_seconds or 0.0))
    # We keep motion pauses at 0 to allow chaining moves smoothly.
    pause_motion = 0.0
    # Backwards-compatible alias: most of the sequence uses `time.sleep(pause)` after moves.
    # Set that to motion pause so we don't introduce visible dead-stops between segments.
    pause = pause_motion
    steps: List[Dict[str, Any]] = []

    pick_tp = tps[req.pick_teachpoint_id]
    place_tp = tps[req.place_teachpoint_id]
    pick_z_above = max(0.0, _tp_z_above_mm(pick_tp))
    place_z_above = max(0.0, _tp_z_above_mm(place_tp))
    pick_tangent_mm = _tp_tangent_mm(pick_tp)
    place_tangent_mm = _tp_tangent_mm(place_tp)
    pick_cart = _tp_cartesian(pick_tp)
    place_cart = _tp_cartesian(place_tp)

    def _tp_joints(tp_id: str) -> List[float]:
        tp = tps[tp_id]
        joints = tp.get("joints") or None
        if not joints or len(joints) < 4:
            raise HTTPException(status_code=400, detail=f"Teachpoint '{tp_id}' has no joint data")
        return list(joints)

    def _joints_with_z_above(joints: List[float], z_above_mm: float) -> List[float]:
        j = list(joints)
        j[0] = float(j[0]) + float(z_above_mm)
        return j

    # Sequence (with pauses + observed gripper values for debugging/visibility)
    if pick_cart and abs(pick_tangent_mm) > 0 and pick_z_above > 0 and hasattr(robot_client.driver, "move_cartesian"):
        # Tangent approach in Cartesian: approach from global +/-Y at Z+z_above, keeping final yaw/pitch/roll.
        pick_j = _tp_joints(req.pick_teachpoint_id)
        j6_mm = pick_j[5] if len(pick_j) > 5 else None
        # NOTE: cartesian X tracks the rail (J6). So doing the tangent MoveC to (x_app, z+z_above)
        # already blends rail + arm extension. However, if we're coming from a very different rail/Z
        # neighborhood, the first tangent MoveC can be unreachable. In that case, we pre-position
        # J6 + J1 (Z-above) while tucked (safe) before attempting the tangent MoveC.
        steps.append({"step": "safe_tuck_before_pick"})
        _move_safe(req.speed_no_plate)
        time.sleep(pause_motion)

        # If pick teachpoint has an explicit obstacle-avoidance path, execute it now.
        # In that case we do NOT add any extra pre-positioning (like ensure_pick_rail_and_z),
        # because the path is intended to be authoritative.
        used_pick_path = _run_teachpoint_path(req.pick_teachpoint_id, req.speed_no_plate, steps, "pick")

        # Pre-position rail + Z-above while tucked to make the initial tangent MoveC reachable.
        j1_above_mm = float(pick_j[0]) + float(pick_z_above)
        if not used_pick_path:
            steps.append({"step": "ensure_pick_rail_and_z", "teachpoint_id": req.pick_teachpoint_id, "j6_mm": j6_mm, "j1_mm": j1_above_mm})
            _ensure_rail_and_z_mm(j6_mm, j1_above_mm, req.speed_no_plate)
            time.sleep(pause_motion)

        x = pick_cart["x"]
        y = pick_cart["y"]
        z = pick_cart["z"]
        yaw = pick_cart["yaw"]
        pitch = pick_cart["pitch"]
        roll = pick_cart["roll"]
        z_above = float(pick_z_above)
        # Tangent should always move the approach point toward the robot origin (0,0).
        # When X < 0 (behind the column), prefer an X-tangent; otherwise prefer a Y-tangent.
        mags = _tangent_magnitudes(pick_tangent_mm)

        # NOTE on "right-hand / left-hand" behavior:
        # PF controllers can choose different IK solution branches ("handedness") for the same pose.
        # The `config` value (returned by whereC) can *force* a branch when passed into MoveC.
        #
        # For tangent approach we command an *approach pose* (x, y_app, z+z_above) that differs from
        # the final pose (x, y, z+z_above). Forcing the final-pose config at the approach pose can
        # trigger long "wrap-around" motions (often perceived as an unexpected X sweep) on one side.
        #
        # So: do the first tangent-above MoveC WITHOUT a forced config, then capture whereC.config
        # at the achieved approach pose and reuse that config for the subsequent moves in this segment.
        tp_cfg = pick_cart.get("config") if isinstance(pick_cart, dict) else None
        used_cfg: Optional[int] = None
        used_x_app = x
        used_y_app = y
        used_tangent = 0.0
        used_axis = "y"
        used_extra_axis_mm: Optional[float] = None

        ok = False
        resp = "unknown"
        attempt_idx = 0
        for d in mags:
            if d <= 0:
                continue

            # Build per-magnitude candidates.
            # Prefer a Y-tangent (approach along Y) so we "extend toward the teachpoint" while the rail (X/J6)
            # blends naturally during the same MoveC. This avoids a late lateral X shift near the teachpoint.
            y_toward = (y - d) if y > 0 else (y + d)
            y_away = (y + d) if y > 0 else (y - d)
            cand_apps = [
                (x, y_toward, "y", +d, None),
                (x, y_away, "y", -d, None),
                # Fallback: X-tangent if Y-tangent is unreachable.
                (x + d, y, "x", +d, None),
                (x - d, y, "x", -d, None),
            ]

            for (x_app, y_app, axis, tan_mm, extra_axis_mm) in cand_apps[:4]:
                attempt_idx += 1
                used_x_app = float(x_app)
                used_y_app = float(y_app)
                used_axis = str(axis)
                used_tangent = float(tan_mm)
                used_extra_axis_mm = extra_axis_mm

                # Try tangent-above without forcing config first. If unreachable and we have a teachpoint
                # config saved, try again with that config as a fallback (away-side often needs this).
                cfg_candidates: List[Optional[int]] = [None]
                if tp_cfg is not None:
                    try:
                        cfg_candidates.append(int(tp_cfg))
                    except Exception:
                        pass

                for cfg_hint in cfg_candidates:
                    steps.append({
                        "step": "move_pick_tangent_above",
                        "teachpoint_id": req.pick_teachpoint_id,
                        "tangent_mm": used_tangent,
                        "tangent_axis": used_axis,
                        "attempt": attempt_idx,
                        "target": {"x": used_x_app, "y": used_y_app, "z": z + z_above, "yaw": yaw, "pitch": pitch, "roll": roll, "config": cfg_hint},
                        "extra_axis_mm": extra_axis_mm,
                        "teachpoint_config": tp_cfg,
                        "config_sent": cfg_hint,
                    })
                    if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                        ok, resp = robot_client.driver.move_cartesian_with_resp(
                            used_x_app, used_y_app, z + z_above, yaw, pitch, roll,
                            profile=req.speed_no_plate, config=cfg_hint, extra_axis_mm=extra_axis_mm,
                        )
                    else:
                        ok = robot_client.driver.move_cartesian(
                            used_x_app, used_y_app, z + z_above, yaw, pitch, roll,
                            profile=req.speed_no_plate, config=cfg_hint, extra_axis_mm=extra_axis_mm,
                        )
                        resp = "unknown"

                    if ok:
                        break

                if ok:
                    try:
                        if hasattr(robot_client.driver, "get_cartesian_position"):
                            pos = robot_client.driver.get_cartesian_position() or {}
                            if isinstance(pos, dict) and pos.get("config") is not None:
                                used_cfg = int(pos.get("config"))
                    except Exception:
                        used_cfg = None
                    steps.append({
                        "step": "pick_tangent_config_captured",
                        "teachpoint_id": req.pick_teachpoint_id,
                        "teachpoint_config": tp_cfg,
                        "captured_config": used_cfg,
                    })
                    break

                # Reachability errors: try opposite direction and then smaller magnitude.
                if str(resp) in ("-1040", "-1012"):
                    steps.append({"step": "pick_tangent_unreachable_trying_next", "resp": resp, "tangent_mm": used_tangent})
                    continue
                # Non-reachability failure: stop trying.
                break

            if ok:
                break

        # IMPORTANT: the IK `config` for the tangent-above pose is not guaranteed to be valid for the
        # subsequent "above" pose at the final X/Y. So we do NOT force config for move_pick_above.
        # Instead, we capture config at the "above" pose and use that for vertical descend/retract.
        cfg = None
        cfg_vertical: Optional[int] = None
        pick_used_fallback = False
        if not ok:
            steps.append({"step": "tangent_pick_failed_fallback_to_joints", "resp": resp})
            pick_above = _joints_with_z_above(pick_j, pick_z_above)
            steps.append({"step": "move_pick_above", "teachpoint_id": req.pick_teachpoint_id, "z_above_mm": pick_z_above, "fallback": True})
            _move_joints_raw(pick_above, req.speed_no_plate, gripper_mm=None)
            time.sleep(pause)

            steps.append({"step": "open_before_pick", "target_gripper_mm": open_mm, "gripper_mm_before": _get_gripper_mm(), "fallback": True})
            _set_grip(float(open_mm), req.speed_no_plate)
            steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(open_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
            steps[-1]["gripper_mm_after"] = _get_gripper_mm()

            steps.append({"step": "descend_to_pick", "teachpoint_id": req.pick_teachpoint_id, "fallback": True})
            _move_joints_raw(pick_j, req.speed_no_plate, gripper_mm=None)
            time.sleep(pause)

            steps.append({"step": "close_at_pick", "target_gripper_mm": closed_mm, "gripper_mm_before": _get_gripper_mm(), "fallback": True})
            _set_grip(float(closed_mm), req.speed_no_plate)
            steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(closed_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
            steps[-1]["gripper_mm_after"] = _get_gripper_mm()

            steps.append({"step": "retract_from_pick", "teachpoint_id": req.pick_teachpoint_id, "z_above_mm": pick_z_above, "fallback": True})
            _move_joints_raw(pick_above, req.speed_no_plate, gripper_mm=None)
            time.sleep(pause)
            pick_used_fallback = True
        else:
            time.sleep(pause)

        if not pick_used_fallback:
            steps.append({"step": "move_pick_above", "teachpoint_id": req.pick_teachpoint_id, "z_above_mm": z_above, "target": {"x": x, "y": y, "z": z + z_above, "yaw": yaw, "pitch": pitch, "roll": roll, "config": None}, "config_used": None})
            if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                ok, resp = robot_client.driver.move_cartesian_with_resp(x, y, z + z_above, yaw, pitch, roll, profile=req.speed_no_plate, config=None)
            else:
                ok = robot_client.driver.move_cartesian(x, y, z + z_above, yaw, pitch, roll, profile=req.speed_no_plate, config=None)
                resp = "unknown"
            if not ok:
                raise HTTPException(status_code=500, detail=f"Tangent approach MoveC failed (pick above): {resp}")
            time.sleep(pause)

            # Capture config at the "above" pose for stable vertical motion
            try:
                if hasattr(robot_client.driver, "get_cartesian_position"):
                    pos = robot_client.driver.get_cartesian_position() or {}
                    if isinstance(pos, dict) and pos.get("config") is not None:
                        cfg_vertical = int(pos.get("config"))
            except Exception:
                cfg_vertical = None
            steps.append({"step": "pick_vertical_config_captured", "teachpoint_id": req.pick_teachpoint_id, "captured_config": cfg_vertical})

            steps.append({"step": "open_before_pick", "target_gripper_mm": open_mm, "gripper_mm_before": _get_gripper_mm()})
            _set_grip(float(open_mm), req.speed_no_plate)
            steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(open_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
            steps[-1]["gripper_mm_after"] = _get_gripper_mm()

            steps.append({"step": "descend_to_pick", "teachpoint_id": req.pick_teachpoint_id, "target": {"x": x, "y": y, "z": z, "yaw": yaw, "pitch": pitch, "roll": roll, "config": cfg_vertical}, "config_used": cfg_vertical})
            if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                ok, resp = robot_client.driver.move_cartesian_with_resp(x, y, z, yaw, pitch, roll, profile=req.speed_no_plate, config=cfg_vertical)
            else:
                ok = robot_client.driver.move_cartesian(x, y, z, yaw, pitch, roll, profile=req.speed_no_plate, config=cfg_vertical)
                resp = "unknown"
            if not ok:
                raise HTTPException(status_code=500, detail=f"Tangent approach MoveC failed (descend to pick): {resp}")
            time.sleep(pause)

            steps.append({"step": "close_at_pick", "target_gripper_mm": closed_mm, "gripper_mm_before": _get_gripper_mm()})
            _set_grip(float(closed_mm), req.speed_no_plate)
            steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(closed_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
            steps[-1]["gripper_mm_after"] = _get_gripper_mm()

            steps.append({"step": "retract_from_pick", "teachpoint_id": req.pick_teachpoint_id, "z_above_mm": z_above, "config_used": cfg_vertical})
            steps[-1]["before"] = _snapshot_state()
            if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                ok, resp = robot_client.driver.move_cartesian_with_resp(x, y, z + z_above, yaw, pitch, roll, profile=req.speed_no_plate, config=cfg_vertical)
            else:
                ok = robot_client.driver.move_cartesian(x, y, z + z_above, yaw, pitch, roll, profile=req.speed_no_plate, config=cfg_vertical)
                resp = "unknown"
            if not ok:
                raise HTTPException(status_code=500, detail=f"Tangent approach MoveC failed (retract from pick): {resp}")
            time.sleep(pause)
            steps[-1]["after"] = _snapshot_state()

            # On some poses (notably away-side), the controller may use a different IK config for the
            # tangent-above pose vs the final X/Y/Z-above pose. Forcing the tangent config on the return
            # can cause a visible J4 "spin". So prefer returning using the current/vertical config.
            cfg_return = cfg_vertical if cfg_vertical is not None else used_cfg
            steps.append({"step": "retract_to_pick_tangent_above", "teachpoint_id": req.pick_teachpoint_id, "tangent_mm": used_tangent, "tangent_axis": used_axis, "config_used": cfg_return, "target": {"x": used_x_app, "y": used_y_app}, "extra_axis_mm": used_extra_axis_mm})
            steps[-1]["before"] = _snapshot_state()
            if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                ok, resp = robot_client.driver.move_cartesian_with_resp(
                    used_x_app, used_y_app, z + z_above, yaw, pitch, roll,
                    profile=req.speed_no_plate, config=cfg_return, extra_axis_mm=used_extra_axis_mm,
                )
            else:
                ok = robot_client.driver.move_cartesian(
                    used_x_app, used_y_app, z + z_above, yaw, pitch, roll,
                    profile=req.speed_no_plate, config=cfg_return, extra_axis_mm=used_extra_axis_mm,
                )
                resp = "unknown"
            if not ok:
                raise HTTPException(status_code=500, detail=f"Tangent approach MoveC failed (retract to pick tangent): {resp}")
            time.sleep(pause)
            steps[-1]["after"] = _snapshot_state()

        # If we had to fall back to joints for the pick, still try (best-effort) to retract back to the
        # tangent-above offset pose BEFORE tucking the wrist. This prevents an immediate J4 "safe tuck"
        # spin right after the pick.
        if pick_used_fallback:
            steps.append({
                "step": "retract_to_pick_tangent_above_after_fallback",
                "teachpoint_id": req.pick_teachpoint_id,
                "tangent_mm": used_tangent,
                "tangent_axis": used_axis,
                "target": {"x": used_x_app, "y": used_y_app, "z": z + z_above},
                "extra_axis_mm": used_extra_axis_mm,
                "config_used": cfg_vertical if cfg_vertical is not None else used_cfg,
            })
            steps[-1]["before"] = _snapshot_state()
            try:
                if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                    ok, resp = robot_client.driver.move_cartesian_with_resp(
                        used_x_app, used_y_app, z + z_above, yaw, pitch, roll,
                        profile=req.speed_no_plate, config=(cfg_vertical if cfg_vertical is not None else used_cfg), extra_axis_mm=used_extra_axis_mm,
                    )
                else:
                    ok = robot_client.driver.move_cartesian(
                        used_x_app, used_y_app, z + z_above, yaw, pitch, roll,
                        profile=req.speed_no_plate, config=(cfg_vertical if cfg_vertical is not None else used_cfg), extra_axis_mm=used_extra_axis_mm,
                    )
                    resp = "unknown"

                if not ok:
                    steps.append({"step": "retract_to_pick_tangent_above_after_fallback_failed", "resp": resp})
                else:
                    time.sleep(pause)
                steps[-1]["after"] = _snapshot_state()
            except Exception as e:
                steps.append({"step": "retract_to_pick_tangent_above_after_fallback_error", "error": str(e)})

        # Always tuck before any subsequent rail move (e.g. going to place)
        steps.append({"step": "safe_tuck_after_pick"})
        steps[-1]["before"] = _snapshot_state()
        _move_safe(req.speed_no_plate)
        time.sleep(pause)
        steps[-1]["after"] = _snapshot_state()
    elif pick_z_above > 0:
        pick_j = _tp_joints(req.pick_teachpoint_id)
        pick_above = _joints_with_z_above(pick_j, pick_z_above)

        steps.append({"step": "move_pick_above", "teachpoint_id": req.pick_teachpoint_id, "z_above_mm": pick_z_above})
        _move_joints_raw(pick_above, req.speed_no_plate, gripper_mm=None)
        time.sleep(pause)

        steps.append({"step": "open_before_pick", "target_gripper_mm": open_mm, "gripper_mm_before": _get_gripper_mm()})
        _set_grip(float(open_mm), req.speed_no_plate)
        steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(open_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
        steps[-1]["gripper_mm_after"] = _get_gripper_mm()

        steps.append({"step": "descend_to_pick", "teachpoint_id": req.pick_teachpoint_id})
        _move_joints_raw(pick_j, req.speed_no_plate, gripper_mm=None)
        time.sleep(pause)

        steps.append({"step": "close_at_pick", "target_gripper_mm": closed_mm, "gripper_mm_before": _get_gripper_mm()})
        _set_grip(float(closed_mm), req.speed_no_plate)
        steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(closed_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
        steps[-1]["gripper_mm_after"] = _get_gripper_mm()

        steps.append({"step": "retract_from_pick", "teachpoint_id": req.pick_teachpoint_id, "z_above_mm": pick_z_above})
        _move_joints_raw(pick_above, req.speed_no_plate, gripper_mm=None)
        time.sleep(pause)
    else:
        steps.append({"step": "open_before_pick", "target_gripper_mm": open_mm, "gripper_mm_before": _get_gripper_mm()})
        _set_grip(float(open_mm), req.speed_no_plate)
        steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(open_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
        steps[-1]["gripper_mm_after"] = _get_gripper_mm()

        steps.append({"step": "move_pick", "teachpoint_id": req.pick_teachpoint_id})
        _move_tp(req.pick_teachpoint_id, req.speed_no_plate)
        time.sleep(pause)

        steps.append({"step": "close_at_pick", "target_gripper_mm": closed_mm, "gripper_mm_before": _get_gripper_mm()})
        _set_grip(float(closed_mm), req.speed_no_plate)
        steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(closed_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
        steps[-1]["gripper_mm_after"] = _get_gripper_mm()

    # Place (vertical: approach above, descend, open, retract)
    if place_cart and abs(place_tangent_mm) > 0 and place_z_above > 0 and hasattr(robot_client.driver, "move_cartesian"):
        place_j = _tp_joints(req.place_teachpoint_id)
        j6_mm = place_j[5] if len(place_j) > 5 else None
        # Same rationale as pick: cartesian X already blends rail (J6). Avoid disjointed pre-positioning.
        steps.append({"step": "safe_tuck_before_place"})
        _move_safe(req.speed_holding_plate)
        time.sleep(pause)

        used_place_path = _run_teachpoint_path(req.place_teachpoint_id, req.speed_holding_plate, steps, "place")

        # IMPORTANT: for the place segment we're often coming from a very different rail position (J6/X).
        # Pre-positioning rail + Z-above while tucked makes the first tangent MoveC reachable, without
        # introducing any late lateral sweep near the teachpoint.
        j1_above_mm = float(place_j[0]) + float(place_z_above)
        if not used_place_path:
            steps.append({"step": "ensure_place_rail_and_z", "teachpoint_id": req.place_teachpoint_id, "j6_mm": j6_mm, "j1_mm": j1_above_mm})
            _ensure_rail_and_z_mm(j6_mm, j1_above_mm, req.speed_holding_plate)
            time.sleep(pause)

        x = place_cart["x"]
        y = place_cart["y"]
        z = place_cart["z"]
        yaw = place_cart["yaw"]
        pitch = place_cart["pitch"]
        roll = place_cart["roll"]
        z_above = float(place_z_above)
        mags = _tangent_magnitudes(place_tangent_mm)

        tp_cfg = place_cart.get("config") if isinstance(place_cart, dict) else None
        used_cfg: Optional[int] = None
        used_x_app = x
        used_y_app = y
        used_tangent = 0.0
        used_axis = "y"
        used_extra_axis_mm: Optional[float] = None

        ok = False
        resp = "unknown"
        attempt_idx = 0
        for d in mags:
            if d <= 0:
                continue
            y_toward = (y - d) if y > 0 else (y + d)
            y_away = (y + d) if y > 0 else (y - d)
            cand_apps = [
                (x, y_toward, "y", +d, None),
                (x, y_away, "y", -d, None),
                # Fallback: X-tangent if Y-tangent is unreachable.
                (x + d, y, "x", +d, None),
                (x - d, y, "x", -d, None),
            ]

            for (x_app, y_app, axis, tan_mm, extra_axis_mm) in cand_apps[:4]:
                attempt_idx += 1
                used_x_app = float(x_app)
                used_y_app = float(y_app)
                used_axis = str(axis)
                used_tangent = float(tan_mm)
                used_extra_axis_mm = extra_axis_mm

                # Try tangent-above without forcing config first. If unreachable and we have a teachpoint
                # config saved, try again with that config as a fallback.
                cfg_candidates: List[Optional[int]] = [None]
                if tp_cfg is not None:
                    try:
                        cfg_candidates.append(int(tp_cfg))
                    except Exception:
                        pass

                for cfg_hint in cfg_candidates:
                    steps.append({
                        "step": "move_place_tangent_above",
                        "teachpoint_id": req.place_teachpoint_id,
                        "tangent_mm": used_tangent,
                        "tangent_axis": used_axis,
                        "attempt": attempt_idx,
                        "target": {"x": used_x_app, "y": used_y_app, "z": z + z_above, "yaw": yaw, "pitch": pitch, "roll": roll, "config": cfg_hint},
                        "extra_axis_mm": extra_axis_mm,
                        "teachpoint_config": tp_cfg,
                        "config_sent": cfg_hint,
                    })
                    if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                        ok, resp = robot_client.driver.move_cartesian_with_resp(
                            used_x_app, used_y_app, z + z_above, yaw, pitch, roll,
                            profile=req.speed_holding_plate, config=cfg_hint, extra_axis_mm=extra_axis_mm,
                        )
                    else:
                        ok = robot_client.driver.move_cartesian(
                            used_x_app, used_y_app, z + z_above, yaw, pitch, roll,
                            profile=req.speed_holding_plate, config=cfg_hint, extra_axis_mm=extra_axis_mm,
                        )
                        resp = "unknown"

                    if ok:
                        break

                if ok:
                    try:
                        if hasattr(robot_client.driver, "get_cartesian_position"):
                            pos = robot_client.driver.get_cartesian_position() or {}
                            if isinstance(pos, dict) and pos.get("config") is not None:
                                used_cfg = int(pos.get("config"))
                    except Exception:
                        used_cfg = None
                    steps.append({
                        "step": "place_tangent_config_captured",
                        "teachpoint_id": req.place_teachpoint_id,
                        "teachpoint_config": tp_cfg,
                        "captured_config": used_cfg,
                    })
                    break

                if str(resp) in ("-1040", "-1012"):
                    steps.append({"step": "place_tangent_unreachable_trying_next", "resp": resp, "tangent_mm": used_tangent})
                    continue
                break

            if ok:
                break

        # Do not force config for the tangent-above pose, but we may need the teachpoint's config
        # to make the final X/Y pose reachable (away-side often needs this).
        cfg = None
        cfg_vertical: Optional[int] = tp_cfg if tp_cfg is not None else None
        place_used_fallback = False
        if not ok:
            steps.append({"step": "tangent_place_failed_fallback_to_joints", "resp": resp})
            place_above = _joints_with_z_above(place_j, place_z_above)
            steps.append({"step": "move_place_above", "teachpoint_id": req.place_teachpoint_id, "z_above_mm": place_z_above, "fallback": True})
            _move_joints_raw(place_above, req.speed_holding_plate, gripper_mm=None)
            time.sleep(pause)

            steps.append({"step": "descend_to_place", "teachpoint_id": req.place_teachpoint_id, "fallback": True})
            _move_joints_raw(place_j, req.speed_holding_plate, gripper_mm=None)
            time.sleep(pause)

            steps.append({"step": "open_at_place", "target_gripper_mm": open_mm, "gripper_mm_before": _get_gripper_mm(), "fallback": True})
            _set_grip(float(open_mm), req.speed_holding_plate)
            steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(open_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
            steps[-1]["gripper_mm_after"] = _get_gripper_mm()

            steps.append({"step": "retract_from_place", "teachpoint_id": req.place_teachpoint_id, "z_above_mm": place_z_above, "fallback": True})
            _move_joints_raw(place_above, req.speed_holding_plate, gripper_mm=None)
            time.sleep(pause)
            place_used_fallback = True
        else:
            time.sleep(pause)

        if not place_used_fallback:
            steps.append({"step": "move_place_above", "teachpoint_id": req.place_teachpoint_id, "z_above_mm": z_above, "target": {"x": x, "y": y, "z": z + z_above, "yaw": yaw, "pitch": pitch, "roll": roll, "config": cfg_vertical}, "config_used": cfg_vertical, "teachpoint_config": tp_cfg})
            if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                ok, resp = robot_client.driver.move_cartesian_with_resp(x, y, z + z_above, yaw, pitch, roll, profile=req.speed_holding_plate, config=cfg_vertical)
            else:
                ok = robot_client.driver.move_cartesian(x, y, z + z_above, yaw, pitch, roll, profile=req.speed_holding_plate, config=cfg_vertical)
                resp = "unknown"
            if not ok:
                raise HTTPException(status_code=500, detail=f"Tangent approach MoveC failed (place above): {resp}")
            time.sleep(pause)

            try:
                if hasattr(robot_client.driver, "get_cartesian_position"):
                    pos = robot_client.driver.get_cartesian_position() or {}
                    if isinstance(pos, dict) and pos.get("config") is not None:
                        # Only override with captured config if the teachpoint didn't specify one
                        if tp_cfg is None:
                            cfg_vertical = int(pos.get("config"))
            except Exception:
                pass
            steps.append({"step": "place_vertical_config_captured", "teachpoint_id": req.place_teachpoint_id, "teachpoint_config": tp_cfg, "captured_config": cfg_vertical})

        if not place_used_fallback:
            steps.append({"step": "descend_to_place", "teachpoint_id": req.place_teachpoint_id, "target": {"x": x, "y": y, "z": z, "yaw": yaw, "pitch": pitch, "roll": roll, "config": cfg_vertical}, "config_used": cfg_vertical})
            if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                ok, resp = robot_client.driver.move_cartesian_with_resp(x, y, z, yaw, pitch, roll, profile=req.speed_holding_plate, config=cfg_vertical)
            else:
                ok = robot_client.driver.move_cartesian(x, y, z, yaw, pitch, roll, profile=req.speed_holding_plate, config=cfg_vertical)
                resp = "unknown"
            if not ok:
                raise HTTPException(status_code=500, detail=f"Tangent approach MoveC failed (descend to place): {resp}")
            time.sleep(pause)

        if not place_used_fallback:
            steps.append({"step": "open_at_place", "target_gripper_mm": open_mm, "gripper_mm_before": _get_gripper_mm()})
            _set_grip(float(open_mm), req.speed_holding_plate)
            steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(open_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
            steps[-1]["gripper_mm_after"] = _get_gripper_mm()

        if not place_used_fallback:
            steps.append({"step": "retract_from_place", "teachpoint_id": req.place_teachpoint_id, "z_above_mm": z_above, "config_used": cfg_vertical})
            if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                ok, resp = robot_client.driver.move_cartesian_with_resp(x, y, z + z_above, yaw, pitch, roll, profile=req.speed_holding_plate, config=cfg_vertical)
            else:
                ok = robot_client.driver.move_cartesian(x, y, z + z_above, yaw, pitch, roll, profile=req.speed_holding_plate, config=cfg_vertical)
                resp = "unknown"
            if not ok:
                raise HTTPException(status_code=500, detail=f"Tangent approach MoveC failed (retract from place): {resp}")
            time.sleep(pause)

        if not place_used_fallback:
            # Prefer the segment/vertical config on return to avoid a J4 "spin" when tangent-config differs.
            cfg_return = cfg_vertical if cfg_vertical is not None else used_cfg
            steps.append({"step": "retract_to_place_tangent_above", "teachpoint_id": req.place_teachpoint_id, "tangent_mm": used_tangent, "tangent_axis": used_axis, "config_used": cfg_return, "target": {"x": used_x_app, "y": used_y_app}, "extra_axis_mm": used_extra_axis_mm})
            if hasattr(robot_client.driver, "move_cartesian_with_resp"):
                ok, resp = robot_client.driver.move_cartesian_with_resp(
                    used_x_app, used_y_app, z + z_above, yaw, pitch, roll,
                    profile=req.speed_holding_plate, config=cfg_return, extra_axis_mm=used_extra_axis_mm,
                )
            else:
                ok = robot_client.driver.move_cartesian(
                    used_x_app, used_y_app, z + z_above, yaw, pitch, roll,
                    profile=req.speed_holding_plate, config=cfg_return, extra_axis_mm=used_extra_axis_mm,
                )
                resp = "unknown"
            if not ok:
                raise HTTPException(status_code=500, detail=f"Tangent approach MoveC failed (retract to place tangent): {resp}")
            time.sleep(pause)
    elif place_z_above > 0:
        place_j = _tp_joints(req.place_teachpoint_id)
        place_above = _joints_with_z_above(place_j, place_z_above)

        steps.append({"step": "move_place_above", "teachpoint_id": req.place_teachpoint_id, "z_above_mm": place_z_above})
        _move_joints_raw(place_above, req.speed_holding_plate, gripper_mm=None)
        time.sleep(pause)

        steps.append({"step": "descend_to_place", "teachpoint_id": req.place_teachpoint_id})
        _move_joints_raw(place_j, req.speed_holding_plate, gripper_mm=None)
        time.sleep(pause)

        steps.append({"step": "open_at_place", "target_gripper_mm": open_mm, "gripper_mm_before": _get_gripper_mm()})
        _set_grip(float(open_mm), req.speed_holding_plate)
        steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(open_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
        steps[-1]["gripper_mm_after"] = _get_gripper_mm()

        steps.append({"step": "retract_from_place", "teachpoint_id": req.place_teachpoint_id, "z_above_mm": place_z_above})
        _move_joints_raw(place_above, req.speed_holding_plate, gripper_mm=None)
        time.sleep(pause)
    else:
        steps.append({"step": "move_place", "teachpoint_id": req.place_teachpoint_id})
        _move_tp(req.place_teachpoint_id, req.speed_holding_plate)
        time.sleep(pause)

        steps.append({"step": "open_at_place", "target_gripper_mm": open_mm, "gripper_mm_before": _get_gripper_mm()})
        _set_grip(float(open_mm), req.speed_holding_plate)
        steps[-1]["settle"] = _wait_for_gripper_settle(target_mm=float(open_mm), tol_mm=float(grip_tol_mm), timeout_s=max(0.8, float(pause_gripper or 0.0)))
        steps[-1]["gripper_mm_after"] = _get_gripper_mm()

    # Return to safe posture at end of sequence
    steps.append({"step": "return_safe"})
    _move_safe(req.speed_no_plate)
    time.sleep(pause)

    return {
        "status": "success",
        "labware_type_id": req.labware_type_id,
        "orientation": orient,
        "pick_z_above_mm": pick_z_above,
        "place_z_above_mm": place_z_above,
        "open_mm": open_mm,
        "closed_mm": closed_mm,
        "pick_teachpoint_id": req.pick_teachpoint_id,
        "place_teachpoint_id": req.place_teachpoint_id,
        "steps": steps,
    }


@app.post("/pf400/safe")
async def pf400_safe(req: PF400SafeRequest):
    """Move to safe tuck posture (J4=-188, J2=4, J3=179) while keeping J1/J6/gripper unchanged."""
    if not robot_client:
        raise HTTPException(status_code=503, detail="Robot client not initialized")
    if not hasattr(robot_client, "driver"):
        raise HTTPException(status_code=501, detail="Safe move not supported by current client")
    try:
        def _is_error_resp(resp: Any) -> bool:
            if resp is None:
                return True
            s = str(resp).strip()
            if not s:
                return True
            return s.startswith("-")

        joints_raw = robot_client.driver.get_joint_states()
        if not joints_raw or len(joints_raw) < 5:
            raise HTTPException(status_code=500, detail="Failed to read current joints")
        target = list(joints_raw)
        target[3] = -188.0
        target[1] = 4.0
        target[2] = 179.0
        if hasattr(robot_client.driver, "safe_tuck"):
            resp = robot_client.driver.safe_tuck(profile=int(req.speed_profile))
        else:
            resp = robot_client.driver.move_joint(target, profile=int(req.speed_profile))
        if _is_error_resp(resp):
            raise HTTPException(status_code=500, detail=f"Safe move failed: {resp}")
        return {"status": "success", "message": "Moved to safe tuck posture"}
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.patch("/labware/types/{labware_type_id}")
async def patch_labware_type(labware_type_id: str, req: LabwareTypeUpdateRequest):
    """Update labware type fields (used by modern Labware UI)."""
    try:
        updates: Dict[str, Any] = {}
        for k in ("name", "vendor", "catalog_number", "description", "base_class", "wells", "well_type", "notes"):
            v = getattr(req, k)
            if v is not None:
                updates[k] = v

        if req.labware_class_ids is not None:
            updates["labware_class_ids"] = list(req.labware_class_ids)

        if req.plate_dimensions_mm is not None:
            updates["plate_dimensions_mm"] = req.plate_dimensions_mm.model_dump()
        if req.well_dimensions_mm is not None:
            updates["well_dimensions_mm"] = req.well_dimensions_mm.model_dump()
        if req.model_3d is not None:
            updates["model_3d"] = req.model_3d.model_dump()
        if req.image_2d is not None:
            updates["image_2d"] = req.image_2d.model_dump()
        if req.pf400 is not None:
            updates["pf400"] = req.pf400.model_dump()
        if req.planar_motor is not None:
            updates["planar_motor"] = req.planar_motor.model_dump()
        if req.plate_properties is not None:
            updates["plate_properties"] = req.plate_properties.model_dump()

        updated = mongodb.update_labware_type(labware_type_id, updates)
        if not updated:
            raise HTTPException(status_code=404, detail="Labware type not found")
        return {"labware_type": updated}
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error updating labware type: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/labware/classes")
async def get_labware_classes():
    """Get all labware classes."""
    try:
        classes = mongodb.get_all_labware_classes()
        return {"labware_classes": classes}
    except Exception as e:
        print(f"Error getting labware classes: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/labware/classes")
async def create_labware_class(req: LabwareClassCreateRequest):
    """Create a labware class."""
    try:
        name = (req.name or "").strip()
        if not name:
            raise HTTPException(status_code=400, detail="name is required")
        created = mongodb.create_labware_class({
            "labware_class_id": _new_ulid_str(),
            "name": name,
            "description": (req.description or "").strip(),
        })
        if not created:
            raise HTTPException(status_code=500, detail="Failed to create labware class")
        return {"labware_class": created}
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error creating labware class: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.patch("/labware/classes/{labware_class_id}")
async def patch_labware_class(labware_class_id: str, req: LabwareClassUpdateRequest):
    """Update labware class fields (rename, description)."""
    try:
        updates: Dict[str, Any] = {}
        if req.name is not None:
            updates["name"] = (req.name or "").strip()
        if req.description is not None:
            updates["description"] = req.description or ""
        updated = mongodb.update_labware_class(labware_class_id, updates)
        if not updated:
            raise HTTPException(status_code=404, detail="Labware class not found")
        return {"labware_class": updated}
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error updating labware class: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.delete("/labware/classes/{labware_class_id}")
async def delete_labware_class(labware_class_id: str):
    """Delete a labware class."""
    try:
        ok = mongodb.delete_labware_class(labware_class_id)
        if not ok:
            raise HTTPException(status_code=404, detail="Labware class not found")
        return {"deleted": True, "labware_class_id": labware_class_id}
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error deleting labware class: {e}")
        raise HTTPException(status_code=500, detail=str(e))


# ============== Labware Assets (Image / 3D Models) ==============

def _labware_upload_root() -> str:
    # Save under the mounted labware directory so it is served by /models/labware/*
    return os.path.join(labware_models_dir, "uploads")


def _safe_ext(filename: str) -> str:
    base = os.path.basename(filename or "")
    _, ext = os.path.splitext(base)
    return (ext or "").lower()


@app.post("/labware/types/{labware_type_id}/assets/image")
async def upload_labware_image(labware_type_id: str, file: UploadFile = File(...)):
    """Upload a 2D image for a labware type (png/jpg/webp/gif)."""
    try:
        ext = _safe_ext(file.filename)
        if ext not in (".png", ".jpg", ".jpeg", ".webp", ".gif"):
            raise HTTPException(status_code=400, detail="Unsupported image type. Use png/jpg/webp/gif.")

        if not os.path.exists(labware_models_dir):
            raise HTTPException(status_code=500, detail="Labware models directory not mounted on backend")

        out_dir = os.path.join(_labware_upload_root(), labware_type_id)
        os.makedirs(out_dir, exist_ok=True)
        out_name = f"image{ext}"
        out_path = os.path.join(out_dir, out_name)
        with open(out_path, "wb") as f:
            shutil.copyfileobj(file.file, f)

        url = f"/models/labware/uploads/{labware_type_id}/{out_name}"
        updated = mongodb.update_labware_type(labware_type_id, {"image_2d": {"url": url, "content_type": file.content_type or "image/png"}})
        if not updated:
            raise HTTPException(status_code=404, detail="Labware type not found")
        return {"labware_type": updated}
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error uploading labware image: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/labware/types/{labware_type_id}/assets/model")
async def upload_labware_model(labware_type_id: str, file: UploadFile = File(...)):
    """Upload a 3D model for a labware type (stl/obj/glb/gltf)."""
    try:
        ext = _safe_ext(file.filename)
        if ext not in (".stl", ".obj", ".glb", ".gltf", ".grbl"):
            raise HTTPException(status_code=400, detail="Unsupported model type. Use stl/obj/glb/gltf.")

        if not os.path.exists(labware_models_dir):
            raise HTTPException(status_code=500, detail="Labware models directory not mounted on backend")

        out_dir = os.path.join(_labware_upload_root(), labware_type_id)
        os.makedirs(out_dir, exist_ok=True)
        out_name = f"model{ext}"
        out_path = os.path.join(out_dir, out_name)
        with open(out_path, "wb") as f:
            shutil.copyfileobj(file.file, f)

        fmt = ext.lstrip(".")
        url = f"/models/labware/uploads/{labware_type_id}/{out_name}"
        updated = mongodb.update_labware_type(labware_type_id, {"model_3d": {"url": url, "format": fmt}})
        if not updated:
            raise HTTPException(status_code=404, detail="Labware type not found")
        return {"labware_type": updated}
    except HTTPException:
        raise
    except Exception as e:
        print(f"Error uploading labware model: {e}")
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
