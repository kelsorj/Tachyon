"""
Unit tests for device_manager.py
"""

import pytest
from scheduler import PlateLocation, PlatePlace, ActivePlate, ActiveSourcePlate
from scheduler.device_manager import (
    DeviceManager, DeviceInterface, PlateSchedulerDeviceInterface,
    AccessibleDeviceInterface, RobotInterface
)
from tests.conftest import MockDevice, MockRobot


class TestDeviceInterface:
    """Tests for DeviceInterface abstract class"""
    
    def test_device_interface_creation(self):
        """Test that DeviceInterface cannot be instantiated directly"""
        with pytest.raises(TypeError):
            DeviceInterface("TestDevice", "TestProduct")


class TestPlateSchedulerDeviceInterface:
    """Tests for PlateSchedulerDeviceInterface"""
    
    def test_plate_scheduler_device_interface(self, mock_device):
        """Test that mock device implements interface"""
        assert isinstance(mock_device, PlateSchedulerDeviceInterface)
        assert isinstance(mock_device, AccessibleDeviceInterface)
        assert mock_device.name == "TestDevice"
        assert mock_device.product_name == "TestProduct"


class TestRobotInterface:
    """Tests for RobotInterface abstract class"""
    
    def test_robot_interface_creation(self):
        """Test that RobotInterface cannot be instantiated directly"""
        with pytest.raises(TypeError):
            RobotInterface("TestRobot")


class TestDeviceManager:
    """Tests for DeviceManager"""
    
    def test_device_manager_creation(self):
        """Test creating a DeviceManager"""
        dm = DeviceManager()
        assert dm.devices == {}
        assert dm.robots == {}
    
    def test_register_device(self, device_manager, mock_device):
        """Test registering a device"""
        device_manager.register_device(mock_device)
        assert len(device_manager.devices) == 1
        assert device_manager.devices["TestDevice"] == mock_device
    
    def test_register_multiple_devices(self, device_manager):
        """Test registering multiple devices"""
        device1 = MockDevice("Device1", "Product1")
        device2 = MockDevice("Device2", "Product2")
        
        device_manager.register_device(device1)
        device_manager.register_device(device2)
        
        assert len(device_manager.devices) == 2
        assert device_manager.devices["Device1"] == device1
        assert device_manager.devices["Device2"] == device2
    
    def test_register_robot(self, device_manager, mock_robot):
        """Test registering a robot"""
        device_manager.register_robot(mock_robot)
        assert len(device_manager.robots) == 1
        assert device_manager.robots["TestRobot"] == mock_robot
    
    def test_register_multiple_robots(self, device_manager):
        """Test registering multiple robots"""
        robot1 = MockRobot("Robot1")
        robot2 = MockRobot("Robot2")
        
        device_manager.register_robot(robot1)
        device_manager.register_robot(robot2)
        
        assert len(device_manager.robots) == 2
        assert device_manager.robots["Robot1"] == robot1
        assert device_manager.robots["Robot2"] == robot2
    
    def test_get_device(self, device_manager, mock_device):
        """Test getting a device by name"""
        device_manager.register_device(mock_device)
        
        retrieved = device_manager.get_device("TestDevice")
        assert retrieved == mock_device
        
        not_found = device_manager.get_device("NonExistent")
        assert not_found is None
    
    def test_get_robot(self, device_manager, mock_robot):
        """Test getting a robot by name"""
        device_manager.register_robot(mock_robot)
        
        retrieved = device_manager.get_robot("TestRobot")
        assert retrieved == mock_robot
        
        not_found = device_manager.get_robot("NonExistent")
        assert not_found is None
    
    def test_get_devices_by_type(self, device_manager):
        """Test getting devices by product type"""
        device1 = MockDevice("Device1", "ProductA")
        device2 = MockDevice("Device2", "ProductA")
        device3 = MockDevice("Device3", "ProductB")
        
        device_manager.register_device(device1)
        device_manager.register_device(device2)
        device_manager.register_device(device3)
        
        product_a_devices = device_manager.get_devices_by_type("ProductA")
        assert len(product_a_devices) == 2
        assert "Device1" in product_a_devices
        assert "Device2" in product_a_devices
        assert "Device3" not in product_a_devices
        
        product_b_devices = device_manager.get_devices_by_type("ProductB")
        assert len(product_b_devices) == 1
        assert "Device3" in product_b_devices
    
    def test_get_accessible_devices(self, device_manager):
        """Test getting accessible devices"""
        device1 = MockDevice("Device1", "ProductA")
        device2 = MockDevice("Device2", "ProductB")
        
        device_manager.register_device(device1)
        device_manager.register_device(device2)
        
        accessible = device_manager.get_accessible_devices()
        assert len(accessible) == 2
        assert device1 in accessible
        assert device2 in accessible
    
    def test_get_robots(self, device_manager):
        """Test getting all robots"""
        robot1 = MockRobot("Robot1")
        robot2 = MockRobot("Robot2")
        
        device_manager.register_robot(robot1)
        device_manager.register_robot(robot2)
        
        robots = device_manager.get_robots()
        assert len(robots) == 2
        assert robot1 in robots
        assert robot2 in robots


