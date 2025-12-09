# Quick Start Guide

## Installation

The scheduler framework uses only Python standard library, so no external dependencies are required.

```bash
cd scheduler_framework
# No pip install needed!
```

## Basic Usage

### 1. Set Up Devices and Robots

```python
from scheduler import DeviceManager, PlateLocation, PlatePlace
from scheduler.device_manager import (
    PlateSchedulerDeviceInterface, AccessibleDeviceInterface, RobotInterface
)

# Create device manager
device_manager = DeviceManager()

# Create and register a robot
class MyRobot(RobotInterface):
    def transfer_plate(self, src_device, src_place, dst_device, dst_place, labware, barcode):
        print(f"Transferring {barcode}...")
        # Your robot control code here
    
    def get_transfer_weight(self, src_device, src_location, src_place,
                           dst_device, dst_location, dst_place):
        return 1.0  # Simple cost model

robot = MyRobot("Robot1")
device_manager.register_robot(robot)

# Create and register devices
# (See examples/basic_usage.py for full device implementation)
```

### 2. Create a Worklist

```python
from scheduler import Worklist, Plate, TransferOverview, Transfer, PlateTask

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

# Add tasks
overview.tasks.source_prehitpick_tasks = [
    PlateTask("Processor", "preprocess")
]

# Create worklist
worklist = Worklist("MyWorklist")
worklist.transfer_overview = overview
worklist.add_source_plate(source_plate)
worklist.add_destination_plate(dest_plate)
```

### 3. Run the Scheduler

```python
from scheduler import PlateScheduler, RobotScheduler

# Create schedulers
robot_scheduler = RobotScheduler(device_manager)
plate_scheduler = PlateScheduler(robot_scheduler, device_manager)

# Start schedulers
robot_scheduler.start_scheduler()
plate_scheduler.start_scheduler()

# Enqueue worklist
plate_scheduler.enqueue_worklist(worklist)

# Wait for completion (or use callbacks)
import time
time.sleep(10)

# Check status
print(plate_scheduler.get_status())

# Stop schedulers
plate_scheduler.stop_scheduler()
robot_scheduler.stop_scheduler()
```

## Key Concepts

### ActivePlates

ActivePlates are created automatically by the scheduler from worklists. They track:
- Current location
- Destination location
- Task list
- State (busy/free)

### Tasks

Tasks define operations to perform:
- `device_type`: Type of device needed (e.g., "Processor", "Reader")
- `command`: Command to execute (e.g., "read", "wash")
- `parameters`: Optional parameters

### Locations and Places

- **PlateLocation**: A location on a device (e.g., "Stacker1_Slot1")
- **PlatePlace**: A specific place within a location (for path planning)

### Path Planning

The PathPlanner automatically finds optimal routes for robot movements using Dijkstra's algorithm.

## Example: Complete Workflow

See `examples/basic_usage.py` for a complete working example.

## Next Steps

1. Implement your own devices by extending `PlateSchedulerDeviceInterface`
2. Implement your own robots by extending `RobotInterface`
3. Customize task scheduling logic in `PlateScheduler._schedule_task()`
4. Add monitoring/logging for production use

