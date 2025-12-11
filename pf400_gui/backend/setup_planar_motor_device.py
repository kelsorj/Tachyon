#!/usr/bin/env python3
"""
Create Planar Motor device type and device in MongoDB.
"""

import db as mongodb
from bson import ObjectId
from datetime import datetime, timezone

def setup_planar_motor_device():
    """Create Planar Motor device type and device."""
    
    db = mongodb.get_db()
    
    # Create or get device type
    device_type_collection = db.device_types
    
    # Check if device type already exists
    device_type = device_type_collection.find_one({
        "vendor": "Planar Motor Inc",
        "product_name": "Planar Motor System"
    })
    
    if not device_type:
        # Create new device type
        device_type_id = ObjectId()
        device_type_collection.insert_one({
            "_id": device_type_id,
            "vendor": "Planar Motor Inc",
            "product_name": "Planar Motor System",
            "description": "Planar Motor Controller (PMC) with XBOT movers",
            "created_at": datetime.now(timezone.utc),
            "updated_at": datetime.now(timezone.utc)
        })
        print(f"Created device type: {device_type_id}")
    else:
        device_type_id = device_type["_id"]
        print(f"Using existing device type: {device_type_id}")
    
    # Create or update device
    devices_collection = db.devices
    
    device_name = "Planar-Motor-001"
    
    device = devices_collection.find_one({"name": device_name})
    
    device_data = {
        "name": device_name,
        "device_type_id": str(device_type_id),
        "status": "active",
        "connection": {
            "ip": "192.168.1.100",
            "port": None,
            "protocol": "TCP"
        },
        "config": {
            "flyway_model": "S4-AS-04-06-OEM-Rev3-FLYWAY-S4-AS",
            "xbot_model": "M3-06-04-OEM-Rev3-XBOT",
            "api_port": 3062
        },
        "created_at": datetime.now(timezone.utc),
        "updated_at": datetime.now(timezone.utc)
    }
    
    if device:
        # Update existing device
        devices_collection.update_one(
            {"name": device_name},
            {"$set": device_data}
        )
        print(f"Updated device: {device_name}")
    else:
        # Create new device
        devices_collection.insert_one(device_data)
        print(f"Created device: {device_name}")
    
    print(f"\nDevice setup complete!")
    print(f"Device Name: {device_name}")
    print(f"Device Type ID: {device_type_id}")
    print(f"PMC IP: 192.168.1.100")
    print(f"API Port: 3062")

if __name__ == "__main__":
    setup_planar_motor_device()