class TestMockDevice:
    """Tests for MockDevice implementation"""
    
    def test_mock_device_get_available_location(self, mock_device, sample_worklist):
        """Test getting available location"""
        plate = ActivePlate(sample_worklist, 0)
        
        location = mock_device.get_available_location(plate)
        assert location is not None
        assert location.name == "TestLoc"
    
    def test_mock_device_get_available_location_when_occupied(self, mock_device, sample_worklist):
        """Test getting location when all are occupied"""
        plate = ActivePlate(sample_worklist, 0)
        
        # Occupy the location
        mock_device.locations[0].occupied.set()
        
        location = mock_device.get_available_location(plate)
        assert location is None
    
    def test_mock_device_reserve_location(self, mock_device, sample_worklist):
        """Test reserving a location"""
        plate = ActivePlate(sample_worklist, 0)
        location = mock_device.locations[0]
        
        result = mock_device.reserve_location(location, plate)
        assert result == True
        assert location.reserved.is_set()
    
    def test_mock_device_reserve_location_not_in_device(self, mock_device, sample_worklist):
        """Test reserving a location not in device"""
        plate = ActivePlate(sample_worklist, 0)
        other_location = PlateLocation("OtherLoc", "OtherDevice")
        
        result = mock_device.reserve_location(other_location, plate)
        assert result == False
    
    def test_mock_device_lock_place(self, mock_device):
        """Test locking a place"""
        mock_device.lock_place("TestPlace")
        assert "TestPlace" in mock_device.locked_places
    
    def test_mock_device_add_job(self, mock_device, sample_worklist):
        """Test adding a job"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        initial_busy = plate.busy
        
        mock_device.add_job(plate)
        
        assert len(mock_device.job_queue) == 1
        assert mock_device.job_queue[0] == plate
        # Job should be marked as completed
        assert not plate.busy


class TestMockRobot:
    """Tests for MockRobot implementation"""
    
    def test_mock_robot_transfer_plate(self, mock_robot):
        """Test transferring a plate"""
        mock_robot.transfer_plate(
            "SrcDevice", "SrcPlace",
            "DstDevice", "DstPlace",
            "96-well", "PLATE001"
        )
        
        assert len(mock_robot.transfer_history) == 1
        transfer = mock_robot.transfer_history[0]
        assert transfer['src_device'] == "SrcDevice"
        assert transfer['dst_device'] == "DstDevice"
        assert transfer['barcode'] == "PLATE001"
    
    def test_mock_robot_get_transfer_weight(self, mock_robot, mock_device):
        """Test getting transfer weight"""
        loc1 = PlateLocation("Loc1", "Device1")
        loc2 = PlateLocation("Loc2", "Device2")
        place1 = PlatePlace("Place1", loc1)
        place2 = PlatePlace("Place2", loc2)
        
        # Different locations should have weight 1.0
        weight = mock_robot.get_transfer_weight(
            mock_device, loc1, place1,
            mock_device, loc2, place2
        )
        assert weight == 1.0
        
        # Same location should have infinite weight
        weight = mock_robot.get_transfer_weight(
            mock_device, loc1, place1,
            mock_device, loc1, place2
        )
        assert weight == float('inf')

