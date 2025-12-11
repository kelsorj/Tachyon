# Planar Motor Backend Installation

## Current Status

The backend server is running and serving GLTF models for the frontend visualization. However, full PMC control functionality requires `pmclib` which depends on `pythonnet`.

## Requirements for Full Functionality

### Python Version
- **Required**: Python 3.7 - 3.13 (Python 3.14 is not supported by pythonnet 3.0+)
- **Current**: Python 3.14.2 (incompatible)

### Dependencies
1. **pythonnet** - Python.NET bridge (required for pmclib)
   - Version 3.0+ requires Python < 3.14
   - On macOS, may require additional setup

2. **pmclib** - Planar Motor Control Library
   - Wheel file: `pmclib-117.9.1-py3-none-any.whl` (already in `shaker/` directory)
   - Requires pythonnet to access PMCLIB.dll

### Installation Options

#### Option 1: Use Python 3.13 or earlier
```bash
# Create a virtual environment with Python 3.13
python3.13 -m venv venv
source venv/bin/activate
pip install pythonnet
pip install shaker/pmclib-117.9.1-py3-none-any.whl
```

#### Option 2: Use existing Python environment (if available)
If you have a Python 3.13 or earlier environment where pmclib is already working:
```bash
# Use that Python interpreter for the backend
/path/to/python3.13 backend/main.py --port 3062 --pmc-ip 192.168.1.100
```

#### Option 3: Windows/Mono setup
On macOS, pythonnet requires Mono framework:
```bash
# Install Mono (if not already installed)
brew install mono

# Then install pythonnet
pip install pythonnet
```

## Current Workaround

The backend is running in "limited mode" - it can:
- ✅ Serve GLTF models for 3D visualization
- ✅ Provide API endpoints (though they'll fail without pmclib)
- ❌ Cannot connect to PMC or control XBOTs

## Testing pmclib Installation

```bash
cd planar_motor/shaker
python3 -c "from pmclib import system_commands; print('pmclib works!')"
```

If this succeeds, the backend will automatically use pmclib when restarted.

## Notes

- The PMCLIB.dll file in `shaker/` is a Windows DLL
- On macOS, pmclib may use a different native library or require Mono
- The backend gracefully handles missing pmclib and still serves static files

