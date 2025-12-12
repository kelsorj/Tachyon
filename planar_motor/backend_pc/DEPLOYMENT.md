# PC Backend Deployment Package

## What's Included

This package contains everything needed to run the Planar Motor backend on a PC at 192.168.0.23:

- `main.py` - FastAPI backend server
- `planar_motor_driver.py` - PMC driver using pmclib
- `requirements.txt` - Python dependencies
- `start_backend.bat` - Windows startup script
- `start_backend.sh` - Linux/Mac startup script
- `README.md` - Quick reference
- `INSTALL.md` - Detailed installation instructions

## Files You Need to Add

1. **pmclib wheel file:**
   - Copy `pmclib-117.9.1-py3-none-any.whl` to this directory
   - This file should be in `planar_motor/shaker/planar_demo/`

## Quick Deployment Steps

1. **Copy this entire `backend_pc` folder to the PC at 192.168.0.23**

2. **Copy the pmclib wheel:**
   ```bash
   # From Mac, copy to PC
   scp planar_motor/shaker/planar_demo/pmclib-117.9.1-py3-none-any.whl user@192.168.0.23:/path/to/backend_pc/
   ```

3. **On the PC, install dependencies:**
   ```bash
   cd backend_pc
   pip install pmclib-117.9.1-py3-none-any.whl
   pip install -r requirements.txt
   ```

4. **Run the backend:**
   - Windows: Double-click `start_backend.bat`
   - Linux: `./start_backend.sh`

## Frontend Configuration

After deploying the backend, update the frontend to point to the PC:

**File:** `pf400_gui/frontend/src/components/PlanarMotorDiagnostics.jsx`

**Change line 15:**
```javascript
// OLD:
const API_URL = "http://localhost:3062"

// NEW:
const API_URL = "http://192.168.0.23:3062"
```

## Network Architecture

```
Mac (192.168.0.2)                    PC (192.168.0.23)              PMC (192.168.10.100)
     │                                      │                                │
     │                                      │                                │
     │  HTTP API (port 3062)               │                                │
     └──────────────────────────────────────>│                                │
                                             │                                │
                                             │  pmclib TCP connection          │
                                             └────────────────────────────────>│
```

- Frontend runs on Mac at 192.168.0.2
- Backend runs on PC at 192.168.0.23
- PC connects to PMC at 192.168.10.100
- Frontend makes HTTP requests to PC backend

## Testing

1. **Start backend on PC:**
   ```bash
   python main.py --port 3062 --pmc-ip 192.168.10.100
   ```

2. **Test from Mac:**
   ```bash
   curl http://192.168.0.23:3062/
   curl http://192.168.0.23:3062/status
   ```

3. **Connect to PMC:**
   ```bash
   curl -X POST http://192.168.0.23:3062/connect
   ```

## Troubleshooting

### Backend won't start
- Check Python version: `python --version` (should be 3.9-3.13)
- Check if Mono is installed (required for pmclib)
- Verify pmclib is installed: `python -c "import pmclib"`

### Connection fails
- Verify PC can ping PMC: `ping 192.168.10.100`
- Ensure no other program has mastership
- Check Windows Firewall allows Python

### Frontend can't connect
- Verify backend is running: `curl http://192.168.0.23:3062/`
- Check network connectivity: `ping 192.168.0.23` from Mac
- Verify frontend API_URL is set to `http://192.168.0.23:3062`

## API Endpoints

All endpoints are the same as the Mac version:

- `GET /` - Root endpoint
- `GET /status` - Connection status
- `POST /connect` - Connect to PMC
- `POST /disconnect` - Disconnect from PMC
- `GET /xbots/status` - Get all XBOT statuses
- `POST /xbots/activate` - Activate XBOTs
- `POST /xbots/levitate` - Levitate all XBOTs
- `POST /xbots/{id}/levitate` - Levitate specific XBOT
- `POST /xbots/{id}/land` - Land XBOT
- `POST /xbots/{id}/stop-motion` - Stop XBOT motion
- `POST /xbots/jog` - Jog XBOT
- `POST /xbots/linear-motion` - Linear motion


