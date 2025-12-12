# Changelog: Multi-Robot Handoff Support

## Summary

Extended the scheduler framework to support multi-robot coordination with plate handoffs and wait operations. This enables complex workflows where multiple robots work together to process plates.

## New Features

### 1. WaitTask Support
- Added `WaitTask` class to `scheduler/worklist.py`
- Allows inserting delays into workflows (e.g., incubation periods, processing delays)
- Automatically handled by PlateScheduler in background threads

### 2. HandoffLocation
- New `HandoffLocation` class in `scheduler/handoff_location.py`
- Special location type for robot-to-robot plate transfers
- Tracks handoff state and robot access permissions
- Extends `PlateLocation` for seamless integration

### 3. Multi-Robot Coordination
- Enhanced scheduler to coordinate multiple robots
- Path planner automatically routes through handoff locations
- Robot transfer methods updated to handle handoff state

## Files Modified

1. **scheduler/worklist.py**
   - Added `WaitTask` dataclass

2. **scheduler/active_plate.py**
   - Updated `todo_list` to support `Union[PlateTask, WaitTask]`
   - Updated `get_current_todo()` return type

3. **scheduler/plate_scheduler.py**
   - Added `_handle_wait_task()` method
   - Updated scheduler loop to handle WaitTask

4. **scheduler/__init__.py**
   - Exported `WaitTask` and `HandoffLocation`

## Files Added

1. **scheduler/handoff_location.py**
   - `HandoffLocation` class implementation

2. **examples/multi_robot_handoff.py**
   - Complete example demonstrating PF400 → Planar handoff workflow
   - Includes: HandoffDevice, PF400Robot, PlanarRobot, PlanarProcessingArea

3. **MULTI_ROBOT_HANDOFF.md**
   - Comprehensive documentation for new features

## Example Workflow

The example demonstrates:
1. PF400 places plate at handoff location
2. Planar picks up plate from handoff
3. Planar moves plate to processing area
4. Wait 10 seconds
5. Planar moves plate back to handoff
6. PF400 picks up plate from handoff

## Usage

```python
from scheduler import WaitTask, HandoffLocation, PlateTask

# Create wait task
wait = WaitTask(10.0, "Wait at processing area")

# Create handoff location
handoff = HandoffLocation(
    name="HandoffStation",
    device_name="HandoffDevice",
    accessible_by_robots=["PF400", "Planar"]
)

# Add to workflow
workflow_tasks = [
    PlateTask("HandoffDevice", "place_at_handoff", {}),
    PlateTask("PlanarProcessingArea", "move_to_processing", {}),
    WaitTask(10.0, "Wait at processing area"),
    PlateTask("HandoffDevice", "return_to_handoff", {}),
]
```

## Testing

Run the example:
```bash
cd scheduler_framework
python examples/multi_robot_handoff.py
```

## Integration Notes

- All changes are backward compatible
- Existing workflows continue to work unchanged
- New features are opt-in (only used when WaitTask/HandoffLocation are used)
- Path planner automatically handles handoff locations

## Next Steps

Potential enhancements:
- Handoff location reservation system
- Automatic handoff location discovery
- Multi-plate handoff queues
- Handoff timeout and error recovery
- Visual handoff state monitoring

