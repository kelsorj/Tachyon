#!/usr/bin/env python3
"""
Startup script for the PF400 backend server.
This script properly handles command-line arguments before starting uvicorn.
"""
import sys
import os
import argparse
import uvicorn

# Add the backend directory to the path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# Parse arguments
parser = argparse.ArgumentParser(description='PF400 Backend Server')
parser.add_argument('--sim', action='store_true', help='Run in simulator mode')
parser.add_argument('--host', default='0.0.0.0', help='Host to bind to')
parser.add_argument('--port', type=int, default=3061, help='Port to bind to')
args = parser.parse_args()

# Set environment variable so main.py can access it
if args.sim:
    os.environ['PF400_SIM_MODE'] = '1'

# Import after setting environment
from main import app

if __name__ == "__main__":
    uvicorn.run(app, host=args.host, port=args.port)

