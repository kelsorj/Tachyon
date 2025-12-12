"""
Example: Multi-Robot Plate Handoff Workflow

This example demonstrates:
1. PF400 robot places a plate at a handoff location
2. Planar robot picks up the plate
3. Planar robot moves to another location
4. Wait 10 seconds
5. Planar robot moves back to handoff location
6. PF400 robot picks up the plate

This workflow shows how the scheduler coordinates multiple robots
for complex plate transfer operations.
"""

import time
import logging
import threading
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

from scheduler import (
    DeviceManager, PlateScheduler, RobotScheduler,
    Worklist, PlateTask, WaitTask
)
from scheduler.active_plate import Plate, ActivePlate, PlateLocation, PlatePlace
from scheduler.handoff_location import HandoffLocation
from scheduler.device_manager import (
    DeviceInterface, PlateSchedulerDeviceInterface, 
    AccessibleDeviceInterface, RobotInterface
)

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class HandoffDevice(PlateSchedulerDeviceInterface, AccessibleDeviceInterface):
    """Device representing a handoff location between robots"""
    
    def __init__(self, name: str):
        super().__init__(name, "HandoffDevice")
        self.handoff_location = HandoffLocation(
            f"{name}_handoff",
            name,
            accessible_by_robots=["PF400", "Planar"]
        )
        self._lock = threading.Lock()
    
    def get_available_location(self, active_plate):
        """Get the handoff location if it's available"""
        with self._lock:
            if not self.handoff_location.occupied.is_set():
                return self.handoff_location
        return None
    
    def reserve_location(self, location, active_plate):
        """Reserve the handoff location"""
        with self._lock:
            if location.reserved.is_set():
                return False
            location.reserved.set()
            return True
    
    def add_job(self, active_plate):
        """Process a job - just mark as complete since handoff is handled by robots"""
        logger.info(f"HandoffDevice {self.name}: Plate {active_plate.barcode} at handoff location")
        # The actual handoff is done by the robots via transfer_plate
        # This job just represents the plate being at the handoff location
        time.sleep(0.1)  # Small delay to simulate processing
        active_plate.mark_job_completed()
    
    def lock_place(self, place_name: str):
        """Lock a place on the device"""
        pass  # Handoff locations don't need place locking
    
    @property
    def plate_location_info(self):
        """Get all plate locations"""
        return [self.handoff_location]


class PF400Robot(RobotInterface):
    """PF400 robot implementation"""
    
    def __init__(self, name: str = "PF400"):
        super().__init__(name)
        self.transfer_history = []
    
    def transfer_plate(self, src_device: str, src_place: str,
                      dst_device: str, dst_place: str,
                      labware_name: str, barcode: str):
        """Transfer a plate"""
        logger.info(f"{self.name}: Transferring {barcode} from {src_device}.{src_place} to {dst_device}.{dst_place}")
        self.transfer_history.append({
            'src_device': src_device,
            'src_place': src_place,
            'dst_device': dst_device,
            'dst_place': dst_place,
            'barcode': barcode
        })
        
        # Simulate transfer time
        time.sleep(0.5)
        
        # If transferring to/from handoff location, update handoff state
        if "handoff" in dst_device.lower():
            device = self._get_device(dst_device)
            if device and hasattr(device, 'handoff_location'):
                device.handoff_location.mark_waiting_for_pickup(self.name)
        elif "handoff" in src_device.lower():
            device = self._get_device(src_device)
            if device and hasattr(device, 'handoff_location'):
                device.handoff_location.mark_picked_up(self.name)
    
    def get_transfer_weight(self, src_device, src_location, src_place,
                           dst_device, dst_location, dst_place) -> float:
        """Calculate transfer weight"""
        # PF400 can reach handoff locations and its own storage
        if "handoff" in dst_device.lower() or "handoff" in src_device.lower():
            return 1.0
        if "pf400" in dst_device.lower() or "pf400" in src_device.lower():
            return 1.0
        return float('inf')  # Cannot reach other locations
    
    def _get_device(self, device_name):
        """Helper to get device (would be injected in real implementation)"""
        return None  # Placeholder


class PlanarRobot(RobotInterface):
    """Planar motor robot implementation"""
    
    def __init__(self, name: str = "Planar"):
        super().__init__(name)
        self.transfer_history = []
    
    def transfer_plate(self, src_device: str, src_place: str,
                      dst_device: str, dst_place: str,
                      labware_name: str, barcode: str):
        """Transfer a plate"""
        logger.info(f"{self.name}: Transferring {barcode} from {src_device}.{src_place} to {dst_device}.{dst_place}")
        self.transfer_history.append({
            'src_device': src_device,
            'src_place': src_place,
            'dst_device': dst_device,
            'dst_place': dst_place,
            'barcode': barcode
        })
        
        # Simulate transfer time
        time.sleep(0.5)
        
        # If transferring to/from handoff location, update handoff state
        if "handoff" in src_device.lower():
            device = self._get_device(src_device)
            if device and hasattr(device, 'handoff_location'):
                device.handoff_location.mark_picked_up(self.name)
        elif "handoff" in dst_device.lower():
            device = self._get_device(dst_device)
            if device and hasattr(device, 'handoff_location'):
                device.handoff_location.mark_waiting_for_pickup(self.name)
    
    def get_transfer_weight(self, src_device, src_location, src_place,
                           dst_device, dst_location, dst_place) -> float:
        """Calculate transfer weight"""
        # Planar can reach handoff locations and its own processing area
        if "handoff" in dst_device.lower() or "handoff" in src_device.lower():
            return 1.0
        if "planar" in dst_device.lower() or "planar" in src_device.lower():
            return 1.0
        return float('inf')  # Cannot reach other locations
    
    def _get_device(self, device_name):
        """Helper to get device (would be injected in real implementation)"""
        return None  # Placeholder


