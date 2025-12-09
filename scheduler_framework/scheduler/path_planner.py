"""
PathPlanner - Finds optimal paths for robot movements.

Uses graph-based path finding (Dijkstra's algorithm) to determine
the best route for moving plates between locations.
"""

from typing import Dict, List, Optional, Set, Tuple
from collections import defaultdict
import math
from .device_manager import DeviceManager, AccessibleDeviceInterface, RobotInterface
from .active_plate import PlateLocation, PlatePlace


class Node:
    """Node in the path finding graph"""
    
    def __init__(self, key):
        self.key = key
        self.connections: List['Connection'] = []
        self.distance = math.inf
        self.previous: Optional['Node'] = None
        self.visited = False
    
    def __repr__(self):
        return f"Node({self.key})"


class Connection:
    """Connection between nodes in the graph"""
    
    def __init__(self, node: Node, cost: float, extra_info=None):
        self.node = node
        self.cost = cost
        self.extra_info = extra_info  # Usually the robot that can make this transfer


class PathPlanner:
    """
    Plans paths for robot movements using Dijkstra's algorithm.
    
    Builds a graph of all plate locations and finds optimal paths
    between them based on robot capabilities and transfer weights.
    """
    
    def __init__(self, device_manager: DeviceManager):
        self.device_manager = device_manager
        self.world_locations: Dict[str, AccessibleDeviceInterface] = {}  # location name -> device
        self.world_location_objects: Dict[str, PlateLocation] = {}  # location name -> PlateLocation
        self.world_places: Dict[str, PlateLocation] = {}  # place name -> PlateLocation
        self.world_place_objects: Dict[str, PlatePlace] = {}  # place name -> PlatePlace
        self.world_nodes: Dict[str, Node] = {}  # place name -> Node
    
    def create_world(self):
        """
        Build the graph of all locations and connections.
        
        This creates nodes for each plate place and edges for
        possible robot transfers.
        """
        # Get all accessible devices and robots
        accessible_devices = self.device_manager.get_accessible_devices()
        robots = self.device_manager.get_robots()
        
        # Build location and place mappings
        self.world_locations = {}
        self.world_location_objects = {}
        self.world_places = {}
        self.world_place_objects = {}
        
        for device in accessible_devices:
            for location in device.plate_location_info:
                # Use location name as key since PlateLocation isn't hashable
                self.world_locations[location.name] = device
                self.world_location_objects[location.name] = location
                # For each place in the location
                for place in location.places:
                    # Use place name as key since PlatePlace isn't hashable
                    place_key = f"{location.name}:{place.name}"
                    self.world_places[place_key] = location
                    self.world_place_objects[place_key] = place
        
        # Create nodes for all places
        all_place_keys = list(self.world_places.keys())
        self.world_nodes = {place_key: Node(place_key) for place_key in all_place_keys}
        
        # Build connections between places
        # Check all pairs of places to see if robots can transfer between them
        for i, src_place_key in enumerate(all_place_keys):
            for dst_place_key in all_place_keys[i+1:]:
                src_location = self.world_places[src_place_key]
                dst_location = self.world_places[dst_place_key]
                src_device = self.world_locations[src_location.name]
                dst_device = self.world_locations[dst_location.name]
                src_place = self.world_place_objects[src_place_key]
                dst_place = self.world_place_objects[dst_place_key]
                
                # Check each robot to see if it can make this transfer
                for robot in robots:
                    try:
                        weight = robot.get_transfer_weight(
                            src_device, src_location, src_place,
                            dst_device, dst_location, dst_place
                        )
                        
                        if weight > 0 and not math.isinf(weight):
                            # Add bidirectional connection
                            src_node = self.world_nodes[src_place_key]
                            dst_node = self.world_nodes[dst_place_key]
                            src_node.connections.append(
                                Connection(dst_node, weight, robot)
                            )
                            dst_node.connections.append(
                                Connection(src_node, weight, robot)
                            )
                    except Exception:
                        # Robot cannot make this transfer
                        pass
        
        self._check_nodes()
    
    def _check_nodes(self):
        """Check for disconnected nodes (places with no connections)"""
        disconnected = [place for place, node in self.world_nodes.items() 
                       if len(node.connections) == 0]
        if disconnected:
            print(f"Warning: The following places are disconnected: {disconnected}")
    
    def plan_path(self, src_location: PlateLocation, 
                  dst_location: PlateLocation) -> Optional[List[Node]]:
        """
        Plan a path from source to destination location.
        
        Uses Dijkstra's algorithm to find the shortest path.
        Returns a list of nodes representing the path, or None if no path exists.
        """
        # Get all places in source and destination locations
        src_places = src_location.places
        dst_places = dst_location.places
        
        if not src_places or not dst_places:
            return None
        
        # Reset all nodes
        for node in self.world_nodes.values():
            node.distance = math.inf
            node.previous = None
            node.visited = False
        
        # Find shortest path from any source place to any destination place
        shortest_distance = math.inf
        shortest_path = None
        
        for src_place in src_places:
            src_place_key = f"{src_location.name}:{src_place.name}"
            if src_place_key not in self.world_nodes:
                continue
            
            for dst_place in dst_places:
                dst_place_key = f"{dst_location.name}:{dst_place.name}"
                if dst_place_key not in self.world_nodes:
                    continue
                
                path = self._dijkstra(
                    self.world_nodes[src_place_key],
                    self.world_nodes[dst_place_key]
                )
                
                if path and len(path) > 0:
                    distance = path[-1].distance
                    if distance < shortest_distance:
                        shortest_distance = distance
                        shortest_path = path
        
        return shortest_path
    
    def _dijkstra(self, start: Node, end: Node) -> Optional[List[Node]]:
        """
        Dijkstra's algorithm to find shortest path.
        
        Returns list of nodes from start to end, or None if no path exists.
        """
        start.distance = 0
        unvisited = {start}
        
        while unvisited:
            # Get node with minimum distance
            current = min(unvisited, key=lambda n: n.distance)
            unvisited.remove(current)
            current.visited = True
            
            if current == end:
                # Reconstruct path
                path = []
                node = end
                while node:
                    path.append(node)
                    node = node.previous
                return list(reversed(path))
            
            # Update distances to neighbors
            for connection in current.connections:
                neighbor = connection.node
                if neighbor.visited:
                    continue
                
                new_distance = current.distance + connection.cost
                if new_distance < neighbor.distance:
                    neighbor.distance = new_distance
                    neighbor.previous = current
                    unvisited.add(neighbor)
        
        return None  # No path found

