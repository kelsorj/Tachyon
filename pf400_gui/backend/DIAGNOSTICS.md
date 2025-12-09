# PF400 Diagnostics Mode

## Overview

The diagnostics mode provides comprehensive information about the robot's state, joints, and system health. It's particularly useful for the PF400SXL model which includes rail (J6) diagnostics.

## API Endpoints

### Get Full Diagnostics
```
GET /diagnostics
```

Returns comprehensive diagnostics including:
- Model information
- Connection status
- Rail status (SXL only)
- System state
- Joint states
- Error information

**Example Response (SXL):**
```json
{
  "model": "400SXL",
  "connected": true,
  "rail_enabled": true,
  "rail_length_mm": 2000.0,
  "rail_position_mm": 500.0,
  "rail_homed": false,
  "sys_state": "0",
  "power_state": "unknown",
  "attach_state": "unknown",
  "connected": true,
  "profile": 2,
  "joints": {
    "j1": {
      "name": "j1",
      "position": 0.178,
      "position_mm": 178.0
    },
    "j6": {
      "name": "rail",
      "position": 0.5,
      "position_mm": 500.0,
      "position_percent": 25.0,
      "limits": {
        "min_mm": 0.0,
        "max_mm": 2000.0,
        "min_m": 0.0,
        "max_m": 2.0
      }
    }
  }
}
```

### Get System State
```
GET /diagnostics/system-state
```

Returns current system state including power, attach, and connection status.

### Get Joint States
```
GET /diagnostics/joints
```

Returns detailed information about all joints including positions, limits, and units.

### Get Rail Status (SXL Only)
```
GET /diagnostics/rail
```

Returns rail-specific information:
- Current position (m and mm)
- Position as percentage of rail length
- Rail limits
- Rail length

**Example Response:**
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

### Jog Rail (SXL Only)
```
POST /diagnostics/jog-rail?distance_m=0.1&profile=1
```

Jogs the rail by a relative distance.

**Parameters:**
- `distance_m` (float): Distance to move in meters (positive or negative)
- `profile` (int, optional): Motion profile ID (default: 1)

### Move Rail to Position (SXL Only)
```
POST /diagnostics/move-rail?position_m=1.0&profile=1
```

Moves the rail to an absolute position.

**Parameters:**
- `position_m` (float): Target position in meters (0 to 2.0 for 2m rail)
- `profile` (int, optional): Motion profile ID (default: 1)

## Usage Examples

### Check Robot Diagnostics
```bash
curl http://localhost:3061/diagnostics
```

### Check Rail Status
```bash
curl http://localhost:3061/diagnostics/rail
```

### Jog Rail Forward 10cm
```bash
curl -X POST "http://localhost:3061/diagnostics/jog-rail?distance_m=0.1"
```

### Move Rail to 1m Position
```bash
curl -X POST "http://localhost:3061/diagnostics/move-rail?position_m=1.0"
```

## Model Selection

To use the PF400SXL driver, set the `ROBOT_MODEL` environment variable:

```bash
export ROBOT_MODEL=400SXL
python main.py --real
```

Or when starting the server:
```bash
ROBOT_MODEL=400SXL python main.py --real
```

## Differences Between Models

### PF400SX (Standard)
- 5 joints (J1-J5)
- No rail support
- Standard diagnostics

### PF400SXL (Extended)
- 6 joints (J1-J6, where J6 is the rail)
- 2m rail support
- Rail-specific diagnostics endpoints
- Rail movement commands

## Error Handling

If you try to use rail-specific endpoints with a PF400SX model, you'll get a 400 error:
```json
{
  "detail": "Rail diagnostics only available for PF400SXL models"
}
```

## Integration with Scheduler

The diagnostics interface can be used by the scheduler framework to:
- Check robot health before scheduling tasks
- Monitor rail position for path planning
- Verify joint limits before movements
- Track system state changes

