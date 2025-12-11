# Installation Guide for PC Backend

## Quick Start

1. **Copy this entire `backend_pc` folder to the PC at 192.168.0.23**

2. **Copy the pmclib wheel file:**
   - Copy `pmclib-117.9.1-py3-none-any.whl` to the `backend_pc` folder

3. **Install Python 3.9-3.13** (if not already installed)
   - Windows: https://www.python.org/downloads/
   - Linux: `sudo apt-get install python3 python3-pip`

4. **Install Mono** (required for pmclib)
   - Windows: Download from https://www.mono-project.com/download/stable/
   - Linux: `sudo apt-get install mono-complete`

5. **Install dependencies:**
   ```bash
   pip install pmclib-117.9.1-py3-none-any.whl
   pip install -r requirements.txt
   ```

6. **Run the backend:**
   - Windows: Double-click `start_backend.bat`
   - Linux: `./start_backend.sh`

## Detailed Steps

### Windows Installation

1. **Install Python:**
   - Download Python 3.9-3.13 from python.org
   - During installation, check "Add Python to PATH"
   - Verify: Open Command Prompt and run `python --version`

2. **Install Mono:**
   - Download Mono Windows installer from mono-project.com
   - Install to default location
   - Add to PATH: `C:\Program Files\Mono\bin` (or your install location)

3. **Install pmclib:**
   ```cmd
   cd backend_pc
   pip install pmclib-117.9.1-py3-none-any.whl
   ```

4. **Install other dependencies:**
   ```cmd
   pip install -r requirements.txt
   ```

5. **Run:**
   ```cmd
   start_backend.bat
   ```
   Or manually:
   ```cmd
   python main.py --port 3062 --pmc-ip 192.168.10.100
   ```

### Linux Installation

1. **Install Python and pip:**
   ```bash
   sudo apt-get update
   sudo apt-get install python3 python3-pip
   ```

2. **Install Mono:**
   ```bash
   sudo apt-get install mono-complete
   ```

3. **Install pmclib:**
   ```bash
   cd backend_pc
   pip3 install pmclib-117.9.1-py3-none-any.whl
   ```

4. **Install other dependencies:**
   ```bash
   pip3 install -r requirements.txt
   ```

5. **Run:**
   ```bash
   chmod +x start_backend.sh
   ./start_backend.sh
   ```
   Or manually:
   ```bash
   python3 main.py --port 3062 --pmc-ip 192.168.10.100
   ```

## Configuration

### Change PMC IP
Edit `start_backend.bat` (Windows) or `start_backend.sh` (Linux) and change:
```bash
PMC_IP=192.168.10.100
```

Or set environment variable:
- Windows: `set PMC_IP=192.168.10.100`
- Linux: `export PMC_IP=192.168.10.100`

### Change Port
Edit the startup script or use command line:
```bash
python main.py --port 3063 --pmc-ip 192.168.10.100
```

## Testing

Once the backend is running, test it:

1. **Check if server is running:**
   ```bash
   curl http://192.168.0.23:3062/
   ```

2. **Check status:**
   ```bash
   curl http://192.168.0.23:3062/status
   ```

3. **Connect to PMC:**
   ```bash
   curl -X POST http://192.168.0.23:3062/connect
   ```

## Frontend Configuration

Update the frontend to point to the PC backend:

In `PlanarMotorDiagnostics.jsx`, change:
```javascript
const API_URL = "http://192.168.0.23:3062"
```

## Troubleshooting

### "pmclib not available"
- Ensure Mono is installed and in PATH
- Reinstall pmclib: `pip uninstall pmclib && pip install pmclib-117.9.1-py3-none-any.whl`

### "Connection failed"
- Verify PMC is powered on
- Check network: `ping 192.168.10.100`
- Ensure no other program has mastership

### "Port already in use"
- Change port: `python main.py --port 3063`
- Or kill process using port 3062

### Windows Firewall
- Allow Python through Windows Firewall
- Or disable firewall temporarily for testing

