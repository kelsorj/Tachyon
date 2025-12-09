"""
Basic usage example of the scheduler framework.

This demonstrates how to:
1. Set up devices and robots
2. Create a worklist
3. Run the scheduler
"""

import logging
import time
from scheduler import (
    PlateScheduler, RobotScheduler, DeviceManager,
    Worklist, PlateTask, TransferOverview, Transfer,
    Plate, PlateLocation, PlatePlace
)
from scheduler.device_manager import (
    DeviceInterface, PlateSchedulerDeviceInterface,
    AccessibleDeviceInterface, RobotInterface
)

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)

logger = logging.getLogger(__name__)


# Example device implementations
class ExampleDevice(PlateSchedulerDeviceInterface, AccessibleDeviceInterface):
    """Example device that can process plates"""
    
    def __init__(self, name: str, product_name: str):
        super().__init__(name, product_name)
        self.locations: List[PlateLocation] = []
        self.job_queue = []
    
    def add_location(self, location: PlateLocation):
        """Add a plate location to this device"""
        self.locations.append(location)
    
    @property
    def plate_location_info(self):
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
        """Lock a place (simplified)"""
        pass
    
    def add_job(self, active_plate):
        """Add a job to process"""
        logger.info(f"{self.name} received job for {active_plate}")
        self.job_queue.append(active_plate)
        # In real implementation, this would trigger device processing
        # For now, we'll simulate completion
        time.sleep(0.1)  # Simulate processing time
        active_plate.mark_job_completed()


class ExampleRobot(RobotInterface):
    """Example robot that can transfer plates"""
    
    def transfer_plate(self, src_device: str, src_place: str,
                      dst_device: str, dst_place: str,
                      labware_name: str, barcode: str):
        """Transfer a plate"""
        logger.info(f"{self.name} transferring {barcode} from "
                   f"{src_device}.{src_place} to {dst_device}.{dst_place}")
        time.sleep(0.1)  # Simulate transfer time
    
    def get_transfer_weight(self, src_device, src_location, src_place,
                           dst_device, dst_location, dst_place) -> float:
        """Calculate transfer weight/cost"""
        # Simple: return 1.0 if transfer is possible, inf if not
        # In real implementation, this would consider distance, robot capabilities, etc.
        if src_location == dst_location:
            return float('inf')  # Can't transfer to same location
        return 1.0


def create_example_setup():
    """Create a basic setup with devices and robots"""
    device_manager = DeviceManager()
    
    # Create a robot
    robot = ExampleRobot("Robot1")
    device_manager.register_robot(robot)
    
    # Create some devices
    source_device = ExampleDevice("SourceStacker", "Stacker")
    dest_device = ExampleDevice("DestStacker", "Stacker")
    processor = ExampleDevice("Processor", "Processor")
    
    # Add locations to devices
    for i in range(3):
        loc = PlateLocation(f"SourceLoc{i}", source_device.name)
        # Add a place to the location
        place = PlatePlace(f"SourcePlace{i}", loc)
        loc.places = [place]
        source_device.add_location(loc)
    
    for i in range(3):
        loc = PlateLocation(f"DestLoc{i}", dest_device.name)
        # Add a place to the location
        place = PlatePlace(f"DestPlace{i}", loc)
        loc.places = [place]
        dest_device.add_location(loc)
    
    proc_loc = PlateLocation("ProcLoc", processor.name)
    proc_place = PlatePlace("ProcPlace", proc_loc)
    proc_loc.places = [proc_place]
    processor.add_location(proc_loc)
    
    # Register devices
    device_manager.register_device(source_device)
    device_manager.register_device(dest_device)
    device_manager.register_device(processor)
    
    return device_manager


def create_example_worklist():
    """Create an example worklist"""
    # Create plates
    source_plate = Plate("SRC001", "96-well", "96-well")
    dest_plate = Plate("DST001", "96-well", "96-well")
    
    # Create transfer overview
    transfer = Transfer(source_plate, dest_plate, volume=10.0)
    overview = TransferOverview(
        transfers=[transfer],
        source_plates={"SRC001": source_plate},
        destination_plates={"DST001": dest_plate}
    )
    
    # Add some tasks
    overview.tasks.source_prehitpick_tasks = [
        PlateTask("Processor", "preprocess")
    ]
    overview.tasks.destination_prehitpick_tasks = [
        PlateTask("Processor", "prepare")
    ]
    
    # Create worklist
    worklist = Worklist("ExampleWorklist")
    worklist.transfer_overview = overview
    worklist.add_source_plate(source_plate)
    worklist.add_destination_plate(dest_plate)
    
    return worklist


def main():
    """Main example"""
    logger.info("Setting up scheduler...")
    
    # Create device manager
    device_manager = create_example_setup()
    
    # Create schedulers
    robot_scheduler = RobotScheduler(device_manager)
    plate_scheduler = PlateScheduler(robot_scheduler, device_manager)
    
    # Start schedulers
    robot_scheduler.start_scheduler()
    plate_scheduler.start_scheduler()
    
    # Create and enqueue worklist
    worklist = create_example_worklist()
    plate_scheduler.enqueue_worklist(worklist)
    
    # Wait for worklist to complete
    logger.info("Waiting for worklist to complete...")
    time.sleep(5)
    
    # Print status
    logger.info("Scheduler status:")
    logger.info(plate_scheduler.get_status())
    
    # Stop schedulers
    plate_scheduler.stop_scheduler()
    robot_scheduler.stop_scheduler()
    
    logger.info("Example completed")


if __name__ == "__main__":
    main()

