# Planar Motor Backend for PC

This is a standalone FastAPI backend that runs on a PC connected to the Planar Motor Controller (PMC).

## Setup Instructions

### 1. Install Python 3.9-3.13
- Download Python from https://www.python.org/downloads/
- Make sure to check "Add Python to PATH" during installation

### 2. Install Mono (Required for pmclib)
**Windows:**
- Download Mono from https://www.mono-project.com/download/stable/
- Install the Windows installer
- Add Mono to PATH (usually `C:\Program Files\Mono\bin`)

**Linux:**
```bash
sudo apt-get update
sudo apt-get install mono-complete
```

### 3. Install pmclib
Copy the `pmclib-117.9.1-py3-none-any.whl` file to this directory, then:
```bash
pip install pmclib-117.9.1-py3-none-any.whl
```

### 4. Install Python Dependencies
```bash
pip install -r requirements.txt
```

### 5. Configure PMC IP
Edit `main.py` or set environment variable:
```bash
# Windows PowerShell
$env:PMC_IP="192.168.10.100"

# Windows CMD
set PMC_IP=192.168.10.100

# Linux/Mac
export PMC_IP=192.168.10.100
```

### 6. Run the Backend
```bash
python main.py --port 3062 --pmc-ip 192.168.10.100
```

Or use the startup script:
- **Windows:** `start_backend.bat`
- **Linux/Mac:** `./start_backend.sh`

## API Endpoints

The backend exposes the same API as the Mac version:
- `GET /` - Root endpoint
- `GET /status` - Connection status
- `POST /connect` - Connect to PMC
- `POST /disconnect` - Disconnect from PMC
- `GET /xbots/status` - Get all XBOT statuses
- `POST /xbots/activate` - Activate XBOTs
- `POST /xbots/{id}/levitate` - Levitate XBOT
- `POST /xbots/{id}/land` - Land XBOT
- `POST /xbots/{id}/jog` - Jog XBOT
- `POST /xbots/{id}/linear-motion` - Linear motion

## Frontend Configuration

Update the frontend to point to the PC's IP:
- Change API URL from `http://localhost:3062` to `http://192.168.0.23:3062`

## Troubleshooting

1. **Connection fails:**
   - Ensure PMC is powered on
   - Check that no other program has mastership
   - Verify network connectivity: `ping 192.168.10.100`

2. **pmclib import error:**
   - Ensure Mono is installed and in PATH
   - Reinstall pmclib: `pip uninstall pmclib && pip install pmclib-117.9.1-py3-none-any.whl`

3. **Port already in use:**
   - Change port: `python main.py --port 3063`
   - Or kill existing process using port 3062

