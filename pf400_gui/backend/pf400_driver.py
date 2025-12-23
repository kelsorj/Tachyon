"""
PF400 Driver - Based on working pf400_module-main implementation
Uses raw sockets (since telnetlib was removed in Python 3.13)
"""
import os
import socket
import time
import math
from threading import Lock
from typing import Optional, Dict, Any, Tuple


# Error codes from working module
ERROR_CODES = {
    "-1009": "*No robot attached*",
    "-1012": "*Joint out-of-range*",
    "-1039": "*Position too close*",
    "-1040": "*Position too far*",
    "-1042": "*Can't change robot config*",
    "-1046": "*Power not enabled*",
    "-1600": "*Power off requested*",
    "-2800": "*Warning Parameter Mismatch*",
    "-2801": "*Warning No Parameters*",
    "-2802": "*Warning Illegal move command*",
    "-2803": "*Warning Invalid joint angles*",
    "-2804": "*Warning: Invalid Cartesian coordinate values*",
    "-2805": "*Unknown command*",
    "-2806": "*Command Exception*",
}

OUTPUT_CODES = {
    "0": "Success",
    "0 7": "Power off - waiting for power request TRUE",
    "0 20": "Power on - ready to have GPL attach robot",
    "0 21": "21 GPL project attached to robot",
}


class TelnetLikeSocket:
    """Socket wrapper that mimics telnetlib behavior."""
    
    def __init__(self, host, port, timeout=5):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.settimeout(timeout)
        self.sock.connect((host, port))
        self.buffer = b""
    
    def write(self, data: bytes):
        """Send data to socket."""
        self.sock.sendall(data)
    
    def read_until(self, match: bytes, timeout=None) -> bytes:
        """Read until match is found (like telnetlib.read_until)."""
        if timeout:
            self.sock.settimeout(timeout)
        
        while match not in self.buffer:
            try:
                chunk = self.sock.recv(1024)
                if not chunk:
                    break
                self.buffer += chunk
            except socket.timeout:
                break
        
        if match in self.buffer:
            idx = self.buffer.index(match) + len(match)
            result = self.buffer[:idx]
            self.buffer = self.buffer[idx:]
            return result
        
        # Return what we have if match not found
        result = self.buffer
        self.buffer = b""
        return result
    
    def close(self):
        """Close socket."""
        try:
            self.sock.close()
        except:
            pass


