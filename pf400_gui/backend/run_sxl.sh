#!/bin/bash
# Helper script to run PF400SXL with proper environment setup

cd "$(dirname "$0")"

# Activate virtual environment
if [ -d "venv" ]; then
    source venv/bin/activate
    echo "✓ Activated virtual environment"
else
    echo "⚠ Warning: venv not found, using system python3"
fi

# Set robot model if not already set
export ROBOT_MODEL=${ROBOT_MODEL:-400SXL}

# Get port from environment or use default
PORT=${PF400_PORT:-3061}

# Get robot IP from environment or use default
export PF400_IP=${PF400_IP:-192.168.0.20}
export PF400_ROBOT_PORT=${PF400_ROBOT_PORT:-10100}

echo "Starting PF400 backend..."
echo "  Model: $ROBOT_MODEL"
echo "  Port: $PORT"
echo ""

# Use venv python3 if available, otherwise system python3
if [ -f "venv/bin/python3" ]; then
    # Use start_server.py which properly handles uvicorn
    exec venv/bin/python3 start_server.py --real --port $PORT
else
    exec python3 start_server.py --real --port $PORT
fi

