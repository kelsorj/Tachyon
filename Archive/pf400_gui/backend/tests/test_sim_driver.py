"""
Unit tests for PF400 simulator driver.
"""
import pytest
from unittest.mock import Mock, patch, MagicMock
import numpy as np
from pathlib import Path


class TestPF400Simulator:
    """Test suite for PF400Simulator class."""
    
    @pytest.fixture
    def urdf_path(self):
        """Get path to URDF file."""
        backend_dir = Path(__file__).parent.parent
        urdf_path = backend_dir.parent.parent / "models" / "pf400_urdf" / "pf400Complete.urdf"
        return str(urdf_path)
    
    @pytest.fixture
    def mock_mujoco(self):
        """Mock mujoco module."""
        with patch('pf400_sim_driver.mujoco') as mock_mj:
            # Mock model
            mock_model = Mock()
            mock_model.njnt = 6
            mock_model.opt.timestep = 0.01
            mock_model.jnt_qposadr = [0, 1, 2, 3, 4, 5]
            
            # Mock data
            mock_data = Mock()
            mock_data.qpos = np.array([0.0, 0.0, 0.0, 0.0, 0.0, 0.0])
            
            # Mock functions
            def mock_id2name(model, obj_type, idx):
                joint_names = ["j1", "j2", "j3", "j4", "j5left", "j5right"]
                return joint_names[idx] if idx < len(joint_names) else ""
            
            def mock_name2id(model, obj_type, name):
                joint_names = ["j1", "j2", "j3", "j4", "j5left", "j5right"]
                return joint_names.index(name) if name in joint_names else -1
            
            mock_mj.MjModel.from_xml_path.return_value = mock_model
            mock_mj.MjData.return_value = mock_data
            mock_mj.mj_id2name.side_effect = mock_id2name
            mock_mj.mj_name2id.side_effect = mock_name2id
            mock_mj.mjtObj.mjOBJ_JOINT = 1
            mock_mj.mj_step = Mock()
            
            yield mock_mj, mock_model, mock_data
    
    def test_simulator_initialization_success(self, urdf_path, mock_mujoco):
        """Test successful simulator initialization."""
        mock_mj, mock_model, mock_data = mock_mujoco
        
        from pf400_sim_driver import PF400Simulator
        
        sim = PF400Simulator(urdf_path)
        
        assert sim.model == mock_model
        assert sim.data == mock_data
        assert len(sim.joint_names) == 6
        mock_mj.MjModel.from_xml_path.assert_called_once_with(urdf_path)
    
    def test_simulator_initialization_failure(self, mock_mujoco):
        """Test simulator initialization failure."""
        mock_mj, _, _ = mock_mujoco
        mock_mj.MjModel.from_xml_path.side_effect = Exception("URDF load error")
        
        from pf400_sim_driver import PF400Simulator
        
        with pytest.raises(RuntimeError) as exc_info:
            PF400Simulator("/invalid/path.urdf")
        
        assert "Failed to load MuJoCo model" in str(exc_info.value)
    
    def test_get_joint_positions(self, urdf_path, mock_mujoco):
        """Test get_joint_positions method."""
        mock_mj, mock_model, mock_data = mock_mujoco
        mock_data.qpos = np.array([0.1, 0.2, 0.3, 0.4, 0.5, 0.6])
        
        from pf400_sim_driver import PF400Simulator
        
        sim = PF400Simulator(urdf_path)
        positions = sim.get_joint_positions()
        
        assert isinstance(positions, dict)
        assert positions["j1"] == 0.1
        assert positions["j2"] == 0.2
        assert positions["j3"] == 0.3
        assert positions["j4"] == 0.4
        assert positions["j5left"] == 0.5
        assert positions["j5right"] == 0.6
    
    def test_move_to_joints_success(self, urdf_path, mock_mujoco):
        """Test move_to_joints with valid joint targets."""
        mock_mj, mock_model, mock_data = mock_mujoco
        mock_data.qpos = np.array([0.0, 0.0, 0.0, 0.0, 0.0, 0.0])
        mock_model.opt.timestep = 0.01  # 100 steps for 1 second
        
        from pf400_sim_driver import PF400Simulator
        
        sim = PF400Simulator(urdf_path)
        
        target_joints = {
            "j1": 0.1,
            "j2": 0.2,
            "j3": 0.3
        }
        
        sim.move_to_joints(target_joints, duration=1.0)
        
        # Verify mj_step was called (should be called multiple times during interpolation)
        assert mock_mj.mj_step.call_count > 0
        # Verify final positions are set
        assert mock_data.qpos[0] == pytest.approx(0.1, abs=0.01)
        assert mock_data.qpos[1] == pytest.approx(0.2, abs=0.01)
        assert mock_data.qpos[2] == pytest.approx(0.3, abs=0.01)
    
    def test_move_to_joints_unknown_joint(self, urdf_path, mock_mujoco, capsys):
        """Test move_to_joints with unknown joint name."""
        mock_mj, mock_model, mock_data = mock_mujoco
        mock_data.qpos = np.array([0.0, 0.0, 0.0, 0.0, 0.0, 0.0])
        mock_mj.mj_name2id.return_value = -1  # Joint not found
        
        from pf400_sim_driver import PF400Simulator
        
        sim = PF400Simulator(urdf_path)
        
        target_joints = {
            "unknown_joint": 0.1
        }
        
        sim.move_to_joints(target_joints, duration=1.0)
        
        # Should print warning but not crash
        captured = capsys.readouterr()
        assert "Warning" in captured.out or "unknown_joint" in captured.out
    
    def test_move_to_joints_zero_duration(self, urdf_path, mock_mujoco):
        """Test move_to_joints with zero duration."""
        mock_mj, mock_model, mock_data = mock_mujoco
        mock_data.qpos = np.array([0.0, 0.0, 0.0, 0.0, 0.0, 0.0])
        mock_model.opt.timestep = 0.01
        
        from pf400_sim_driver import PF400Simulator
        
        sim = PF400Simulator(urdf_path)
        
        target_joints = {"j1": 0.1}
        
        # Should handle zero duration gracefully
        sim.move_to_joints(target_joints, duration=0.0)
        
        # Should still call mj_step at least once
        assert mock_mj.mj_step.call_count >= 1
    
    def test_step(self, urdf_path, mock_mujoco):
        """Test step method."""
        mock_mj, mock_model, mock_data = mock_mujoco
        
        from pf400_sim_driver import PF400Simulator
        
        sim = PF400Simulator(urdf_path)
        sim.step()
        
        mock_mj.mj_step.assert_called_once_with(mock_model, mock_data)
    
    def test_move_to_joints_all_joints(self, urdf_path, mock_mujoco):
        """Test move_to_joints with all joints."""
        mock_mj, mock_model, mock_data = mock_mujoco
        mock_data.qpos = np.array([0.0, 0.0, 0.0, 0.0, 0.0, 0.0])
        
        from pf400_sim_driver import PF400Simulator
        
        sim = PF400Simulator(urdf_path)
        
        target_joints = {
            "j1": 0.1,
            "j2": 0.2,
            "j3": 0.3,
            "j4": 0.4,
            "j5left": 0.01,
            "j5right": 0.01
        }
        
        sim.move_to_joints(target_joints, duration=0.1)
        
        # Verify all joints are updated
        for i, (name, expected_value) in enumerate(target_joints.items()):
            assert mock_data.qpos[i] == pytest.approx(expected_value, abs=0.1)

