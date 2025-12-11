#!/usr/bin/env python3
"""
Check what device name the backend is using.
"""
import os

device_name = os.environ.get('DEVICE_NAME', 'PF400-015')
print(f"Environment DEVICE_NAME: {device_name}")

# Also check what the main.py would use
print(f"main.py would use: {device_name}")

# Check if there are any other env vars
for key, value in os.environ.items():
    if 'DEVICE' in key.upper() or 'PF400' in key.upper():
        print(f"{key}: {value}")
