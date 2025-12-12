# PF400SXL Support and Diagnostics

## Overview

This update adds support for multiple PF400 robot models, with special focus on the **PF400SXL** model which includes a 2-meter rail (joint 6).

## What's New

### 1. Model Abstraction System
- **File**: `pf400_models.py`
- Defines model variants (400SX, 400SXL)
- Provides model configurations and factory functions
- Includes `DiagnosticsInterface` for robot diagnostics

### 2. PF400SXL Driver
- **File**: `pf400_sxl_driver.py`
- Extends `PF400Driver` to add J6 (rail) support
- Handles 6-joint movements (J1-J6)
- Implements diagnostics interface
- Rail-specific methods:
  - `move_rail()` - Move rail to absolute position
  - `move_rail_raw()` - Move rail in mm
  - `jog_rail()` - Jog rail by relative distance

### 3. Diagnostics API
- **Endpoints added to `main.py`**:
  - `GET /diagnostics` - Full diagnostics
  - `GET /diagnostics/system-state` - System state
  - `GET /diagnostics/joints` - Joint states
  - `GET /diagnostics/rail` - Rail status (SXL only)
  - `POST /diagnostics/jog-rail` - Jog rail
  - `POST /diagnostics/move-rail` - Move rail to position

### 4. Model Selection
- Environment variable: `ROBOT_MODEL=400SXL` or `ROBOT_MODEL=400SX`
- Automatically selects appropriate driver based on model

## Quick Start

### Starting the Server with PF400SXL

```bash
cd pf400_gui/backend
ROBOT_MODEL=400SXL python main.py --real
```

Or set it in your environment:
```bash
export ROBOT_MODEL=400SXL
python main.py --real
```

### Testing Diagnostics

1. Start the server (see above)
2. Run the test script:
```bash
python test_diagnostics.py
```

3. Or test manually with curl:
```bash
# Get full diagnostics
curl http://localhost:3061/diagnostics

# Get rail status
curl http://localhost:3061/diagnostics/rail

# Move rail to 1 meter
curl -X POST "http://localhost:3061/diagnostics/move-rail?position_m=1.0"
```

## Key Features

### Rail Support (SXL Only)

The PF400SXL has a 2-meter rail that uses joint 6 (J6):

- **Rail Length**: 2000mm (2.0m)
- **Position Range**: 0.0m to 2.0m
- **Units**: Meters (SI) or millimeters (raw)

### Joint Positions

For PF400SXL, `get_joint_positions()` returns:
```python
{
    "j1": 0.178,      # Vertical (m)
    "j2": 1.234,      # Shoulder (rad)
    "j3": 0.567,      # Elbow (rad)
    "j4": -0.123,     # Wrist (rad)
    "gripper": 0.082, # Gripper (m)
    "j6": 0.5,        # Rail (m) - SXL only
    "rail": 0.5       # Rail alias (m) - SXL only
}
```

### Movement Commands

**Standard 5-joint move (works for both models):**
```python
driver.move_to_joints(j1_m, j2_rad, j3_rad, j4_rad, gripper_m)
```

**6-joint move with rail (SXL only):**
```python
driver.move_to_joints(j1_m, j2_rad, j3_rad, j4_rad, gripper_m, j6_m)
```

**Rail-only move (SXL only):**
```python
driver.move_rail(1.0)  # Move to 1 meter
driver.jog_rail(0.1)   # Jog forward 10cm
```

## API Examples

### Get Diagnostics
```bash
curl http://localhost:3061/diagnostics
```

Response includes:
- Model information
- Connection status
- Rail status (if SXL)
- System state
- Joint states

### Get Rail Status
```bash
curl http://localhost:3061/diagnostics/rail
```

Response:
```json
{
  "rail_enabled": true,
  "position_m": 0.5,
  "position_mm": 500.0,
  "position_percent": 25.0,
  "rail_length_mm": 2000.0,
  "rail_length_m": 2.0,
  "limits": {
    "min_mm": 0.0,
    "max_mm": 2000.0,
    "min_m": 0.0,
    "max_m": 2.0
  }
}
```

### Jog Rail
```bash
curl -X POST "http://localhost:3061/diagnostics/jog-rail?distance_m=0.1&profile=1"
```

### Move Rail
```bash
curl -X POST "http://localhost:3061/diagnostics/move-rail?position_m=1.0&profile=1"
```

## Files Created/Modified

### New Files
- `pf400_models.py` - Model definitions and diagnostics interface
- `pf400_sxl_driver.py` - SXL-specific driver with rail support
- `test_diagnostics.py` - Test script for diagnostics
- `DIAGNOSTICS.md` - Detailed diagnostics documentation
- `README_SXL.md` - This file

### Modified Files
- `main.py` - Added diagnostics endpoints and model selection

## Next Steps

1. **Test with real hardware**: Connect to your PF400SXL and test diagnostics
2. **Verify rail movement**: Test rail jogging and absolute positioning
3. **Integrate with scheduler**: Use diagnostics in the scheduler framework
4. **Add error handling**: Enhance error detection and reporting
5. **Add homing**: Implement rail homing sequence if needed

## Troubleshooting

### "Rail diagnostics only available for PF400SXL models"
- Make sure you set `ROBOT_MODEL=400SXL` before starting the server
- Check that the driver is `PF400SXLDriver` (not `PF400Driver`)

### J6 not appearing in joint positions
- The robot's `WhereJ` command should return 7 values (status + 6 joints)
- If it only returns 6, the driver will use the stored rail position
- Check robot firmware version - older versions may not report J6

### Rail movement not working
- Verify the robot accepts 6-joint `MoveJ` commands
- Check that J6 is enabled in robot configuration
- Ensure rail is properly initialized/homed

## Integration with Scheduler Framework

The diagnostics can be used by the scheduler to:
- Check robot health before scheduling
- Monitor rail position for path planning
- Verify joint limits before movements
- Track system state for error recovery

Example:
```python
from pf400_sxl_driver import PF400SXLDriver

driver = PF400SXLDriver(ip="192.168.10.69")
driver.connect()

# Get diagnostics
diag = driver.get_diagnostics()
if diag["connected"] and not diag.get("errors"):
    # Robot is healthy, proceed with scheduling
    pass
```



