"""
RobotScheduler - Manages robot movement jobs.

Handles the queue of plate movements and executes them using
the PathPlanner to find optimal routes.
"""

import threading
import time
import logging
from queue import Queue
from typing import Optional
from .active_plate import ActivePlate
from .device_manager import DeviceManager
from .path_planner import PathPlanner

logger = logging.getLogger(__name__)


class RobotScheduler:
    """
    Schedules and executes robot movements for plate transfers.
    
    Similar to the C# RobotScheduler, this manages a queue of
    movement jobs and processes them using path planning.
    """
    
    def __init__(self, device_manager: DeviceManager):
        self.device_manager = device_manager
        self.pending_jobs: Queue[ActivePlate] = Queue()
        self.scheduler_thread: Optional[threading.Thread] = None
        self.stop_event = threading.Event()
        self.path_planner = PathPlanner(device_manager)
        
        # Events for move plate operations
        self.entering_move_plate_callbacks = []
        self.exiting_move_plate_callbacks = []
    
    def start_scheduler(self):
        """Start the robot scheduler thread"""
        if self.scheduler_thread and self.scheduler_thread.is_alive():
            return
        
        self.stop_event.clear()
        self.scheduler_thread = threading.Thread(
            target=self._scheduler_thread_runner,
            name="RobotScheduler",
            daemon=True
        )
        self.scheduler_thread.start()
        logger.info("RobotScheduler started")
    
    def stop_scheduler(self):
        """Stop the robot scheduler thread"""
        self.stop_event.set()
        if self.scheduler_thread:
            self.scheduler_thread.join(timeout=5.0)
        logger.info("RobotScheduler stopped")
    
    def add_job(self, active_plate: ActivePlate):
        """Add a plate movement job to the queue"""
        self.pending_jobs.put(active_plate)
        logger.debug(f"Added robot job for {active_plate}")
    
    def _scheduler_thread_runner(self):
        """Main scheduler loop - processes movement jobs"""
        while not self.stop_event.is_set():
            try:
                # Try to get a job (with timeout to allow checking stop_event)
                try:
                    active_plate = self.pending_jobs.get(timeout=0.1)
                except:
                    continue
                
                # Plan and execute the move
                self._move_plate(active_plate)
                
            except Exception as e:
                logger.error(f"Error in robot scheduler: {e}", exc_info=True)
                time.sleep(0.1)
    
    def _move_plate(self, active_plate: ActivePlate):
        """
        Move a plate from its current location to destination.
        
        Uses PathPlanner to find the optimal route and executes
        the transfers using the appropriate robots.
        """
        if not active_plate.current_location or not active_plate.destination_location:
            logger.warning(f"Cannot move {active_plate}: missing location info")
            return
        
        if active_plate.current_location == active_plate.destination_location:
            logger.warning(f"Plate {active_plate} is already at destination")
            return
        
        logger.debug(f"Moving {active_plate} from {active_plate.current_location.name} "
                    f"to {active_plate.destination_location.name}")
        
        # Rebuild world (in case devices/locations changed)
        self.path_planner.create_world()
        
        # Plan path
        path = self.path_planner.plan_path(
            active_plate.current_location,
            active_plate.destination_location
        )
        
        if not path:
            logger.warning(f"No path found for {active_plate} from "
                          f"{active_plate.current_location.name} to "
                          f"{active_plate.destination_location.name}")
            return
        
        # Execute the path
        self._execute_path(active_plate, path)
        
        # Update plate location
        active_plate.current_location.occupied.clear()
        active_plate.destination_location.occupied.set()
        active_plate.current_location = active_plate.destination_location
    
    def _execute_path(self, active_plate: ActivePlate, path: list):
        """
        Execute a planned path by performing transfers between nodes.
        
        Each step in the path represents a transfer that needs to be
        performed by a robot.
        """
        if len(path) < 2:
            return
        
        current_node = path[0]
        
        for next_node in path[1:]:
            # Find the connection and robot
            connection = None
            for conn in next_node.connections:
                if conn.node == current_node:
                    connection = conn
                    break
            
            if not connection:
                logger.error(f"No connection found between nodes")
                return
            
            robot = connection.extra_info
            if not robot:
                logger.error(f"No robot found for connection")
                return
            
                # Get locations and devices
                # current_node.key is now a place key string like "location:place"
                current_place_key = current_node.key
                next_place_key = next_node.key
                
                current_location = self.path_planner.world_places.get(current_place_key)
                next_location = self.path_planner.world_places.get(next_place_key)
                current_place = self.path_planner.world_place_objects.get(current_place_key)
                next_place = self.path_planner.world_place_objects.get(next_place_key)
                
                if not current_location or not next_location or not current_place or not next_place:
                    logger.error(f"Missing location info for places")
                    return
                
                # Use location name as key since world_locations now uses names
                current_device = self.path_planner.world_locations.get(current_location.name)
                next_device = self.path_planner.world_locations.get(next_location.name)
                
                if not current_device or not next_device:
                    logger.error(f"Missing device info for locations")
                    return
                
                # Only transfer if locations are different
                if current_location != next_location:
                    # Lock places
                    if hasattr(current_device, 'lock_place'):
                        current_device.lock_place(current_place.name)
                    if hasattr(next_device, 'lock_place'):
                        next_device.lock_place(next_place.name)
                
                # Notify callbacks
                for callback in self.entering_move_plate_callbacks:
                    callback()
                
                # Perform transfer
                try:
                    robot.transfer_plate(
                        current_device.name,
                        current_place.name,
                        next_device.name,
                        next_place.name,
                        active_plate.labware_name,
                        active_plate.barcode
                    )
                except Exception as e:
                    logger.error(f"Error during transfer: {e}", exc_info=True)
                
                # Notify callbacks
                for callback in self.exiting_move_plate_callbacks:
                    callback()
            
            current_node = next_node
    
    def get_status(self) -> str:
        """Get status string for monitoring"""
        if self.pending_jobs.empty():
            return "RobotScheduler has no pending jobs"
        
        status = f"RobotScheduler has {self.pending_jobs.qsize()} pending jobs:\n"
        # Note: We can't easily iterate Queue without consuming items
        # In production, you might want a separate list for status
        return status
    
    def add_entering_move_plate_callback(self, callback):
        """Add callback to be called when entering move plate operation"""
        self.entering_move_plate_callbacks.append(callback)
    
    def add_exiting_move_plate_callback(self, callback):
        """Add callback to be called when exiting move plate operation"""
        self.exiting_move_plate_callbacks.append(callback)

