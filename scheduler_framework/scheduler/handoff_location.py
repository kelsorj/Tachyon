"""
HandoffLocation - Special location for robot-to-robot plate handoffs.

This represents a location where two robots can transfer plates between each other.
For example, PF400 can place a plate at a handoff location, and Planar can pick it up.
"""

from dataclasses import dataclass
from typing import List, Optional
from .active_plate import PlateLocation, PlatePlace


@dataclass
class HandoffLocation(PlateLocation):
    """
    A special location where robots can hand off plates to each other.
    
    This extends PlateLocation to add handoff-specific functionality:
    - Tracks which robots can access this location
    - Manages handoff state (waiting for pickup, etc.)
    """
    
    def __init__(self, name: str, device_name: str, 
                 accessible_by_robots: List[str]):
        """
        Create a handoff location.
        
        Args:
            name: Name of the location
            device_name: Name of the device hosting this location
            accessible_by_robots: List of robot names that can access this location
        """
        super().__init__(name, device_name)
        self.accessible_by_robots = accessible_by_robots
        self.handoff_state = "free"  # "free", "waiting_for_pickup", "occupied"
        self.waiting_robot: Optional[str] = None
        self.pickup_robot: Optional[str] = None
    
    def can_robot_access(self, robot_name: str) -> bool:
        """Check if a robot can access this handoff location"""
        return robot_name in self.accessible_by_robots
    
    def mark_waiting_for_pickup(self, robot_name: str):
        """Mark that a robot has placed a plate and is waiting for pickup"""
        if not self.can_robot_access(robot_name):
            raise ValueError(f"Robot {robot_name} cannot access handoff location {self.name}")
        self.handoff_state = "waiting_for_pickup"
        self.waiting_robot = robot_name
        self.occupied.set()
    
    def mark_picked_up(self, robot_name: str):
        """Mark that a robot has picked up the plate"""
        if not self.can_robot_access(robot_name):
            raise ValueError(f"Robot {robot_name} cannot access handoff location {self.name}")
        if self.handoff_state != "waiting_for_pickup":
            raise ValueError(f"Handoff location {self.name} is not in waiting_for_pickup state")
        self.handoff_state = "free"
        self.pickup_robot = robot_name
        self.waiting_robot = None
        self.occupied.clear()
    
    def is_waiting_for_pickup(self) -> bool:
        """Check if a plate is waiting to be picked up"""
        return self.handoff_state == "waiting_for_pickup"

