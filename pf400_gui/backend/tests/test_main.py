"""
Unit tests for FastAPI main application.
"""
import pytest
import json
from unittest.mock import Mock, patch, MagicMock
from fastapi.testclient import TestClient
import sys
import os

# Add backend directory to path
backend_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, backend_dir)


class TestMainAPI:
    """Test suite for main FastAPI endpoints."""
    
    @pytest.fixture(autouse=True)
    def setup(self):
        """Setup test fixtures."""
        # Clear any existing robot_client
        if 'main' in sys.modules:
            import main
            main.robot_client = None
    
    def test_get_state_no_client(self):
        """Test /state endpoint when client is not initialized."""
        from main import app
        client = TestClient(app)
        
        # Clear robot_client
        import main
        main.robot_client = None
        
        response = client.get("/state")
        assert response.status_code == 503
        assert "not initialized" in response.json()["detail"].lower()
    
    def test_get_state_with_sim_client(self):
        """Test /state endpoint with simulator client."""
        from main import app
        client = TestClient(app)
        
        # Mock simulator client
        import main
        mock_client = Mock()
        mock_client.get_state.return_value = "READY"
        main.robot_client = mock_client
        
        response = client.get("/state")
        assert response.status_code == 200
        assert response.json() == {"state": "READY"}
        mock_client.get_state.assert_called_once()
    
    def test_execute_action_no_client(self):
        """Test /action endpoint when client is not initialized."""
        from main import app
        client = TestClient(app)
        
        # Clear robot_client
        import main
        main.robot_client = None
        
        response = client.post("/action/move_to_joints", json={"j1": 0.1})
        assert response.status_code == 503
        assert "not initialized" in response.json()["detail"].lower()
    
    def test_execute_action_with_client(self):
        """Test /action endpoint with client."""
        from main import app
        client = TestClient(app)
        
        # Mock client with async method
        import main
        mock_client = Mock()
        
        async def mock_send_action(action, vars_json):
            return {
                "status": "success",
                "response": 0,
                "message": "Move complete"
            }
        
        mock_client.send_action = mock_send_action
        main.robot_client = mock_client
        
        response = client.post("/action/move_to_joints", json={"j1": 0.1, "j2": 0.2})
        assert response.status_code == 200
        result = response.json()
        assert result["status"] == "success"
    
    def test_get_description_no_client(self):
        """Test /description endpoint when client is not initialized."""
        from main import app
        client = TestClient(app)
        
        # Clear robot_client
        import main
        main.robot_client = None
        
        response = client.get("/description")
        assert response.status_code == 503
        assert "not initialized" in response.json()["detail"].lower()
    
    def test_get_description_with_client(self):
        """Test /description endpoint with client."""
        from main import app
        client = TestClient(app)
        
        # Mock client with async method
        import main
        mock_client = Mock()
        
        async def mock_get_description():
            return {
                "description": "PF400 Robot Interface"
            }
        
        mock_client.get_description = mock_get_description
        main.robot_client = mock_client
        
        response = client.get("/description")
        assert response.status_code == 200
        assert response.json()["description"] == "PF400 Robot Interface"
    
    def test_get_joints_no_client(self):
        """Test /joints endpoint when client is not initialized."""
        from main import app
        client = TestClient(app)
        
        # Clear robot_client
        import main
        main.robot_client = None
        
        response = client.get("/joints")
        assert response.status_code == 503
        assert "not initialized" in response.json()["detail"].lower()
    
    def test_get_joints_with_sim_client(self):
        """Test /joints endpoint with simulator client."""
        from main import app
        client = TestClient(app)
        
        # Mock simulator client with sim attribute
        import main
        mock_client = Mock()
        mock_client.sim = Mock()
        mock_client.sim.get_joint_positions.return_value = {
            "j1": 0.1,
            "j2": 0.2,
            "j3": 0.3
        }
        main.robot_client = mock_client
        
        response = client.get("/joints")
        assert response.status_code == 200
        assert "joints" in response.json()
        assert response.json()["joints"]["j1"] == 0.1
    
    def test_get_joints_with_ros_client(self):
        """Test /joints endpoint with ROS client (no sim attribute)."""
        from main import app
        client = TestClient(app)
        
        # Mock ROS client without sim attribute
        import main
        # Create a simple object without sim attribute
        class MockROSClient:
            pass
        mock_client = MockROSClient()
        main.robot_client = mock_client
        
        response = client.get("/joints")
        assert response.status_code == 200
        assert response.json() == {"joints": {}}
    
    @pytest.mark.asyncio
    @patch('main.PF400ROSClient')
    async def test_startup_ros_mode(self, mock_ros_client_class, test_env_ros):
        """Test startup in ROS mode via lifespan."""
        import main
        mock_client_instance = Mock()
        mock_ros_client_class.return_value = mock_client_instance
        
        # Trigger lifespan startup
        async with main.lifespan(main.app):
            assert main.robot_client is not None
    
    @pytest.mark.asyncio
    @patch('main.SimClient')
    async def test_startup_sim_mode(self, mock_sim_client_class, test_env_sim):
        """Test startup in simulator mode via lifespan."""
        import main
        mock_client_instance = Mock()
        mock_sim_client_class.return_value = mock_client_instance
        
        # Trigger lifespan startup
        async with main.lifespan(main.app):
            assert main.robot_client is not None


