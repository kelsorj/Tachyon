"""
Worklist and PlateTask definitions.

A Worklist represents a batch of work to be performed,
containing source plates, destination plates, and transfer operations.
"""

from dataclasses import dataclass, field
from typing import List, Optional, Dict
from .active_plate import Plate
from .tasks import PlateTask, WaitTask
from .ids import new_ulid_str


@dataclass
class TransferTasks:
    """Container for tasks associated with transfers"""
    source_prehitpick_tasks: List[PlateTask] = field(default_factory=list)
    source_posthitpick_tasks: List[PlateTask] = field(default_factory=list)
    destination_prehitpick_tasks: List[PlateTask] = field(default_factory=list)
    destination_posthitpick_tasks: List[PlateTask] = field(default_factory=list)


@dataclass
class Transfer:
    """Represents a single transfer operation"""
    source_plate: Plate
    destination_plate: Plate
    source_well: Optional[str] = None
    destination_well: Optional[str] = None
    volume: Optional[float] = None


@dataclass
class TransferOverview:
    """Overview of all transfers in a worklist"""
    transfers: List[Transfer] = field(default_factory=list)
    source_plates: Dict[str, Plate] = field(default_factory=dict)
    destination_plates: Dict[str, Plate] = field(default_factory=dict)
    tasks: TransferTasks = field(default_factory=TransferTasks)


class Worklist:
    """
    Represents a batch of work to be performed.
    
    Contains:
    - Source plates (plates to transfer from)
    - Destination plates (plates to transfer to)
    - Transfer overview (defines the operations)
    """
    
    def __init__(self, name: str):
        self.name = name
        self.worklist_id = new_ulid_str()
        self.source_plates: List[Plate] = []
        self.destination_plates: List[Plate] = []
        self.transfer_overview: Optional[TransferOverview] = None
        self._worklist_complete_callbacks = []
    
    def add_source_plate(self, plate: Plate):
        """Add a source plate to the worklist"""
        self.source_plates.append(plate)
    
    def add_destination_plate(self, plate: Plate):
        """Add a destination plate to the worklist"""
        self.destination_plates.append(plate)
    
    def on_worklist_complete(self):
        """Called when worklist is completed"""
        for callback in self._worklist_complete_callbacks:
            callback(self)
    
    def add_completion_callback(self, callback):
        """Add a callback to be called when worklist completes"""
        self._worklist_complete_callbacks.append(callback)
    
    def __repr__(self):
        return f"Worklist(name={self.name}, {len(self.source_plates)} sources, {len(self.destination_plates)} destinations)"


def create_worklist_from_transfer_overview(name: str, transfer_overview: TransferOverview) -> Worklist:
    """
    Create a worklist from a transfer overview.
    
    This is similar to WorksetSequencer.DetermineSequence in the C# code.
    """
    worklist = Worklist(name)
    worklist.transfer_overview = transfer_overview
    
    # Add source plates
    for plate in transfer_overview.source_plates.values():
        worklist.add_source_plate(plate)
    
    # Add destination plates
    for plate in transfer_overview.destination_plates.values():
        worklist.add_destination_plate(plate)
    
    return worklist



