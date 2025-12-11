#!/usr/bin/env python3
"""
Test the teachpoint linking API
"""

import requests
import json

BASE_URL = "http://localhost:3061"

def test_linking_api():
    print("Testing teachpoint linking API...")

    # First, check if devices endpoint works
    try:
        response = requests.get(f"{BASE_URL}/devices")
        print(f"Devices endpoint: {response.status_code}")
        if response.status_code == 200:
            devices = response.json()
            print(f"Found {len(devices.get('devices', []))} devices")
        else:
            print(f"Devices endpoint failed: {response.text}")
            return
    except Exception as e:
        print(f"Error accessing devices endpoint: {e}")
        return

    # Test the linking endpoint
    payload = {
        "source_teachpoint_id": "test_source",
        "target_device": "Planar-Motor-001",
        "target_teachpoint_id": "test_target",
        "transfer_type": "handoff"
    }

    print(f"Sending linking request to: {BASE_URL}/devices/PF400-021/teachpoints/link")
    print(f"Payload: {json.dumps(payload, indent=2)}")

    try:
        response = requests.post(
            f"{BASE_URL}/devices/PF400-021/teachpoints/link",
            json=payload,
            headers={'Content-Type': 'application/json'}
        )

        print(f"Response status: {response.status_code}")
        print(f"Response text: {response.text}")

        if response.status_code == 422:
            print("422 Error - likely validation issue")
            try:
                error_data = response.json()
                print(f"Error details: {json.dumps(error_data, indent=2)}")
            except:
                print("Could not parse error response as JSON")

    except Exception as e:
        print(f"Error making request: {e}")

if __name__ == "__main__":
    test_linking_api()
