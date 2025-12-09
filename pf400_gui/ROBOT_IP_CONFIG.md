# Robot IP Configuration

## Current Configuration

The robot IP address is configured as: **192.168.0.20**

## How It Works

The robot IP can be set via environment variables:

- `PF400_IP` - Robot IP address (default: 192.168.0.20)
- `PF400_ROBOT_PORT` - Robot port (default: 10100)
- `PF400_PORT` - Backend API port (default: 3061)

## Changing the Robot IP

### Option 1: Environment Variable (Recommended)

```bash
export PF400_IP=192.168.0.20
export PF400_ROBOT_PORT=10100
pm2 restart pf400-backend
```

### Option 2: Update PM2 Config

Edit `ecosystem.dev.config.js`:
```javascript
env: {
  PF400_IP: '192.168.0.20',
  PF400_ROBOT_PORT: '10100',
  // ...
}
```

Then restart:
```bash
pm2 restart pf400-backend
```

### Option 3: Update Default in Code

Edit the driver files:
- `backend/pf400_driver.py`
- `backend/pf400_sxl_driver.py`
- `backend/main.py`

Change the default IP from `192.168.10.69` to `192.168.0.20`

## Testing Connection

### Test Robot Connection
```bash
cd pf400_gui/backend
source venv/bin/activate
python3 test_connection.py
```

Or use curl to test the backend:
```bash
curl http://localhost:3061/state
curl http://localhost:3061/joints
```

### Check Backend Logs
```bash
pm2 logs pf400-backend
```

Look for connection messages like:
```
Connected to PF400 at 192.168.0.20:10100
```

## Network Requirements

- Your computer must be on the same network as the robot
- If robot is on 192.168.0.x, your computer should be on 192.168.0.x
- Check your network: `ifconfig` or `ip addr`

## Troubleshooting

### Connection Refused
1. Check robot is powered on
2. Verify IP address is correct
3. Check network connectivity:
   ```bash
   ping 192.168.0.20
   ```
4. Check firewall settings

### Wrong IP Address
1. Update `PF400_IP` environment variable
2. Restart backend: `pm2 restart pf400-backend`
3. Check logs: `pm2 logs pf400-backend`

### Port Issues
- Robot port: 10100 (PF400 default)
- Backend API port: 3061 (configurable via `PF400_PORT`)

