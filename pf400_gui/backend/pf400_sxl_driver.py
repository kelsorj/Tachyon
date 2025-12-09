"""
PF400SXL Driver - Extended driver for PF400SXL with rail support (J6)

The PF400SXL has a 2m rail that uses joint 6 (J6) for positioning.
This driver extends the base PF400Driver to handle the 6th joint.
"""

import os
import math
from typing import Dict, Any, Optional
from pf400_driver import PF400Driver
from pf400_models import PF400Model, PF400ModelConfig, DiagnosticsInterface, get_model_config


class PF400SXLDriver(PF400Driver, DiagnosticsInterface):
    """
    Driver for PF400SXL robot with 2m rail support.
    
    Extends PF400Driver to add:
    - Joint 6 (J6) rail control
    - Rail-specific diagnostics
    - Model-aware joint handling
    """
    
    def __init__(self, ip: str = None, port: int = 10100):
        # Get IP from environment or use default
        if ip is None:
            ip = os.environ.get("PF400_IP", "192.168.0.20")
        # Get port from environment or use default
        port = int(os.environ.get("PF400_ROBOT_PORT", port))
        super().__init__(ip, port)
        self.model = PF400Model.SXL
        self.config = get_model_config(self.model)
        
        # Rail parameters
        self.rail_length_mm = self.config.rail_length_mm  # 2000mm = 2m
        self.rail_joint_index = self.config.rail_joint_index  # J6
        
        # Rail state
        self.rail_position_mm = 0.0  # Current rail position in mm
        self.rail_homed = False
    
    def get_joint_positions(self):
        """
        Get current joint positions including J6 (rail).
        Returns dict with keys j1, j2, j3, j4, j5left, j5right, j6 (rail).
        """
        try:
            # Get raw response from robot
            response = self.send_command("WhereJ")
            
            if not response or "error" in response.lower():
                print(f"Error getting joints: {response}")
                return {}
            
            parts = response.split()
            
            # For SXL, we expect 7 values: Status + 6 joints (J1-J6)
            if len(parts) >= 7:
                try:
                    # Parse all 6 joints + status
                    # Format: [Status, J1(Z), J2(Sh), J3(El), J4(Wr), J5(Gr), J6(Rail)]
                    j1_mm = float(parts[1])  # Vertical (mm)
                    j2_deg = float(parts[2])  # Shoulder (deg)
                    j3_deg = float(parts[3])  # Elbow (deg)
                    j4_deg = float(parts[4])  # Wrist (deg)
                    j5_val = float(parts[5])  # Gripper (mm or deg)
                    j6_mm = float(parts[6])  # Rail (mm)
                    
                    # Convert to SI units
                    import math
                    j1_m = j1_mm / 1000.0
                    j2_rad = math.radians(j2_deg)
                    j3_rad = math.radians(j3_deg)
                    j4_rad = math.radians(j4_deg)
                    j5_m = j5_val / 1000.0  # Assuming mm for gripper
                    j6_m = j6_mm / 1000.0
                    
                    # Update stored rail position
                    self.rail_position_mm = j6_mm
                    
                    return {
                        "j1": j1_m,
                        "j2": j2_rad,
                        "j3": j3_rad,
                        "j4": j4_rad,
                        "j5left": j5_m / 2,
                        "j5right": j5_m / 2,
                        "gripper": j5_m,
                        "j6": j6_m,
                        "rail": j6_m
                    }
                except (ValueError, IndexError) as e:
                    print(f"Error parsing joint values '{response}': {e}")
                    # Fall back to parent implementation
                    return self._get_base_joints_fallback(response)
            elif len(parts) >= 6:
                # Got 6 values (status + 5 joints) - no J6 in response
                # Use parent implementation and add stored rail position
                joints = self._get_base_joints_fallback(response)
                joints["j6"] = self.rail_position_mm / 1000.0
                joints["rail"] = self.rail_position_mm / 1000.0
                return joints
            else:
                print(f"Unexpected response format: {response}")
                return {}
                
        except Exception as e:
            print(f"Exception in get_joint_positions: {e}")
            # Fallback to parent + stored rail position
            joints = super().get_joint_positions()
            if joints:
                joints["j6"] = self.rail_position_mm / 1000.0
                joints["rail"] = self.rail_position_mm / 1000.0
            return joints
    
    def _get_base_joints_fallback(self, response: str):
        """Helper to parse base 5 joints (fallback method)"""
        import math
        parts = response.split()
        
        if len(parts) >= 6:
            try:
                j1_mm = float(parts[1])
                j2_deg = float(parts[2])
                j3_deg = float(parts[3])
                j4_deg = float(parts[4])
                j5_val = float(parts[5])
                
                return {
                    "j1": j1_mm / 1000.0,
                    "j2": math.radians(j2_deg),
                    "j3": math.radians(j3_deg),
                    "j4": math.radians(j4_deg),
                    "j5left": (j5_val / 1000.0) / 2,
                    "j5right": (j5_val / 1000.0) / 2,
                    "gripper": j5_val / 1000.0
                }
            except ValueError:
                return {}
        return {}
    
    def move_to_joints(self, j1_m, j2_rad, j3_rad, j4_rad, gripper_m=None, j6_m=None, profile=1):
        """
        Move to absolute joint coordinates including J6 (rail).
        
        Args:
            j1_m: J1 position in meters (vertical)
            j2_rad: J2 position in radians (shoulder)
            j3_rad: J3 position in radians (elbow)
            j4_rad: J4 position in radians (wrist)
            gripper_m: Gripper position in meters (optional)
            j6_m: J6 (rail) position in meters (optional, SXL only)
            profile: Motion profile ID
        """
        # First move base 5 joints
        success = super().move_to_joints(j1_m, j2_rad, j3_rad, j4_rad, gripper_m, profile)
        
        if not success:
            return False
        
        # Then move rail (J6) if specified
        # Use move_rail_raw directly with flag to prevent recursion
        if j6_m is not None:
            position_mm = j6_m * 1000.0
            # Clamp to rail limits
            rail_min_mm = -self.rail_length_mm / 2.0
            rail_max_mm = self.rail_length_mm / 2.0
            if position_mm < rail_min_mm:
                position_mm = rail_min_mm
            elif position_mm > rail_max_mm:
                position_mm = rail_max_mm
            # Pass _from_move_to_joints=True to prevent recursion
            return self.move_rail_raw(position_mm, profile, _from_move_to_joints=True)
        
        return True
    
    def move_to_joints_raw(self, j1_mm, j2_deg, j3_deg, j4_deg, gripper_mm=None, j6_mm=None, profile=1):
        """
        Move to absolute joint coordinates in robot native units (mm/deg).
        Includes J6 (rail) support.
        """
        # Move base 5 joints
        success = super().move_to_joints_raw(j1_mm, j2_deg, j3_deg, j4_deg, gripper_mm, profile)
        
        if not success:
            return False
        
        # Move rail if specified
        if j6_mm is not None:
            return self.move_rail_raw(j6_mm, profile)
        
        return True
    
    def move_rail(self, position_m: float, profile: int = 1):
        """
        Move rail (J6) to absolute position.
        
        Args:
            position_m: Rail position in meters (-rail_length_mm/2000 to +rail_length_mm/2000)
            profile: Motion profile ID
        """
        position_mm = position_m * 1000.0
        
        # Rail range is -1000mm to +1000mm (centered at 0, total 2000mm)
        # Clamp to rail limits
        rail_min_mm = -self.rail_length_mm / 2.0  # -1000mm
        rail_max_mm = self.rail_length_mm / 2.0    # +1000mm
        
        if position_mm < rail_min_mm:
            position_mm = rail_min_mm
        elif position_mm > rail_max_mm:
            position_mm = rail_max_mm
        
        # Get current joint positions
        current = self.get_joint_positions()
        if not current:
            return False
        
        # Use move_to_joints with current positions + new rail position
        # This avoids the circular dependency (move_rail -> move_rail_raw -> move_to_joints -> move_rail)
        # by directly calling move_to_joints with all positions
        return self.move_to_joints(
            current.get("j1", 0),
            current.get("j2", 0),
            current.get("j3", 0),
            current.get("j4", 0),
            current.get("gripper", 0),
            position_m,  # New rail position
            profile
        )
    
    def move_rail_raw(self, position_mm: float, profile: int = 1, _from_move_to_joints=False):
        """
        Move rail (J6) to absolute position in mm.
        
        Args:
            position_mm: Rail position in mm (-rail_length_mm/2 to +rail_length_mm/2)
            profile: Motion profile ID
            _from_move_to_joints: Internal flag to prevent recursion
        """
        # Rail range is -1000mm to +1000mm (centered at 0, total 2000mm)
        # Clamp to rail limits
        rail_min_mm = -self.rail_length_mm / 2.0  # -1000mm
        rail_max_mm = self.rail_length_mm / 2.0    # +1000mm
        
        if position_mm < rail_min_mm:
            position_mm = rail_min_mm
        elif position_mm > rail_max_mm:
            position_mm = rail_max_mm
        
        try:
            if not self.connected:
                import sys
                sys.stderr.write("move_rail_raw: Robot not connected\n")
                sys.stderr.flush()
                return False
                
            # Set profile first
            self.set_profile(profile)
            
            # If called from move_to_joints, we can't call move_to_joints again (would create loop)
            # Since MoveJ with 6 joints doesn't work, we need to accept that rail can't move independently
            if _from_move_to_joints:
                import sys
                sys.stderr.write(f"move_rail_raw: Called from move_to_joints, cannot move rail independently (MoveJ 6-joint not supported)\n")
                sys.stderr.write(f"move_rail_raw: Rail position updated in state only: {position_mm}mm\n")
                sys.stderr.flush()
                # Update stored position but return False since we can't actually move it
                self.rail_position_mm = position_mm
                return False
            
            # Not called from move_to_joints, so we can use move_to_joints
            # Get current positions and use move_to_joints with current positions + new rail
            current = self.get_joint_positions()
            if not current:
                import sys
                sys.stderr.write("move_rail_raw: Failed to get current joint positions\n")
                sys.stderr.flush()
                return False
            
            # Convert position_mm to meters
            j6_m = position_mm / 1000.0
            
            import sys
            sys.stderr.write(f"move_rail_raw: Moving rail to {position_mm}mm via move_to_joints with all joints\n")
            sys.stderr.flush()
            
            # Use move_to_joints with current positions + new rail position
            # Pass _from_move_to_joints=True to prevent recursion
            result = self.move_to_joints(
                current.get("j1", 0),
                current.get("j2", 0),
                current.get("j3", 0),
                current.get("j4", 0),
                current.get("gripper", 0),
                j6_m,
                profile
            )
            
            if result:
                self.rail_position_mm = position_mm
                sys.stderr.write(f"move_rail_raw: Success, rail moved to {position_mm}mm\n")
                sys.stderr.flush()
            else:
                sys.stderr.write(f"move_rail_raw: move_to_joints returned False\n")
                sys.stderr.flush()
            
            return result
                
        except Exception as e:
            print(f"Error moving rail: {e}")
            import traceback
            traceback.print_exc()
            return False
    
    def jog_rail(self, distance_m: float, profile: int = 1):
        """
        Jog rail (J6) by relative distance.
        
        Args:
            distance_m: Distance to move in meters (positive = one direction, negative = other)
            profile: Motion profile ID
        """
        import sys
        try:
            sys.stderr.write(f"jog_rail called: distance={distance_m}m, profile={profile}\n")
            sys.stderr.flush()
            
            if not self.connected:
                sys.stderr.write("jog_rail: Robot not connected\n")
                sys.stderr.flush()
                return False
                
            current = self.get_joint_positions()
            sys.stderr.write(f"jog_rail: get_joint_positions returned: {current}\n")
            sys.stderr.flush()
            
            if not current:
                sys.stderr.write("jog_rail: Failed to get current joint positions\n")
                sys.stderr.flush()
                return False
                
            current_rail_m = current.get("j6", 0) or current.get("rail", 0)
            if current_rail_m is None:
                current_rail_m = 0.0
                
            target_rail_m = current_rail_m + distance_m
            sys.stderr.write(f"jog_rail: current={current_rail_m}m, distance={distance_m}m, target={target_rail_m}m\n")
            sys.stderr.flush()
            
            result = self.move_rail(target_rail_m, profile)
            sys.stderr.write(f"jog_rail: move_rail returned: {result}\n")
            sys.stderr.flush()
            return result
        except Exception as e:
            import sys
            import traceback
            sys.stderr.write(f"Error in jog_rail: {e}\n")
            traceback.print_exc(file=sys.stderr)
            sys.stderr.flush()
            return False
    
    # Diagnostics Interface Implementation
    
    def get_diagnostics(self) -> Dict[str, Any]:
        """Get comprehensive diagnostics information"""
        diagnostics = {
            "model": self.model.value,
            "connected": self.connected,
            "rail_enabled": True,
            "rail_length_mm": self.rail_length_mm,
            "rail_position_mm": self.rail_position_mm,
            "rail_homed": self.rail_homed,
        }
        
        # Add system state
        diagnostics.update(self.get_system_state())
        
        # Add joint states
        diagnostics["joints"] = self.get_joint_states()
        
        return diagnostics
    
    def get_system_state(self) -> Dict[str, Any]:
        """Get current system state"""
        state = {}
        
        try:
            if self.connected:
                # Get system state from robot
                sys_state = self.send_command("sysState")
                state["sys_state"] = sys_state
                
                # Get power state
                # Note: May need to parse response or use different command
                state["power_state"] = "unknown"
                
                # Get attach state
                state["attach_state"] = "unknown"
        except Exception as e:
            state["error"] = str(e)
        
        state["connected"] = self.connected
        state["profile"] = self.current_profile
        
        return state
    
    def get_joint_states(self) -> Dict[str, Any]:
        """Get detailed joint state information"""
        joints = self.get_joint_positions()
        
        joint_states = {}
        for i, (key, value) in enumerate(joints.items(), 1):
            joint_states[f"j{i}"] = {
                "name": key,
                "position": value,
                "position_mm": value * 1000.0 if "j1" in key or "rail" in key or "gripper" in key else None,
                "position_deg": math.degrees(value) if "j2" in key or "j3" in key or "j4" in key else None,
            }
        
        # Add rail-specific info
        if "j6" in joints or "rail" in joints:
            rail_pos = joints.get("j6") or joints.get("rail", 0)
            joint_states["rail"] = {
                "name": "rail",
                "position": rail_pos,
                "position_mm": rail_pos * 1000.0,
                "position_percent": (rail_pos * 1000.0 / self.rail_length_mm) * 100.0,
                "limits": {
                    "min_mm": 0.0,
                    "max_mm": self.rail_length_mm,
                    "min_m": 0.0,
                    "max_m": self.rail_length_mm / 1000.0
                }
            }
        
        return joint_states
    
    def get_error_log(self) -> list:
        """Get error log"""
        # TODO: Implement error log retrieval from robot
        # This may require specific robot commands
        return []
    
    def clear_errors(self) -> bool:
        """Clear error log"""
        # TODO: Implement error clearing
        return True

