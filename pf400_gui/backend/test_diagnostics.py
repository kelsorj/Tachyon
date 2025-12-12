#!/usr/bin/env python3
"""
Test script for PF400SXL diagnostics mode.

This script tests the diagnostics endpoints to verify they work correctly.
Run this after starting the server with: ROBOT_MODEL=400SXL python main.py --real
"""

import requests
import json
import sys

BASE_URL = "http://localhost:3061"

def test_diagnostics():
    """Test the main diagnostics endpoint"""
    print("=" * 60)
    print("Testing GET /diagnostics")
    print("=" * 60)
    
    try:
        response = requests.get(f"{BASE_URL}/diagnostics")
        response.raise_for_status()
        data = response.json()
        
        print(json.dumps(data, indent=2))
        
        # Check for SXL-specific features
        if "rail_enabled" in data:
            print("\n✓ Rail diagnostics available (SXL model detected)")
        else:
            print("\n⚠ Rail diagnostics not available (may be SX model)")
        
        return True
    except requests.exceptions.RequestException as e:
        print(f"✗ Error: {e}")
        return False

def test_system_state():
    """Test system state endpoint"""
    print("\n" + "=" * 60)
    print("Testing GET /diagnostics/system-state")
    print("=" * 60)
    
    try:
        response = requests.get(f"{BASE_URL}/diagnostics/system-state")
        response.raise_for_status()
        data = response.json()
        
        print(json.dumps(data, indent=2))
        return True
    except requests.exceptions.RequestException as e:
        print(f"✗ Error: {e}")
        return False

def test_joint_states():
    """Test joint states endpoint"""
    print("\n" + "=" * 60)
    print("Testing GET /diagnostics/joints")
    print("=" * 60)
    
    try:
        response = requests.get(f"{BASE_URL}/diagnostics/joints")
        response.raise_for_status()
        data = response.json()
        
        print(json.dumps(data, indent=2))
        return True
    except requests.exceptions.RequestException as e:
        print(f"✗ Error: {e}")
        return False

def test_rail_status():
    """Test rail status endpoint (SXL only)"""
    print("\n" + "=" * 60)
    print("Testing GET /diagnostics/rail")
    print("=" * 60)
    
    try:
        response = requests.get(f"{BASE_URL}/diagnostics/rail")
        response.raise_for_status()
        data = response.json()
        
        print(json.dumps(data, indent=2))
        print(f"\n✓ Rail position: {data.get('position_m', 0):.3f}m ({data.get('position_mm', 0):.1f}mm)")
        print(f"  Rail position: {data.get('position_percent', 0):.1f}% of rail length")
        return True
    except requests.exceptions.HTTPError as e:
        if e.response.status_code == 400:
            print("⚠ Rail diagnostics only available for PF400SXL models")
            print("  Make sure you started the server with: ROBOT_MODEL=400SXL")
        else:
            print(f"✗ Error: {e}")
        return False
    except requests.exceptions.RequestException as e:
        print(f"✗ Error: {e}")
        return False

def test_joints_endpoint():
    """Test the standard joints endpoint for comparison"""
    print("\n" + "=" * 60)
    print("Testing GET /joints (standard endpoint)")
    print("=" * 60)
    
    try:
        response = requests.get(f"{BASE_URL}/joints")
        response.raise_for_status()
        data = response.json()
        
        print("Joints:", json.dumps(data.get("joints", {}), indent=2))
        if "j6" in data.get("joints", {}) or "rail" in data.get("joints", {}):
            print("\n✓ J6/Rail detected in standard joints endpoint")
        return True
    except requests.exceptions.RequestException as e:
        print(f"✗ Error: {e}")
        return False

def main():
    """Run all diagnostic tests"""
    print("PF400SXL Diagnostics Test Suite")
    print("=" * 60)
    print(f"Testing against: {BASE_URL}")
    print("Make sure the server is running with: ROBOT_MODEL=400SXL python main.py --real")
    print("=" * 60)
    
    results = []
    
    # Test basic endpoints
    results.append(("Diagnostics", test_diagnostics()))
    results.append(("System State", test_system_state()))
    results.append(("Joint States", test_joint_states()))
    results.append(("Standard Joints", test_joints_endpoint()))
    
    # Test SXL-specific endpoints
    results.append(("Rail Status", test_rail_status()))
    
    # Summary
    print("\n" + "=" * 60)
    print("Test Summary")
    print("=" * 60)
    
    passed = sum(1 for _, result in results if result)
    total = len(results)
    
    for name, result in results:
        status = "✓ PASS" if result else "✗ FAIL"
        print(f"{status}: {name}")
    
    print(f"\nTotal: {passed}/{total} tests passed")
    
    if passed == total:
        print("\n✓ All tests passed!")
        return 0
    else:
        print("\n⚠ Some tests failed. Check the output above for details.")
        return 1

if __name__ == "__main__":
    sys.exit(main())



