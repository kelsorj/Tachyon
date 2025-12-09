"""
Unit tests for ROS client.
"""
import pytest
from unittest.mock import Mock, patch, MagicMock
import json


class TestPF400ROSClient:
    """Test suite for PF400ROSClient class."""
    
    @patch('ros_client.rclpy')
    def test_ros_client_initialization(self, mock_rclpy):
        """Test ROS client initialization."""
        from ros_client import PF400ROSClient
        
        mock_rclpy.init.return_value = None
        
        with patch('ros_client.Node.__init__', return_value=None):
            client = PF400ROSClient()
            assert client.state == "UNKNOWN"
            mock_rclpy.init.assert_called_once()
    
    def test_get_state(self):
        """Test get_state method."""
        from ros_client import PF400ROSClient
        
        client = PF400ROSClient.__new__(PF400ROSClient)
        client.state = "READY"
        
        assert client.get_state() == "READY"
    
    def test_state_callback(self):
        """Test state callback updates state."""
        from ros_client import PF400ROSClient
        
        client = PF400ROSClient.__new__(PF400ROSClient)
        client.state = "UNKNOWN"
        
        # Mock message
        mock_msg = Mock()
        mock_msg.data = "MOVING"
        
        client.state_callback(mock_msg)
        
        assert client.state == "MOVING"
    
    @pytest.mark.asyncio
    async def test_send_action_service_not_available(self):
        """Test send_action when service is not available."""
        from ros_client import PF400ROSClient
        
        client = PF400ROSClient.__new__(PF400ROSClient)
        client.action_client = Mock()
        client.action_client.wait_for_service.return_value = False
        
        result = await client.send_action("move_to_joints", '{"j1": 0.1}')
        
        assert result["status"] == "error"
        assert "not available" in result["message"].lower()
    
    @pytest.mark.asyncio
    async def test_send_action_success(self):
        """Test send_action with successful response."""
        from ros_client import PF400ROSClient
        
        client = PF400ROSClient.__new__(PF400ROSClient)
        client.action_client = Mock()
        client.action_client.wait_for_service.return_value = True
        
        # Mock successful response
        mock_response = Mock()
        mock_response.action_response = 0
        mock_response.action_msg = "Success"
        
        # Create a proper async mock
        async def mock_call_async(req):
            return mock_response
        
        client.action_client.call_async = mock_call_async
        
        result = await client.send_action("move_to_joints", '{"j1": 0.1}')
        
        assert result["status"] == "success"
        assert result["response"] == 0
        assert result["message"] == "Success"
    
    @pytest.mark.asyncio
    async def test_send_action_failure(self):
        """Test send_action with failure response."""
        from ros_client import PF400ROSClient
        
        client = PF400ROSClient.__new__(PF400ROSClient)
        client.action_client = Mock()
        client.action_client.wait_for_service.return_value = True
        
        # Mock failure response
        mock_response = Mock()
        mock_response.action_response = 1
        mock_response.action_msg = "Failed"
        
        # Create a proper async mock
        async def mock_call_async(req):
            return mock_response
        
        client.action_client.call_async = mock_call_async
        
        result = await client.send_action("move_to_joints", '{"j1": 0.1}')
        
        assert result["status"] == "failure"
        assert result["response"] == 1
        assert result["message"] == "Failed"
    
    @pytest.mark.asyncio
    async def test_send_action_exception(self):
        """Test send_action exception handling."""
        from ros_client import PF400ROSClient
        
        client = PF400ROSClient.__new__(PF400ROSClient)
        client.action_client = Mock()
        client.action_client.wait_for_service.return_value = True
        
        # Mock exception
        async def mock_call_async(req):
            raise Exception("Test error")
        
        client.action_client.call_async = mock_call_async
        
        result = await client.send_action("move_to_joints", '{"j1": 0.1}')
        
        assert result["status"] == "error"
        assert "Test error" in result["message"]
    
    @pytest.mark.asyncio
    async def test_get_description_service_not_available(self):
        """Test get_description when service is not available."""
        from ros_client import PF400ROSClient
        
        client = PF400ROSClient.__new__(PF400ROSClient)
        client.description_client = Mock()
        client.description_client.wait_for_service.return_value = False
        
        result = await client.get_description()
        
        assert result["status"] == "error"
        assert "not available" in result["message"].lower()
    
    @pytest.mark.asyncio
    async def test_get_description_success(self):
        """Test get_description with successful response."""
        from ros_client import PF400ROSClient
        
        client = PF400ROSClient.__new__(PF400ROSClient)
        client.description_client = Mock()
        client.description_client.wait_for_service.return_value = True
        
        # Mock successful response
        mock_response = Mock()
        mock_response.description_response = "PF400 Robot Description"
        
        # Create a proper async mock
        async def mock_call_async(req):
            return mock_response
        
        client.description_client.call_async = mock_call_async
        
        result = await client.get_description()
        
        assert result["description"] == "PF400 Robot Description"
    
    @pytest.mark.asyncio
    async def test_get_description_exception(self):
        """Test get_description exception handling."""
        from ros_client import PF400ROSClient
        
        client = PF400ROSClient.__new__(PF400ROSClient)
        client.description_client = Mock()
        client.description_client.wait_for_service.return_value = True
        
        # Mock exception
        async def mock_call_async(req):
            raise Exception("Test error")
        
        client.description_client.call_async = mock_call_async
        
        result = await client.get_description()
        
        assert result["status"] == "error"
        assert "Test error" in result["message"]


class TestMockClasses:
    """Test suite for mock classes when ROS dependencies are missing."""
    
    def test_mock_node_class(self):
        """Test that mock Node class can be instantiated."""
        # This tests the fallback mock classes when rclpy is not available
        # The mock classes should be defined in ros_client.py
        from ros_client import Node
        
        # Should be able to create instance without errors
        node = Node("test_node")
        assert node is not None
    
    def test_mock_string_class(self):
        """Test that mock String class exists."""
        from ros_client import String
        
        msg = String()
        assert msg.data == ""
    
    def test_mock_wei_services(self):
        """Test that mock WEI service classes exist."""
        from ros_client import WeiActions, WeiDescription
        
        # Test WeiActions
        req = WeiActions.Request()
        assert req.action_handle == ""
        assert req.vars == ""
        
        resp = WeiActions.Response()
        assert resp.action_response == 0
        assert resp.action_msg == ""
        
        # Test WeiDescription
        desc_req = WeiDescription.Request()
        assert desc_req is not None
        
        desc_resp = WeiDescription.Response()
        assert desc_resp.description_response == ""

