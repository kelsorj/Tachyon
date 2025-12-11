#!/usr/bin/env python3
"""
Reset PF400-021 gui_teachpoints to just "home" and "away".
These will be used for linking to Planar-Motor-001.

Note: This uses 'gui_teachpoints' field (not 'teachpoints') to avoid 
conflict with the legacy teachpoints array.
"""

import sys
import os
from datetime import datetime, timezone

sys.path.insert(0, os.path.dirname(__file__))
import db as mongodb

def reset_pf400_teachpoints():
    """Reset PF400-021 gui_teachpoints to have only 'home' and 'away'."""
    
    db = mongodb.get_db()
    
    print("\n" + "="*60)
    print("Resetting PF400-021 GUI Teachpoints")
    print("="*60)
    
    # Get current device
    device = db.devices.find_one({"name": "PF400-021"})
    if not device:
        print("✗ PF400-021 not found!")
        return False
    
    # Show legacy teachpoints (array) info
    legacy_tps = device.get("teachpoints", [])
    if isinstance(legacy_tps, list):
        print(f"\nLegacy teachpoints (array): {len(legacy_tps)} entries")
        print("   (These are kept unchanged - used by other systems)")
    
    # Show current gui_teachpoints
    current_gui_tps = device.get("gui_teachpoints", {})
    if isinstance(current_gui_tps, dict) and current_gui_tps:
        print(f"\nCurrent gui_teachpoints: {len(current_gui_tps)}")
        for tp_id in list(current_gui_tps.keys()):
            print(f"   - {tp_id}: {current_gui_tps[tp_id].get('name', 'N/A')}")
    else:
        print("\nNo existing gui_teachpoints found.")
    
    # Define the two teachpoints we want
    # These are placeholder positions - they should be taught properly later via the GUI
    new_gui_teachpoints = {
        "home": {
            "id": "home",
            "name": "Home",
            "description": "Home/safe position for PF400",
            "type": "joints",
            "joints": [],  # Will be taught via GUI
            "cartesian": {},
            "created_at": datetime.now(timezone.utc),
            "updated_at": datetime.now(timezone.utc)
        },
        "away": {
            "id": "away",
            "name": "Away", 
            "description": "Away position - handoff point with Planar Motor",
            "type": "joints",
            "joints": [],  # Will be taught via GUI
            "cartesian": {},
            "created_at": datetime.now(timezone.utc),
            "updated_at": datetime.now(timezone.utc)
        }
    }
    
    # Update device with new gui_teachpoints
    result = db.devices.update_one(
        {"name": "PF400-021"},
        {
            "$set": {
                "gui_teachpoints": new_gui_teachpoints,
                "updated_at": datetime.now(timezone.utc)
            }
        }
    )
    
    if result.modified_count > 0:
        print("\n✓ Reset gui_teachpoints to:")
        print("   - home: Home/safe position")
        print("   - away: Handoff point with Planar Motor")
        print("\nNote: Joint positions are empty - teach them via the PF400 GUI")
    else:
        print("\n⚠ No changes made (gui_teachpoints may already be set)")
    
    # Verify
    device = db.devices.find_one({"name": "PF400-021"})
    gui_tps = device.get("gui_teachpoints", {})
    print(f"\nVerification: PF400-021 now has {len(gui_tps)} gui_teachpoints:")
    for tp_id, tp_data in gui_tps.items():
        print(f"   - {tp_id}: {tp_data.get('name', 'N/A')}")
    
    return True


if __name__ == "__main__":
    # Ask for confirmation
    print("\nThis will REPLACE all gui_teachpoints on PF400-021 with just 'home' and 'away'.")
    print("(Legacy 'teachpoints' array will NOT be modified)")
    confirm = input("Continue? (y/n): ").strip().lower()
    
    if confirm == 'y':
        reset_pf400_teachpoints()
    else:
        print("Cancelled.")
