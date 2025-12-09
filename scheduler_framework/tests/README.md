# Unit Tests for Scheduler Framework

This directory contains comprehensive unit tests for the Laboratory Automation Scheduler Framework.

## Test Structure

- `conftest.py` - Shared fixtures and mock objects for all tests
- `test_worklist.py` - Tests for Worklist, PlateTask, Transfer, and related classes
- `test_active_plate.py` - Tests for ActivePlate, PlateLocation, PlatePlace, and plate state management
- `test_active_plate_factory.py` - Tests for ActivePlateFactory and its implementations
- `test_device_manager.py` - Tests for DeviceManager and device interfaces
- `test_path_planner.py` - Tests for PathPlanner, Node, Connection, and path finding algorithms
- `test_robot_scheduler.py` - Tests for RobotScheduler and robot movement coordination
- `test_plate_scheduler.py` - Tests for PlateScheduler and overall workflow orchestration

## Running Tests

To run all tests:
```bash
pytest
```

To run a specific test file:
```bash
pytest tests/test_worklist.py
```

To run with verbose output:
```bash
pytest -v
```

To run a specific test:
```bash
pytest tests/test_worklist.py::TestWorklist::test_worklist_creation
```

## Test Coverage

The test suite covers:
- All core data structures (Plate, PlateLocation, PlatePlace, etc.)
- Worklist creation and management
- Active plate lifecycle and state management
- Factory pattern for creating active plates
- Device registration and management
- Path planning and graph algorithms
- Robot scheduling and movement coordination
- Plate scheduling and task orchestration

## Mock Objects

The `conftest.py` file provides:
- `MockDevice` - Mock implementation of device interfaces for testing
- `MockRobot` - Mock implementation of robot interface for testing
- Various fixtures for creating test data (plates, locations, worklists, etc.)

## Notes

- Tests use pytest fixtures for setup and teardown
- Active plates are automatically reset between tests
- Mock objects simulate device and robot behavior without requiring actual hardware
- Some tests are timing-dependent (scheduler thread tests) and may need adjustment based on system performance

