# C# to Python Mapping Guide

This document maps the C# PlateScheduler classes to their Python equivalents.

## Core Classes

| C# Class | Python Class | Location |
|----------|--------------|----------|
| `PlateScheduler` | `PlateScheduler` | `scheduler/plate_scheduler.py` |
| `RobotScheduler` | `RobotScheduler` | `scheduler/robot_scheduler.py` |
| `PathPlanner` | `PathPlanner` | `scheduler/path_planner.py` |
| `ActivePlate` | `ActivePlate` | `scheduler/active_plate.py` |
| `ActiveSourcePlate` | `ActiveSourcePlate` | `scheduler/active_plate.py` |
| `ActiveDestinationPlate` | `ActiveDestinationPlate` | `scheduler/active_plate.py` |
| `Worklist` | `Worklist` | `scheduler/worklist.py` |
| `PlateTask` | `PlateTask` | `scheduler/worklist.py` |
| `TransferOverview` | `TransferOverview` | `scheduler/worklist.py` |
| `DeviceManager` | `DeviceManager` | `scheduler/device_manager.py` |

## Interfaces

| C# Interface | Python Abstract Class | Location |
|--------------|----------------------|----------|
| `IPlateScheduler` | `PlateScheduler` (concrete) | `scheduler/plate_scheduler.py` |
| `IRobotScheduler` | `RobotScheduler` (concrete) | `scheduler/robot_scheduler.py` |
| `DeviceInterface` | `DeviceInterface` | `scheduler/device_manager.py` |
| `PlateSchedulerDeviceInterface` | `PlateSchedulerDeviceInterface` | `scheduler/device_manager.py` |
| `AccessibleDeviceInterface` | `AccessibleDeviceInterface` | `scheduler/device_manager.py` |
| `RobotInterface` | `RobotInterface` | `scheduler/device_manager.py` |

## Key Differences

### Threading

**C#:**
```csharp
Thread schedulerThread = new Thread(SchedulerThreadRunner) { IsBackground = true };
schedulerThread.Start();
```

**Python:**
```python
scheduler_thread = threading.Thread(target=self._scheduler_thread_runner, daemon=True)
scheduler_thread.start()
```

### Collections

**C#:**
```csharp
ConcurrentQueue<Worklist> worklistQueue;
Dictionary<ActivePlate, string> destinationWorklistMap;
```

**Python:**
```python
worklist_queue: Queue[Worklist] = Queue()
destination_worklist_map: Dict[ActivePlate, str] = {}
```

### Events

**C#:**
```csharp
ManualResetEvent plateIsFree = new ManualResetEvent(true);
plateIsFree.Set();
plateIsFree.Reset();
bool busy = !plateIsFree.WaitOne(0);
```

**Python:**
```python
plate_is_free = threading.Event()
plate_is_free.set()
plate_is_free.clear()
busy = not plate_is_free.is_set()
```

### Task Iteration

**C#:**
```csharp
IEnumerator<PlateTask> currentToDo;
StillHaveToDos = currentToDo.MoveNext();
PlateTask task = currentToDo.Current;
```

**Python:**
```python
current_task_index: int = 0
self.still_have_todos = self.current_task_index < len(self.todo_list)
task = self.todo_list[self.current_task_index]
```

### Path Finding

**C#:**
```csharp
Djikstra<PlatePlace> finder = new Djikstra<PlatePlace>();
List<Node<PlatePlace>> path = finder.FindShortestPath(srcNode, dstNode);
```

**Python:**
```python
path = self._dijkstra(start_node, end_node)
# Returns List[Node] or None
```

## Method Mappings

### PlateScheduler

| C# Method | Python Method |
|-----------|---------------|
| `StartScheduler()` | `start_scheduler()` |
| `StopScheduler()` | `stop_scheduler()` |
| `EnqueueWorklist(worklist)` | `enqueue_worklist(worklist)` |
| `GetStatus()` | `get_status()` |
| `DoWorklist(worklist)` | `_do_worklist(worklist)` (private) |

### RobotScheduler

| C# Method | Python Method |
|-----------|---------------|
| `StartScheduler()` | `start_scheduler()` |
| `StopScheduler()` | `stop_scheduler()` |
| `AddJob(activePlate)` | `add_job(active_plate)` |
| `GetStatus()` | `get_status()` |
| `RobotSchedulerThreadRunner()` | `_scheduler_thread_runner()` (private) |

### ActivePlate

| C# Property/Method | Python Property/Method |
|-------------------|----------------------|
| `PlateIsFree` | `plate_is_free` (Event) |
| `Busy` | `busy` (property) |
| `CurrentLocation` | `current_location` |
| `DestinationLocation` | `destination_location` |
| `GetCurrentToDo()` | `get_current_todo()` |
| `AdvanceCurrentToDo()` | `advance_current_todo()` |
| `IsFinished()` | `is_finished()` |
| `MarkJobCompleted()` | `mark_job_completed()` |
| `GetStatus()` | `get_status()` |

## Design Patterns Preserved

1. **Factory Pattern**: `ActivePlateFactory` → `ActivePlateFactory` (same)
2. **Thread-based Execution**: Both use background threads
3. **Queue-based Job Management**: Both use thread-safe queues
4. **Path Finding**: Both use Dijkstra's algorithm
5. **Device Abstraction**: Both use interface/abstract class patterns

## Notable Changes

1. **No MEF (Managed Extensibility Framework)**: Python version uses direct instantiation instead of dependency injection via MEF
2. **Simplified Error Handling**: Python version uses standard exceptions instead of `IError` interface
3. **No Static ActivePlates List Locking**: Python version uses instance-level locking
4. **Simplified Place/Location Model**: Python version has a simpler relationship between places and locations

## Migration Tips

1. **Replace C# events with Python callbacks**: Use lists of callbacks instead of C# events
2. **Use Python dataclasses**: For simple data structures like `Plate`, `PlateTask`
3. **Type hints**: Use Python type hints instead of C# type annotations
4. **Logging**: Use Python `logging` module instead of log4net

