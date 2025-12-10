# Quick Start Guide

## Setup

### 1. Add Python Alias (One-time setup)

Run this to add `python` as an alias for `python3`:

```bash
cd pf400_gui/backend
./setup_python_alias.sh
source ~/.zshrc  # or restart terminal
```

Or manually add to `~/.zshrc`:
```bash
echo 'alias python=python3' >> ~/.zshrc
source ~/.zshrc
```

### 2. Install Dependencies

The virtual environment already has fastapi installed. Just activate it:

```bash
cd pf400_gui/backend
source venv/bin/activate
```

If you need to reinstall dependencies:
```bash
pip install -r requirements.txt
```

## Running the Server

### Option 1: Use the helper script (Recommended)

```bash
cd pf400_gui/backend
./run_sxl.sh
```

This script:
- Activates the virtual environment
- Sets ROBOT_MODEL=400SXL
- Runs the server

### Option 2: Manual activation

```bash
cd pf400_gui/backend
source venv/bin/activate
ROBOT_MODEL=400SXL python3 main.py --real
```

### Option 3: Use start_backend.sh (updated)

```bash
cd pf400_gui/backend
ROBOT_MODEL=400SXL ./start_backend.sh
```

## Testing

Once the server is running, test diagnostics:

```bash
# In another terminal
cd pf400_gui/backend
source venv/bin/activate
python3 test_diagnostics.py
```

Or use curl:
```bash
curl http://localhost:3061/diagnostics
curl http://localhost:3061/diagnostics/rail
```

## Troubleshooting

### "command not found: python"
- Run `./setup_python_alias.sh` and restart terminal
- Or always use `python3` instead of `python`

### "ModuleNotFoundError: No module named 'fastapi'"
- Activate the virtual environment: `source venv/bin/activate`
- Or install: `pip install fastapi uvicorn`

### "Rail diagnostics only available for PF400SXL models"
- Make sure you set `ROBOT_MODEL=400SXL` before starting
- Or use `./run_sxl.sh` which sets it automatically


