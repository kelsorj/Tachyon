import os
import socket
import time
import json
import threading
import math

class PF400Driver:
    # Motion profile configurations
    # Format: {speed%, accel%, decel%, inRange, straight}
    # Profile 1 = Slow/Safe, Profile 2 = Fast, Profile 3 = Medium
    MOTION_PROFILES = {
        1: {  # Slow/Safe profile for precise movements
            "speed": 20,      # 20% speed
            "accel": 30,      # 30% acceleration  
            "decel": 30,      # 30% deceleration
            "inRange": 0,     # Stop at target
            "straight": 0,    # Joint interpolation
        },
        2: {  # Fast profile for quick movements
            "speed": 80,      # 80% speed
            "accel": 80,      # 80% acceleration
            "decel": 80,      # 80% deceleration
            "inRange": 0,     # Stop at target
            "straight": 0,    # Joint interpolation
        },
        3: {  # Medium profile - balanced
            "speed": 50,      # 50% speed
            "accel": 50,      # 50% acceleration
            "decel": 50,      # 50% deceleration
            "inRange": 0,     # Stop at target
            "straight": 0,    # Joint interpolation
        },
    }
    
    def __init__(self, ip: str = None, port: int = 10100):
        # Get IP from environment or use default
        if ip is None:
            ip = os.environ.get("PF400_IP", "192.168.0.20")
        # Get port from environment or use default
        port = int(os.environ.get("PF400_ROBOT_PORT", port))
        self.ip = ip
        self.port = port
        self.socket = None
        self.connected = False
        self.lock = threading.Lock()
        
        # Default settings
        self.current_profile = 2  # Use fast profile by default
        
        # Kinematics parameters from PF400 specs (in mm)
        self.shoulder_length = 302  # upper arm (J2 to J3)
        self.elbow_length = 289     # forearm (J3 to J4)
        self.end_effector_length = 162  # wrist to TCP
        
    def connect(self, auto_initialize=True):
        """
        Connect to PF400 robot.
        
        Args:
            auto_initialize: If True, attempt to initialize robot if connection succeeds
                           but robot is unresponsive (e.g., stuck in GPL executing mode).
        """
        # Close existing connection if any
        if self.socket:
            try:
                self.socket.close()
            except:
                pass
            self.socket = None
            self.connected = False
        
        try:
            self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            # Set socket options to help with connection on macOS
            self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.socket.settimeout(5.0)  # Increased timeout
            print(f"Attempting to connect to PF400 at {self.ip}:{self.port}...")
            # Try connecting - on macOS, sometimes need to retry
            try:
                self.socket.connect((self.ip, self.port))
            except OSError as e:
                if e.errno == 65:  # No route to host
                    # Close and retry once
                    self.socket.close()
                    time.sleep(0.5)
                    self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                    self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
                    self.socket.settimeout(5.0)
                    self.socket.connect((self.ip, self.port))
                else:
                    raise
            self.connected = True
            print(f"✓ Connected to PF400 at {self.ip}:{self.port}")
            # Clear any welcome message
            try:
                self.socket.settimeout(0.5)
                self.socket.recv(1024)
            except socket.timeout:
                pass
            except:
                pass
            self.socket.settimeout(5.0)  # Reset timeout for commands
            
            # Test if robot is responsive by sending a simple command
            if auto_initialize:
                try:
                    print("Testing robot responsiveness...")
                    # Try a simple command with short timeout
                    old_timeout = self.socket.gettimeout()
                    self.socket.settimeout(2.0)
                    test_response = self.send_command("sysState")
                    self.socket.settimeout(old_timeout)
                    
                    if not test_response or "error" in test_response.lower():
                        print("⚠ Robot connected but not responding. Attempting initialization to reset...")
                        if self.initialize_robot():
                            print("✓ Robot initialized successfully")
                        else:
                            print("⚠ Initialization failed, but connection is established")
                    else:
                        print(f"✓ Robot is responsive: {test_response}")
                except (socket.timeout, ConnectionError, Exception) as e:
                    print(f"⚠ Robot connected but command failed: {e}")
                    print("Attempting initialization to reset robot from stuck state...")
                    try:
                        if self.initialize_robot():
                            print("✓ Robot initialized successfully after reset")
                        else:
                            print("⚠ Initialization failed, but connection is established")
                    except Exception as init_error:
                        print(f"⚠ Initialization error: {init_error}")
                        # Connection is still established, just robot might be unresponsive
                        pass
            
            return True
        except socket.timeout as e:
            print(f"✗ Connection timeout to PF400: {e}")
            self.connected = False
            if self.socket:
                try:
                    self.socket.close()
                except:
                    pass
                self.socket = None
            return False
        except Exception as e:
            print(f"✗ Failed to connect to PF400: {e} (type: {type(e).__name__})")
            self.connected = False
            if self.socket:
                try:
                    self.socket.close()
                except:
                    pass
                self.socket = None
            return False

    def disconnect(self):
        if self.socket:
            self.socket.close()
            self.socket = None
        self.connected = False

    def send_command(self, command: str) -> str:
        with self.lock:
            if not self.connected:
                if not self.connect():
                    raise ConnectionError("Not connected to robot")
            
            try:
                # Append CR/LF if not present
                if not command.endswith("\r\n"):
                    command += "\r\n"
                
                # Clear buffer before sending
                try:
                    self.socket.settimeout(0.1)
                    while self.socket.recv(1024): pass
                except:
                    pass
                self.socket.settimeout(5.0) # Longer timeout for commands
                
                self.socket.sendall(command.encode('ascii'))
                
                # Receive response
                data = self.socket.recv(4096).decode('ascii')
                return data.strip()
            except Exception as e:
                print(f"Error sending command {command.strip()}: {e}")
                self.disconnect()
                raise

    def get_joint_positions(self):
        """
        Get current joint positions from the robot.
        Returns a dict with keys j1, j2, j3, j4, j5left, j5right.
        """
        try:
            # "WhereJ" usually returns joint angles in degrees or mm
            response = self.send_command("WhereJ")
            
            # If response contains error or is empty
            if not response or "error" in response.lower():
                print(f"Error getting joints: {response}")
                return {}

            parts = response.split()
            # Expecting 6 values (Status/Ext + 5 Joints) or 5 values
            # Raw: "0 178.878 -61.337 154.764 -93.429 82.731"
            # Map: [Status, J1(Z), J2(Sh), J3(El), J4(Wr), J5(Gr)]
            
            if len(parts) >= 6:
                try:
                    # Parse values
                    # parts[0] is usually 0 (status/unused)
                    j1_mm = float(parts[1]) # Vertical (mm)
                    j2_deg = float(parts[2]) # Shoulder (deg)
                    j3_deg = float(parts[3]) # Elbow (deg)
                    j4_deg = float(parts[4]) # Wrist (deg)
                    j5_val = float(parts[5]) # Gripper (mm or deg)

                    # Conversions
                    j1_m = j1_mm / 1000.0
                    j2_rad = math.radians(j2_deg)
                    j3_rad = math.radians(j3_deg)
                    j4_rad = math.radians(j4_deg)
                    j5_m = j5_val / 1000.0 # Assuming mm for gripper

                    return {
                        "j1": j1_m,
                        "j2": j2_rad,
                        "j3": j3_rad,
                        "j4": j4_rad,
                        "j5left": j5_m / 2, # Assuming centered
                        "j5right": j5_m / 2,
                        "gripper": j5_m
                    }
                except ValueError as e:
                    print(f"Error parsing joint values '{response}': {e}")
                    return {}
            elif len(parts) >= 4:
                # Fallback
                try:
                    j1_mm = float(parts[0])
                    j2_deg = float(parts[1])
                    j3_deg = float(parts[2])
                    j4_deg = float(parts[3])
                    
                    j1_m = j1_mm / 1000.0
                    j2_rad = math.radians(j2_deg)
                    j3_rad = math.radians(j3_deg)
                    j4_rad = math.radians(j4_deg)
                    
                    return {
                        "j1": j1_m,
                        "j2": j2_rad,
                        "j3": j3_rad,
                        "j4": j4_rad,
                        "gripper": 0.0
                    }
                except ValueError:
                    return {}
            else:
                print(f"Unexpected response format: {response}")
                return {}

        except Exception as e:
            print(f"Exception in get_joint_positions: {e}")
            return {}

    def get_cartesian_position(self):
        """
        Get current Cartesian position using "whereC" (like AD-SDL driver).
        Returns dict with X, Y, Z, Yaw, Pitch, Roll.
        """
        try:
            response = self.send_command("whereC")
            if not response or "error" in response.lower():
                print(f"whereC error: {response}")
                return {}
            
            parts = response.split()
            # Response format: "0 X Y Z Yaw Pitch Roll" (status + 6 values)
            # AD-SDL strips the first element (status) and last element
            
            if len(parts) >= 7:
                # Skip status code (parts[0]), take next 6
                return {
                    "x": float(parts[1]),
                    "y": float(parts[2]),
                    "z": float(parts[3]),
                    "yaw": float(parts[4]),
                    "pitch": float(parts[5]),
                    "roll": float(parts[6])
                }
            elif len(parts) >= 6:
                # No status code
                return {
                    "x": float(parts[0]),
                    "y": float(parts[1]),
                    "z": float(parts[2]),
                    "yaw": float(parts[3]),
                    "pitch": float(parts[4]),
                    "roll": float(parts[5])
                }
                
            print(f"whereC unexpected format: {response}")
            return {}
        except Exception as e:
            print(f"Error getting cartesian: {e}")
            return {}

    def configure_profiles(self):
        """
        Configure motion profiles on the robot with speed/accel/decel values.
        
        WARNING: The Profile command format is unknown and may corrupt robot settings.
        The robot has pre-configured profiles (1, 2, etc.) that should be used instead
        via SelectProfile command. Only call this if you know the correct format.
        """
        print("WARNING: configure_profiles() disabled - use SelectProfile instead")
        return False
        # Original code disabled - format unknown:
        # for profile_id, params in self.MOTION_PROFILES.items():
        #     cmd = f"Profile {profile_id} {params['speed']} {params['accel']} {params['decel']} {params['inRange']} {params['straight']}"
        #     self.send_command(cmd)
    
    def set_profile(self, profile_id: int = 2):
        """Select a motion profile (1-based index) on the robot."""
        try:
            # Valid profiles usually 1-4 or similar
            if not (1 <= profile_id <= 10):
                print(f"Invalid profile id {profile_id}")
                return False
            
            # Send SelectProfile command to the robot to use the pre-configured profile
            cmd = f"SelectProfile {profile_id}"
            response = self.send_command(cmd)
            if response and "error" not in response.lower():
                self.current_profile = profile_id
                return True
            print(f"SelectProfile failed: {response}")
            return False
        except Exception as e:
            print(f"Error setting profile: {e}")
            return False

    def move_to_joints(self, j1_m, j2_rad, j3_rad, j4_rad, gripper_m=None, profile=1):
        """
        Move to absolute joint coordinates.
        Converts SI units (m/rad) back to robot units (mm/deg).
        """
        try:
            # Set profile first
            self.set_profile(profile)
            
            # Convert to robot units
            j1_mm = j1_m * 1000.0
            j2_deg = math.degrees(j2_rad)
            j3_deg = math.degrees(j3_rad)
            j4_deg = math.degrees(j4_rad)
            
            j5_val = 0.0
            if gripper_m is not None:
                j5_val = gripper_m * 1000.0
                
            cmd = f"MoveJ {profile} {j1_mm:.3f} {j2_deg:.3f} {j3_deg:.3f} {j4_deg:.3f} {j5_val:.3f}"
            print(f"Sending move: {cmd}")
            
            response = self.send_command(cmd)
            print(f"MoveJ response: '{response}'")
            if response and not "error" in response.lower():
                return True
            print(f"Move failed: {response}")
            return False
        except Exception as e:
            print(f"Error moving: {e}")
            return False

    def move_to_joints_raw(self, j1_mm, j2_deg, j3_deg, j4_deg, gripper_mm=None, profile=1):
        """
        Move to absolute joint coordinates in robot native units (mm/deg).
        No unit conversion - values passed directly to robot.
        """
        try:
            # Ensure robot is connected and initialized
            if not self.connected:
                if not self.connect(auto_initialize=True):
                    print("Cannot move: robot not connected")
                    return False
            
            # Set profile first
            self.set_profile(profile)
            
            j5_val = gripper_mm if gripper_mm is not None else 0.0
                
            cmd = f"MoveJ {profile} {j1_mm:.3f} {j2_deg:.3f} {j3_deg:.3f} {j4_deg:.3f} {j5_val:.3f}"
            print(f"Sending move (raw): {cmd}")
            
            response = self.send_command(cmd)
            print(f"MoveJ response: '{response}'")
            
            # Check for success (0) or error codes (negative numbers)
            try:
                response_code = int(response.strip())
                if response_code == 0:
                    return True
                else:
                    # Error code - try to get more info
                    print(f"MoveJ error code: {response_code}")
                    # Common error codes:
                    # -1009 might be "not initialized" or "safety error"
                    # Try initializing if we get this error
                    if response_code == -1009:
                        print("Error -1009: Robot may need initialization. Attempting to initialize...")
                        if self.initialize_robot():
                            print("Re-initialized. Retrying move...")
                            # Retry the move after initialization
                            response = self.send_command(cmd)
                            response_code = int(response.strip())
                            if response_code == 0:
                                return True
                    print(f"Move failed with error code: {response_code}")
                    return False
            except ValueError:
                # Response is not a number - check for "error" string
                if response and "error" not in response.lower() and response.strip() != "":
                    # Might be a different success indicator
                    return True
                print(f"Move failed: {response}")
                return False
        except Exception as e:
            print(f"Error moving: {e}")
            return False

    def move_cartesian(self, x_mm, y_mm, z_mm, yaw_deg, pitch_deg, roll_deg, profile=1):
        """
        Move to absolute Cartesian position using robot's MoveC command.
        All units in mm and degrees (robot native units).
        """
        try:
            cmd = f"MoveC {profile} {x_mm:.3f} {y_mm:.3f} {z_mm:.3f} {yaw_deg:.3f} {pitch_deg:.3f} {roll_deg:.3f}"
            print(f"MoveC command: {cmd}")
            response = self.send_command(cmd)
            print(f"MoveC response: '{response}'")
            if response and "error" not in response.lower():
                return True
            print(f"MoveC failed: {response}")
            return False
        except Exception as e:
            print(f"Error in move_cartesian: {e}")
            return False

    def jog_cartesian(self, axis: str, distance: float):
        """
        Jog in Cartesian space using robot's native MoveC command.
        axis: 'z', 'yaw', 'gripper' use joint moves
        axis: 'r', 't', 'x', 'y' use Cartesian MoveC
        distance: meters for linear, radians for angular
        """
        # Get current joint positions for joint-based moves
        current_joints = self.get_joint_positions()
        if not current_joints:
            print("Failed to get joint positions")
            return False

        j1_m = current_joints.get("j1", 0)
        j2_rad = current_joints.get("j2", 0)
        j3_rad = current_joints.get("j3", 0)
        j4_rad = current_joints.get("j4", 0)
        grip_m = current_joints.get("gripper", 0)

        # Z axis - direct J1 control (vertical rail)
        if axis == 'z':
            print(f"Jog Z: J1 {j1_m*1000:.1f}mm -> {(j1_m+distance)*1000:.1f}mm")
            return self.move_to_joints(j1_m + distance, j2_rad, j3_rad, j4_rad, grip_m, self.current_profile)
        
        # Yaw - direct J4 control (wrist rotation)
        elif axis == 'yaw':
            print(f"Jog Yaw: J4 {math.degrees(j4_rad):.1f}° -> {math.degrees(j4_rad+distance):.1f}°")
            return self.move_to_joints(j1_m, j2_rad, j3_rad, j4_rad + distance, grip_m, self.current_profile)
        
        # Gripper - direct J5 control
        elif axis == 'gripper':
            print(f"Jog Gripper: {grip_m*1000:.1f}mm -> {(grip_m+distance)*1000:.1f}mm")
            return self.move_to_joints(j1_m, j2_rad, j3_rad, j4_rad, grip_m + distance, self.current_profile)

        # For X, Y, Radial, Tangential - use MoveC (Cartesian move)
        # Get current Cartesian position from robot
        current_cart = self.get_cartesian_position()
        if not current_cart:
            print("Failed to get Cartesian position")
            return False
        
        x_mm = current_cart.get("x", 0)
        y_mm = current_cart.get("y", 0)
        z_mm = current_cart.get("z", 0)
        yaw_deg = current_cart.get("yaw", 0)
        pitch_deg = current_cart.get("pitch", 90)
        roll_deg = current_cart.get("roll", 0)
        
        # Convert distance to mm
        dist_mm = distance * 1000.0
        
        # Calculate target based on axis
        target_x = x_mm
        target_y = y_mm
        
        if axis == 'x':
            target_x = x_mm + dist_mm
            print(f"Jog X: {x_mm:.1f} -> {target_x:.1f} mm")
        
        elif axis == 'y':
            target_y = y_mm + dist_mm
            print(f"Jog Y: {y_mm:.1f} -> {target_y:.1f} mm")
        
        elif axis == 'r':  # Radial (Out/In) - move along line from origin
            angle = math.atan2(y_mm, x_mm)
            target_x = x_mm + dist_mm * math.cos(angle)
            target_y = y_mm + dist_mm * math.sin(angle)
            print(f"Jog Radial: ({x_mm:.1f}, {y_mm:.1f}) -> ({target_x:.1f}, {target_y:.1f}) mm")
        
        elif axis == 't':  # Tangential (Left/Right) - perpendicular to radial
            angle = math.atan2(y_mm, x_mm)
            target_x = x_mm - dist_mm * math.sin(angle)
            target_y = y_mm + dist_mm * math.cos(angle)
            print(f"Jog Tangential: ({x_mm:.1f}, {y_mm:.1f}) -> ({target_x:.1f}, {target_y:.1f}) mm")
        
        else:
            print(f"Unknown axis: {axis}")
            return False
        
        # Use MoveC to go to target position
        return self.move_cartesian(target_x, target_y, z_mm, yaw_deg, pitch_deg, roll_deg, self.current_profile)

    def jog_joint(self, joint_idx: int, distance_si: float):
        """
        Jog a specific joint by a relative amount.
        joint_idx: 1=J1(Z), 2=J2(Sh), 3=J3(El), 4=J4(Wr), 5=Gr
        distance_si: meters or radians
        """
        # Get current state first
        current = self.get_joint_positions()
        if not current:
            return False
            
        # Map idx to keys
        key_map = {1: "j1", 2: "j2", 3: "j3", 4: "j4", 5: "gripper"}
        if joint_idx not in key_map:
            return False
            
        key = key_map[joint_idx]
        if key not in current:
            return False
            
        # Calculate new target
        target_val = current[key] + distance_si
        
        # Prepare full target dict
        targets = {
            "j1": current.get("j1", 0),
            "j2": current.get("j2", 0),
            "j3": current.get("j3", 0),
            "j4": current.get("j4", 0),
            "gripper": current.get("gripper", 0)
        }
        targets[key] = target_val
        
        return self.move_to_joints(
            targets["j1"], 
            targets["j2"], 
            targets["j3"], 
            targets["j4"], 
            targets["gripper"],
            profile=self.current_profile
        )

    def get_status(self):
        return "CONNECTED" if self.connected else "DISCONNECTED"

    def initialize_robot(self):
        """
        Initialize robot to GPL Ready mode.
        Based on AD-SDL driver sequence.
        """
        try:
            print("Initializing robot to GPL Ready mode...")
            
            # Set mode to non-verbose (like AD-SDL)
            resp = self.send_command("mode 0")
            print(f"  mode 0: {resp}")
            
            # Select robot 1 (like AD-SDL)
            resp = self.send_command("selectRobot 1")
            print(f"  selectRobot 1: {resp}")
            
            # Disable high power first (exits Jog Mode)
            resp = self.send_command("hp 0")
            print(f"  hp 0 (disable power): {resp}")
            time.sleep(1)
            
            # Enable high power with -1 flag (like AD-SDL: "hp 1 -1")
            resp = self.send_command("hp 1 -1")
            print(f"  hp 1 -1 (enable power): {resp}")
            time.sleep(2)
            
            # Attach to robot 1
            resp = self.send_command("attach 1")
            print(f"  attach 1: {resp}")
            time.sleep(0.5)
            
            # Select fast profile (profile 2) - uses robot's pre-configured profiles
            print("Selecting fast motion profile...")
            self.set_profile(2)
            
            # Check system state
            resp = self.send_command("sysState")
            print(f"  sysState: {resp}")
            
            print("Robot initialization complete")
            return True
        except Exception as e:
            print(f"Error initializing robot: {e}")
            return False

    def disable_power(self):
        """Disable high power (hp 0) - exits Jog Mode."""
        try:
            resp = self.send_command("hp 0")
            print(f"hp 0: {resp}")
            return True
        except Exception as e:
            print(f"Error disabling power: {e}")
            return False

    def enable_power(self):
        """Enable high power (hp 1) - enters GPL Ready mode."""
        try:
            resp = self.send_command("hp 1")
            print(f"hp 1: {resp}")
            return True
        except Exception as e:
            print(f"Error enabling power: {e}")
            return False

    def abort(self):
        """Send abort command to stop current operation."""
        try:
            resp = self.send_command("Abort")
            print(f"Abort: {resp}")
            return True
        except Exception as e:
            print(f"Error sending abort: {e}")
            return False

    def set_speed(self, profile_id: int, speed: int, accel: int = None, decel: int = None):
        """
        Update a motion profile's speed settings.
        
        WARNING: Profile command format is unknown - this function is disabled.
        Use SelectProfile to select one of the robot's pre-configured profiles instead.
        """
        print("WARNING: set_speed() disabled - Profile command format unknown")
        print("Use SelectProfile to select pre-configured profiles (1=slow, 2=fast)")
        # Just select the profile instead of trying to configure it
        return self.set_profile(profile_id)

    def get_current_profile(self):
        """Get current profile settings."""
        return {
            "profile_id": self.current_profile,
            "settings": self.MOTION_PROFILES.get(self.current_profile, {})
        }
