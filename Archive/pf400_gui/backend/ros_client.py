try:
    import rclpy
    from rclpy.node import Node
except ImportError:
    print("Warning: rclpy not found. Using mock classes.")
    class Node:
        def __init__(self, name): pass
        def create_subscription(self, *args): pass
        def create_publisher(self, *args): pass
        def create_client(self, *args): pass
    class rclpy:
        @staticmethod
        def init(args=None): pass
        @staticmethod
        def spin(node): pass

try:
    from std_msgs.msg import String
except ImportError:
    class String:
        data = ""

import json
import time
from threading import Thread

# Try to import WEI services, if not available, define mocks for development
try:
    from wei_services.srv import WeiActions, WeiDescription
except ImportError:
    print("Warning: wei_services not found. Using mock classes.")
    class WeiActions:
        class Request:
            action_handle = ""
            vars = ""
        class Response:
            action_response = 0
            action_msg = ""
            
    class WeiDescription:
        class Request:
            pass
        class Response:
            description_response = ""

class PF400ROSClient(Node):
    def __init__(self):
        rclpy.init()
        super().__init__('pf400_gui_backend')
        
        self.state = "UNKNOWN"
        
        # Subscriber for state
        # Assuming the topic is /pf400_client/state based on the repo exploration
        # The repo code says: self.statePub = self.create_publisher(String, node_name + '/state', 10)
        # If the node name is 'pf400_client', then the topic is '/pf400_client/state'
        self.state_sub = self.create_subscription(
            String,
            '/pf400_client/state',
            self.state_callback,
            10
        )
        
        # Service Clients
        self.action_client = self.create_client(WeiActions, '/pf400_client/action_handler')
        self.description_client = self.create_client(WeiDescription, '/pf400_client/description_handler')
        
    def state_callback(self, msg):
        self.state = msg.data
        
    def get_state(self):
        return self.state
        
    def spin(self):
        rclpy.spin(self)
        
    async def send_action(self, action_handle, vars_json):
        if not self.action_client.wait_for_service(timeout_sec=1.0):
            return {"status": "error", "message": "Action service not available"}
            
        req = WeiActions.Request()
        req.action_handle = action_handle
        req.vars = vars_json
        
        future = self.action_client.call_async(req)
        # In a real async app we might want to await this properly
        # For now, we'll wait for the result in a way compatible with FastAPI
        try:
            response = await future
            return {
                "status": "success" if response.action_response == 0 else "failure",
                "response": response.action_response,
                "message": response.action_msg
            }
        except Exception as e:
            return {"status": "error", "message": str(e)}

    async def get_description(self):
        if not self.description_client.wait_for_service(timeout_sec=1.0):
            return {"status": "error", "message": "Description service not available"}
            
        req = WeiDescription.Request()
        future = self.description_client.call_async(req)
        
        try:
            response = await future
            return {"description": response.description_response}
        except Exception as e:
            return {"status": "error", "message": str(e)}
