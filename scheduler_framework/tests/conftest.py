"""
Shared fixtures and utilities for tests
"""

import pytest
import threading
from typing import List
from scheduler import (
    Plate, PlateLocation, PlatePlace, Worklist, PlateTask, 
    TransferOverview, Transfer, TransferTasks
)
from scheduler.device_manager import (
    DeviceManager, DeviceInterface, PlateSchedulerDeviceInterface,
    AccessibleDeviceInterface, RobotInterface
)
from scheduler.active_plate import ActivePlate


class MockDevice(AccessibleDeviceInterface):
    """Mock device for testing"""
    
    def __init__(self, name: str, product_name: str):
        super().__init__(name, product_name)
        self.locations: List[PlateLocation] = []
        self.job_queue = []
        self.locked_places = set()
    
    def add_location(self, location: PlateLocation):
        """Add a plate location to this device"""
        self.locations.append(location)
    
    @property
    def plate_location_info(self) -> List[PlateLocation]:
        return self.locations
    
    def get_available_location(self, active_plate):
        """Find an available location"""
        for location in self.locations:
            if location.available and not location.occupied.is_set():
                return location
        return None
    
    def reserve_location(self, location: PlateLocation, active_plate):
        """Reserve a location"""
        if location in self.locations:
            location.reserved.set()
            return True
        return False
    
    def lock_place(self, place_name: str):
        """Lock a place"""
        self.locked_places.add(place_name)
    
    def add_job(self, active_plate):
        """Add a job to process"""
        self.job_queue.append(active_plate)
        # Simulate immediate completion for testing
        active_plate.mark_job_completed()


class MockRobot(RobotInterface):
    """Mock robot for testing"""
    
    def __init__(self, name: str):
        super().__init__(name)
        self.transfer_history = []
    
    def transfer_plate(self, src_device: str, src_place: str,
                      dst_device: str, dst_place: str,
                      labware_name: str, barcode: str):
        """Transfer a plate"""
        self.transfer_history.append({
            'src_device': src_device,
            'src_place': src_place,
            'dst_device': dst_device,
            'dst_place': dst_place,
            'labware_name': labware_name,
            'barcode': barcode
        })
    
    def get_transfer_weight(self, src_device, src_location, src_place,
                           dst_device, dst_location, dst_place) -> float:
        """Calculate transfer weight/cost"""
        if src_location == dst_location:
            return float('inf')
        return 1.0


@pytest.fixture
def sample_plate():
    """Create a sample plate"""
    return Plate("PLATE001", "96-well", "96-well")


@pytest.fixture
def sample_location():
    """Create a sample location"""
    loc = PlateLocation("TestLocation", "TestDevice")
    place = PlatePlace("TestPlace", loc)
    loc.places = [place]
    return loc


@pytest.fixture
def sample_worklist():
    """Create a sample worklist"""
    worklist = Worklist("TestWorklist")
    source_plate = Plate("SRC001", "96-well", "96-well")
    dest_plate = Plate("DST001", "96-well", "96-well")
    
    worklist.add_source_plate(source_plate)
    worklist.add_destination_plate(dest_plate)
    
    transfer = Transfer(source_plate, dest_plate, volume=10.0)
    overview = TransferOverview(
        transfers=[transfer],
        source_plates={"SRC001": source_plate},
        destination_plates={"DST001": dest_plate}
    )
    worklist.transfer_overview = overview
    
    return worklist


@pytest.fixture
def device_manager():
    """Create a device manager with mock devices"""
    dm = DeviceManager()
    return dm


@pytest.fixture
def mock_device():
    """Create a mock device"""
    device = MockDevice("TestDevice", "TestProduct")
    loc = PlateLocation("TestLoc", device.name)
    place = PlatePlace("TestPlace", loc)
    loc.places = [place]
    device.add_location(loc)
    return device


@pytest.fixture
def mock_robot():
    """Create a mock robot"""
    return MockRobot("TestRobot")


@pytest.fixture
def setup_with_devices(device_manager, mock_device, mock_robot):
    """Set up device manager with devices and robot"""
    device_manager.register_device(mock_device)
    device_manager.register_robot(mock_robot)
    return device_manager, mock_device, mock_robot


@pytest.fixture(autouse=True)
def reset_active_plates():
    """Reset active plates list before each test"""
    with ActivePlate._lock:
        ActivePlate._active_plates.clear()
        ActivePlate._plate_serial_counter = 0
    yield
    with ActivePlate._lock:
        ActivePlate._active_plates.clear()
        ActivePlate._plate_serial_counter = 0

