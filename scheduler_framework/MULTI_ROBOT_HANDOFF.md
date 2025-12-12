# Multi-Robot Handoff and Wait Operations

This document describes the multi-robot coordination features added to the scheduler framework, including plate handoffs between robots and wait/timing operations.

## Overview

The scheduler framework now supports:
1. **WaitTask**: Tasks that wait for a specified duration
2. **HandoffLocation**: Special locations where robots can transfer plates to each other
3. **Multi-Robot Coordination**: Coordinated workflows involving multiple robots

## WaitTask

`WaitTask` allows you to insert delays into workflows. This is useful for:
- Incubation periods
- Processing delays
- Synchronization between robots
- Time-based operations

### Usage

```python
from scheduler import WaitTask

# Create a wait task for 10 seconds
wait_task = WaitTask(duration_seconds=10.0, description="Wait at processing area")

# Add to a plate's todo list
active_plate.todo_list.append(wait_task)
```

The scheduler automatically handles wait tasks by:
1. Marking the plate as busy
2. Starting a background thread to wait for the specified duration
3. Marking the task as complete and advancing to the next task

## HandoffLocation

`HandoffLocation` is a special type of `PlateLocation` designed for robot-to-robot plate transfers. It tracks:
- Which robots can access the location
- Handoff state (free, waiting for pickup, occupied)
- Which robot placed the plate and which robot should pick it up

### Creating a HandoffLocation

```python
from scheduler.handoff_location import HandoffLocation

handoff = HandoffLocation(
    name="HandoffStation",
    device_name="HandoffDevice",
    accessible_by_robots=["PF400", "Planar"]
)
```

### Handoff States

- **free**: No plate at the location
- **waiting_for_pickup**: A robot has placed a plate and is waiting for another robot to pick it up
- **occupied**: A plate is at the location (can be either waiting or being processed)

### Methods

- `can_robot_access(robot_name)`: Check if a robot can access this location
- `mark_waiting_for_pickup(robot_name)`: Mark that a robot has placed a plate
- `mark_picked_up(robot_name)`: Mark that a robot has picked up the plate
- `is_waiting_for_pickup()`: Check if a plate is waiting to be picked up

## Multi-Robot Workflow Example

The following example demonstrates a complete workflow:
1. PF400 places a plate at a handoff location
2. Planar picks up the plate from the handoff location
3. Planar moves the plate to a processing area
4. Wait 10 seconds
5. Planar moves the plate back to the handoff location
6. PF400 picks up the plate from the handoff location

See `examples/multi_robot_handoff.py` for the complete implementation.

### Key Components

1. **HandoffDevice**: A device that hosts a handoff location
2. **PF400Robot**: Robot that can place/pick up plates at handoff locations
3. **PlanarRobot**: Robot that can pick up/place plates at handoff locations
4. **PlanarProcessingArea**: Device representing where the Planar robot processes plates

### Workflow Tasks

```python
workflow_tasks = [
    PlateTask("HandoffDevice", "place_at_handoff", {}),  # PF400 places
    PlateTask("PlanarProcessingArea", "move_to_processing", {}),  # Planar picks up and moves
    WaitTask(10.0, "Wait at processing area"),  # Wait 10 seconds
    PlateTask("HandoffDevice", "return_to_handoff", {}),  # Planar returns
    # PF400 picks up (handled automatically by scheduler)
]
```

## Robot Transfer Implementation

Robots must implement the `RobotInterface` and handle handoff locations specially:

```python
class MyRobot(RobotInterface):
    def transfer_plate(self, src_device: str, src_place: str,
                      dst_device: str, dst_place: str,
                      labware_name: str, barcode: str):
        # Perform the physical transfer
        # ...
        
        # Update handoff state if transferring to/from handoff location
        if "handoff" in dst_device.lower():
            device = self._get_device(dst_device)
            if device and hasattr(device, 'handoff_location'):
                device.handoff_location.mark_waiting_for_pickup(self.name)
        elif "handoff" in src_device.lower():
            device = self._get_device(src_device)
            if device and hasattr(device, 'handoff_location'):
                device.handoff_location.mark_picked_up(self.name)
```

## Path Planning with Handoffs

The path planner automatically handles handoff locations. When planning a path:
1. It identifies handoff locations accessible by both source and destination robots
2. It includes handoff locations in the path when direct transfer is not possible
3. It ensures proper sequencing of robot operations

## Best Practices

1. **Handoff Location Design**: Place handoff locations in positions accessible by all required robots
2. **State Management**: Always update handoff state when robots place/pick up plates
3. **Error Handling**: Check handoff state before operations to avoid conflicts
4. **Timing**: Use WaitTask for operations that require specific durations
5. **Logging**: Log all handoff operations for debugging and audit trails

## Integration with Existing Scheduler

The new features integrate seamlessly with the existing scheduler:
- WaitTask is handled automatically by PlateScheduler
- HandoffLocation extends PlateLocation, so it works with existing path planning
- Multi-robot coordination is handled by RobotScheduler through path planning

## Future Enhancements

Potential future improvements:
- Handoff location reservation system
- Automatic handoff location discovery
- Multi-plate handoff queues
- Handoff timeout and error recovery
- Visual handoff state monitoring

