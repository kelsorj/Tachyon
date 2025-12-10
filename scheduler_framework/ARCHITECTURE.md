# Architecture Documentation

## Overview

This Python scheduler framework is based on the C# PlateScheduler architecture found in `trunk/vs2010/PlateScheduler`. It provides a modern, extensible framework for scheduling laboratory automation workflows.

## Key Components

### 1. PlateScheduler
**Location**: `scheduler/plate_scheduler.py`

The main orchestrator that:
- Manages worklist queues
- Creates and tracks ActivePlates
- Schedules tasks on available devices
- Coordinates with RobotScheduler for plate movements

**Key Methods**:
- `enqueue_worklist(worklist)`: Add a worklist to process
- `start_scheduler()`: Start the scheduler thread
- `stop_scheduler()`: Stop the scheduler thread

### 2. RobotScheduler
**Location**: `scheduler/robot_scheduler.py`

Manages robot movement operations:
- Queues plate movement jobs
- Uses PathPlanner to find optimal routes
- Executes robot transfers between locations

**Key Methods**:
- `add_job(active_plate)`: Queue a plate movement
- `start_scheduler()`: Start the robot scheduler thread

### 3. PathPlanner
**Location**: `scheduler/path_planner.py`

Graph-based path finding engine:
- Builds a graph of all plate locations
- Uses Dijkstra's algorithm for shortest path finding
- Considers robot capabilities and transfer weights

**Key Methods**:
- `create_world()`: Build the location graph
- `plan_path(src, dst)`: Find optimal path between locations

### 4. ActivePlate
**Location**: `scheduler/active_plate.py`

Represents a plate being actively processed:
- Tracks current and destination locations
- Maintains a task list (ToDo items)
- Manages plate state (busy/free)

**Subclasses**:
- `ActiveSourcePlate`: Source plates being processed
- `ActiveDestinationPlate`: Destination plates being processed

### 5. Worklist
**Location**: `scheduler/worklist.py`

Defines a batch of work:
- Contains source and destination plates
- Defines transfer operations
- Tracks completion status

**Related Classes**:
- `PlateTask`: Individual task to perform
- `TransferOverview`: Overview of all transfers
- `Transfer`: Single transfer operation

### 6. DeviceManager
**Location**: `scheduler/device_manager.py`

Manages all devices in the system:
- Registers devices and robots
- Provides device lookup by type
- Manages device interfaces

**Key Interfaces**:
- `DeviceInterface`: Base interface for all devices
- `PlateSchedulerDeviceInterface`: Devices that can be scheduled
- `AccessibleDeviceInterface`: Devices accessible by robots
- `RobotInterface`: Robot devices

## Data Flow

1. **Worklist Creation**: User creates a Worklist with plates and transfers
2. **Enqueue**: Worklist is enqueued to PlateScheduler
3. **ActivePlate Creation**: PlateScheduler creates ActivePlates via factories
4. **Task Scheduling**: For each ActivePlate's current task:
   - Find available device of required type
   - Reserve location on device
   - Queue robot movement (if needed)
   - Queue device job
5. **Robot Movement**: RobotScheduler plans path and executes transfer
6. **Device Processing**: Device processes the plate
7. **Task Completion**: Device marks job complete, ActivePlate advances
8. **Worklist Completion**: When all plates are finished, worklist completes

## Threading Model

The framework uses Python threads (similar to C# threads):

- **PlateScheduler Thread**: Main scheduling loop
- **RobotScheduler Thread**: Robot movement execution

Both threads run continuously, polling queues and processing jobs.

## Extensibility

### Adding a New Device

1. Create a class implementing `PlateSchedulerDeviceInterface` and `AccessibleDeviceInterface`
2. Implement required methods:
   - `get_available_location()`
   - `reserve_location()`
   - `add_job()`
   - `lock_place()`
3. Register with DeviceManager

### Adding a New Robot

1. Create a class implementing `RobotInterface`
2. Implement:
   - `transfer_plate()`: Execute the transfer
   - `get_transfer_weight()`: Calculate transfer cost
3. Register with DeviceManager

## Differences from C# Version

1. **Threading**: Uses Python `threading` instead of C# `Thread`
2. **Type System**: Uses Python type hints instead of C# types
3. **Collections**: Uses Python lists/dicts instead of C# collections
4. **Events**: Uses `threading.Event` instead of `ManualResetEvent`
5. **Queues**: Uses `queue.Queue` instead of `ConcurrentQueue`

## Future Enhancements

- Async/await support for better concurrency
- Database persistence for worklists
- Web API for remote control
- Real-time monitoring dashboard
- Priority-based scheduling
- Resource conflict resolution
- Retry mechanisms for failed operations