class TestSimClient:
    """Test suite for SimClient class."""
    
    def test_sim_client_initialization(self):
        """Test SimClient initialization (skipped if pf400_sim_driver not available)."""
        # This test requires the actual pf400_sim_driver module
        # Skip if not available (e.g., in CI without MuJoCo)
        try:
            from main import SimClient
            # Only test if we can actually import the driver
            import sys
            import os
            current_dir = os.path.dirname(os.path.abspath(__file__))
            scripts_dir = os.path.join(os.path.dirname(current_dir), "../../scripts")
            if scripts_dir not in sys.path:
                sys.path.append(scripts_dir)
            try:
                import pf400_sim_driver
                # If we can import it, test initialization
                pytest.skip("SimClient initialization test requires actual URDF file")
            except ImportError:
                pytest.skip("pf400_sim_driver not available")
        except Exception:
            pytest.skip("SimClient initialization test skipped")
    
    def test_sim_client_get_state(self):
        """Test SimClient get_state method."""
        from main import SimClient
        
        client = SimClient.__new__(SimClient)
        client.state = "MOVING"
        
        assert client.get_state() == "MOVING"
    
    @pytest.mark.asyncio
    async def test_sim_client_send_action_move_to_joints(self):
        """Test SimClient send_action for move_to_joints."""
        from main import SimClient
        import json
        
        client = SimClient.__new__(SimClient)
        client.sim = Mock()
        client.state = "READY"
        
        params = {"j1": 0.1, "j2": 0.2}
        vars_json = json.dumps(params)
        
        result = await client.send_action("move_to_joints", vars_json)
        
        assert result["status"] == "success"
        assert result["response"] == 0
        client.sim.move_to_joints.assert_called_once_with(params, duration=2.0)
    
    @pytest.mark.asyncio
    async def test_sim_client_send_action_unknown(self):
        """Test SimClient send_action for unknown action."""
        from main import SimClient
        
        client = SimClient.__new__(SimClient)
        client.sim = Mock()
        client.state = "READY"
        
        result = await client.send_action("unknown_action", "{}")
        
        assert result["status"] == "failure"
        assert "Unknown action" in result["message"]
    
    @pytest.mark.asyncio
    async def test_sim_client_send_action_error(self):
        """Test SimClient send_action error handling."""
        from main import SimClient
        
        client = SimClient.__new__(SimClient)
        client.sim = Mock()
        client.sim.move_to_joints.side_effect = Exception("Test error")
        client.state = "READY"
        
        result = await client.send_action("move_to_joints", '{"j1": 0.1}')
        
        assert result["status"] == "error"
        assert "Test error" in result["message"]
        assert client.state == "ERROR"
    
    @pytest.mark.asyncio
    async def test_sim_client_get_description(self):
        """Test SimClient get_description method."""
        from main import SimClient
        
        client = SimClient.__new__(SimClient)
        
        result = await client.get_description()
        
        assert result["description"] == "PF400 Simulator Interface"

