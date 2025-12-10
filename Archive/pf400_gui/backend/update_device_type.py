#!/usr/bin/env python3
"""Update PF400 device type with 3D model configuration."""

from pymongo import MongoClient
from bson import ObjectId

# MongoDB connection
MONGO_URI = "mongodb://ekmbalps1.corp.eikontx.com:27017/lab-llm"

def update_pf400_device_type():
    """Add 3D model configuration to PF400 device type."""
    
    client = MongoClient(MONGO_URI)
    db = client["lab-llm"]
    
    # The PF400 device type ID from the user's data
    device_type_id = ObjectId("675dc7ea3bab8d2f14e4b2f4")
    
    # 3D Model configuration
    model_3d = {
        # Rendering approach: "primitive" (built-in shapes), "urdf" (load URDF), "static" (single mesh)
        "render_type": "primitive",
        
        # Primary color for the device
        "color": "#3b82f6",
        
        # Device category for model selection
        "category": "articulated_arm",
        
        # URDF configuration (for future high-fidelity rendering)
        "urdf": {
            "path": "pf400_urdf/pf400Complete.urdf",
            "meshes_path": "pf400_urdf/meshes",
            "scale": 0.001  # STL files are in mm, convert to meters
        },
        
        # Primitive model configuration (current simple rendering)
        "primitive": {
            "base": {
                "type": "cylinder",
                "radius": 0.15,
                "height": 0.8,
                "color": "#3b82f6"
            },
            "arm_segments": [
                {"name": "shoulder", "length": 0.4, "color": "#60a5fa"},
                {"name": "elbow", "length": 0.35, "color": "#60a5fa"},
                {"name": "wrist", "length": 0.15, "color": "#93c5fd"}
            ]
        },
        
        # Joint configuration for animation
        "joints": [
            {
                "id": 1,
                "name": "z_lift",
                "type": "prismatic",
                "axis": "y",
                "unit": "mm",
                "range": [0, 500]
            },
            {
                "id": 2,
                "name": "shoulder",
                "type": "revolute",
                "axis": "y",
                "unit": "degrees",
                "range": [-180, 180]
            },
            {
                "id": 3,
                "name": "elbow",
                "type": "revolute",
                "axis": "y",
                "unit": "degrees",
                "range": [-180, 180]
            },
            {
                "id": 4,
                "name": "wrist",
                "type": "revolute",
                "axis": "y",
                "unit": "degrees",
                "range": [-360, 360]
            },
            {
                "id": 5,
                "name": "gripper",
                "type": "prismatic",
                "axis": "z",
                "unit": "mm",
                "range": [0, 130]
            }
        ],
        
        # Display settings
        "label": {
            "height": 1.8,
            "show_joints": True
        },
        
        # Scale in the 3D scene
        "scale": 1.0
    }
    
    # Update the device type
    result = db.device_types.update_one(
        {"_id": device_type_id},
        {
            "$set": {
                "model_3d": model_3d,
                "updated_at": __import__("datetime").datetime.utcnow()
            }
        }
    )
    
    if result.modified_count > 0:
        print("✓ Updated PF400 device type with 3D model configuration")
        
        # Fetch and display the updated document
        doc = db.device_types.find_one({"_id": device_type_id})
        print(f"\nDevice Type: {doc['vendor']} {doc['device_name']}")
        print(f"Render Type: {doc['model_3d']['render_type']}")
        print(f"URDF Path: {doc['model_3d']['urdf']['path']}")
        print(f"Joints: {len(doc['model_3d']['joints'])}")
    else:
        print("⚠ No changes made (document may not exist or already updated)")
    
    client.close()


if __name__ == "__main__":
    update_pf400_device_type()

