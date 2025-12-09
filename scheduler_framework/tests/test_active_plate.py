"""
Unit tests for active_plate.py
"""

import pytest
import threading
from scheduler import (
    ActivePlate, ActiveSourcePlate, ActiveDestinationPlate,
    Plate, PlateLocation, PlatePlace, PlateState
)
from scheduler.worklist import Worklist, PlateTask, TransferOverview, Transfer


class TestPlate:
    """Tests for Plate dataclass"""
    
    def test_plate_creation(self):
        """Test creating a Plate"""
        plate = Plate("PLATE001", "96-well", "96-well")
        assert plate.barcode == "PLATE001"
        assert plate.labware_name == "96-well"
        assert plate.labware_format == "96-well"
        assert plate.currently_lidded == True
    
    def test_plate_with_lid(self):
        """Test creating a Plate with lid status"""
        plate = Plate("PLATE001", "96-well", "96-well", currently_lidded=False)
        assert plate.currently_lidded == False


class TestPlatePlace:
    """Tests for PlatePlace"""
    
    def test_plate_place_creation(self):
        """Test creating a PlatePlace"""
        place = PlatePlace("TestPlace")
        assert place.name == "TestPlace"
        assert place.location is None
    
    def test_plate_place_with_location(self):
        """Test creating a PlatePlace with location"""
        loc = PlateLocation("TestLoc", "TestDevice")
        place = PlatePlace("TestPlace", loc)
        assert place.location == loc
    
    def test_plate_place_repr(self):
        """Test PlatePlace string representation"""
        place = PlatePlace("TestPlace")
        assert "TestPlace" in repr(place)


class TestPlateLocation:
    """Tests for PlateLocation"""
    
    def test_plate_location_creation(self):
        """Test creating a PlateLocation"""
        loc = PlateLocation("TestLoc", "TestDevice")
        assert loc.name == "TestLoc"
        assert loc.device_name == "TestDevice"
        assert loc.available == True
        assert not loc.occupied.is_set()
        assert not loc.reserved.is_set()
        assert len(loc.places) == 1  # Default place created
    
    def test_plate_location_with_places(self):
        """Test creating a PlateLocation with custom places"""
        loc = PlateLocation("TestLoc", "TestDevice")
        place1 = PlatePlace("Place1", loc)
        place2 = PlatePlace("Place2", loc)
        loc.places = [place1, place2]
        
        assert len(loc.places) == 2
        assert place1 in loc.places
        assert place2 in loc.places
    
    def test_plate_location_occupied(self):
        """Test plate location occupied event"""
        loc = PlateLocation("TestLoc", "TestDevice")
        assert not loc.occupied.is_set()
        
        loc.occupied.set()
        assert loc.occupied.is_set()
        
        loc.occupied.clear()
        assert not loc.occupied.is_set()
    
    def test_plate_location_reserved(self):
        """Test plate location reserved event"""
        loc = PlateLocation("TestLoc", "TestDevice")
        assert not loc.reserved.is_set()
        
        loc.reserved.set()
        assert loc.reserved.is_set()
        
        loc.reserved.clear()
        assert not loc.reserved.is_set()


