"""
ActivePlateFactory - Factory pattern for creating ActivePlates.

Manages the creation and release of active plates based on
worklist requirements and concurrency limits.
"""

from abc import ABC, abstractmethod
from typing import Type, List
from .worklist import Worklist
from .active_plate import ActivePlate, ActiveSourcePlate, ActiveDestinationPlate


class ActivePlateFactory(ABC):
    """Abstract factory for creating active plates"""
    
    def __init__(self, worklist: Worklist):
        self.worklist = worklist
        self.number_of_plates_to_create = 0
        self.plate_instance_index = 0
        self._set_number_of_plates_to_create()
    
    @abstractmethod
    def get_active_plate_type(self) -> Type[ActivePlate]:
        """Get the type of active plate this factory creates"""
        pass
    
    @abstractmethod
    def _set_number_of_plates_to_create(self):
        """Set how many plates need to be created"""
        pass
    
    @abstractmethod
    def number_of_simultaneous_plates(self) -> int:
        """Get maximum number of plates that can be active simultaneously"""
        pass
    
    def create_active_plate(self) -> ActivePlate:
        """Create a new active plate"""
        if self.number_of_plates_to_create <= 0:
            return None
        
        self.number_of_plates_to_create -= 1
        plate_type = self.get_active_plate_type()
        active_plate = plate_type(self.worklist, self.plate_instance_index)
        self.plate_instance_index += 1
        return active_plate
    
    def release_active_plate(self) -> bool:
        """
        Check if a new active plate can be released.
        
        Returns True if a plate can be released based on concurrency limits.
        """
        active_plates_of_type = [
            ap for ap in ActivePlate._active_plates
            if isinstance(ap, self.get_active_plate_type())
        ]
        return len(active_plates_of_type) < self.number_of_simultaneous_plates()
    
    def try_release_active_plate(self) -> ActivePlate:
        """
        Try to release a new active plate.
        
        Returns the new active plate if one can be released, None otherwise.
        """
        if self.release_active_plate():
            return self.create_active_plate()
        return None


class ActiveSourcePlateFactory(ActivePlateFactory):
    """Factory for creating ActiveSourcePlate instances"""
    
    def get_active_plate_type(self) -> Type[ActivePlate]:
        return ActiveSourcePlate
    
    def _set_number_of_plates_to_create(self):
        """Set number based on unique source plates in transfers"""
        if self.worklist.transfer_overview:
            # Use barcode to identify unique plates since Plate objects aren't hashable
            unique_sources = set(
                transfer.source_plate.barcode 
                for transfer in self.worklist.transfer_overview.transfers
            )
            self.number_of_plates_to_create = len(unique_sources)
        else:
            self.number_of_plates_to_create = len(self.worklist.source_plates)
    
    def number_of_simultaneous_plates(self) -> int:
        """Maximum 3 source plates can be active simultaneously"""
        return 3


class ActiveDestinationPlateFactory(ActivePlateFactory):
    """Factory for creating ActiveDestinationPlate instances"""
    
    def get_active_plate_type(self) -> Type[ActivePlate]:
        return ActiveDestinationPlate
    
    def _set_number_of_plates_to_create(self):
        """Set number based on unique destination plates in transfers"""
        if self.worklist.transfer_overview:
            # Use barcode to identify unique plates since Plate objects aren't hashable
            unique_destinations = set(
                transfer.destination_plate.barcode 
                for transfer in self.worklist.transfer_overview.transfers
            )
            self.number_of_plates_to_create = len(unique_destinations)
        else:
            self.number_of_plates_to_create = len(self.worklist.destination_plates)
    
    def number_of_simultaneous_plates(self) -> int:
        """Maximum 2 destination plates can be active simultaneously"""
        return 2

