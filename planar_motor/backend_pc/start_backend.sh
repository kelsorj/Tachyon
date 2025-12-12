#!/bin/bash
# Start Planar Motor Backend on Linux/Mac PC

echo "Starting Planar Motor Backend..."
echo

# Set PMC IP (can be overridden by command line)
PMC_IP=${PMC_IP:-192.168.10.100}
PORT=${PORT:-3062}

# Check if Python is available
if ! command -v python3 &> /dev/null; then
    echo "ERROR: Python 3 is not installed or not in PATH"
    echo "Please install Python 3.9-3.13"
    exit 1
fi

# Check if pmclib is installed
python3 -c "import pmclib" 2>/dev/null
if [ $? -ne 0 ]; then
    echo "WARNING: pmclib is not installed"
    echo "Install it with: pip install pmclib-117.9.1-py3-none-any.whl"
    echo
fi

# Start the server
echo "Starting server on port $PORT..."
echo "PMC IP: $PMC_IP"
echo "Frontend should connect to: http://192.168.0.23:$PORT"
echo
python3 main.py --port $PORT --pmc-ip $PMC_IP


