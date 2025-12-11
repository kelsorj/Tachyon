#!/usr/bin/env python3
"""
Check MongoDB for teachpoints "home" and "away" in all devices.
"""

import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'pf400_gui/backend'))
import db as mongodb

def check_all_teachpoints():
    """Check all devices for teachpoints containing 'home' or 'away'."""
    db = mongodb.get_db()

    print("="*80)
    print("Checking MongoDB for teachpoints 'home' and 'away'")
    print("="*80)

    devices = db.devices.find({})
    found_any = False

    for device in devices:
        device_name = device.get('name', 'Unknown')
        print(f"\nDevice: {device_name}")

        # Check gui_teachpoints (dict format)
        gui_tps = device.get('gui_teachpoints', {})
        if gui_tps and isinstance(gui_tps, dict):
            print(f"  gui_teachpoints: {len(gui_tps)} entries")
            for tp_id, tp_data in gui_tps.items():
                if tp_id in ['home', 'away'] or tp_data.get('name', '').lower() in ['home', 'away']:
                    found_any = True
                    print(f"    ✓ {tp_id}: {tp_data.get('name', 'N/A')}")
                    print(f"      joints: {tp_data.get('joints', [])}")
                    print(f"      cartesian: {tp_data.get('cartesian', {})}")
                    print(f"      position: {tp_data.get('position', {})}")
        else:
            print("  gui_teachpoints: None or empty"

        # Check legacy teachpoints (array format)
        legacy_tps = device.get('teachpoints', [])
        if legacy_tps and isinstance(legacy_tps, list):
            print(f"  teachpoints (array): {len(legacy_tps)} entries")
            for i, tp in enumerate(legacy_tps):
                tp_name = tp.get('name', '')
                if 'home' in tp_name.lower() or 'away' in tp_name.lower():
                    found_any = True
                    print(f"    ✓ [{i}] {tp_name}")
                    # Show first position if it exists
                    positions = tp.get('positions', [])
                    if positions:
                        pos = positions[0]
                        joints = pos.get('joints', {})
                        cartesian = pos.get('cartesian', {})
                        print(f"      joints: {joints}")
                        print(f"      cartesian: {cartesian}")
        else:
            print("  teachpoints (array): None or empty"

    if not found_any:
        print("\n❌ No teachpoints named 'home' or 'away' found in any device!")
    else:
        print(f"\n✅ Found teachpoints with 'home'/'away' in names.")

    return found_any

if __name__ == "__main__":
    check_all_teachpoints()
