#!/bin/bash
cd "$(dirname "$0")"

# Activate virtual environment if it exists
if [ -d "venv" ]; then
    source venv/bin/activate
fi

# Port can be overridden with PF400_PORT env var (default: 3061)
PORT=${PF400_PORT:-3061}
ROBOT_MODEL=${ROBOT_MODEL:-400SX}

# Use python from venv if available, otherwise use system python3
if command -v python &> /dev/null; then
    exec python main.py --real --port $PORT
else
    exec python3 main.py --real --port $PORT
fi

