#!/usr/bin/env python3
"""
Create Plate Pad device type and device in MongoDB.

A Plate Pad is a stationary location where plates can be placed.
It has no active control - just a defined position that can be
associated with robot teachpoints for pick/place operations.
"""

import db as mongodb
from bson import ObjectId
from datetime import datetime, timezone


def setup_plate_pad_device_type():
    """Create the Plate Pad device type."""
    
    db = mongodb.get_db()
    device_type_collection = db.device_types
    
    # Check if device type already exists
    device_type = device_type_collection.find_one({
        "vendor": "Generic",
        "product_name": "Plate Pad"
    })
    
    if not device_type:
        # Create new device type
        device_type_id = ObjectId()
        device_type_collection.insert_one({
            "_id": device_type_id,
            "vendor": "Generic",
            "product_name": "Plate Pad",
            "device_category": "static_position",
            "description": "A stationary plate pad - a defined location where plates can be placed. "
                          "Used to associate robot teachpoints with physical locations.",
            "capabilities": [
                "plate_storage",
                "handoff_location"
            ],
            "plate_capacity": 1,  # How many plates can be stacked
            "model_3d": {
                "render_type": "primitive",
                "primitive": {
                    "type": "box",
                    "dimensions": {
                        "width_mm": 150,
                        "depth_mm": 150,
                        "height_mm": 10
                    },
                    "color": "#4a90d9"
                }
            },
            "created_at": datetime.now(timezone.utc),
            "updated_at": datetime.now(timezone.utc)
        })
        print(f"✓ Created device type 'Plate Pad': {device_type_id}")
        return device_type_id
    else:
        print(f"  Using existing device type 'Plate Pad': {device_type['_id']}")
        return device_type["_id"]


def create_plate_pad_device(
    device_name: str,
    device_type_id,
    description: str = "",
    position: dict = None,
    linked_robot: str = None,
    linked_teachpoint: str = None
):
    """
    Create a Plate Pad device instance.
    
    Args:
        device_name: Unique name for this plate pad (e.g., "PlatePad-001")
        device_type_id: The ObjectId of the Plate Pad device type
        description: Optional description of this plate pad's purpose
        position: Optional 3D position dict {x, y, z} in mm
        linked_robot: Optional name of robot that can access this pad
        linked_teachpoint: Optional teachpoint ID on the robot for this pad
    """
    db = mongodb.get_db()
    devices_collection = db.devices
    
    # Check if device already exists
    existing = devices_collection.find_one({"name": device_name})
    
    device_data = {
        "name": device_name,
        "device_type_id": str(device_type_id),
        "device_category": "static_position",
        "status": "active",
        "description": description or f"Plate pad location: {device_name}",
        "connection": {
            "type": "passive",  # No active connection - it's just a location
            "protocol": None
        },
        "config": {
            "plate_capacity": 1,
            "current_plate_count": 0,
            "accepts_orientation": ["landscape", "portrait"],
            "default_orientation": "landscape"
        },
        "position": position or {
            "x_mm": 0,
            "y_mm": 0,
            "z_mm": 0,
            "rotation_deg": 0
        },
        # Robot linkage - which robot(s) can access this pad and via which teachpoints
        "robot_access": [],
        "updated_at": datetime.now(timezone.utc)
    }
    
    # Add robot linkage if specified
    if linked_robot and linked_teachpoint:
        device_data["robot_access"].append({
            "robot_name": linked_robot,
            "teachpoint_id": linked_teachpoint,
            "access_type": "pick_place",
            "linked_at": datetime.now(timezone.utc)
        })
    
    if existing:
        # Update existing device
        device_data["created_at"] = existing.get("created_at", datetime.now(timezone.utc))
        devices_collection.update_one(
            {"name": device_name},
            {"$set": device_data}
        )
        print(f"✓ Updated device: {device_name}")
    else:
        # Create new device
        device_data["created_at"] = datetime.now(timezone.utc)
        devices_collection.insert_one(device_data)
        print(f"✓ Created device: {device_name}")
    
    return device_name


def setup_sample_plate_pads():
    """Create sample Plate Pad devices for testing."""
    
    print("\n=== Setting up Plate Pad Device Type and Sample Devices ===\n")
    
    # Create the device type
    device_type_id = setup_plate_pad_device_type()
    
    # Create sample plate pads
    print("\nCreating sample Plate Pad devices...")
    
    # Plate Pad 1 - linked to PF400 "home" teachpoint
    create_plate_pad_device(
        device_name="PlatePad-001",
        device_type_id=device_type_id,
        description="Home position plate pad - PF400 home teachpoint",
        position={"x_mm": 264, "y_mm": 625, "z_mm": 267, "rotation_deg": 85},
        linked_robot="PF400-021",
        linked_teachpoint="home"
    )
    
    # Plate Pad 2 - not yet linked to any teachpoint
    create_plate_pad_device(
        device_name="PlatePad-002",
        device_type_id=device_type_id,
        description="Auxiliary plate storage location",
        position={"x_mm": 0, "y_mm": 0, "z_mm": 500, "rotation_deg": 0}
    )
    
    # Plate Pad 3 - staging area
    create_plate_pad_device(
        device_name="PlatePad-Staging",
        device_type_id=device_type_id,
        description="Staging area for plate transfers",
        position={"x_mm": -500, "y_mm": 300, "z_mm": 600, "rotation_deg": 0}
    )
    
    print("\n=== Setup Complete ===")
    print(f"\nDevice Type ID: {device_type_id}")
    print("\nCreated Plate Pads:")
    print("  - PlatePad-001 (linked to PF400-021 'home' teachpoint)")
    print("  - PlatePad-002 (unlinked)")
    print("  - PlatePad-Staging (unlinked)")
    print("\nYou can now associate these with robot teachpoints in the GUI.")


if __name__ == "__main__":
    setup_sample_plate_pads()
