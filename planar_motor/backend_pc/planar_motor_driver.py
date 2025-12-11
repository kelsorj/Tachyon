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
                # Use the raw bot call for better performance in a tight loop
                status = bot.get_xbot_status(xbot_id)
                if status.xbot_state in [pm.XBOTSTATE.XBOT_IDLE, pm.XBOTSTATE.XBOT_STOPPED]:
                    return True
            except Exception:
                pass
            time.sleep(poll_interval)
        return False   

    def move_to_xy(self, xbot_id: int, x: float, y: float,
                   max_speed: float = 1.0, max_acceleration: float = 10.0,
                   final_speed: float = 0.0,
                   path_type: Optional["pm.LINEARPATHTYPE"] = None,
                   wait: bool = True) -> bool:
        """
        [DROP-IN SUBSTITUTE] Move XBOT to absolute XY position using linear_motion_si.
        """
        print(f"Absolute Linear Motion: XBOT {xbot_id} to X={x:.4f}m, Y={y:.4f}m")
        
        return self._linear_motion_base(
            xbot_id=xbot_id,
            x=x,
            y=y,
            mode=pm.POSITIONMODE.ABSOLUTE,
            max_speed=max_speed,
            max_accel=max_acceleration,
            final_speed=final_speed,
            wait=wait
        )

    def linear_motion(self, xbot_id: int, x: float, y: float,
                      final_speed: float = 0.0, max_speed: float = 1.0,
                      max_acceleration: float = 10.0,
                      wait: bool = True) -> bool:
        """Compatibility wrapper: calls move_to_xy (now using linear_motion_si)."""
        return self.move_to_xy(
            xbot_id=xbot_id,
            x=x,
            y=y,
            max_speed=max_speed,
            max_acceleration=max_acceleration,
            final_speed=final_speed,
            wait=wait,
        )

    def move_to_position(self, xbot_id: int, x: float, y: float,
                         max_speed: float = 1.0,
                         max_acceleration: float = 10.0) -> bool:
        """Move XBOT to an absolute XY position, used for teachpoints."""
        return self.move_to_xy(
            xbot_id=xbot_id,
            x=x,
            y=y,
            max_speed=max_speed,
            max_acceleration=max_acceleration,
            wait=True,
        )

    def jog(self, xbot_id: int, axis: str, distance: float,
            max_speed: float = 0.5, max_acceleration: float = 5.0) -> bool:
        """
        [DROP-IN SUBSTITUTE] Jog XBOT along an axis using linear_motion_si (relative move).
        
        This method uses the preferred linear_motion_si function with POSITIONMODE.RELATIVE,
        which is simpler and more reliable than the single-axis motion command for X/Y.
        """
        if not self.connected:
            print("Jog failed: Not connected")
            return False
            
        if axis.lower() == 'x':
            dx = distance
            dy = 0.0
        elif axis.lower() == 'y':
            dx = 0.0
            dy = distance
        else:
            # For Z/Rx/Ry/Rz, you would need SixDofMotionSI or the separate single-axis call.
            print(f"Jog failed: Invalid axis '{axis}' for standard linear jog. Use 'x' or 'y'.")
            return False

        print(f"Relative Jog Motion: XBOT {xbot_id} by dX={dx:.4f}m, dY={dy:.4f}m")

        return self._linear_motion_base(
            xbot_id=xbot_id, 
            x=dx, 
            y=dy, 
            mode=pm.POSITIONMODE.RELATIVE, # Key change for jogging
            max_speed=max_speed, 
            max_accel=max_acceleration, 
            final_speed=0.0,
            wait=True
        )

    def _linear_motion_base(self, xbot_id: int, x: float, y: float, 
                            mode: "pm.POSITIONMODE",
                            max_speed: float, max_accel: float, 
                            final_speed: float = 0.0,
                            wait: bool = True) -> bool:
        """
        Internal method for linear motion using the 10-parameter LinearMotionSI 
        required by this version of pmclib.
        """
        if not self.connected: return False
        
        cmd_id = 100 # Arbitrary Command ID
        path_type = pm.LINEARPATHTYPE.DIRECT
        # Add the missing parameter with a default value of 0.0 (straight line corners)
        corner_radius = 0.0 
        
        # Always wait until the bot is idle/stopped before issuing a motion command
        if not self.wait_until_idle(xbot_id, timeout=10.0):
            print(f"Motion command failed: XBOT {xbot_id} is not idle.")
            return False

        try:
            with self.lock:
                print(f"  Sending linear_motion_si (Mode: {mode}) command...")
                
                # CORRECTED 10-PARAMETER CALL: added corner_radius
                result = bot.linear_motion_si(
                    cmd_id,
                    xbot_id,
                    mode,               # 3: POSITIONMODE.ABSOLUTE or POSITIONMODE.RELATIVE
                    path_type,          # 4: LINEARPATHTYPE.DIRECT
                    x,                  # 5: targetX (m) or deltaX (m)
                    y,                  # 6: targetY (m) or deltaY (m)
                    final_speed,        # 7: final speed (m/s)
                    max_speed,          # 8: max speed (m/s)
                    max_accel,          # 9: max acceleration (m/s^2)
                    corner_radius       # 10: REQUIRED positional argument
                )
                
            pmc_rtn = getattr(result, "PmcRtn", None)

            if pmc_rtn is not None and pmc_rtn != pm.PMCRTN.ALLOK:
                print(f"linear_motion_si failed with PMC rtn: {pmc_rtn}")
                return False

            if wait:
                if not self.wait_until_idle(xbot_id, timeout=30.0):
                    print("linear motion: timed out waiting for idle")
                    return False
                
                final_status = self.get_xbot_status(xbot_id)
                if final_status:
                    fpos = final_status.get("position", {})
                    print(f"  Final Position: X={fpos.get('x', 0):.4f}m, Y={fpos.get('y', 0):.4f}m")

            return True

        except Exception as e:
            import traceback
            print(f"Error in linear motion: {e}")
            print(traceback.format_exc())
            return False