class PlanarProcessingArea(PlateSchedulerDeviceInterface, AccessibleDeviceInterface):
    """Device representing Planar robot's processing area"""
    
    def __init__(self, name: str):
        super().__init__(name, "PlanarProcessingArea")
        self.location = PlateLocation(f"{name}_location", name)
        self.location.places = [PlatePlace(f"{name}_place", self.location)]
        self._lock = threading.Lock()
    
    def get_available_location(self, active_plate):
        """Get available location"""
        with self._lock:
            if not self.location.occupied.is_set():
                return self.location
        return None
    
    def reserve_location(self, location, active_plate):
        """Reserve location"""
        with self._lock:
            if location.reserved.is_set():
                return False
            location.reserved.set()
            return True
    
    def add_job(self, active_plate):
        """Process a job - plate is at processing area"""
        logger.info(f"PlanarProcessingArea {self.name}: Plate {active_plate.barcode} at processing area")
        # This represents the plate being at the processing area
        # The actual processing would happen here
        time.sleep(0.1)
        active_plate.mark_job_completed()
    
    def lock_place(self, place_name: str):
        """Lock a place"""
        pass
    
    @property
    def plate_location_info(self):
        """Get all plate locations"""
        return [self.location]


def create_multi_robot_workflow():
    """Create the multi-robot handoff workflow"""
    
    # Create devices and robots
    device_manager = DeviceManager()
    
    # Create handoff device
    handoff_device = HandoffDevice("HandoffStation")
    device_manager.register_device(handoff_device)
    
    # Create Planar processing area
    planar_area = PlanarProcessingArea("PlanarArea")
    device_manager.register_device(planar_area)
    
    # Create robots
    pf400 = PF400Robot("PF400")
    planar = PlanarRobot("Planar")
    device_manager.register_robot(pf400)
    device_manager.register_robot(planar)
    
    # Create worklist with handoff workflow
    worklist = Worklist("MultiRobotHandoff")
    
    # Create a plate
    plate = Plate("PLATE001", "96-well", "96-well")
    
    # Create tasks for the workflow:
    # 1. PF400 places plate at handoff location (handoff device)
    # 2. Planar picks up from handoff and moves to processing area
    # 3. Wait 10 seconds at processing area
    # 4. Planar moves back to handoff location
    # 5. PF400 picks up from handoff
    
    # For this example, we'll create a destination plate that goes through this workflow
    # The tasks will be:
    # - Task 1: Handoff device (PF400 places plate)
    # - Task 2: Planar processing area (Planar picks up and moves there)
    # - Task 3: Wait 10 seconds
    # - Task 4: Handoff device (Planar moves back)
    # - Task 5: Complete (PF400 picks up)
    
    worklist.add_destination_plate(plate)
    
    # Create schedulers
    robot_scheduler = RobotScheduler(device_manager)
    plate_scheduler = PlateScheduler(robot_scheduler, device_manager)
    
    # We need to manually create the task sequence for this workflow
    # In a real implementation, this would be generated from a worklist definition
    # For now, we'll create a custom ActivePlate with the workflow tasks
    
    class HandoffWorkflowPlate(ActivePlate):
        """Custom ActivePlate for handoff workflow"""
        
        def __init__(self, worklist, plate):
            super().__init__(worklist, 0)
            self.plate = plate
            
            # Create task sequence
            self.todo_list = [
                PlateTask("HandoffDevice", "place_at_handoff", {}),
                PlateTask("PlanarProcessingArea", "move_to_processing", {}),
                WaitTask(10.0, "Wait at processing area"),
                PlateTask("HandoffDevice", "return_to_handoff", {}),
            ]
            self.still_have_todos = len(self.todo_list) > 0
    
    workflow_plate = HandoffWorkflowPlate(worklist, plate)
    ActivePlate._active_plates.append(workflow_plate)
    
    # Start schedulers
    robot_scheduler.start_scheduler()
    plate_scheduler.start_scheduler()
    
    logger.info("Multi-robot handoff workflow started")
    logger.info("Workflow: PF400 → Handoff → Planar → Processing → Wait 10s → Handoff → PF400")
    
    # Wait for workflow to complete
    while not workflow_plate.is_finished():
        time.sleep(0.5)
        status = plate_scheduler.get_status()
        logger.info(f"Status:\n{status}")
    
    logger.info("Workflow completed!")
    
    # Stop schedulers
    plate_scheduler.stop_scheduler()
    robot_scheduler.stop_scheduler()
    
    # Print transfer history
    logger.info("\nPF400 Transfer History:")
    for transfer in pf400.transfer_history:
        logger.info(f"  {transfer}")
    
    logger.info("\nPlanar Transfer History:")
    for transfer in planar.transfer_history:
        logger.info(f"  {transfer}")


if __name__ == "__main__":
    create_multi_robot_workflow()

