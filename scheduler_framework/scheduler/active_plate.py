"""
ActivePlate - Represents a plate with associated tasks and state.

Based on the C# ActivePlate class, this tracks:
- Plate location (current and destination)
- Task list (ToDo items)
- Plate state (busy/free)
- Completion status
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import List, Optional, Union
from enum import Enum
import threading
from .worklist import PlateTask, WaitTask


class PlateState(Enum):
    """State of a plate in the system"""
    FREE = "free"
    BUSY = "busy"
    MOVING = "moving"
    PROCESSING = "processing"
    COMPLETED = "completed"


@dataclass
class Plate:
    """Represents a physical plate"""
    barcode: str
    labware_name: str
    labware_format: str  # e.g., "96-well", "384-well"
    currently_lidded: bool = True


@dataclass
class PlatePlace:
    """Represents a specific place within a location where a plate can be placed"""
    name: str
    location: 'PlateLocation' = None
    
    def __repr__(self):
        return f"PlatePlace({self.name})"


@dataclass
class PlateLocation:
    """Represents a location where a plate can be placed"""
    name: str
    device_name: str
    available: bool = True
    occupied: threading.Event = field(default_factory=lambda: threading.Event())
    reserved: threading.Event = field(default_factory=lambda: threading.Event())
    places: List[PlatePlace] = field(default_factory=list)
    
    def __post_init__(self):
        # Initially not occupied
        self.occupied.clear()
        self.reserved.clear()
        # Create a default place if none exist
        if not self.places:
            self.places = [PlatePlace(f"{self.name}_place", self)]


class ActivePlate:
    """
    Abstract base class for plates that are actively being processed.
    
    Tracks:
    - Current location and destination
    - Task list (ToDo items)
    - State (busy/free)
    - Completion status
    """
    
    _active_plates: List['ActivePlate'] = []
    _plate_serial_counter = 0
    _lock = threading.Lock()
    
    def __init__(self, worklist: 'Worklist', instance_index: int):
        self.instance_index = instance_index
        self.plate_is_free = threading.Event()
        self.plate_is_free.set()  # Initially free
        
        with ActivePlate._lock:
            ActivePlate._plate_serial_counter += 1
            self.plate_serial_number = ActivePlate._plate_serial_counter
            ActivePlate._active_plates.append(self)
        
        self.current_location: Optional[PlateLocation] = None
        self.destination_location: Optional[PlateLocation] = None
        self.plate: Optional[Plate] = None
        
        # Task management - can contain both PlateTask and WaitTask
        self.todo_list: List[Union[PlateTask, WaitTask]] = []
        self.current_task_index: int = 0
        self.still_have_todos = False
        
    @property
    def busy(self) -> bool:
        """Check if plate is currently busy"""
        return not self.plate_is_free.is_set()
    
    @property
    def barcode(self) -> str:
        """Get plate barcode"""
        return self.plate.barcode if self.plate else ""
    
    @property
    def labware_name(self) -> str:
        """Get labware name"""
        return self.plate.labware_name if self.plate else ""
    
    def get_current_todo(self) -> Optional[Union[PlateTask, WaitTask]]:
        """Get the current task to do (can be PlateTask or WaitTask)"""
        if self.still_have_todos and self.current_task_index < len(self.todo_list):
            return self.todo_list[self.current_task_index]
        return None
    
    def advance_current_todo(self):
        """Move to the next task"""
        self.current_task_index += 1
        self.still_have_todos = self.current_task_index < len(self.todo_list)
    
    def is_finished(self) -> bool:
        """Check if all tasks are completed"""
        return not self.busy and not self.still_have_todos
    
    def mark_job_completed(self):
        """Mark the current job as completed and advance to next task"""
        self.advance_current_todo()
        if self.destination_location:
            self.destination_location.reserved.clear()
        self.current_location = self.destination_location
        self.plate_is_free.set()
    
    def get_status(self) -> str:
        """Get status string for debugging/monitoring"""
        plate_type = "source" if isinstance(self, ActiveSourcePlate) else "destination"
        status = f"Info for {plate_type} plate S/N: {self.plate_serial_number}\n"
        status += f"\tInstanceIndex: {self.instance_index}\n"
        status += f"\tBarcode: {self.barcode}\n"
        status += f"\tLabware: {self.labware_name}\n"
        status += f"\tBusy: {self.busy}\n"
        
        if self.current_location:
            status += f"\tCurrent location: {self.current_location.name}\n"
        if self.destination_location:
            status += f"\tDestination location: {self.destination_location.name}\n"
        
        status += f"\tPlate is free: {self.plate_is_free.is_set()}\n"
        status += "\tToDoList:\n"
        for i, task in enumerate(self.todo_list, 1):
            status += f"\t\tTask #{i}\n"
            status += f"\t\tDeviceType: {task.device_type}\n"
            status += f"\t\tCommand: {task.command}\n"
            status += f"\t\tCompleted: {task.completed}\n"
        
        return status
    
    def __repr__(self):
        return f"{self.__class__.__name__}{self.instance_index}"


class ActiveSourcePlate(ActivePlate):
    """Represents a source plate being processed"""
    
    def __init__(self, worklist: 'Worklist', instance_index: int):
        super().__init__(worklist, instance_index)
        
        # Build task list: pre-hitpick tasks + source_hitpick + post-hitpick tasks
        from .worklist import TransferOverview, PlateTask
        
        if worklist.transfer_overview:
            tasks = []
            # Add pre-hitpick tasks
            tasks.extend(worklist.transfer_overview.tasks.source_prehitpick_tasks)
            # Add the hitpick task
            tasks.append(PlateTask("Bumblebee", "source_hitpick"))
            # Add post-hitpick tasks
            tasks.extend(worklist.transfer_overview.tasks.source_posthitpick_tasks)
            
            self.todo_list = tasks
        
        # Set up task tracking
        self.current_task_index = 0
        self.still_have_todos = len(self.todo_list) > 0
        
        # Get the plate from worklist
        if instance_index < len(worklist.source_plates):
            self.plate = worklist.source_plates[instance_index]


class ActiveDestinationPlate(ActivePlate):
    """Represents a destination plate being processed"""
    
    def __init__(self, worklist: 'Worklist', instance_index: int):
        super().__init__(worklist, instance_index)
        
        # Build task list from transfer overview
        from .worklist import PlateTask
        
        if worklist.transfer_overview:
            tasks = []
            # Add pre-hitpick tasks
            tasks.extend(worklist.transfer_overview.tasks.destination_prehitpick_tasks)
            # Add the hitpick task
            tasks.append(PlateTask("Bumblebee", "destination_hitpick"))
            # Add post-hitpick tasks
            tasks.extend(worklist.transfer_overview.tasks.destination_posthitpick_tasks)
            
            self.todo_list = tasks
        
        # Set up task tracking
        self.current_task_index = 0
        self.still_have_todos = len(self.todo_list) > 0
        
        # Get the plate from worklist
        if instance_index < len(worklist.destination_plates):
            self.plate = worklist.destination_plates[instance_index]