class PF400Driver:
    """
    PF400 Driver using socket communication (matching pf400_module-main behavior).
    """
    
    def __init__(self, ip: str = None, port: int = 10100, status_port: int = 10000):
        # Get IP from environment or use default
        if ip is None:
            ip = os.environ.get("PF400_IP", "192.168.0.20")
        port = int(os.environ.get("PF400_ROBOT_PORT", port))
        
        self.ip = ip
        self.port = port
        self.status_port = status_port
        
        # Connections
        self.robot_connection = None
        self.status_connection = None
        self.connected = False
        
        # Locks for thread safety
        self.command_lock = Lock()
        self.status_lock = Lock()
        
        # Settings
        self.current_profile = 1
        self.movement_state = 0
        
    def connect(self, auto_initialize=True):
        """Connect to PF400 robot."""
        try:
            print(f"Connecting to PF400 at {self.ip}:{self.port}...")
            
            # Close existing connections
            self.disconnect()
            
            # Connect using telnet-like socket wrapper
            self.robot_connection = TelnetLikeSocket(self.ip, self.port, 5)
            self.status_connection = TelnetLikeSocket(self.ip, self.status_port, 5)
            
            self.connected = True
            print(f"✓ Connected to PF400 at {self.ip}:{self.port}")
            
            # Configure robot (like working module)
            if auto_initialize:
                self.configure_robot()
            
            return True
            
        except Exception as e:
            print(f"✗ Failed to connect to PF400: {e}")
            self.connected = False
            return False
    
    def configure_robot(self):
        """Configure robot by setting mode and selecting robot ID (like working module)."""
        try:
            self.send_command("mode 0")
            self.send_status_command("mode 0")
            self.send_command("selectRobot 1")
            self.send_status_command("selectRobot 1")
            
            # Set up motion profiles (like working module)
            # Profile format: Profile <id> speed speed2 accel decel accelramp decelramp inrange straight
            profile1 = "Profile 1 30 0 50 50 0.1 0.1 0 0"  # Slow profile
            profile2 = "Profile 2 120 0 100 100 0.1 0.1 0 0"  # Fast profile
            self.send_command(profile1)
            self.send_command(profile2)
            
            # Ensure robot is attached
            attach_resp = self.send_command("attach 1")
            print(f"attach 1: {attach_resp}")
            
            print("✓ Robot configured")
        except Exception as e:
            print(f"⚠ Configure robot error: {e}")
    
    def disconnect(self):
        """Disconnect from robot."""
        if self.robot_connection:
            try:
                self.robot_connection.close()
            except:
                pass
            self.robot_connection = None
        if self.status_connection:
            try:
                self.status_connection.close()
            except:
                pass
            self.status_connection = None
        self.connected = False
    
    def send_command(self, command: str) -> str:
        """
        Send command to robot and return response.
        Uses exact same logic as working module.
        """
        with self.command_lock:
            try:
                if not self.robot_connection:
                    self.connect(auto_initialize=False)
                    if not self.robot_connection:
                        raise ConnectionError("Not connected to robot")
                
                # Send command with newline (like working module: command + "\n")
                self.robot_connection.write((command + "\n").encode("ascii"))
                
                # Read response until \r\n (like working module)
                response = (
                    self.robot_connection.read_until(b"\r\n")
                    .decode("ascii")
                    .rstrip("\r\n")
                )
                
                # Check for errors (like working module)
                if response != "" and response in ERROR_CODES:
                    print(f"Error response: {ERROR_CODES[response]}")
                
                return response
                
            except Exception as e:
                print(f"Error sending command '{command}': {e}")
                self.disconnect()
                raise
    
    def send_status_command(self, command: str) -> str:
        """Send command to status port."""
        with self.status_lock:
            try:
                if not self.status_connection:
                    self.connect(auto_initialize=False)
                    if not self.status_connection:
                        raise ConnectionError("Not connected to robot")
                
                self.status_connection.write((command + "\n").encode("ascii"))
                response = (
                    self.status_connection.read_until(b"\r\n")
                    .decode("ascii")
                    .rstrip("\r\n")
                )
                return response
            except Exception as e:
                print(f"Error sending status command '{command}': {e}")
                raise

    # =========================
    # Parameter DB (PDB) helpers
    # =========================

    def pdb_get(self, data_id: int, unit: Optional[int] = None, subunit: int = 0, index: Optional[int] = None) -> float:
        """
        Read a numeric parameter database item using `pd`.
        Manual: `pd <DataID> [unit] [subunit] [array_index]`
        """
        parts = ["pd", str(int(data_id))]
        if unit is not None:
            parts.append(str(int(unit)))
            parts.append(str(int(subunit)))
            if index is not None:
                parts.append(str(int(index)))

        resp = self.send_status_command(" ".join(parts))
        # PC-mode responses: "0 <value>" or "-#### ..."
        if not resp:
            raise RuntimeError("Empty response from pd")
        if resp.strip().startswith("-"):
            raise RuntimeError(f"pd error: {resp}")

        tokens = resp.split()
        if not tokens:
            raise RuntimeError(f"pd parse error: {resp}")
        # If response is just "0", treat as no value
        if tokens[0] == "0" and len(tokens) < 2:
            raise RuntimeError(f"pd returned no value: {resp}")
        # If it begins with "0", value is token[1]
        if tokens[0] == "0":
            return float(tokens[1])
        # Otherwise assume first token is the value (some firmwares omit leading 0)
        return float(tokens[0])

    def pdb_set(self, data_id: int, value: float, unit: Optional[int] = None, subunit: int = 0, index: Optional[int] = None) -> str:
        """
        Write a numeric parameter database item using `pc`.
        Manual: `pc <DataID> <value>` (2 args) or
                `pc <DataID> <unit> <subunit> <array_index> <value>` (5 args)
        """
        if unit is None:
            cmd = f"pc {int(data_id)} {value}"
        else:
            if index is None:
                raise ValueError("pdb_set with unit requires index (array index)")
            cmd = f"pc {int(data_id)} {int(unit)} {int(subunit)} {int(index)} {value}"

        resp = self.send_status_command(cmd)
        if not resp:
            raise RuntimeError("Empty response from pc")
        if resp.strip().startswith("-"):
            raise RuntimeError(f"pc error: {resp}")
        return resp

    # =========================
    # Gripper squeeze per manual
    # =========================

    @staticmethod
    def estimate_gripper_forces_simple(rated_current_amps: float, rated_current_nominal_amps: float = 1.26) -> Dict[str, float]:
        """
        Manual (PF400 23N gripper):
          squeeze ≈ 7 N + (RatedCurrent/1.26A) * 18 N
          opening_force ≈ (RatedCurrent/1.26A) * 18 N - 9 N
        """
        rc = float(rated_current_amps)
        nom = float(rated_current_nominal_amps)
        if nom <= 0:
            nom = 1.26
        motor_n = (rc / nom) * 18.0
        squeeze_n = 7.0 + motor_n
        open_n = motor_n - 9.0
        return {"rated_current_amps": rc, "motor_force_n": motor_n, "squeeze_n": squeeze_n, "opening_force_n": open_n}

    def gripper_set_rated_current_simple(self, rated_current_amps: float, unit: int = 1, array_index: int = 5) -> Dict[str, Any]:
        """
        Manual: rated current is stored in the 5th field in PDB #10611.
        We implement via `pc 10611 <unit> 0 <array_index> <amps>`.

        NOTE: array index base (0 vs 1) depends on controller config; default is 5 per manual wording,
        but we expose it so you can adjust if needed.
        """
        rc = float(rated_current_amps)
        if rc <= 0:
            raise ValueError("rated_current_amps must be > 0")

        write_resp = self.pdb_set(10611, rc, unit=unit, subunit=0, index=int(array_index))
        forces = self.estimate_gripper_forces_simple(rc)
        return {"status": "success", "pdb": {"data_id": 10611, "unit": unit, "subunit": 0, "index": int(array_index), "value": rc, "write_resp": write_resp}, "estimate": forces}

    def gripper_set_torque_limits_asymmetric(self, tcnts_pos_10351: Optional[int] = None, tcnts_neg_10352: Optional[int] = None, unit: Optional[int] = None) -> Dict[str, Any]:
        """
        Manual: use 10351 (positive direction) and 10352 (negative direction) to limit PID torque (tcnts).
        If `unit` is provided we write to that unit; otherwise controller-global.
        """
        out: Dict[str, Any] = {"status": "success", "writes": []}
        if tcnts_pos_10351 is not None:
            resp = self.pdb_set(10351, float(int(tcnts_pos_10351)), unit=unit, subunit=0, index=0) if unit is not None else self.pdb_set(10351, float(int(tcnts_pos_10351)))
            out["writes"].append({"data_id": 10351, "value": int(tcnts_pos_10351), "resp": resp})
        if tcnts_neg_10352 is not None:
            resp = self.pdb_set(10352, float(int(tcnts_neg_10352)), unit=unit, subunit=0, index=0) if unit is not None else self.pdb_set(10352, float(int(tcnts_neg_10352)))
            out["writes"].append({"data_id": 10352, "value": int(tcnts_neg_10352), "resp": resp})
        if not out["writes"]:
            raise ValueError("Provide at least one of tcnts_pos_10351 or tcnts_neg_10352")
        return out

    def gripper_get_rated_current_simple(self, unit: int = 1, array_index: int = 5) -> Dict[str, Any]:
        """Read the rated current (simple squeeze method) from PDB #10611 'field' index."""
        val = self.pdb_get(10611, unit=unit, subunit=0, index=int(array_index))
        return {"status": "success", "pdb": {"data_id": 10611, "unit": unit, "subunit": 0, "index": int(array_index), "value": val}, "estimate": self.estimate_gripper_forces_simple(val)}
    
    def get_robot_movement_state(self) -> int:
        """Get robot movement state (like working module)."""
        try:
            response = self.send_status_command("state")
            self.movement_state = int(float(response.split(" ")[1]))
            return self.movement_state
        except:
            return 0
    
    def await_movement_completion(self) -> None:
        """Wait until robot has finished moving (like working module)."""
        while True:
            if self.get_robot_movement_state() <= 1:
                return
            time.sleep(0.1)
    
    def get_joint_states(self) -> list:
        """
        Get current joint states in robot units (like working module).
        Returns list: [j1_mm, j2_deg, j3_deg, j4_deg, j5_mm, j6_mm]
        """
        response = self.send_command("wherej")
        joints = response.split(" ")
        joints = joints[1:]  # Skip status code
        return [float(x) for x in joints]
    
    def get_gripper_length(self) -> float:
        """Get current gripper length in mm (like working module)."""
        try:
            response = self.send_command("wherej")
            joints = response.split(" ")[1:]  # Skip status code
            joint_angles = [float(x) for x in joints]
            return joint_angles[4] if len(joint_angles) > 4 else 0.0
        except Exception as e:
            print(f"get_gripper_length error: {e}")
            return 0.0

    def set_gripper_mm(self, gripper_mm: float, profile: int = 1) -> bool:
        """
        Set gripper opening to an absolute value in mm while keeping other joints unchanged.
        This uses a full movej so it works on SX (5 joints) and SXL (6 joints).
        """
        try:
            joints = self.get_joint_states()
            if len(joints) < 5:
                return False

            target = list(joints)
            target[4] = float(gripper_mm)
            resp = self.move_joint(target, profile=profile)
            return resp == "0" or (resp and not resp.strip().startswith("-") and resp not in ERROR_CODES)
        except Exception as e:
            print(f"set_gripper_mm error: {e}")
            return False

    def close_gripper_until_contact(
        self,
        target_closed_mm: float,
        profile: int = 1,
        step_mm: float = 1.0,
        min_motion_mm: float = 0.2,
        settle_seconds: float = 0.15,
        max_steps: int = 200,
    ) -> Dict[str, Any]:
        """
        Best-effort "force sensing" proxy using only position feedback:
        we close the gripper in small steps until the gripper stops moving (stall/contact).

        This does NOT measure force directly (no force/current telemetry is exposed in this driver),
        but it's often sufficient for "detect contact while closing".

        Returns:
          {
            "status": "success"|"failure",
            "contact_detected": bool,
            "start_gripper_mm": float,
            "final_gripper_mm": float,
            "target_closed_mm": float,
            "steps": int,
            "reason": str,
          }
        """
        start_mm = float(self.get_gripper_length())
        prev_mm = start_mm
        target_closed_mm = float(target_closed_mm)
        step_mm = abs(float(step_mm))
        min_motion_mm = abs(float(min_motion_mm))
        settle_seconds = max(0.0, float(settle_seconds))

        # If already at/under target, consider contact/closed.
        if prev_mm <= target_closed_mm:
            return {
                "status": "success",
                "contact_detected": True,
                "start_gripper_mm": start_mm,
                "final_gripper_mm": prev_mm,
                "target_closed_mm": target_closed_mm,
                "steps": 0,
                "reason": "already_at_or_below_target",
            }

        for i in range(max_steps):
            next_mm = max(target_closed_mm, prev_mm - step_mm)
            ok = self.set_gripper_mm(next_mm, profile=profile)
            if not ok:
                return {
                    "status": "failure",
                    "contact_detected": False,
                    "start_gripper_mm": start_mm,
                    "final_gripper_mm": float(self.get_gripper_length()),
                    "target_closed_mm": target_closed_mm,
                    "steps": i + 1,
                    "reason": "move_failed",
                }

            if settle_seconds:
                time.sleep(settle_seconds)

            cur_mm = float(self.get_gripper_length())
            moved = prev_mm - cur_mm

            # If we commanded close but barely moved, assume contact/stall.
            if moved < min_motion_mm and next_mm < prev_mm:
                return {
                    "status": "success",
                    "contact_detected": True,
                    "start_gripper_mm": start_mm,
                    "final_gripper_mm": cur_mm,
                    "target_closed_mm": target_closed_mm,
                    "steps": i + 1,
                    "reason": "stall_detected",
                }

            prev_mm = cur_mm
            if prev_mm <= target_closed_mm:
                return {
                    "status": "success",
                    "contact_detected": False,
                    "start_gripper_mm": start_mm,
                    "final_gripper_mm": prev_mm,
                    "target_closed_mm": target_closed_mm,
                    "steps": i + 1,
                    "reason": "reached_target_without_stall",
                }

        return {
            "status": "failure",
            "contact_detected": False,
            "start_gripper_mm": start_mm,
            "final_gripper_mm": float(self.get_gripper_length()),
            "target_closed_mm": target_closed_mm,
            "steps": max_steps,
            "reason": "max_steps_reached",
        }
    
    def get_joint_positions(self):
        """
        Get current joint positions converted to SI units for API compatibility.
        Returns dict with keys j1 (m), j2-j4 (rad), gripper (m).
        """
        try:
            joints = self.get_joint_states()
            if len(joints) >= 5:
                return {
                    "j1": joints[0] / 1000.0,  # mm -> m
                    "j2": math.radians(joints[1]),  # deg -> rad
                    "j3": math.radians(joints[2]),  # deg -> rad
                    "j4": math.radians(joints[3]),  # deg -> rad
                    "j5left": joints[4] / 2000.0,
                    "j5right": joints[4] / 2000.0,
                    "gripper": joints[4] / 1000.0,  # mm -> m
                }
            return {}
        except Exception as e:
            print(f"Error getting joint positions: {e}")
            return {}
    
    def get_cartesian_position(self):
        """Get current Cartesian position."""
        try:
            response = self.send_command("whereC")
            parts = response.split(" ")
            parts = parts[1:-1]  # Skip status and last element (like working module)
            coords = [float(x) for x in parts]
            if len(coords) >= 6:
                return {
                    "x": coords[0],
                    "y": coords[1],
                    "z": coords[2],
                    "yaw": coords[3],
                    "pitch": coords[4],
                    "roll": coords[5],
                }
            return {}
        except Exception as e:
            print(f"Error getting cartesian position: {e}")
            return {}
    
    def move_joint(self, target_joint_angles: list, profile: int = 1) -> str:
        """
        Move to joint angles.
        For SXL models with 6 joints, ALL 6 values must be sent!
        target_joint_angles: [j1_mm, j2_deg, j3_deg, j4_deg, j5_mm] or [j1, j2, j3, j4, j5, j6]
        """
        target_joint_angles = list(target_joint_angles)
        
        # Get current joint states in robot units to fill in missing values
        raw_response = self.send_command("wherej")
        raw_joints = raw_response.split(" ")[1:]  # Skip status code
        current_raw = [float(x) for x in raw_joints]
        num_robot_joints = len(current_raw)
        
        # For SXL models with 6 joints, all 6 values must be sent
        
        # Fill in missing joint values from current position
        while len(target_joint_angles) < num_robot_joints:
            idx = len(target_joint_angles)
            target_joint_angles.append(current_raw[idx])
        
        # Use current gripper if j5 is very small (likely placeholder)
        if len(target_joint_angles) > 4 and abs(target_joint_angles[4]) < 0.1:
            target_joint_angles[4] = current_raw[4] if len(current_raw) > 4 else 0.0
        
        # Build command with ALL joint values (crucial for SXL with 6 joints!)
        move_command = (
            "movej" + " " + str(profile) + " " + " ".join(map(str, target_joint_angles))
        )
        
        print(f"Sending: {move_command}")
        response = self.send_command(move_command)
        print(f"Response: {response}")
        
        # Wait for movement to complete (like working module)
        self.await_movement_completion()
        
        return response
    
    def move_to_joints(self, j1_m, j2_rad, j3_rad, j4_rad, gripper_m=None, j6_m=None, profile=1):
        """
        Move to joint coordinates (API compatible version).
        Converts SI units (m/rad) to robot units (mm/deg).
        For SXL models, j6_m specifies the rail position in meters.
        """
        try:
            # Convert to robot units
            j1_mm = j1_m * 1000.0
            j2_deg = math.degrees(j2_rad)
            j3_deg = math.degrees(j3_rad)
            j4_deg = math.degrees(j4_rad)
            
            # Get current gripper if not specified
            if gripper_m is not None:
                j5_mm = gripper_m * 1000.0
            else:
                j5_mm = self.get_gripper_length()
            
            # Build target - include J6 if specified (for SXL rail)
            target = [j1_mm, j2_deg, j3_deg, j4_deg, j5_mm]
            if j6_m is not None:
                j6_mm = j6_m * 1000.0
                target.append(j6_mm)
            
            response = self.move_joint(target, profile)
            
            # Check response - "0" means success
            if response == "0":
                return True
            
            # Check for error codes
            if response in ERROR_CODES:
                print(f"Move failed: {ERROR_CODES[response]}")
                return False
            
            # Check if response starts with negative number (error code)
            if response and response.strip().startswith("-"):
                error_code = response.strip().split()[0]
                print(f"Move failed with error code: {error_code}")
                return False
            
            # Any other non-error response, assume success
            return True
            
        except Exception as e:
            import traceback
            print(f"Error in move_to_joints: {e}")
            traceback.print_exc()
            return False
    
    def move_to_joints_raw(self, j1_mm, j2_deg, j3_deg, j4_deg, gripper_mm=None, j6_mm=None, profile=1):
        """
        Move to absolute joint coordinates in robot native units (mm/deg).
        This is a convenience method that converts to SI units and calls move_to_joints.
        """
        j1_m = j1_mm / 1000.0
        j2_rad = math.radians(j2_deg)
        j3_rad = math.radians(j3_deg)
        j4_rad = math.radians(j4_deg)
        gripper_m = gripper_mm / 1000.0 if gripper_mm is not None else None
        j6_m = j6_mm / 1000.0 if j6_mm is not None else None
        
        return self.move_to_joints(j1_m, j2_rad, j3_rad, j4_rad, gripper_m, j6_m, profile)
    
    def move_cartesian(self, x_mm, y_mm, z_mm, yaw_deg, pitch_deg, roll_deg, profile=1):
        """
        Move to Cartesian position (like working module).
        """
        try:
            target = [x_mm, y_mm, z_mm, yaw_deg, pitch_deg, roll_deg]
            move_command = (
                "MoveC" + " " + str(profile) + " " + " ".join(map(str, target))
            )
            
            print(f"Sending: {move_command}")
            response = self.send_command(move_command)
            print(f"Response: {response}")
            
            self.await_movement_completion()
            
            if response in ERROR_CODES:
                print(f"MoveC failed: {ERROR_CODES[response]}")
                return False
            return True
            
        except Exception as e:
            print(f"Error in move_cartesian: {e}")
            return False
    
    def move_in_one_axis(self, profile: int = 1, axis_x: int = 0, axis_y: int = 0, axis_z: int = 0) -> str:
        """
        Move end effector on single axis (like working module).
        """
        print(f"move_in_one_axis: axis_x={axis_x}, axis_y={axis_y}, axis_z={axis_z}")
        coords = self.get_cartesian_position()
        print(f"move_in_one_axis: coords={coords}")
        if not coords:
            return "Error: couldn't get position"
        
        coords["x"] += axis_x
        coords["y"] += axis_y
        coords["z"] += axis_z
        
        target = [coords["x"], coords["y"], coords["z"], coords["yaw"], coords["pitch"], coords["roll"]]
        move_command = "MoveC " + str(profile) + " " + " ".join(map(str, target))
        
        print(f"move_in_one_axis Sending: {move_command}")
        response = self.send_command(move_command)
        print(f"move_in_one_axis Response: {response}")
        self.await_movement_completion()
        return response
    
    def jog_cartesian(self, axis: str, distance: float):
        """
        Jog in Cartesian space.
        axis: 'z', 'yaw', 'gripper' use joint moves; 'x', 'y', 'r', 't' use Cartesian
        distance: meters for linear, radians for angular
        """
        # Get current state
        joints = self.get_joint_positions()
        if not joints:
            print("Failed to get joint positions")
            return False
        
        j1_m = joints.get("j1", 0)
        j2_rad = joints.get("j2", 0)
        j3_rad = joints.get("j3", 0)
        j4_rad = joints.get("j4", 0)
        grip_m = joints.get("gripper", 0)
        
        # Z axis - direct J1 control (vertical rail)
        if axis == 'z':
            print(f"Jog Z: {j1_m*1000:.1f}mm -> {(j1_m+distance)*1000:.1f}mm")
            return self.move_to_joints(j1_m + distance, j2_rad, j3_rad, j4_rad, gripper_m=grip_m, profile=self.current_profile)
        
        # Yaw - direct J4 control
        elif axis == 'yaw':
            print(f"Jog Yaw: {math.degrees(j4_rad):.1f}° -> {math.degrees(j4_rad+distance):.1f}°")
            return self.move_to_joints(j1_m, j2_rad, j3_rad, j4_rad + distance, gripper_m=grip_m, profile=self.current_profile)
        
        # Gripper
        elif axis == 'gripper':
            print(f"Jog Gripper: {grip_m*1000:.1f}mm -> {(grip_m+distance)*1000:.1f}mm")
            return self.move_to_joints(j1_m, j2_rad, j3_rad, j4_rad, gripper_m=grip_m + distance, profile=self.current_profile)
        
        # For X, Y, R, T - use Cartesian MoveC
        cart = self.get_cartesian_position()
        if not cart:
            print("Failed to get Cartesian position")
            return False
        
        x_mm = cart.get("x", 0)
        y_mm = cart.get("y", 0)
        z_mm = cart.get("z", 0)
        yaw_deg = cart.get("yaw", 0)
        pitch_deg = cart.get("pitch", 90)
        roll_deg = cart.get("roll", 0)
        
        dist_mm = distance * 1000.0
        
        if axis == 'x':
            print(f"Jog X: {x_mm:.1f} -> {x_mm + dist_mm:.1f} mm")
            return self.move_cartesian(x_mm + dist_mm, y_mm, z_mm, yaw_deg, pitch_deg, roll_deg, self.current_profile)
        
        elif axis == 'y':
            print(f"Jog Y: {y_mm:.1f} -> {y_mm + dist_mm:.1f} mm")
            return self.move_cartesian(x_mm, y_mm + dist_mm, z_mm, yaw_deg, pitch_deg, roll_deg, self.current_profile)
        
        elif axis == 'r':  # Radial
            angle = math.atan2(y_mm, x_mm)
            target_x = x_mm + dist_mm * math.cos(angle)
            target_y = y_mm + dist_mm * math.sin(angle)
            print(f"Jog Radial: ({x_mm:.1f}, {y_mm:.1f}) -> ({target_x:.1f}, {target_y:.1f})")
            return self.move_cartesian(target_x, target_y, z_mm, yaw_deg, pitch_deg, roll_deg, self.current_profile)
        
        elif axis == 't':  # Tangential
            angle = math.atan2(y_mm, x_mm)
            target_x = x_mm - dist_mm * math.sin(angle)
            target_y = y_mm + dist_mm * math.cos(angle)
            print(f"Jog Tangential: ({x_mm:.1f}, {y_mm:.1f}) -> ({target_x:.1f}, {target_y:.1f})")
            return self.move_cartesian(target_x, target_y, z_mm, yaw_deg, pitch_deg, roll_deg, self.current_profile)
        
        else:
            print(f"Unknown axis: {axis}")
            return False
    
    def jog_joint(self, joint_idx: int, distance_si: float):
        """
        Jog a specific joint.
        joint_idx: 1=J1(Z), 2=J2, 3=J3, 4=J4, 5=Gripper
        distance_si: meters or radians
        """
        joints = self.get_joint_positions()
        if not joints:
            return False
        
        key_map = {1: "j1", 2: "j2", 3: "j3", 4: "j4", 5: "gripper"}
        if joint_idx not in key_map:
            return False
        
        key = key_map[joint_idx]
        targets = {
            "j1": joints.get("j1", 0),
            "j2": joints.get("j2", 0),
            "j3": joints.get("j3", 0),
            "j4": joints.get("j4", 0),
            "gripper": joints.get("gripper", 0)
        }
        targets[key] += distance_si
        
        return self.move_to_joints(
            targets["j1"], targets["j2"], targets["j3"], targets["j4"],
            gripper_m=targets["gripper"], profile=self.current_profile
        )
    
    def set_profile(self, profile_id: int = 1):
        """Set motion profile."""
        self.current_profile = profile_id
        return True
    
    def get_status(self):
        return "CONNECTED" if self.connected else "DISCONNECTED"
    
    def initialize_robot(self):
        """Initialize robot for GPL mode (like working module)."""
        try:
            print("Initializing robot...")
            
            # Check home state first
            home_resp = self.send_status_command("pd 2800")
            print(f"Home state (pd 2800): {home_resp}")
            home_state = home_resp.split(" ")[1] if " " in home_resp else "0"
            
            # Power cycle (like working module)
            self.send_command("hp 0")
            time.sleep(1)
            self.send_command("hp 1 -1")
            time.sleep(2)
            self.send_command("attach 1")
            time.sleep(0.5)
            
            # Home if needed
            if home_state != "1":
                print("Robot needs homing, sending home command...")
                home_result = self.send_command("home")
                print(f"Home result: {home_result}")
                # Homing takes ~15 seconds according to working module
                time.sleep(15)
            
            # Check state
            resp = self.send_command("sysState")
            print(f"sysState: {resp}")
            
            print("✓ Robot initialized")
            return True
        except Exception as e:
            print(f"Error initializing: {e}")
            return False
    
    def enable_power(self):
        """Enable high power."""
        return self.send_command("hp 1 -1")
    
    def disable_power(self):
        """Disable high power."""
        return self.send_command("hp 0")
    
    def abort(self):
        """Abort current operation."""
        return self.send_command("Abort")
    
    def get_current_profile(self):
        """Get current profile."""
        return {"profile_id": self.current_profile, "settings": {}}
