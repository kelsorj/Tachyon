"""
Planar Motor Controller (PMC) Driver

Handles connection and control of Planar Motor systems via pmclib.
"""

import sys
import os
from typing import Optional, Dict, Any, List
from threading import Lock

# Try to import pmclib - it should be installed via pip
try:
    from pmclib import xbot_commands as bot
    from pmclib import system_commands as sys_cmd
    from pmclib import pmc_types as pm
    PMCLIB_AVAILABLE = True
except ImportError as e:
    print(f"Warning: pmclib not available: {e}")
    print("Install pmclib with: pip install pmclib-117.9.1-py3-none-any.whl")
    PMCLIB_AVAILABLE = False
    bot = None
    sys_cmd = None
    pm = None


class PlanarMotorDriver:
    """Driver for Planar Motor Controller (PMC) systems."""
    
    def __init__(self, pmc_ip: str = "192.168.10.100", pmc_port: Optional[int] = None):
        """
        Initialize Planar Motor driver.
        
        Args:
            pmc_ip: IP address of the PMC
            pmc_port: Port for PMC connection (optional)
        """
        if not PMCLIB_AVAILABLE:
            print("Warning: pmclib is not available. Driver will be in limited mode.")
        
        self.pmc_ip = pmc_ip
        self.pmc_port = pmc_port
        self.connected = False
        self.has_mastership = False
        self.xbot_ids = []
        self.lock = Lock()
        
    def connect(self) -> bool:
        """Connect to the PMC and gain mastership."""
        if not PMCLIB_AVAILABLE:
            print("Cannot connect: pmclib not available")
            return False
        
        try:
            with self.lock:
                print(f"Connecting to Planar Motor Controller at {self.pmc_ip}...")
                
                # Connect to PMC
                if self.pmc_ip:
                    is_connected = sys_cmd.connect_to_specific_pmc(self.pmc_ip)
                else:
                    is_connected = sys_cmd.auto_search_and_connect_to_pmc()
                
                if not is_connected:
                    print("Failed to connect to Planar Motor Controller")
                    self.connected = False
                    return False
                
                # Gain mastership
                print("Gaining mastership....")
                sys_cmd.gain_mastership()
                self.has_mastership = True
                
                # Get XBOT IDs
                xbot_ids_result = bot.get_xbot_ids()
                if xbot_ids_result.PmcRtn == pm.PMCRTN.ALLOK:
                    self.xbot_ids = list(xbot_ids_result.xbot_ids_array) if xbot_ids_result.xbot_count > 0 else []
                    print(f"Connected. Found {xbot_ids_result.xbot_count} XBOT(s)")
                else:
                    print(f"Failed to get xbot IDs. Error: {xbot_ids_result.PmcRtn}")
                    self.xbot_ids = []
                
                self.connected = True
                return True
                
        except Exception as e:
            import traceback
            error_details = traceback.format_exc()
            print(f"Error connecting to PMC: {e}")
            print(f"Traceback: {error_details}")
            self.connected = False
            return False
    
    def disconnect(self):
        """Disconnect from the PMC."""
        with self.lock:
            self.connected = False
            self.has_mastership = False
            self.xbot_ids = []
    
    def get_pmc_status(self) -> Optional[str]:
        """Get PMC status."""
        if not self.connected:
            return None
        
        try:
            with self.lock:
                status = sys_cmd.get_pmc_status()
                # Convert enum to string
                status_map = {
                    pm.PMCSTATUS.PMC_FULLCTRL: "FULLCTRL",
                    pm.PMCSTATUS.PMC_INTELLIGENTCTRL: "INTELLIGENTCTRL",
                    pm.PMCSTATUS.PMC_ACTIVATING: "ACTIVATING",
                    pm.PMCSTATUS.PMC_BOOTING: "BOOTING",
                    pm.PMCSTATUS.PMC_DEACTIVATING: "DEACTIVATING",
                    pm.PMCSTATUS.PMC_ERRORHANDLING: "ERRORHANDLING",
                    pm.PMCSTATUS.PMC_ERROR: "ERROR",
                    pm.PMCSTATUS.PMC_INACTIVE: "INACTIVE",
                }
                return status_map.get(status, "UNKNOWN")
        except Exception as e:
            print(f"Error getting PMC status: {e}")
            return None
    
    def get_xbot_ids(self) -> List[int]:
        """Get list of XBOT IDs."""
        if not self.connected:
            return []
        
        try:
            with self.lock:
                result = bot.get_xbot_ids()
                if result.PmcRtn == pm.PMCRTN.ALLOK:
                    return list(result.xbot_ids_array) if result.xbot_count > 0 else []
                return []
        except Exception as e:
            print(f"Error getting XBOT IDs: {e}")
            return []
    
    def get_xbot_status(self, xbot_id: int) -> Optional[Dict[str, Any]]:
        """Get status of a specific XBOT."""
        if not self.connected:
            return None
        
        try:
            with self.lock:
                status = bot.get_xbot_status(xbot_id)
                if status.PmcRtn != pm.PMCRTN.ALLOK:
                    return None
                
                # Convert state enum to string
                state_map = {
                    pm.XBOTSTATE.XBOT_IDLE: "IDLE",
                    pm.XBOTSTATE.XBOT_STOPPED: "STOPPED",
                    pm.XBOTSTATE.XBOT_LANDED: "LANDED",
                    pm.XBOTSTATE.XBOT_STOPPING: "STOPPING",
                    pm.XBOTSTATE.XBOT_DISCOVERING: "DISCOVERING",
                    pm.XBOTSTATE.XBOT_MOTION: "MOTION",
                    pm.XBOTSTATE.XBOT_WAIT: "WAIT",
                    pm.XBOTSTATE.XBOT_OBSTACLE_DETECTED: "OBSTACLE_DETECTED",
                    pm.XBOTSTATE.XBOT_HOLDPOSITION: "HOLDPOSITION",
                    pm.XBOTSTATE.XBOT_DISABLED: "DISABLED",
                }
                
                # Get position (feedback_position_si is a list: [x, y, z, rx, ry, rz])
                position = list(status.feedback_position_si) if hasattr(status, 'feedback_position_si') else [0, 0, 0, 0, 0, 0]
                
                return {
                    "xbot_id": xbot_id,
                    "state": state_map.get(status.xbot_state, "UNKNOWN"),
                    "position": {
                        "x": position[0] if len(position) > 0 else 0.0,  # meters
                        "y": position[1] if len(position) > 1 else 0.0,  # meters
                        "z": position[2] if len(position) > 2 else 0.0,  # meters
                        "rx": position[3] if len(position) > 3 else 0.0,  # radians
                        "ry": position[4] if len(position) > 4 else 0.0,  # radians
                        "rz": position[5] if len(position) > 5 else 0.0,  # radians
                    }
                }
        except Exception as e:
            print(f"Error getting XBOT status: {e}")
            return None
    
    def get_all_xbot_statuses(self) -> Dict[int, Dict[str, Any]]:
        """Get status of all XBOTs."""
        xbot_ids = self.get_xbot_ids()
        statuses = {}
        for xbot_id in xbot_ids:
            status = self.get_xbot_status(xbot_id)
            if status:
                statuses[xbot_id] = status
        return statuses
    
    def activate_xbots(self) -> bool:
        """Activate all XBOTs."""
        if not self.connected:
            return False
        
        try:
            with self.lock:
                bot.activate_xbots()
                return True
        except Exception as e:
            print(f"Error activating XBOTs: {e}")
            return False
    
    def levitate_xbots(self, xbot_id: int = 0) -> bool:
        """Levitate XBOT(s). xbot_id=0 means all XBOTs."""
        if not self.connected:
            return False
        
        try:
            with self.lock:
                bot.levitation_command(xbot_id, pm.LEVITATEOPTIONS.LEVITATE)
                return True
        except Exception as e:
            print(f"Error levitating XBOTs: {e}")
            return False
    
    def land_xbots(self, xbot_id: int = 0) -> bool:
        """Land XBOT(s). xbot_id=0 means all XBOTs."""
        if not self.connected:
            return False
        
        try:
            with self.lock:
                bot.levitation_command(xbot_id, pm.LEVITATEOPTIONS.LAND)
                return True
        except Exception as e:
            print(f"Error landing XBOTs: {e}")
            return False
    
    def stop_motion(self, xbot_id: int = 0) -> bool:
        """Stop motion of XBOT(s). xbot_id=0 means all XBOTs."""
        if not self.connected:
            return False
        
        try:
            with self.lock:
                bot.stop_motion(xbot_id)
                return True
        except Exception as e:
            print(f"Error stopping motion: {e}")
            return False
    
    def wait_until_idle(self, xbot_id: int, poll_interval: float = 0.1, timeout: float = 10.0) -> bool:
        """Wait until XBOT is idle or stopped."""
        import time
        start_time = time.time()
        while time.time() - start_time < timeout:
            try:
                status = bot.get_xbot_status(xbot_id)
                if status.xbot_state in [pm.XBOTSTATE.XBOT_IDLE, pm.XBOTSTATE.XBOT_STOPPED]:
                    return True
            except Exception:
                pass
            time.sleep(poll_interval)
        return False
    
    def linear_motion_direct(self, xbot_id: int, x: float, y: float, 
                            final_speed: float = 0.0, max_speed: float = 1.0, 
                            max_acceleration: float = 10.0, 
                            position_mode: str = "ABSOLUTE",
                            wait: bool = True) -> bool:
        """
        Move XBOT using linear_motion_si.
        
        From PMCLIB.XML (actual DLL signature - 9 parameters, NO cornerRadius):
        LinearMotionSI(UInt16 cmdLabel, Int32 xbotID, POSITIONMODE positionMode, 
                      LINEARPATHTYPE pathType, Double targetXmeters, Double targetYmeters, 
                      Double finalSpeedMetersPs, Double maxSpeedMetersPs, Double maxAccelerationMetersPs2)
        
        Args:
            xbot_id: XBOT ID
            x: Target X position in meters (or delta if RELATIVE)
            y: Target Y position in meters (or delta if RELATIVE)
            final_speed: Final speed in m/s (0 to stop)
            max_speed: Maximum speed in m/s
            max_acceleration: Maximum acceleration in m/s²
            position_mode: "ABSOLUTE" or "RELATIVE"
            wait: Wait for motion to complete
        """
        if not self.connected:
            return False
        
        try:
            pos_mode = pm.POSITIONMODE.RELATIVE if position_mode == "RELATIVE" else pm.POSITIONMODE.ABSOLUTE
            
            print(f"linear_motion_si ({position_mode}): xbot={xbot_id}")
            print(f"  Target: X={float(x):.4f}m, Y={float(y):.4f}m")
            print(f"  Speed: final={float(final_speed)}, max={float(max_speed)}, accel={float(max_acceleration)}")
            
            with self.lock:
                # Actual signature from PMCLIB.XML - 9 parameters (NO cornerRadius!)
                # LinearMotionSI(UInt16, Int32, POSITIONMODE, LINEARPATHTYPE, Double, Double, Double, Double, Double)
                result = bot.linear_motion_si(
                    int(100),                     # cmdLabel (UInt16)
                    int(xbot_id),                 # xbotID (Int32)
                    pos_mode,                     # positionMode (POSITIONMODE enum)
                    pm.LINEARPATHTYPE.DIRECT,     # pathType (LINEARPATHTYPE enum)
                    float(x),                     # targetXmeters (Double)
                    float(y),                     # targetYmeters (Double)
                    float(final_speed),           # finalSpeedMetersPs (Double)
                    float(max_speed),             # maxSpeedMetersPs (Double)
                    float(max_acceleration)       # maxAccelerationMetersPs2 (Double)
                    # NO cornerRadius parameter!
                )
                print(f"linear_motion_si result: {result}")
            
            if wait:
                success = self.wait_until_idle(xbot_id, timeout=15.0)
                if success:
                    print(f"linear_motion_si: complete")
                else:
                    print(f"linear_motion_si: timed out waiting for idle")
                return success
            return True
        except Exception as e:
            import traceback
            print(f"Error in linear_motion_si: {e}")
            print(traceback.format_exc())
            # Fall back to auto_driving_motion_si if linear fails
            print("Falling back to auto_driving_motion_si...")
            return self.auto_driving_motion(xbot_id, x, y, wait)
    
    def auto_driving_motion(self, xbot_id: int, x: float, y: float, wait: bool = True) -> bool:
        """
        Move XBOT using auto_driving_motion_si (recommended over deprecated async_motion_si).
        
        From PMCLIB.XML:
        AutoDrivingMotionSI(Int32 xbotCount, ASYNCOPTIONS mode, Int32[] xbotIDs, Double[] targetsXMeters, Double[] targetsYMeters)
        """
        if not self.connected:
            return False
        
        try:
            print(f"auto_driving_motion_si: xbot={xbot_id}, target=({x:.4f}, {y:.4f})")
            
            with self.lock:
                # Use auto_driving_motion_si (async_motion_si is deprecated)
                # Parameters: xbotCount, mode, xbotIDs[], targetsX[], targetsY[]
                import array
                xbot_ids = [int(xbot_id)]
                targets_x = [float(x)]
                targets_y = [float(y)]
                
                bot.auto_driving_motion_si(
                    int(1),                       # xbotCount
                    pm.ASYNCOPTIONS.MOVEALL,      # mode
                    xbot_ids,                     # xbotIDs array
                    targets_x,                    # targetsXMeters array
                    targets_y                     # targetsYMeters array
                )
            
            if wait:
                success = self.wait_until_idle(xbot_id, timeout=15.0)
                if success:
                    print(f"auto_driving_motion_si: complete")
                else:
                    print(f"auto_driving_motion_si: timed out")
                return success
            return True
        except Exception as e:
            import traceback
            print(f"Error in auto_driving_motion_si: {e}")
            print(traceback.format_exc())
            return False
    
    def linear_motion(self, xbot_id: int, x: float, y: float, 
                     final_speed: float = 0.0, max_speed: float = 1.0, 
                     max_acceleration: float = 10.0, wait: bool = True) -> bool:
        """
        Move XBOT to an absolute position. Tries linear_motion_si first, falls back to auto_driving.
        """
        return self.linear_motion_direct(xbot_id, x, y, final_speed, max_speed, 
                                        max_acceleration, "ABSOLUTE", wait)
    
    def move_to_position(self, xbot_id: int, x: float, y: float,
                        max_speed: float = 1.0, max_acceleration: float = 10.0) -> bool:
        """
        Move XBOT to an absolute XY position (for teachpoints).
        """
        return self.linear_motion(xbot_id, x, y, 0.0, max_speed, max_acceleration, wait=True)
    
    def jog(self, xbot_id: int, axis: str, distance: float, 
            max_speed: float = 0.5, max_acceleration: float = 5.0) -> bool:
        """Jog XBOT along an axis using RELATIVE positioning."""
        if not self.connected:
            print("Jog failed: Not connected")
            return False
        
        try:
            # Get current status
            status = self.get_xbot_status(xbot_id)
            if not status:
                print(f"Jog failed: Could not get status for XBOT {xbot_id}")
                return False
            
            state = status.get("state", "UNKNOWN")
            print(f"XBOT {xbot_id} state: {state}")
            print(f"XBOT {xbot_id} position: {status['position']}")
            
            # Wait for XBOT to be ready if it's in motion
            if state not in ["IDLE", "STOPPED"]:
                print(f"Jog: Waiting for XBOT to be ready...")
                if not self.wait_until_idle(xbot_id, timeout=5.0):
                    print(f"Jog failed: XBOT {xbot_id} not ready (state: {state})")
                    return False
            
            # Calculate delta for RELATIVE positioning
            if axis.lower() == 'x':
                delta_x = distance
                delta_y = 0.0
            elif axis.lower() == 'y':
                delta_x = 0.0
                delta_y = distance
            else:
                print(f"Jog failed: Invalid axis '{axis}'")
                return False
            
            print(f"=== JOG COMMAND (RELATIVE) ===")
            print(f"  Axis: {axis.upper()}")
            print(f"  Delta: X={delta_x:.4f}m, Y={delta_y:.4f}m")
            print(f"  Speed: max={max_speed}m/s, accel={max_acceleration}m/s²")
            print(f"==============================")
            
            # Execute motion using RELATIVE positioning
            result = self.linear_motion_direct(
                xbot_id, 
                delta_x, delta_y,  # These are deltas, not absolute positions
                final_speed=0.0,
                max_speed=max_speed,
                max_acceleration=max_acceleration,
                position_mode="RELATIVE",
                wait=True
            )
            
            # Check final position
            final_status = self.get_xbot_status(xbot_id)
            if final_status:
                final_x = final_status["position"]["x"]
                final_y = final_status["position"]["y"]
                print(f"Jog complete: Final position X={final_x:.4f}m, Y={final_y:.4f}m")
            
            return result
        except Exception as e:
            import traceback
            print(f"Error in jog: {e}")
            print(traceback.format_exc())
            return False

