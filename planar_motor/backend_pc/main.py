"""
Planar Motor Controller Backend API for PC

FastAPI server for controlling Planar Motor systems.
Runs on PC at 192.168.0.23, connects to PMC at 192.168.10.100
"""

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
from typing import Dict, Any, Optional, List
import uvicorn
import os
import argparse

from planar_motor_driver import PlanarMotorDriver, PMCLIB_AVAILABLE

app = FastAPI(title="Planar Motor Control API (PC)")

# Note: Models are served from the Mac backend (PF400 backend on port 3061)
# Models stay on Mac at /Users/kelsorj/Tachyon/models/planar_motor/

# Allow CORS for frontend (running on Mac at 192.168.0.2)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify exact origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Global driver instance
pmc_driver: Optional[PlanarMotorDriver] = None

# Default PMC IP from environment or command line
PMC_IP = os.environ.get("PMC_IP", "192.168.10.100")

class JogRequest(BaseModel):
    xbot_id: int
    axis: str  # 'x' or 'y'
    distance: float  # in meters
    max_speed: float = 0.5
    max_acceleration: float = 5.0

class LinearMotionRequest(BaseModel):
    xbot_id: int
    x: float  # meters
    y: float  # meters
    final_speed: float = 0.0
    max_speed: float = 1.0
    max_acceleration: float = 10.0

@app.on_event("startup")
async def startup_event():
    """Initialize PMC driver on startup."""
    global pmc_driver
    try:
        pmc_driver = PlanarMotorDriver(pmc_ip=PMC_IP)
        if PMCLIB_AVAILABLE:
            print(f"Planar Motor driver initialized for PMC at {PMC_IP}")
        else:
            print(f"Planar Motor driver initialized (limited mode - pmclib not available)")
    except Exception as e:
        print(f"Failed to initialize Planar Motor driver: {e}")
        pmc_driver = None

@app.get("/")
async def root():
    """Root endpoint."""
    return {
        "message": "Planar Motor Control API (PC)",
        "pmc_ip": PMC_IP,
        "pmclib_available": PMCLIB_AVAILABLE
    }

@app.get("/status")
async def get_status():
    """Get connection status."""
    if not pmc_driver:
        return {"connected": False, "error": "Driver not initialized"}
    
    return {
        "connected": pmc_driver.connected,
        "has_mastership": pmc_driver.has_mastership,
        "pmc_ip": pmc_driver.pmc_ip
    }

@app.post("/connect")
async def connect():
    """Connect to PMC and gain mastership."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    if not PMCLIB_AVAILABLE:
        raise HTTPException(status_code=503, detail="pmclib not available - cannot connect to PMC")
    
    try:
        success = pmc_driver.connect()
        if success:
            return {"status": "connected", "xbot_count": len(pmc_driver.xbot_ids)}
        else:
            raise HTTPException(
                status_code=503, 
                detail=f"Failed to connect to PMC at {pmc_driver.pmc_ip}. Check network connectivity and ensure PMC is powered on."
            )
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(
            status_code=500, 
            detail=f"Error connecting to PMC: {str(e)}"
        )

@app.post("/disconnect")
async def disconnect():
    """Disconnect from PMC."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    pmc_driver.disconnect()
    return {"status": "disconnected"}

@app.get("/pmc/status")
async def get_pmc_status():
    """Get PMC status."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    status = pmc_driver.get_pmc_status()
    if status is None:
        raise HTTPException(status_code=500, detail="Failed to get PMC status")
    
    return {"status": status}

@app.get("/xbots")
async def get_xbots():
    """Get list of XBOT IDs."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    xbot_ids = pmc_driver.get_xbot_ids()
    return {"xbot_ids": xbot_ids, "count": len(xbot_ids)}

@app.get("/xbots/{xbot_id}/status")
async def get_xbot_status(xbot_id: int):
    """Get status of a specific XBOT."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    status = pmc_driver.get_xbot_status(xbot_id)
    if status is None:
        raise HTTPException(status_code=404, detail=f"XBOT {xbot_id} not found or error getting status")
    
    return status

@app.get("/xbots/status")
async def get_all_xbot_statuses():
    """Get status of all XBOTs."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    statuses = pmc_driver.get_all_xbot_statuses()
    return {"xbots": statuses}

