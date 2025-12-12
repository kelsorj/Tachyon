# Quick Testing Guide

## 1. Check Services Are Running

```bash
cd pf400_gui
pm2 status
```

Both services should show "online".

## 2. Test Frontend (Browser)

Open in your browser:
```
http://localhost:5173
```

You should see the PF400 GUI with robot controls.

## 3. Test Backend API

### Quick Health Check
```bash
curl http://localhost:3061/state
```

Expected: `{"state":"READY"}` or similar

### Get Joint Positions
```bash
curl http://localhost:3061/joints
```

### Get Diagnostics (SXL)
```bash
curl http://localhost:3061/diagnostics
```

## 4. Test Robot Connection

The robot is configured at: **192.168.0.20:10100**

### Check Connection
```bash
# Ping the robot
ping -c 3 192.168.0.20

# Check backend logs for connection status
pm2 logs pf400-backend | grep -i "connect\|error"
```

### Test Connection Script
```bash
cd pf400_gui/backend
source venv/bin/activate
python3 test_connection.py
```

## 5. Run Automated Tests

```bash
# Test API endpoints
cd pf400_gui
./test_api.sh

# Test diagnostics (if SXL)
cd backend
source venv/bin/activate
python3 test_diagnostics.py
```

## Quick Test Checklist

- [ ] Services running: `pm2 status`
- [ ] Frontend loads: http://localhost:5173
- [ ] Backend responds: `curl http://localhost:3061/state`
- [ ] Robot reachable: `ping 192.168.0.20`
- [ ] Joints endpoint: `curl http://localhost:3061/joints`
- [ ] Diagnostics work: `curl http://localhost:3061/diagnostics`

## Troubleshooting

### Backend Not Responding
```bash
pm2 logs pf400-backend
pm2 restart pf400-backend
```

### Robot Connection Failed
1. Check robot IP: Should be 192.168.0.20
2. Check network: `ping 192.168.0.20`
3. Check logs: `pm2 logs pf400-backend | grep -i connect`
4. Verify robot is powered on

### Frontend Not Loading
```bash
pm2 logs pf400-frontend
pm2 restart pf400-frontend
```

## Next Steps

Once basic tests pass:
1. Test robot movement (if connected)
2. Test teachpoints
3. Test rail movement (SXL)
4. Test diagnostics endpoints



