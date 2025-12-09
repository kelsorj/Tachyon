#!/bin/bash
cd "$(dirname "$0")"
# Port can be overridden with PF400_PORT env var (default: 3061)
PORT=${PF400_PORT:-3061}
exec /opt/homebrew/bin/python3 main.py --real --port $PORT

