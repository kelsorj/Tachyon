"""
Pytest configuration and shared fixtures for backend tests.
"""
import pytest
import os
import sys
from unittest.mock import Mock, MagicMock, patch
from pathlib import Path

# Add backend directory to path
backend_dir = Path(__file__).parent.parent
sys.path.insert(0, str(backend_dir))

# Add scripts directory to path for simulator imports
scripts_dir = backend_dir.parent.parent / "scripts"
sys.path.insert(0, str(scripts_dir))


@pytest.fixture
def mock_ros_client():
    """Mock ROS client for testing."""
    mock_client = Mock()
    mock_client.get_state.return_value = "READY"
    mock_client.send_action = Mock()
    mock_client.get_description = Mock()
    return mock_client


@pytest.fixture
def mock_sim_client():
    """Mock simulator client for testing."""
    mock_client = Mock()
    mock_client.get_state.return_value = "READY"
    mock_client.send_action = Mock()
    mock_client.get_description = Mock()
    mock_client.sim = Mock()
    mock_client.sim.get_joint_positions.return_value = {
        "j1": 0.0,
        "j2": 0.0,
        "j3": 0.0,
        "j4": 0.0,
        "j5left": 0.0,
        "j5right": 0.0
    }
    return mock_client


@pytest.fixture
def urdf_path():
    """Path to URDF file for testing."""
    backend_dir = Path(__file__).parent.parent
    urdf_path = backend_dir.parent.parent / "models" / "pf400_urdf" / "pf400Complete.urdf"
    return str(urdf_path)


@pytest.fixture
def test_env_sim():
    """Set environment variable for simulator mode."""
    os.environ['PF400_SIM_MODE'] = '1'
    yield
    if 'PF400_SIM_MODE' in os.environ:
        del os.environ['PF400_SIM_MODE']


@pytest.fixture
def test_env_ros():
    """Clear simulator mode environment variable."""
    if 'PF400_SIM_MODE' in os.environ:
        del os.environ['PF400_SIM_MODE']
    yield