@app.post("/xbots/activate")
async def activate_xbots():
    """Activate all XBOTs."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    success = pmc_driver.activate_xbots()
    if success:
        return {"status": "activated"}
    else:
        raise HTTPException(status_code=500, detail="Failed to activate XBOTs")

@app.post("/xbots/levitate")
async def levitate_xbots():
    """Levitate all XBOTs."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    success = pmc_driver.levitate_xbots(0)  # 0 = all XBOTs
    if success:
        return {"status": "levitating"}
    else:
        raise HTTPException(status_code=500, detail="Failed to levitate XBOTs")

@app.post("/xbots/{xbot_id}/levitate")
async def levitate_xbot(xbot_id: int):
    """Levitate a specific XBOT (or all if xbot_id=0)."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    success = pmc_driver.levitate_xbots(xbot_id)
    if success:
        return {"status": "levitating" if xbot_id > 0 else "levitating_all"}
    else:
        raise HTTPException(status_code=500, detail="Failed to levitate XBOT(s)")

@app.post("/xbots/{xbot_id}/land")
async def land_xbot(xbot_id: int):
    """Land a specific XBOT (or all if xbot_id=0)."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    success = pmc_driver.land_xbots(xbot_id)
    if success:
        return {"status": "landing" if xbot_id > 0 else "landing_all"}
    else:
        raise HTTPException(status_code=500, detail="Failed to land XBOT(s)")

@app.post("/xbots/{xbot_id}/stop-motion")
async def stop_xbot(xbot_id: int):
    """Stop motion of a specific XBOT (or all if xbot_id=0)."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    success = pmc_driver.stop_motion(xbot_id)
    if success:
        return {"status": "stopped" if xbot_id > 0 else "stopped_all"}
    else:
        raise HTTPException(status_code=500, detail="Failed to stop XBOT(s)")

@app.post("/xbots/jog")
async def jog_xbot(req: JogRequest):
    """Jog XBOT along an axis."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    if req.axis.lower() not in ['x', 'y']:
        raise HTTPException(status_code=400, detail="Axis must be 'x' or 'y'")
    
    try:
        success = pmc_driver.jog(
            xbot_id=req.xbot_id,
            axis=req.axis,
            distance=req.distance,
            max_speed=req.max_speed,
            max_acceleration=req.max_acceleration
        )
        
        if success:
            return {"status": "jogging", "axis": req.axis, "distance": req.distance}
        else:
            # Get more detailed error from driver logs
            raise HTTPException(
                status_code=500, 
                detail="Failed to jog XBOT. Check that XBOT is levitated and in IDLE state."
            )
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(
            status_code=500,
            detail=f"Error during jog: {str(e)}"
        )

@app.post("/xbots/linear-motion")
async def linear_motion(req: LinearMotionRequest):
    """Move XBOT in a straight line to target position."""
    if not pmc_driver:
        raise HTTPException(status_code=500, detail="Driver not initialized")
    
    success = pmc_driver.linear_motion(
        xbot_id=req.xbot_id,
        x=req.x,
        y=req.y,
        final_speed=req.final_speed,
        max_speed=req.max_speed,
        max_acceleration=req.max_acceleration
    )
    
    if success:
        return {"status": "moving", "target": {"x": req.x, "y": req.y}}
    else:
        raise HTTPException(status_code=500, detail="Failed to move XBOT")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=3062, help="Port to run server on (default: 3062)")
    parser.add_argument("--pmc-ip", type=str, default=PMC_IP, help=f"PMC IP address (default: {PMC_IP})")
    parser.add_argument("--host", type=str, default="0.0.0.0", help="Host to bind to (default: 0.0.0.0)")
    args = parser.parse_args()
    
    PMC_IP = args.pmc_ip
    print(f"Starting Planar Motor API server on {args.host}:{args.port}")
    print(f"PMC IP: {PMC_IP}")
    print(f"Frontend should connect to: http://192.168.0.23:{args.port}")
    uvicorn.run(app, host=args.host, port=args.port)

