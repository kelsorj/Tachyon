#!/usr/bin/env python3
"""
Setup script to configure device linking between PF400-021 and Planar-Motor-001.

This establishes the data model for robots to indicate they can reach teachpoints
on other devices, enabling coordinated plate transfers.

Data Model:
- Device.reachable_devices: List of devices this robot can physically access
- Teachpoint.linked_to: Reference to a teachpoint on another device (for handoffs)
"""

import sys
import os
from datetime import datetime, timezone

# Add parent directory to path to import db module
sys.path.insert(0, os.path.dirname(__file__))
import db as mongodb

def setup_device_linking():
    """Configure PF400-021 to indicate it can reach Planar-Motor-001."""
    
    db = mongodb.get_db()
    
    # ============== Update PF400-021 ==============
    print("\n1. Updating PF400-021 with reachable_devices...")
    
    pf400_update = db.devices.update_one(
        {"name": "PF400-021"},
        {
            "$set": {
                "reachable_devices": [
                    {
                        "device_name": "Planar-Motor-001",
                        "device_type": "planar_motor",
                        "access_type": "handoff",  # Can both drop off and pick up
                        "description": "Planar motor accessible from PF400 workspace",
                        "added_at": datetime.now(timezone.utc)
                    }
                ],
                "updated_at": datetime.now(timezone.utc)
            }
        }
    )
    
    if pf400_update.modified_count > 0:
        print("   ✓ Added reachable_devices to PF400-021")
    else:
        print("   ℹ PF400-021 already has reachable_devices or not found")
    
    # ============== Verify Planar-Motor-001 exists ==============
    print("\n2. Verifying Planar-Motor-001 exists...")
    
    planar_device = db.devices.find_one({"name": "Planar-Motor-001"})
    if planar_device:
        print(f"   ✓ Found Planar-Motor-001")
        print(f"     - Device Type: {planar_device.get('device_type_id', 'N/A')}")
        print(f"     - Status: {planar_device.get('status', 'N/A')}")
        
        # Ensure it has the reachable_from field
        if "reachable_from" not in planar_device:
            db.devices.update_one(
                {"name": "Planar-Motor-001"},
                {
                    "$set": {
                        "reachable_from": [
                            {
                                "device_name": "PF400-021",
                                "device_type": "pf400",
                                "access_type": "handoff",
                                "added_at": datetime.now(timezone.utc)
                            }
                        ],
                        "updated_at": datetime.now(timezone.utc)
                    }
                }
            )
            print("   ✓ Added reachable_from to Planar-Motor-001")
    else:
        print("   ✗ Planar-Motor-001 not found!")
        return False
    
    # ============== Show current teachpoints ==============
    print("\n3. Current teachpoints on each device:")
    
    # PF400-021 teachpoints
    pf400_device = db.devices.find_one({"name": "PF400-021"})
    if pf400_device:
        teachpoints = pf400_device.get("teachpoints", {})
        if isinstance(teachpoints, dict):
            print(f"\n   PF400-021 has {len(teachpoints)} teachpoints:")
            for tp_id, tp_data in list(teachpoints.items())[:5]:  # Show first 5
                name = tp_data.get("name", tp_id) if isinstance(tp_data, dict) else tp_id
                linked = tp_data.get("linked_to") if isinstance(tp_data, dict) else None
                link_status = f" → linked to {linked['device_name']}:{linked['teachpoint_id']}" if linked else ""
                print(f"      - {name}{link_status}")
            if len(teachpoints) > 5:
                print(f"      ... and {len(teachpoints) - 5} more")
        elif isinstance(teachpoints, list):
            print(f"\n   PF400-021 has {len(teachpoints)} teachpoints (array format)")
    
    # Planar-Motor-001 teachpoints
    planar_teachpoints = planar_device.get("teachpoints", {})
    if isinstance(planar_teachpoints, dict):
        print(f"\n   Planar-Motor-001 has {len(planar_teachpoints)} teachpoints:")
        for tp_id, tp_data in planar_teachpoints.items():
            name = tp_data.get("name", tp_id) if isinstance(tp_data, dict) else tp_id
            linked = tp_data.get("linked_from") if isinstance(tp_data, dict) else None
            link_status = f" ← linked from {linked['device_name']}:{linked['teachpoint_id']}" if linked else ""
            print(f"      - {name}{link_status}")
    else:
        print(f"\n   Planar-Motor-001 has no teachpoints yet")
    
    print("\n" + "="*60)
    print("Device linking setup complete!")
    print("="*60)
    print("\nNext steps:")
    print("1. Create teachpoints on both devices at the handoff locations")
    print("2. Use the API to link matching teachpoints together")
    print("3. The linked teachpoints can then be used for coordinated transfers")
    
    return True


def show_linking_status():
    """Show current linking status between devices."""
    db = mongodb.get_db()
    
    print("\n" + "="*60)
    print("Device Linking Status")
    print("="*60)
    
    # Check PF400-021
    pf400 = db.devices.find_one({"name": "PF400-021"})
    if pf400:
        reachable = pf400.get("reachable_devices", [])
        print(f"\nPF400-021 can reach {len(reachable)} device(s):")
        for dev in reachable:
            print(f"   → {dev.get('device_name')} ({dev.get('access_type', 'unknown')})")
    
    # Check Planar-Motor-001
    planar = db.devices.find_one({"name": "Planar-Motor-001"})
    if planar:
        reachable_from = planar.get("reachable_from", [])
        print(f"\nPlanar-Motor-001 is reachable from {len(reachable_from)} device(s):")
        for dev in reachable_from:
            print(f"   ← {dev.get('device_name')} ({dev.get('access_type', 'unknown')})")


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--status":
        show_linking_status()
    else:
        setup_device_linking()
        show_linking_status()


