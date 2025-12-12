"""
PlateScheduler - Main orchestrator for laboratory automation scheduling.

This is the core scheduler that:
- Manages worklists
- Creates and tracks ActivePlates
- Schedules tasks on available devices
- Coordinates with RobotScheduler for movements
"""

import threading
import time
import logging
from queue import Queue
from typing import List, Dict, Optional
from .worklist import Worklist
from .tasks import WaitTask
from .active_plate import ActivePlate, ActiveSourcePlate, ActiveDestinationPlate, PlateLocation
from .device_manager import DeviceManager, DeviceInterface, PlateSchedulerDeviceInterface
from .robot_scheduler import RobotScheduler
from .active_plate_factory import ActivePlateFactory, ActiveSourcePlateFactory, ActiveDestinationPlateFactory

logger = logging.getLogger(__name__)


class PlateScheduler:
    """
    Main scheduler for laboratory automation.
    
    Based on the C# PlateScheduler, this orchestrates the entire
    workflow of processing plates through various devices.
    """
    
    def __init__(self, robot_scheduler: RobotScheduler, 
                 device_manager: DeviceManager):
        self.robot_scheduler = robot_scheduler
        self.device_manager = device_manager
        self.worklist_queue: Queue[Worklist] = Queue()
        self.scheduler_thread: Optional[threading.Thread] = None
        self.stop_event = threading.Event()
        self.destination_worklist_map: Dict[ActivePlate, str] = {}
        self._lock = threading.Lock()
    
    def start_scheduler(self):
        """Start the plate scheduler thread"""
        if self.scheduler_thread and self.scheduler_thread.is_alive():
            return
        
        self.stop_event.clear()
        self.scheduler_thread = threading.Thread(
            target=self._scheduler_thread_runner,
            name="PlateScheduler",
            daemon=True
        )
        self.scheduler_thread.start()
        logger.info("PlateScheduler started")
    
    def stop_scheduler(self):
        """Stop the plate scheduler thread"""
        self.stop_event.set()
        if self.scheduler_thread and self.scheduler_thread.is_alive():
            self.scheduler_thread.join(timeout=1.0)
            if self.scheduler_thread.is_alive():
                logger.warning("PlateScheduler thread did not stop within timeout")
        logger.info("PlateScheduler stopped")
    
    def enqueue_worklist(self, worklist: Worklist):
        """Add a worklist to the processing queue"""
        self.worklist_queue.put(worklist)
        logger.info(f"Enqueued worklist: {worklist.name}")
    
    def _scheduler_thread_runner(self):
        """Main scheduler loop - processes worklists"""
        while not self.stop_event.is_set():
            try:
                # Try to get a worklist
                try:
                    worklist = self.worklist_queue.get(timeout=0.1)
                except:
                    continue
                
                # Process the worklist
                self._do_worklist(worklist)
                
            except Exception as e:
                logger.error(f"Error in plate scheduler: {e}", exc_info=True)
                time.sleep(0.1)
    
    def _do_worklist(self, worklist: Worklist):
        """
        Process a worklist.
        
        Creates ActivePlates from the worklist and schedules
        their tasks on available devices.
        """
        logger.info(f"Processing worklist: {worklist.name}")
        
        # Create factories for active plates
        active_plate_factories: List[ActivePlateFactory] = [
            ActiveSourcePlateFactory(worklist),
            ActiveDestinationPlateFactory(worklist)
        ]
        
        # Main processing loop
        while True:
            # Check if we're done
            plates_to_create = sum(factory.number_of_plates_to_create 
                                 for factory in active_plate_factories)
            active_plates_count = len(ActivePlate._active_plates)
            
            if plates_to_create == 0 and active_plates_count == 0:
                break
            
            time.sleep(0.05)  # Small delay to prevent CPU spinning
            
            # Remove finished plates
            with self._lock:
                finished_plates = [ap for ap in ActivePlate._active_plates 
                                 if ap.is_finished()]
                for plate in finished_plates:
                    logger.info(f"Removed finished plate: {plate}")
                    ActivePlate._active_plates.remove(plate)
            
            # Release new active plates from factories
            with self._lock:
                for factory in active_plate_factories:
                    active_plate = factory.try_release_active_plate()
                    if active_plate:
                        ActivePlate._active_plates.append(active_plate)
                        logger.debug(f"Released new active plate: {active_plate}")
                        
                        # Track destination plates
                        if isinstance(active_plate, ActiveDestinationPlate):
                            self.destination_worklist_map[active_plate] = worklist.name
            
            # Advance active plates
            with self._lock:
                active_plates = list(ActivePlate._active_plates)
            
            for active_plate in active_plates:
                # Skip if there's a plate with same task and lower instance index
                same_task_plates = [ap for ap in active_plates 
                                  if ap != active_plate and 
                                  ap.get_current_todo() == active_plate.get_current_todo() and
                                  ap.instance_index < active_plate.instance_index]
                if same_task_plates:
                    continue
                
                # Skip if busy
                if active_plate.busy:
                    continue
                
                # Get current task
                current_task = active_plate.get_current_todo()
                if not current_task:
                    continue
                
                # Handle WaitTask specially (no device needed)
                if isinstance(current_task, WaitTask):
                    if not active_plate.busy:
                        # Start wait task
                        self._handle_wait_task(active_plate, current_task)
                    continue
                
                # Find available device for this task
                device_scheduled = self._schedule_task(active_plate, current_task)
                
                if not device_scheduled:
                    logger.debug(f"Could not schedule task {current_task} for {active_plate}")
        
        logger.info(f"Worklist {worklist.name} completed")
        worklist.on_worklist_complete()
    
    def _handle_wait_task(self, active_plate: ActivePlate, wait_task: WaitTask):
        """
        Handle a wait task by waiting for the specified duration.
        
        This runs in a separate thread to avoid blocking the scheduler.
        """
        if wait_task.completed:
            return
        
        logger.info(f"Starting wait task: {wait_task.duration_seconds}s for {active_plate}")
        active_plate.plate_is_free.clear()  # Mark as busy
        
        def wait_and_complete():
            import time
            time.sleep(wait_task.duration_seconds)
            wait_task.completed = True
            logger.info(f"Wait task completed for {active_plate}")
            active_plate.mark_job_completed()
        
        # Run wait in background thread
        wait_thread = threading.Thread(
            target=wait_and_complete,
            name=f"WaitTask-{active_plate.plate_serial_number}",
            daemon=True
        )
        wait_thread.start()
    
    def _schedule_task(self, active_plate: ActivePlate, 
                      task) -> bool:
        """
        Schedule a task on an available device.
        
        Returns True if task was scheduled, False otherwise.
        """
        device_type = task.device_type
        
        # Get devices of the required type
        devices_of_type = self.device_manager.get_devices_by_type(device_type)
        
        if not devices_of_type:
            logger.error(f"No devices of type {device_type} available")
            return False
        
        # Try each device to find an available location
        for device_name, device in devices_of_type.items():
            if not isinstance(device, PlateSchedulerDeviceInterface):
                logger.error(f"Device {device_name} is not plate scheduler compliant")
                continue
            
            # Get available location
            location = device.get_available_location(active_plate)
            if not location:
                continue
            
            # Try to reserve location
            if not device.reserve_location(location, active_plate):
                continue
            
            logger.debug(f"Reserved location {location.name} on {device_name} for {active_plate}")
            
            # Commit the plate to this job
            active_plate.plate_is_free.clear()
            
            if active_plate.current_location is None:
                # First time sourcing the plate
                logger.debug(f"Sourcing plate {active_plate} at device {device_name}")
                active_plate.current_location = location
                active_plate.destination_location = location
            else:
                # Moving plate to new location
                logger.debug(f"Queueing robot job to move {active_plate} to {device_name}")
                active_plate.destination_location = location
                self.robot_scheduler.add_job(active_plate)
            
            # Queue device job
            logger.debug(f"Queueing device job {task.command} on {active_plate}")
            device.add_job(active_plate)
            
            return True
        
        return False
    
    def get_status(self) -> str:
        """Get status string for monitoring"""
        status = []
        
        # Robot scheduler status
        status.append(self.robot_scheduler.get_status())
        
        # Active plates status
        status.append("\tActive plates:")
        with self._lock:
            for plate in ActivePlate._active_plates:
                status.append(plate.get_status())
        
        return "\n".join(status)

