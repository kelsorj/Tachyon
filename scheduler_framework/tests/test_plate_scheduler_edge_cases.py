"""
Additional edge case tests for plate_scheduler.py
"""

import pytest
from scheduler import (
    PlateScheduler, RobotScheduler, PlateLocation, PlatePlace,
    ActiveSourcePlate, ActivePlate
)
from scheduler.device_manager import DeviceManager, DeviceInterface
from scheduler.worklist import Worklist, Plate, PlateTask
from tests.conftest import MockDevice, MockRobot


class NonCompliantDevice(DeviceInterface):
    """Device that doesn't implement PlateSchedulerDeviceInterface"""
    
    def __init__(self, name: str, product_name: str):
        super().__init__(name, product_name)
    
    def get_available_location(self, active_plate):
        return None
    
    def reserve_location(self, location, active_plate):
        return False
    
    def add_job(self, active_plate):
        pass


class TestPlateSchedulerEdgeCases:
    """Edge case tests for PlateScheduler"""
    
    def test_schedule_task_non_compliant_device(self, device_manager, sample_worklist):
        """Test scheduling task with non-compliant device"""
        robot_scheduler = RobotScheduler(device_manager)
        scheduler = PlateScheduler(robot_scheduler, device_manager)
        
        # Register non-compliant device
        non_compliant = NonCompliantDevice("NonCompliant", "Product")
        device_manager.register_device(non_compliant)
        
        plate = ActiveSourcePlate(sample_worklist, 0)
        task = PlateTask("Product", "test_command")
        
        result = scheduler._schedule_task(plate, task)
        # Should skip non-compliant device
        assert result == False
    
    def test_schedule_task_no_available_location(self, device_manager, sample_worklist):
        """Test scheduling when no location is available"""
        robot_scheduler = RobotScheduler(device_manager)
        scheduler = PlateScheduler(robot_scheduler, device_manager)
        
        # Device with all locations occupied
        device = MockDevice("Device1", "Product1")
        loc = PlateLocation("Loc1", device.name)
        loc.occupied.set()  # Mark as occupied
        place = PlatePlace("Place1", loc)
        loc.places = [place]
        device.add_location(loc)
        device_manager.register_device(device)
        
        plate = ActiveSourcePlate(sample_worklist, 0)
        task = PlateTask("Product1", "test_command")
        
        result = scheduler._schedule_task(plate, task)
        # Should return False when no location available
        assert result == False
    
    def test_schedule_task_reservation_failure(self, device_manager, sample_worklist):
        """Test scheduling when location reservation fails"""
        robot_scheduler = RobotScheduler(device_manager)
        scheduler = PlateScheduler(robot_scheduler, device_manager)
        
        # Device that fails reservation
        class FailingReservationDevice(MockDevice):
            def reserve_location(self, location, active_plate):
                return False  # Always fail
        
        device = FailingReservationDevice("Device1", "Product1")
        loc = PlateLocation("Loc1", device.name)
        place = PlatePlace("Place1", loc)
        loc.places = [place]
        device.add_location(loc)
        device_manager.register_device(device)
        
        plate = ActiveSourcePlate(sample_worklist, 0)
        task = PlateTask("Product1", "test_command")
        
        result = scheduler._schedule_task(plate, task)
        # Should return False when reservation fails
        assert result == False
    
    def test_schedule_task_multiple_devices_try_all(self, device_manager, sample_worklist):
        """Test that scheduler tries all devices of same type"""
        robot_scheduler = RobotScheduler(device_manager)
        scheduler = PlateScheduler(robot_scheduler, device_manager)
        
        # First device - occupied
        device1 = MockDevice("Device1", "Product1")
        loc1 = PlateLocation("Loc1", device1.name)
        loc1.occupied.set()
        place1 = PlatePlace("Place1", loc1)
        loc1.places = [place1]
        device1.add_location(loc1)
        
        # Second device - available
        device2 = MockDevice("Device2", "Product1")
        loc2 = PlateLocation("Loc2", device2.name)
        place2 = PlatePlace("Place2", loc2)
        loc2.places = [place2]
        device2.add_location(loc2)
        
        device_manager.register_device(device1)
        device_manager.register_device(device2)
        
        plate = ActiveSourcePlate(sample_worklist, 0)
        task = PlateTask("Product1", "test_command")
        
        result = scheduler._schedule_task(plate, task)
        # Should succeed with second device
        assert result == True
    
    def test_do_worklist_with_no_transfer_overview(self, device_manager):
        """Test processing worklist without transfer overview"""
        robot_scheduler = RobotScheduler(device_manager)
        scheduler = PlateScheduler(robot_scheduler, device_manager)
        
        worklist = Worklist("NoTransferWorklist")
        worklist.add_source_plate(Plate("SRC001", "96-well", "96-well"))
        
        # Should complete quickly without transfer overview
        scheduler._do_worklist(worklist)
        # Should not raise exception
    
    def test_get_status_with_active_plates(self, device_manager, sample_worklist):
        """Test get_status when there are active plates"""
        robot_scheduler = RobotScheduler(device_manager)
        scheduler = PlateScheduler(robot_scheduler, device_manager)
        
        plate = ActiveSourcePlate(sample_worklist, 0)
        with ActivePlate._lock:
            ActivePlate._active_plates.append(plate)
        
        status = scheduler.get_status()
        assert "Active plates" in status
        assert "SRC001" in status or plate.barcode in status
    
    def test_scheduler_thread_exception_handling(self, device_manager):
        """Test that scheduler thread handles exceptions gracefully"""
        robot_scheduler = RobotScheduler(device_manager)
        scheduler = PlateScheduler(robot_scheduler, device_manager)
        
        # Create a worklist that might cause issues
        worklist = Worklist("ProblemWorklist")
        scheduler.enqueue_worklist(worklist)
        
        scheduler.start_scheduler()
        
        # Wait a bit
        import time
        time.sleep(0.2)
        
        # Should still be running (exception should be caught)
        assert scheduler.scheduler_thread.is_alive()
        
        scheduler.stop_scheduler()

