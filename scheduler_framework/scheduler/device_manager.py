"""
DeviceManager and DeviceInterface abstractions.

Devices represent lab equipment (robots, readers, washers, etc.)
that can perform operations on plates.
"""

from abc import ABC, abstractmethod
from typing import List, Dict, Optional
from .active_plate import ActivePlate, PlateLocation
from .node_interface import NodeClient


class DeviceInterface(ABC):
    """Base interface for all devices"""
    
    def __init__(self, name: str, product_name: str):
        self.name = name
        self.product_name = product_name
    
    @abstractmethod
    def get_available_location(self, active_plate: ActivePlate) -> Optional[PlateLocation]:
        """Get an available location for the given plate"""
        pass
    
    @abstractmethod
    def reserve_location(self, location: PlateLocation, active_plate: ActivePlate) -> bool:
        """Reserve a location for a plate"""
        pass
    
    @abstractmethod
    def add_job(self, active_plate: ActivePlate):
        """Add a job to the device's queue"""
        pass


class PlateSchedulerDeviceInterface(DeviceInterface):
    """Interface for devices that can be scheduled by PlateScheduler"""
    
    @abstractmethod
    def lock_place(self, place_name: str):
        """Lock a specific place on the device"""
        pass
    
    @property
    @abstractmethod
    def plate_location_info(self) -> List[PlateLocation]:
        """Get all plate locations on this device"""
        pass


class AccessibleDeviceInterface(PlateSchedulerDeviceInterface):
    """Interface for devices that can be accessed by robots"""
    pass


class RobotInterface(ABC):
    """Interface for robot devices"""
    
    def __init__(self, name: str):
        self.name = name
    
    @abstractmethod
    def transfer_plate(self, src_device: str, src_place: str, 
                      dst_device: str, dst_place: str, 
                      labware_name: str, barcode: str):
        """Transfer a plate from one location to another"""
        pass
    
    @abstractmethod
    def get_transfer_weight(self, src_device: 'AccessibleDeviceInterface',
                           src_location: PlateLocation, src_place,
                           dst_device: 'AccessibleDeviceInterface',
                           dst_location: PlateLocation, dst_place) -> float:
        """
        Get the weight/cost for transferring between two places.
        Returns positive infinity if transfer is not possible.
        """
        pass


class DeviceManager:
    """Manages all devices in the system"""
    
    def __init__(self):
        self.devices: Dict[str, DeviceInterface] = {}
        self.robots: Dict[str, RobotInterface] = {}
        self.nodes: Dict[str, NodeClient] = {}
    
    def register_device(self, device: DeviceInterface):
        """Register a device"""
        self.devices[device.name] = device
    
    def register_robot(self, robot: RobotInterface):
        """Register a robot"""
        self.robots[robot.name] = robot

    def register_node(self, name: str, node: NodeClient):
        """
        Register a device microservice ("Node").

        Nodes are separate from scheduler "devices":
        - A Node is a networked service (FastAPI) that implements the Node contract.
        - A DeviceInterface is the scheduler-side view (locations, availability, add_job).
        """
        self.nodes[name] = node

    def get_node(self, name: str) -> Optional[NodeClient]:
        """Get a registered Node by name."""
        return self.nodes.get(name)
    
    def get_device(self, name: str) -> Optional[DeviceInterface]:
        """Get a device by name"""
        return self.devices.get(name)
    
    def get_robot(self, name: str) -> Optional[RobotInterface]:
        """Get a robot by name"""
        return self.robots.get(name)
    
    def get_devices_by_type(self, product_name: str) -> Dict[str, DeviceInterface]:
        """Get all devices of a specific product type"""
        return {name: device for name, device in self.devices.items() 
                if device.product_name == product_name}
    
    def get_accessible_devices(self) -> List[AccessibleDeviceInterface]:
        """Get all accessible devices"""
        return [device for device in self.devices.values() 
                if isinstance(device, AccessibleDeviceInterface)]
    
    def get_robots(self) -> List[RobotInterface]:
        """Get all robots"""
        return list(self.robots.values())



