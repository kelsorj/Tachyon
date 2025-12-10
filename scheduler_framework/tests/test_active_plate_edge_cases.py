"""
Additional edge case tests for active_plate.py
"""

import pytest
from scheduler import (
    ActivePlate, ActiveSourcePlate, ActiveDestinationPlate,
    Plate, PlateLocation, PlatePlace
)
from scheduler.worklist import Worklist, PlateTask


class TestActivePlateEdgeCases:
    """Edge case tests for ActivePlate"""
    
    def test_get_current_todo_empty_list(self):
        """Test get_current_todo with empty todo list"""
        worklist = Worklist("EmptyWorklist")
        plate = ActiveSourcePlate(worklist, 0)
        
        plate.todo_list = []
        plate.still_have_todos = False
        
        assert plate.get_current_todo() is None
    
    def test_get_current_todo_index_out_of_bounds(self, sample_worklist):
        """Test get_current_todo when index is out of bounds"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        plate.todo_list = [PlateTask("Device", "task1")]
        plate.current_task_index = 10  # Out of bounds
        plate.still_have_todos = True
        
        assert plate.get_current_todo() is None
    
    def test_advance_current_todo_to_end(self, sample_worklist):
        """Test advance_current_todo when reaching end of list"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        plate.todo_list = [PlateTask("Device", "task1")]
        plate.current_task_index = 0
        plate.still_have_todos = True
        
        plate.advance_current_todo()
        
        assert plate.current_task_index == 1
        assert plate.still_have_todos == False
    
    def test_mark_job_completed_no_destination(self, sample_worklist):
        """Test mark_job_completed when there's no destination location"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        plate.current_location = PlateLocation("Loc1", "Device1")
        plate.destination_location = None
        plate.todo_list = [PlateTask("Device", "task1")]
        plate.current_task_index = 0
        plate.still_have_todos = True
        
        # Should handle None destination gracefully
        plate.mark_job_completed()
        
        assert plate.current_task_index == 1
        assert plate.current_location is None
    
    def test_get_status_with_no_tasks(self):
        """Test get_status when there are no tasks"""
        worklist = Worklist("NoTasksWorklist")
        plate = ActiveSourcePlate(worklist, 0)
        
        plate.todo_list = []
        
        status = plate.get_status()
        assert "ToDoList" in status
        assert "source plate" in status.lower()
    
    def test_get_status_with_no_locations(self, sample_worklist):
        """Test get_status when plate has no locations"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        plate.current_location = None
        plate.destination_location = None
        
        status = plate.get_status()
        # Should not include location info
        assert "Current location" not in status or "None" in status
    
    def test_is_finished_busy_with_todos(self, sample_worklist):
        """Test is_finished when plate is busy but has todos"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        plate.plate_is_free.clear()  # Make busy
        plate.still_have_todos = True
        
        assert plate.is_finished() == False
    
    def test_is_finished_not_busy_no_todos(self, sample_worklist):
        """Test is_finished when plate is not busy and has no todos"""
        plate = ActiveSourcePlate(sample_worklist, 0)
        
        plate.plate_is_free.set()
        plate.still_have_todos = False
        
        assert plate.is_finished() == True
    
    def test_plate_location_post_init_default_place(self):
        """Test PlateLocation __post_init__ creates default place"""
        loc = PlateLocation("TestLoc", "TestDevice")
        
        # Should have created a default place
        assert len(loc.places) == 1
        assert loc.places[0].name == "TestLoc_place"
        assert loc.places[0].location == loc
    
    def test_plate_location_post_init_with_existing_places(self):
        """Test PlateLocation __post_init__ doesn't override existing places"""
        place1 = PlatePlace("Place1")
        place2 = PlatePlace("Place2")
        
        loc = PlateLocation("TestLoc", "TestDevice")
        loc.places = [place1, place2]
        
        # Should not create default place since places already exist
        assert len(loc.places) == 2
        assert place1 in loc.places
        assert place2 in loc.places
    
    def test_active_plate_barcode_no_plate(self):
        """Test barcode property when plate is None"""
        worklist = Worklist("NoPlateWorklist")
        plate = ActiveSourcePlate(worklist, 0)
        
        plate.plate = None
        
        assert plate.barcode == ""
    
    def test_active_plate_labware_name_no_plate(self):
        """Test labware_name property when plate is None"""
        worklist = Worklist("NoPlateWorklist")
        plate = ActiveSourcePlate(worklist, 0)
        
        plate.plate = None
        
        assert plate.labware_name == ""
    
    def test_active_source_plate_index_out_of_bounds(self):
        """Test ActiveSourcePlate when instance_index exceeds source plates"""
        worklist = Worklist("LimitedWorklist")
        worklist.add_source_plate(Plate("SRC001", "96-well", "96-well"))
        
        # Create plate with index beyond available plates
        plate = ActiveSourcePlate(worklist, 10)
        
        # Plate should be None since index is out of bounds
        assert plate.plate is None
    
    def test_active_destination_plate_index_out_of_bounds(self):
        """Test ActiveDestinationPlate when instance_index exceeds destination plates"""
        worklist = Worklist("LimitedWorklist")
        worklist.add_destination_plate(Plate("DST001", "96-well", "96-well"))
        
        # Create plate with index beyond available plates
        plate = ActiveDestinationPlate(worklist, 10)
        
        # Plate should be None since index is out of bounds
        assert plate.plate is None

