# Testing Guide - PF400 GUI

## Quick Verification

### 1. Check Services Are Running

```bash
cd pf400_gui
pm2 status
```

You should see both services online:
- `pf400-backend` - Running
- `pf400-frontend` - Running

### 2. Test Frontend (Browser)

Open your browser and navigate to:
```
http://localhost:5173
```

You should see the PF400 GUI interface with:
- Robot 3D viewer
- Joint controls
- Cartesian controls
- Teachpoints
- Diagnostics

### 3. Test Backend API (Command Line)

#### Basic Health Check
```bash
# Check backend state
curl http://localhost:3061/state

# Get robot description
curl http://localhost:3061/description
```

#### Get Joint Positions
```bash
curl http://localhost:3061/joints
```

This returns current joint positions and cartesian coordinates.

#### Diagnostics (SXL Only)
```bash
# Full diagnostics
curl http://localhost:3061/diagnostics

# System state
curl http://localhost:3061/diagnostics/system-state

# Joint states
curl http://localhost:3061/diagnostics/joints

# Rail status (SXL only)
curl http://localhost:3061/diagnostics/rail
```

### 4. Test Robot Movement (If Connected)

#### Jog Joint
```bash
# Jog joint 1 (vertical) by 0.01m
curl -X POST "http://localhost:3061/jog" \
  -H "Content-Type: application/json" \
  -d '{"joint": 1, "distance": 0.01, "speed_profile": 1}'
```

#### Jog Cartesian Axis
```bash
# Jog Z axis by 0.01m
curl -X POST "http://localhost:3061/jog" \
  -H "Content-Type: application/json" \
  -d '{"axis": "z", "distance": 0.01, "speed_profile": 1}'
```

#### Initialize Robot
```bash
curl -X POST http://localhost:3061/initialize
```

### 5. Test Teachpoints

#### Get All Teachpoints
```bash
curl http://localhost:3061/teachpoints
```

#### Save Current Position as Teachpoint
```bash
curl -X POST "http://localhost:3061/teachpoints/save-current?name=Home&description=Home%20position"
```

#### Move to Teachpoint
```bash
curl -X POST "http://localhost:3061/teachpoints/move/teachpoint_id?speed_profile=1"
```

### 6. Test Rail (SXL Only)

#### Get Rail Status
```bash
curl http://localhost:3061/diagnostics/rail
```

#### Jog Rail
```bash
# Move rail forward 10cm
curl -X POST "http://localhost:3061/diagnostics/jog-rail?distance_m=0.1&profile=1"
```

#### Move Rail to Position
```bash
# Move rail to 1 meter
curl -X POST "http://localhost:3061/diagnostics/move-rail?position_m=1.0&profile=1"
```

## Automated Testing Script

Use the provided test script:

```bash
cd pf400_gui/backend
source venv/bin/activate
python3 test_diagnostics.py
```

## Browser Testing

### Frontend Interface
1. Open http://localhost:5173
2. Check that the 3D robot model loads
3. Verify joint positions update
4. Test jog controls
5. Test teachpoint save/load

### Browser DevTools
1. Open browser DevTools (F12)
2. Check Console for errors
3. Check Network tab for API calls
4. Verify API responses

## Common Test Scenarios

### Scenario 1: Basic Connectivity
```bash
# 1. Check services
pm2 status

# 2. Check backend
curl http://localhost:3061/state

# 3. Check frontend
curl http://localhost:5173
```

### Scenario 2: Robot Diagnostics
```bash
# Get full diagnostics
curl http://localhost:3061/diagnostics | jq

# Check rail (if SXL)
curl http://localhost:3061/diagnostics/rail | jq
```

### Scenario 3: Joint Monitoring
```bash
# Get joints (watch for updates)
watch -n 1 'curl -s http://localhost:3061/joints | jq'
```

### Scenario 4: Movement Test
```bash
# Small safe movement
curl -X POST "http://localhost:3061/jog" \
  -H "Content-Type: application/json" \
  -d '{"joint": 1, "distance": 0.001, "speed_profile": 1}'

# Wait and check position
sleep 2
curl http://localhost:3061/joints | jq
```

## Troubleshooting Tests

### Backend Not Responding
```bash
# Check logs
pm2 logs pf400-backend

# Check if port is in use
lsof -i :3061

# Restart backend
pm2 restart pf400-backend
```

### Frontend Not Loading
```bash
# Check logs
pm2 logs pf400-frontend

# Check if port is in use
lsof -i :5173

# Restart frontend
pm2 restart pf400-frontend
```

### API Errors
```bash
# Check backend logs
pm2 logs pf400-backend --lines 50

# Test with verbose curl
curl -v http://localhost:3061/state
```

## Expected Responses

### State Endpoint
```json
{"state": "READY"}
```

### Joints Endpoint
```json
{
  "joints": {
    "j1": 0.178,
    "j2": 1.234,
    "j3": 0.567,
    "j4": -0.123,
    "gripper": 0.082
  },
  "cartesian": {
    "x": 100.5,
    "y": 200.3,
    "z": 150.2,
    "yaw": 45.0,
    "pitch": 90.0,
    "roll": 0.0
  }
}
```

### Diagnostics (SXL)
```json
{
  "model": "400SXL",
  "connected": true,
  "rail_enabled": true,
  "rail_position_mm": 500.0,
  "sys_state": "0"
}
```

## Quick Test Checklist

- [ ] Services running (`pm2 status`)
- [ ] Frontend loads (http://localhost:5173)
- [ ] Backend responds (`curl http://localhost:3061/state`)
- [ ] Joints endpoint works (`curl http://localhost:3061/joints`)
- [ ] Diagnostics work (`curl http://localhost:3061/diagnostics`)
- [ ] Frontend shows robot model
- [ ] Joint positions update in frontend
- [ ] No errors in browser console
- [ ] No errors in PM2 logs

## Next Steps

Once basic tests pass:
1. Test robot connection (if hardware available)
2. Test movement commands
3. Test teachpoints
4. Test rail movement (SXL)
5. Test error handling

