#!/bin/bash
# Start Planar Motor backend server

cd "$(dirname "$0")"

# Create logs directory if it doesn't exist
mkdir -p logs

# Activate Python 3.13 virtual environment and set Mono path
source venv313/bin/activate
export MONO_GAC_PREFIX="/opt/homebrew"

# Start the server
python backend/main.py --port 3062 --pmc-ip 192.168.10.100 > logs/backend.log 2>&1 &

echo "Planar Motor backend started (PID: $!)"
echo "Logs: logs/backend.log"
echo "API: http://localhost:3062"
echo "Using Python 3.13 with pmclib support"

