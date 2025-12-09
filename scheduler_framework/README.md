# Laboratory Automation Scheduler Framework

This is a Python port/adaptation of the C# PlateScheduler architecture found in the `trunk/vs2010/PlateScheduler` directory.

## Architecture Overview

The original C# scheduler implements a sophisticated multi-threaded system for managing laboratory automation workflows:

### Core Components

1. **PlateScheduler** - Main orchestrator that:
   - Manages worklists (queues of work to be done)
   - Creates and tracks ActivePlates (plates with associated tasks)
   - Schedules tasks on available devices
   - Coordinates with RobotScheduler for plate movements

2. **RobotScheduler** - Handles robot movement:
   - Manages a queue of plate movement jobs
   - Uses PathPlanner to find optimal routes
   - Executes robot transfers between locations

3. **PathPlanner** - Path finding engine:
   - Builds a graph of all plate locations and connections
   - Uses Dijkstra's algorithm to find shortest paths
   - Considers robot capabilities and transfer weights

4. **ActivePlate** - Represents a plate in the system:
   - Tracks current and destination locations
   - Maintains a ToDo list of tasks
   - Manages plate state (busy/free, completed tasks)

5. **Worklist** - Defines a batch of work:
   - Contains source and destination plates
   - Defines transfer operations
   - Tracks completion status

### Key Design Patterns

- **Factory Pattern**: ActivePlateFactory creates different types of active plates
- **Thread-based Execution**: Separate threads for plate scheduling and robot scheduling
- **Concurrent Queues**: Thread-safe job queues
- **Device Abstraction**: Devices implement interfaces for scheduling compliance
- **Path Finding**: Graph-based routing for optimal robot movements

## Python Implementation

The Python version modernizes this architecture with:
- Async/await instead of threads
- Type hints for better code clarity
- Modern Python patterns (dataclasses, enums, etc.)
- Extensible plugin architecture
- Better error handling and logging