class TestActivePlate:
    """Tests for ActivePlate base class"""
    
    def test_active_plate_creation(self, sample_worklist):
        """Test creating an ActivePlate"""
        # Create a simple active plate (using ActiveSourcePlate as concrete implementation)
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        assert plate.instance_index == 0
        assert plate.plate_serial_number == 1
        assert not plate.busy
        assert plate.current_location is None
        assert plate.destination_location is None
        assert plate.current_task_index == 0
    
    def test_active_plate_serial_numbers(self, sample_worklist):
        """Test that serial numbers are unique and sequential"""
        plate1 = ActiveSourcePlate(sample_worklist, 0)
        plate2 = ActiveSourcePlate(sample_worklist, 1)
        plate3 = ActiveDestinationPlate(sample_worklist, 0)
        
        assert plate1.plate_serial_number == 1
        assert plate2.plate_serial_number == 2
        assert plate3.plate_serial_number == 3
    
    def test_active_plate_busy_property(self, sample_worklist):
        """Test busy property"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        assert not plate.busy
        
        plate.plate_is_free.clear()
        assert plate.busy
        
        plate.plate_is_free.set()
        assert not plate.busy
    
    def test_active_plate_barcode(self, sample_worklist):
        """Test barcode property"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        assert plate.barcode == "SRC001"
        
        # Test with no plate
        plate.plate = None
        assert plate.barcode == ""
    
    def test_active_plate_labware_name(self, sample_worklist):
        """Test labware_name property"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        assert plate.labware_name == "96-well"
        
        # Test with no plate
        plate.plate = None
        assert plate.labware_name == ""
    
    def test_get_current_todo(self, sample_worklist):
        """Test getting current todo"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        # Should have tasks from transfer overview
        if sample_worklist.transfer_overview:
            current = plate.get_current_todo()
            # Should return first task if still_have_todos is True
            if plate.still_have_todos:
                assert current is not None
                assert isinstance(current, PlateTask)
    
    def test_advance_current_todo(self, sample_worklist):
        """Test advancing to next todo"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        initial_index = plate.current_task_index
        
        plate.advance_current_todo()
        assert plate.current_task_index == initial_index + 1
    
    def test_is_finished(self, sample_worklist):
        """Test is_finished method"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        # Initially not finished if there are todos
        if plate.still_have_todos:
            assert not plate.is_finished()
        
        # Mark as not busy and no todos
        plate.plate_is_free.set()
        plate.still_have_todos = False
        assert plate.is_finished()
    
    def test_mark_job_completed(self, sample_worklist):
        """Test marking job as completed"""
        loc1 = PlateLocation("Loc1", "Device1")
        loc2 = PlateLocation("Loc2", "Device2")
        
        plate = ActiveSourcePlate(sample_worklist, 0)
        plate.current_location = loc1
        plate.destination_location = loc2
        loc2.reserved.set()
        
        initial_index = plate.current_task_index
        plate.mark_job_completed()
        
        assert plate.current_task_index == initial_index + 1
        assert not loc2.reserved.is_set()
        assert plate.current_location == loc2
        assert plate.plate_is_free.is_set()
    
    def test_get_status(self, sample_worklist):
        """Test get_status method"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        status = plate.get_status()
        
        assert "source plate" in status.lower()
        assert str(plate.plate_serial_number) in status
        assert "SRC001" in status or plate.barcode in status
    
    def test_active_plate_repr(self, sample_worklist):
        """Test ActivePlate string representation"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        repr_str = repr(plate)
        assert "ActiveSourcePlate" in repr_str
        assert "0" in repr_str
    
    def test_active_plate_tracking(self, sample_worklist):
        """Test that active plates are tracked in class variable"""
        with ActivePlate._lock:
            initial_count = len(ActivePlate._active_plates)
        
        plate1 = ActiveSourcePlate(sample_worklist, 0)
        plate2 = ActiveSourcePlate(sample_worklist, 1)
        
        with ActivePlate._lock:
            assert len(ActivePlate._active_plates) == initial_count + 2
            assert plate1 in ActivePlate._active_plates
            assert plate2 in ActivePlate._active_plates


class TestActiveSourcePlate:
    """Tests for ActiveSourcePlate"""
    
    def test_active_source_plate_creation(self, sample_worklist):
        """Test creating an ActiveSourcePlate"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        assert isinstance(plate, ActiveSourcePlate)
        assert isinstance(plate, ActivePlate)
        assert plate.plate is not None
        assert plate.plate.barcode == "SRC001"
    
    def test_active_source_plate_tasks(self, sample_worklist):
        """Test that ActiveSourcePlate has correct tasks"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        if sample_worklist.transfer_overview:
            # Should have tasks including source_hitpick
            assert len(plate.todo_list) > 0
            # Check for source_hitpick task
            hitpick_tasks = [t for t in plate.todo_list if t.command == "source_hitpick"]
            assert len(hitpick_tasks) > 0
    
    def test_active_source_plate_without_transfer_overview(self):
        """Test ActiveSourcePlate without transfer overview"""
        worklist = Worklist("TestWorklist")
        source_plate = Plate("SRC001", "96-well", "96-well")
        worklist.add_source_plate(source_plate)
        
        plate = ActiveSourcePlate(worklist, 0)
        assert plate.plate == source_plate
        assert len(plate.todo_list) == 0
        assert not plate.still_have_todos


class TestActiveDestinationPlate:
    """Tests for ActiveDestinationPlate"""
    
    def test_active_destination_plate_creation(self, sample_worklist):
        """Test creating an ActiveDestinationPlate"""
        plate = ActiveDestinationPlate(sample_worklist, 0)
        
        assert isinstance(plate, ActiveDestinationPlate)
        assert isinstance(plate, ActivePlate)
        assert plate.plate is not None
        assert plate.plate.barcode == "DST001"
    
    def test_active_destination_plate_tasks(self, sample_worklist):
        """Test that ActiveDestinationPlate has correct tasks"""
        plate = ActiveDestinationPlate(sample_worklist, 0)
        
        if sample_worklist.transfer_overview:
            # Should have tasks including destination_hitpick
            assert len(plate.todo_list) > 0
            # Check for destination_hitpick task
            hitpick_tasks = [t for t in plate.todo_list if t.command == "destination_hitpick"]
            assert len(hitpick_tasks) > 0
    
    def test_active_destination_plate_without_transfer_overview(self):
        """Test ActiveDestinationPlate without transfer overview"""
        worklist = Worklist("TestWorklist")
        dest_plate = Plate("DST001", "96-well", "96-well")
        worklist.add_destination_plate(dest_plate)
        
        plate = ActiveDestinationPlate(worklist, 0)
        assert plate.plate == dest_plate
        assert len(plate.todo_list) == 0
        assert not plate.still_have_todos

