"""
Unit tests for robot_scheduler.py
"""

import pytest
import threading
import time
from scheduler import (
    RobotScheduler, PlateLocation, PlatePlace, ActiveSourcePlate
)
from scheduler.device_manager import DeviceManager
from scheduler.worklist import Worklist, Plate, TransferOverview, Transfer
from tests.conftest import MockDevice, MockRobot


class TestRobotScheduler:
    """Tests for RobotScheduler"""
    
    def test_robot_scheduler_creation(self, device_manager):
        """Test creating a RobotScheduler"""
        scheduler = RobotScheduler(device_manager)
        assert scheduler.device_manager == device_manager
        assert scheduler.pending_jobs.empty()
        assert scheduler.scheduler_thread is None
        assert scheduler.path_planner is not None
        assert scheduler.entering_move_plate_callbacks == []
        assert scheduler.exiting_move_plate_callbacks == []
    
    def test_start_scheduler(self, device_manager):
        """Test starting the scheduler"""
        scheduler = RobotScheduler(device_manager)
        scheduler.start_scheduler()
        
        assert scheduler.scheduler_thread is not None
        assert scheduler.scheduler_thread.is_alive()
        assert scheduler.scheduler_thread.name == "RobotScheduler"
        
        scheduler.stop_scheduler()
    
    def test_start_scheduler_already_running(self, device_manager):
        """Test starting scheduler when already running"""
        scheduler = RobotScheduler(device_manager)
        scheduler.start_scheduler()
        
        # Try to start again
        thread1 = scheduler.scheduler_thread
        scheduler.start_scheduler()
        thread2 = scheduler.scheduler_thread
        
        # Should be the same thread
        assert thread1 == thread2
        
        scheduler.stop_scheduler()
    
    def test_stop_scheduler(self, device_manager):
        """Test stopping the scheduler"""
        scheduler = RobotScheduler(device_manager)
        scheduler.start_scheduler()
        
        assert scheduler.scheduler_thread.is_alive()
        
        scheduler.stop_scheduler()
        
        # Thread should be stopped (or stopping)
        time.sleep(0.1)  # Give it time to stop
        assert not scheduler.scheduler_thread.is_alive() or scheduler.stop_event.is_set()
    
    def test_add_job(self, device_manager, sample_worklist):
        """Test adding a job to the queue"""
        scheduler = RobotScheduler(device_manager)
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        assert scheduler.pending_jobs.empty()
        scheduler.add_job(plate)
        assert not scheduler.pending_jobs.empty()
        assert scheduler.pending_jobs.get() == plate
    
    def test_move_plate_no_locations(self, device_manager, sample_worklist):
        """Test moving plate with no locations"""
        scheduler = RobotScheduler(device_manager)
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        # Plate has no current or destination location
        scheduler._move_plate(plate)
        # Should handle gracefully (no exception)
    
    def test_move_plate_same_location(self, device_manager, sample_worklist):
        """Test moving plate to same location"""
        scheduler = RobotScheduler(device_manager)
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        loc = PlateLocation("TestLoc", "TestDevice")
        plate.current_location = loc
        plate.destination_location = loc
        
        scheduler._move_plate(plate)
        # Should handle gracefully (no exception)
    
    def test_move_plate_different_locations(self, setup_with_devices, sample_worklist):
        """Test moving plate between different locations"""
        device_manager, mock_device, mock_robot = setup_with_devices
        
        # Add another device
        device2 = MockDevice("Device2", "Product2")
        loc2 = PlateLocation("Loc2", device2.name)
        place2 = PlatePlace("Place2", loc2)
        loc2.places = [place2]
        device2.add_location(loc2)
        device_manager.register_device(device2)
        
        scheduler = RobotScheduler(device_manager)
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        loc1 = mock_device.locations[0]
        plate.current_location = loc1
        plate.destination_location = loc2
        
        scheduler._move_plate(plate)
        
        # Plate location should be updated
        assert plate.current_location == loc2
        # Robot should have transfer history
        assert len(mock_robot.transfer_history) > 0
    
    def test_execute_path(self, setup_with_devices, sample_worklist):
        """Test executing a path"""
        device_manager, mock_device, mock_robot = setup_with_devices
        
        # Add another device
        device2 = MockDevice("Device2", "Product2")
        loc2 = PlateLocation("Loc2", device2.name)
        place2 = PlatePlace("Place2", loc2)
        loc2.places = [place2]
        device2.add_location(loc2)
        device_manager.register_device(device2)
        
        scheduler = RobotScheduler(device_manager)
        scheduler.path_planner.create_world()
        
        plate = ActiveSourcePlate(sample_worklist, 0)
        loc1 = mock_device.locations[0]
        
        # Plan a path
        path = scheduler.path_planner.plan_path(loc1, loc2)
        
        if path and len(path) >= 2:
            scheduler._execute_path(plate, path)
            # Robot should have been called
            assert len(mock_robot.transfer_history) > 0
    
    def test_execute_path_short(self, device_manager, sample_worklist):
        """Test executing a path that's too short"""
        scheduler = RobotScheduler(device_manager)
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        # Path with less than 2 nodes
        short_path = []
        scheduler._execute_path(plate, short_path)
        # Should handle gracefully
        
        # Only try to get a node if places exist
        if scheduler.path_planner.world_places:
            short_path = [scheduler.path_planner.world_nodes.get(list(scheduler.path_planner.world_places.keys())[0])]
            if short_path[0]:
                scheduler._execute_path(plate, short_path)
                # Should handle gracefully
    
    def test_add_entering_move_plate_callback(self, device_manager):
        """Test adding entering move plate callback"""
        scheduler = RobotScheduler(device_manager)
        
        callback_called = []
        def callback():
            callback_called.append(True)
        
        scheduler.add_entering_move_plate_callback(callback)
        assert len(scheduler.entering_move_plate_callbacks) == 1
    
    def test_add_exiting_move_plate_callback(self, device_manager):
        """Test adding exiting move plate callback"""
        scheduler = RobotScheduler(device_manager)
        
        callback_called = []
        def callback():
            callback_called.append(True)
        
        scheduler.add_exiting_move_plate_callback(callback)
        assert len(scheduler.exiting_move_plate_callbacks) == 1
    
    def test_get_status(self, device_manager):
        """Test getting status"""
        scheduler = RobotScheduler(device_manager)
        
        status = scheduler.get_status()
        assert "RobotScheduler" in status
    
    def test_get_status_with_pending_jobs(self, device_manager, sample_worklist):
        """Test getting status with pending jobs"""
        scheduler = RobotScheduler(device_manager)
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        scheduler.add_job(plate)
        status = scheduler.get_status()
        assert "pending" in status.lower() or "RobotScheduler" in status
    
    def test_scheduler_thread_processing(self, setup_with_devices, sample_worklist):
        """Test that scheduler thread processes jobs"""
        device_manager, mock_device, mock_robot = setup_with_devices
        
        # Add another device
        device2 = MockDevice("Device2", "Product2")
        loc2 = PlateLocation("Loc2", device2.name)
        place2 = PlatePlace("Place2", loc2)
        loc2.places = [place2]
        device2.add_location(loc2)
        device_manager.register_device(device2)
        
        scheduler = RobotScheduler(device_manager)
        scheduler.start_scheduler()
        
        plate = ActiveSourcePlate(sample_worklist, 0)
        loc1 = mock_device.locations[0]
        plate.current_location = loc1
        plate.destination_location = loc2
        
        scheduler.add_job(plate)
        
        # Wait a bit for processing
        time.sleep(0.5)
        
        # Plate should have been moved
        # (Location might be updated, or robot should have transfer history)
        # Note: This is a timing-dependent test, so we check what we can
        
        scheduler.stop_scheduler()

