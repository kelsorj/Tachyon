#!/usr/bin/env python3
import sys
import os
sys.path.insert(0, 'pf400_gui/backend')
import db as mongodb

print("Checking MongoDB for teachpoints...")

# Check all devices
db = mongodb.get_db()
devices = list(db.devices.find({}, {'name': 1, 'gui_teachpoints': 1, 'teachpoints': 1}))

for device in devices:
    name = device.get('name', 'Unknown')
    print(f"\nDevice: {name}")

    gui_tps = device.get('gui_teachpoints', {})
    if gui_tps:
        print(f"  gui_teachpoints: {list(gui_tps.keys())}")
        for tp_id, tp_data in gui_tps.items():
            if tp_id in ['home', 'away']:
                print(f"    {tp_id}: joints={tp_data.get('joints', [])} cartesian={tp_data.get('cartesian', {})}")
    else:
        print("  gui_teachpoints: empty"

    legacy_tps = device.get('teachpoints', [])
    if isinstance(legacy_tps, list):
        print(f"  teachpoints (array): {len(legacy_tps)} entries")
    else:
        print("  teachpoints (array): not an array"

print("\nDone.")
