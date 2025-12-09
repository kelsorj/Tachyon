import mujoco
import time
import numpy as np

class PF400Simulator:
    def __init__(self, urdf_path):
        """
        Initialize the PF400 Simulator.
        
        Args:
            urdf_path (str): Absolute path to the URDF file.
        """
        try:
            self.model = mujoco.MjModel.from_xml_path(urdf_path)
            self.data = mujoco.MjData(self.model)
        except Exception as e:
            raise RuntimeError(f"Failed to load MuJoCo model from {urdf_path}: {e}")

        self.joint_names = [mujoco.mj_id2name(self.model, mujoco.mjtObj.mjOBJ_JOINT, i) for i in range(self.model.njnt)]
        print(f"PF400 Simulator initialized. Joints: {self.joint_names}")

    def get_joint_positions(self):
        """
        Get current joint positions.
        
        Returns:
            dict: Mapping of joint names to angles (radians).
        """
        positions = {}
        for name in self.joint_names:
            jnt_id = mujoco.mj_name2id(self.model, mujoco.mjtObj.mjOBJ_JOINT, name)
            qpos_adr = self.model.jnt_qposadr[jnt_id]
            positions[name] = self.data.qpos[qpos_adr]
        return positions

    def move_to_joints(self, target_joints, duration=2.0):
        """
        Move robot to target joint positions using kinematic interpolation.
        
        Args:
            target_joints (dict): Target joint angles {name: angle}.
            duration (float): Time to reach target in seconds.
        """
        start_qpos = self.data.qpos.copy()
        end_qpos = start_qpos.copy()
        
        # Map targets to qpos indices
        for name, val in target_joints.items():
            if name in self.joint_names:
                jnt_id = mujoco.mj_name2id(self.model, mujoco.mjtObj.mjOBJ_JOINT, name)
                qpos_adr = self.model.jnt_qposadr[jnt_id]
                end_qpos[qpos_adr] = val
            else:
                print(f"Warning: Joint {name} not found in model.")

        # Interpolate
        steps = int(duration / self.model.opt.timestep)
        if steps == 0: steps = 1
        
        for i in range(steps):
            alpha = (i + 1) / steps
            # Linear interpolation
            self.data.qpos[:] = (1 - alpha) * start_qpos + alpha * end_qpos
            
            # Step physics (even though we override qpos, this updates sensors/visuals)
            mujoco.mj_step(self.model, self.data)
            
            # Optional: sleep to match real-time if running interactively
            # time.sleep(self.model.opt.timestep) 

    def step(self):
        """Advance simulation by one step."""
        mujoco.mj_step(self.model, self.data)
