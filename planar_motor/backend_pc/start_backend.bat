@echo off
REM Start Planar Motor Backend on Windows PC

echo Starting Planar Motor Backend...
echo.

REM Set PMC IP (can be overridden by command line)
set PMC_IP=192.168.10.100
set PORT=3062

REM Check if Python is available
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python is not installed or not in PATH
    echo Please install Python 3.9-3.13 from https://www.python.org/downloads/
    pause
    exit /b 1
)

REM Check if pmclib is installed
python -c "import pmclib" >nul 2>&1
if errorlevel 1 (
    echo WARNING: pmclib is not installed
    echo Install it with: pip install pmclib-117.9.1-py3-none-any.whl
    echo.
)

REM Start the server
echo Starting server on port %PORT%...
echo PMC IP: %PMC_IP%
echo Frontend should connect to: http://192.168.0.23:%PORT%
echo.
python main.py --port %PORT% --pmc-ip %PMC_IP%

pause

