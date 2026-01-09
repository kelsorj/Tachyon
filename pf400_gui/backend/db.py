"""MongoDB integration for device configuration, teachpoints, and spatial management.

Collections:
- devices: Individual devices (robots, instruments, etc.)
- sites: Physical locations (Millbrae CA, New York NY, etc.)
- floors: Floors within a site
- spaces: Rooms/areas within a floor
"""

from pymongo import MongoClient
from bson import ObjectId
from datetime import datetime
from typing import Optional, Dict, Any, List
import os
import time
import secrets

# MongoDB connection settings
MONGO_URI = os.environ.get("MONGO_URI", "mongodb://ekmbalps1.corp.eikontx.com:27017")
MONGO_DB = os.environ.get("MONGO_DB", "lab-llm")

# Global client
_client: Optional[MongoClient] = None
_db = None


def get_db():
    """Get MongoDB database connection."""
    global _client, _db
    if _client is None:
        print(f"Connecting to MongoDB: {MONGO_URI}/{MONGO_DB}")
        _client = MongoClient(MONGO_URI, serverSelectionTimeoutMS=5000)
        _db = _client[MONGO_DB]
        # Test connection
        try:
            _client.admin.command('ping')
            print("MongoDB connection successful")
        except Exception as e:
            print(f"MongoDB connection failed: {e}")
            _client = None
            _db = None
            raise
    return _db


# ============== SITES ==============

def get_all_sites() -> List[Dict[str, Any]]:
    """Get all sites."""
    try:
        db = get_db()
        sites = list(db.sites.find({}))
        for site in sites:
            site["_id"] = str(site["_id"])
        return sites
    except Exception as e:
        print(f"Error getting sites: {e}")
        return []


def get_site(site_id: str) -> Optional[Dict[str, Any]]:
    """Get a site by ID."""
    try:
        db = get_db()
        site = db.sites.find_one({"site_id": site_id})
        if site:
            site["_id"] = str(site["_id"])
        return site
    except Exception as e:
        print(f"Error getting site {site_id}: {e}")
        return None


def create_site(site_data: Dict[str, Any]) -> bool:
    """Create a new site."""
    try:
        db = get_db()
        site_data["created_at"] = datetime.utcnow()
        site_data["updated_at"] = datetime.utcnow()
        result = db.sites.insert_one(site_data)
        print(f"Created site: {site_data.get('name')}")
        return result.inserted_id is not None
    except Exception as e:
        print(f"Error creating site: {e}")
        return False


def update_site(site_id: str, updates: Dict[str, Any]) -> bool:
    """Update a site."""
    try:
        db = get_db()
        updates["updated_at"] = datetime.utcnow()
        result = db.sites.update_one(
            {"site_id": site_id},
            {"$set": updates}
        )
        return result.modified_count > 0
    except Exception as e:
        print(f"Error updating site: {e}")
        return False


def delete_site(site_id: str) -> bool:
    """Delete a site and all its floors/spaces."""
    try:
        db = get_db()
        # Delete all spaces in floors of this site
        floors = list(db.floors.find({"site_id": site_id}))
        for floor in floors:
            db.spaces.delete_many({"floor_id": floor["floor_id"]})
        # Delete all floors
        db.floors.delete_many({"site_id": site_id})
        # Delete the site
        result = db.sites.delete_one({"site_id": site_id})
        return result.deleted_count > 0
    except Exception as e:
        print(f"Error deleting site: {e}")
        return False


# ============== FLOORS ==============

def get_floors_by_site(site_id: str) -> List[Dict[str, Any]]:
    """Get all floors for a site."""
    try:
        db = get_db()
        floors = list(db.floors.find({"site_id": site_id}).sort("order", 1))
        for floor in floors:
            floor["_id"] = str(floor["_id"])
        return floors
    except Exception as e:
        print(f"Error getting floors for site {site_id}: {e}")
        return []


def get_floor(floor_id: str) -> Optional[Dict[str, Any]]:
    """Get a floor by ID."""
    try:
        db = get_db()
        floor = db.floors.find_one({"floor_id": floor_id})
        if floor:
            floor["_id"] = str(floor["_id"])
        return floor
    except Exception as e:
        print(f"Error getting floor {floor_id}: {e}")
        return None


def create_floor(floor_data: Dict[str, Any]) -> bool:
    """Create a new floor."""
    try:
        db = get_db()
        floor_data["created_at"] = datetime.utcnow()
        floor_data["updated_at"] = datetime.utcnow()
        result = db.floors.insert_one(floor_data)
        print(f"Created floor: {floor_data.get('name')}")
        return result.inserted_id is not None
    except Exception as e:
        print(f"Error creating floor: {e}")
        return False


def update_floor(floor_id: str, updates: Dict[str, Any]) -> bool:
    """Update a floor."""
    try:
        db = get_db()
        updates["updated_at"] = datetime.utcnow()
        result = db.floors.update_one(
            {"floor_id": floor_id},
            {"$set": updates}
        )
        return result.modified_count > 0
    except Exception as e:
        print(f"Error updating floor: {e}")
        return False


def delete_floor(floor_id: str) -> bool:
    """Delete a floor and all its spaces."""
    try:
        db = get_db()
        # Delete all spaces in this floor
        db.spaces.delete_many({"floor_id": floor_id})
        # Delete the floor
        result = db.floors.delete_one({"floor_id": floor_id})
        return result.deleted_count > 0
    except Exception as e:
        print(f"Error deleting floor: {e}")
        return False


# ============== SPACES (ROOMS) ==============

def get_spaces_by_floor(floor_id: str) -> List[Dict[str, Any]]:
    """Get all spaces for a floor."""
    try:
        db = get_db()
        spaces = list(db.spaces.find({"floor_id": floor_id}))
        for space in spaces:
            space["_id"] = str(space["_id"])
        return spaces
    except Exception as e:
        print(f"Error getting spaces for floor {floor_id}: {e}")
        return []


def get_space(space_id: str) -> Optional[Dict[str, Any]]:
    """Get a space by ID."""
    try:
        db = get_db()
        space = db.spaces.find_one({"space_id": space_id})
        if space:
            space["_id"] = str(space["_id"])
        return space
    except Exception as e:
        print(f"Error getting space {space_id}: {e}")
        return None


def create_space(space_data: Dict[str, Any]) -> bool:
    """Create a new space."""
    try:
        db = get_db()
        space_data["created_at"] = datetime.utcnow()
        space_data["updated_at"] = datetime.utcnow()
        result = db.spaces.insert_one(space_data)
        print(f"Created space: {space_data.get('name')}")
        return result.inserted_id is not None
    except Exception as e:
        print(f"Error creating space: {e}")
        return False


def update_space(space_id: str, updates: Dict[str, Any]) -> bool:
    """Update a space (including boundary points)."""
    try:
        db = get_db()
        updates["updated_at"] = datetime.utcnow()
        result = db.spaces.update_one(
            {"space_id": space_id},
            {"$set": updates}
        )
        return result.modified_count > 0
    except Exception as e:
        print(f"Error updating space: {e}")
        return False


def delete_space(space_id: str) -> bool:
    """Delete a space."""
    try:
        db = get_db()
        result = db.spaces.delete_one({"space_id": space_id})
        return result.deleted_count > 0
    except Exception as e:
        print(f"Error deleting space: {e}")
        return False


# ============== DEVICES WITH SPATIAL INFO ==============

def get_devices_by_space(space_id: str) -> List[Dict[str, Any]]:
    """Get all devices in a space."""
    try:
        db = get_db()
        devices = list(db.devices.find({"space_id": space_id}))
        for device in devices:
            device["_id"] = str(device["_id"])
        return devices
    except Exception as e:
        print(f"Error getting devices for space {space_id}: {e}")
        return []


def get_devices_by_floor(floor_id: str) -> List[Dict[str, Any]]:
    """Get all devices on a floor (across all spaces)."""
    try:
        db = get_db()
        # Get all spaces on this floor
        spaces = get_spaces_by_floor(floor_id)
        space_ids = [s["space_id"] for s in spaces]
        # Get all devices in those spaces
        devices = list(db.devices.find({"space_id": {"$in": space_ids}}))
        for device in devices:
            device["_id"] = str(device["_id"])
        return devices
    except Exception as e:
        print(f"Error getting devices for floor {floor_id}: {e}")
        return []


def update_device_position(device_name: str, position: Dict[str, float], rotation: Dict[str, float] = None) -> bool:
    """Update a device's position in 3D space."""
    try:
        db = get_db()
        updates = {
            "position": position,
            "updated_at": datetime.utcnow()
        }
        if rotation:
            updates["rotation"] = rotation
        
        result = db.devices.update_one(
            {"name": device_name},
            {"$set": updates}
        )
        return result.modified_count > 0
    except Exception as e:
        print(f"Error updating device position: {e}")
        return False


def assign_device_to_space(device_name: str, space_id: str) -> bool:
    """Assign a device to a space."""
    try:
        db = get_db()
        result = db.devices.update_one(
            {"name": device_name},
            {"$set": {
                "space_id": space_id,
                "updated_at": datetime.utcnow()
            }}
        )
        return result.modified_count > 0
    except Exception as e:
        print(f"Error assigning device to space: {e}")
        return False


def get_all_devices() -> List[Dict[str, Any]]:
    """Get all devices."""
    try:
        db = get_db()
        devices = list(db.devices.find({}))
        for device in devices:
            device["_id"] = str(device["_id"])
        return devices
    except Exception as e:
        print(f"Error getting all devices: {e}")
        return []


# ============== LABWARE TYPES ==============

def _new_ulid_str() -> str:
    """Dependency-free ULID generator (26 chars)."""
    alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"

    def enc(v: int, n: int) -> str:
        out = ["0"] * n
        for i in range(n - 1, -1, -1):
            out[i] = alphabet[v & 31]
            v >>= 5
        return "".join(out)

    ts_ms = int(time.time() * 1000) & ((1 << 48) - 1)
    rnd = int.from_bytes(secrets.token_bytes(10), "big")  # 80 bits
    return enc(ts_ms, 10) + enc(rnd, 16)


def get_all_labware_types() -> List[Dict[str, Any]]:
    """Get all labware types."""
    try:
        db = get_db()
        labware_types = list(db.labware_types.find({}).sort([("name", 1), ("created_at", -1)]))
        for lt in labware_types:
            lt["_id"] = str(lt["_id"])
        return labware_types
    except Exception as e:
        print(f"Error getting all labware types: {e}")
        return []


def create_labware_type(labware_type: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Create a new labware type. Returns the created document (with string _id) on success."""
    try:
        db = get_db()
        labware_type = dict(labware_type or {})
        if not labware_type.get("labware_type_id"):
            labware_type["labware_type_id"] = _new_ulid_str()
        labware_type["created_at"] = datetime.utcnow()
        labware_type["updated_at"] = datetime.utcnow()
        result = db.labware_types.insert_one(labware_type)
        if not result.inserted_id:
            return None
        created = db.labware_types.find_one({"_id": result.inserted_id})
        if not created:
            return None
        created["_id"] = str(created["_id"])
        return created
    except Exception as e:
        print(f"Error creating labware type: {e}")
        return None


def delete_labware_type(labware_type_id: str) -> bool:
    """Delete a labware type by labware_type_id."""
    try:
        db = get_db()
        result = db.labware_types.delete_one({"labware_type_id": labware_type_id})
        return result.deleted_count > 0
    except Exception as e:
        print(f"Error deleting labware type {labware_type_id}: {e}")
        return False


def get_labware_type_by_id(labware_type_id: str) -> Optional[Dict[str, Any]]:
    """Get a labware type by labware_type_id or _id."""
    try:
        db = get_db()
        # Try searching by labware_type_id field first
        doc = db.labware_types.find_one({"labware_type_id": labware_type_id})
        # If not found, try by MongoDB _id
        if not doc:
            try:
                doc = db.labware_types.find_one({"_id": ObjectId(labware_type_id)})
            except Exception:
                pass  # Invalid ObjectId format, ignore
        if doc:
            doc["_id"] = str(doc["_id"])
        return doc
    except Exception as e:
        print(f"Error getting labware type {labware_type_id}: {e}")
        return None


def update_labware_type(labware_type_id: str, updates: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Update a labware type by labware_type_id. Returns updated document."""
    try:
        db = get_db()
        updates = dict(updates or {})
        updates["updated_at"] = datetime.utcnow()
        result = db.labware_types.update_one(
            {"labware_type_id": labware_type_id},
            {"$set": updates}
        )
        if result.matched_count == 0:
            return None
        return get_labware_type_by_id(labware_type_id)
    except Exception as e:
        print(f"Error updating labware type {labware_type_id}: {e}")
        return None


# ============== LABWARE CLASSES ==============

def get_all_labware_classes() -> List[Dict[str, Any]]:
    """Get all labware classes."""
    try:
        db = get_db()
        classes = list(db.labware_classes.find({}).sort([("name", 1), ("created_at", -1)]))
        for c in classes:
            c["_id"] = str(c["_id"])
        return classes
    except Exception as e:
        print(f"Error getting all labware classes: {e}")
        return []


def create_labware_class(labware_class: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Create a labware class. Returns created document."""
    try:
        db = get_db()
        labware_class = dict(labware_class or {})
        if not labware_class.get("labware_class_id"):
            labware_class["labware_class_id"] = _new_ulid_str()
        labware_class["created_at"] = datetime.utcnow()
        labware_class["updated_at"] = datetime.utcnow()
        result = db.labware_classes.insert_one(labware_class)
        if not result.inserted_id:
            return None
        created = db.labware_classes.find_one({"_id": result.inserted_id})
        if not created:
            return None
        created["_id"] = str(created["_id"])
        return created
    except Exception as e:
        print(f"Error creating labware class: {e}")
        return None


def delete_labware_class(labware_class_id: str) -> bool:
    """Delete a labware class by labware_class_id. Also removes it from all labware types."""
    try:
        db = get_db()
        db.labware_types.update_many(
            {"labware_class_ids": labware_class_id},
            {"$pull": {"labware_class_ids": labware_class_id}, "$set": {"updated_at": datetime.utcnow()}}
        )
        result = db.labware_classes.delete_one({"labware_class_id": labware_class_id})
        return result.deleted_count > 0
    except Exception as e:
        print(f"Error deleting labware class {labware_class_id}: {e}")
        return False


def update_labware_class(labware_class_id: str, updates: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Update a labware class by id. Returns updated document."""
    try:
        db = get_db()
        updates = dict(updates or {})
        updates["updated_at"] = datetime.utcnow()
        result = db.labware_classes.update_one(
            {"labware_class_id": labware_class_id},
            {"$set": updates}
        )
        if result.matched_count == 0:
            return None
        doc = db.labware_classes.find_one({"labware_class_id": labware_class_id})
        if doc:
            doc["_id"] = str(doc["_id"])
        return doc
    except Exception as e:
        print(f"Error updating labware class {labware_class_id}: {e}")
        return None


def get_device_by_name(name: str) -> Optional[Dict[str, Any]]:
    """Get a device document by name."""
    try:
        db = get_db()
        device = db.devices.find_one({"name": name})
        if device:
            device["_id"] = str(device["_id"])  # Convert ObjectId to string
        return device
    except Exception as e:
        print(f"Error getting device {name}: {e}")
        return None


def create_device(data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Create a new device."""
    try:
        db = get_db()
        
        # Check if device with this name already exists
        if db.devices.find_one({"name": data.get("name")}):
            print(f"Device with name '{data.get('name')}' already exists")
            return None
        
        # Add timestamps
        data["created_at"] = datetime.utcnow()
        data["updated_at"] = datetime.utcnow()
        
        result = db.devices.insert_one(data)
        if not result.inserted_id:
            return None
        
        created = db.devices.find_one({"_id": result.inserted_id})
        if created:
            created["_id"] = str(created["_id"])
        return created
    except Exception as e:
        print(f"Error creating device: {e}")
        return None


def get_device_connection(name: str) -> Optional[Dict[str, Any]]:
    """Get connection info for a device."""
    device = get_device_by_name(name)
    if device:
        return device.get("connection", {})
    return None


def get_device_teachpoints(name: str) -> Dict[str, Any]:
    """Get all GUI-managed teachpoints for a device.
    
    Note: Uses 'gui_teachpoints' field to avoid conflict with legacy 'teachpoints' 
    array format used by some devices (e.g., PF400).
    """
    device = get_device_by_name(name)
    if device:
        # Use gui_teachpoints field for our GUI-managed teachpoints
        gui_tps = device.get("gui_teachpoints", {})
        if isinstance(gui_tps, dict):
            return gui_tps
    return {}


def save_teachpoint(device_name: str, teachpoint_id: str, teachpoint_data: Dict[str, Any]) -> bool:
    """Save or update a GUI-managed teachpoint for a device.
    
    Note: Uses 'gui_teachpoints' field to avoid conflict with legacy 'teachpoints' array.
    """
    try:
        db = get_db()
        print(f"save_teachpoint: device={device_name}, id={teachpoint_id}")
        
        # Add timestamp and id to the data
        teachpoint_data["updated_at"] = datetime.utcnow()
        teachpoint_data["id"] = teachpoint_id
        if "created_at" not in teachpoint_data:
            teachpoint_data["created_at"] = datetime.utcnow()
        
        # Save to gui_teachpoints field (avoids conflict with legacy teachpoints array)
        result = db.devices.update_one(
            {"name": device_name},
            {
                "$set": {
                    f"gui_teachpoints.{teachpoint_id}": teachpoint_data,
                    "updated_at": datetime.utcnow()
                },
                "$setOnInsert": {
                    "name": device_name,
                    "created_at": datetime.utcnow()
                }
            },
            upsert=True
        )
        
        print(f"save_teachpoint: modified={result.modified_count}, upserted={result.upserted_id}, matched={result.matched_count}")
        
        if result.modified_count > 0 or result.upserted_id:
            print(f"Saved teachpoint '{teachpoint_id}' for {device_name} (gui_teachpoints)")
            return True
        else:
            # Check if data was matched but not modified (same data)
            if result.matched_count > 0:
                print(f"Teachpoint '{teachpoint_id}' unchanged (same data)")
                return True
            print(f"No document modified for teachpoint '{teachpoint_id}'")
            return False
            
    except Exception as e:
        print(f"Error saving teachpoint: {e}")
        import traceback
        traceback.print_exc()
        return False


def delete_teachpoint(device_name: str, teachpoint_id: str) -> bool:
    """Delete a GUI-managed teachpoint from a device."""
    try:
        db = get_db()
        
        result = db.devices.update_one(
            {"name": device_name},
            {
                "$unset": {f"gui_teachpoints.{teachpoint_id}": ""},
                "$set": {"updated_at": datetime.utcnow()}
            }
        )
        
        if result.modified_count > 0:
            print(f"Deleted teachpoint '{teachpoint_id}' from {device_name} (gui_teachpoints)")
            return True
        return False
        
    except Exception as e:
        print(f"Error deleting teachpoint: {e}")
        return False


def update_device_state(device_name: str, state: Dict[str, Any]) -> bool:
    """Update the current state of a device."""
    try:
        db = get_db()
        
        state["last_seen"] = datetime.utcnow()
        
        result = db.devices.update_one(
            {"name": device_name},
            {
                "$set": {
                    "state": state,
                    "updated_at": datetime.utcnow()
                }
            }
        )
        
        return result.modified_count > 0
        
    except Exception as e:
        print(f"Error updating device state: {e}")
        return False


# ============== DEVICE REACHABILITY & TEACHPOINT LINKING ==============

def get_reachable_devices(device_name: str) -> List[Dict[str, Any]]:
    """Get list of devices that this device can physically reach."""
    try:
        db = get_db()
        device = db.devices.find_one({"name": device_name})
        if device:
            return device.get("reachable_devices", [])
        return []
    except Exception as e:
        print(f"Error getting reachable devices: {e}")
        return []


def add_reachable_device(device_name: str, target_device: str, access_type: str = "handoff", description: str = "") -> bool:
    """Add a device to the list of reachable devices."""
    try:
        db = get_db()
        
        # Check if already exists
        device = db.devices.find_one({"name": device_name})
        if device:
            reachable = device.get("reachable_devices", [])
            for r in reachable:
                if r.get("device_name") == target_device:
                    print(f"{target_device} is already in reachable_devices for {device_name}")
                    return True
        
        result = db.devices.update_one(
            {"name": device_name},
            {
                "$push": {
                    "reachable_devices": {
                        "device_name": target_device,
                        "access_type": access_type,
                        "description": description,
                        "added_at": datetime.utcnow()
                    }
                },
                "$set": {"updated_at": datetime.utcnow()}
            }
        )
        
        return result.modified_count > 0
    except Exception as e:
        print(f"Error adding reachable device: {e}")
        return False


def remove_reachable_device(device_name: str, target_device: str) -> bool:
    """Remove a device from the reachable devices list."""
    try:
        db = get_db()
        
        result = db.devices.update_one(
            {"name": device_name},
            {
                "$pull": {"reachable_devices": {"device_name": target_device}},
                "$set": {"updated_at": datetime.utcnow()}
            }
        )
        
        return result.modified_count > 0
    except Exception as e:
        print(f"Error removing reachable device: {e}")
        return False


def link_teachpoints(
    source_device: str, 
    source_teachpoint_id: str, 
    target_device: str, 
    target_teachpoint_id: str,
    transfer_type: str = "dropoff"  # "dropoff" = source drops here, "pickup" = source picks up here
) -> bool:
    """
    Link a GUI-managed teachpoint on one device to a teachpoint on another device.
    This creates a bidirectional link for handoff coordination.
    
    Args:
        source_device: The device initiating the transfer (e.g., PF400-021)
        source_teachpoint_id: Teachpoint ID on source device
        target_device: The device receiving/providing (e.g., Planar-Motor-001)
        target_teachpoint_id: Teachpoint ID on target device
        transfer_type: "dropoff" if source drops plate here, "pickup" if source picks up
    """
    try:
        db = get_db()
        
        # Update source device teachpoint with linked_to (using gui_teachpoints)
        source_result = db.devices.update_one(
            {"name": source_device},
            {
                "$set": {
                    f"gui_teachpoints.{source_teachpoint_id}.linked_to": {
                        "device_name": target_device,
                        "teachpoint_id": target_teachpoint_id,
                        "transfer_type": transfer_type,
                        "linked_at": datetime.utcnow()
                    },
                    "updated_at": datetime.utcnow()
                }
            }
        )
        
        # Update target device teachpoint with linked_from
        inverse_type = "pickup" if transfer_type == "dropoff" else "dropoff"
        target_result = db.devices.update_one(
            {"name": target_device},
            {
                "$set": {
                    f"gui_teachpoints.{target_teachpoint_id}.linked_from": {
                        "device_name": source_device,
                        "teachpoint_id": source_teachpoint_id,
                        "transfer_type": inverse_type,
                        "linked_at": datetime.utcnow()
                    },
                    "updated_at": datetime.utcnow()
                }
            }
        )
        
        if source_result.modified_count > 0 or target_result.modified_count > 0:
            print(f"Linked {source_device}:{source_teachpoint_id} → {target_device}:{target_teachpoint_id} (gui_teachpoints)")
            return True
        return False
        
    except Exception as e:
        print(f"Error linking teachpoints: {e}")
        import traceback
        traceback.print_exc()
        return False


def unlink_teachpoints(source_device: str, source_teachpoint_id: str) -> bool:
    """
    Remove the link from a GUI-managed teachpoint. Also removes the corresponding linked_from on the target.
    """
    try:
        db = get_db()
        
        # First, get the current link info
        source = db.devices.find_one({"name": source_device})
        if not source:
            return False
            
        gui_teachpoints = source.get("gui_teachpoints", {})
        if not isinstance(gui_teachpoints, dict):
            return False
            
        tp = gui_teachpoints.get(source_teachpoint_id, {})
        linked_to = tp.get("linked_to")
        
        if not linked_to:
            print(f"Teachpoint {source_teachpoint_id} is not linked")
            return True
        
        target_device = linked_to.get("device_name")
        target_teachpoint_id = linked_to.get("teachpoint_id")
        
        # Remove linked_to from source (gui_teachpoints)
        db.devices.update_one(
            {"name": source_device},
            {
                "$unset": {f"gui_teachpoints.{source_teachpoint_id}.linked_to": ""},
                "$set": {"updated_at": datetime.utcnow()}
            }
        )
        
        # Remove linked_from from target (gui_teachpoints)
        if target_device and target_teachpoint_id:
            db.devices.update_one(
                {"name": target_device},
                {
                    "$unset": {f"gui_teachpoints.{target_teachpoint_id}.linked_from": ""},
                    "$set": {"updated_at": datetime.utcnow()}
                }
            )
        
        print(f"Unlinked {source_device}:{source_teachpoint_id} (gui_teachpoints)")
        return True
        
    except Exception as e:
        print(f"Error unlinking teachpoints: {e}")
        return False


def get_linked_teachpoints(device_name: str) -> List[Dict[str, Any]]:
    """Get all GUI-managed teachpoints on a device that have links to other devices."""
    try:
        db = get_db()
        device = db.devices.find_one({"name": device_name})
        if not device:
            return []
        
        gui_teachpoints = device.get("gui_teachpoints", {})
        if not isinstance(gui_teachpoints, dict):
            return []
        
        linked = []
        for tp_id, tp_data in gui_teachpoints.items():
            if not isinstance(tp_data, dict):
                continue
            if tp_data.get("linked_to") or tp_data.get("linked_from"):
                linked.append({
                    "id": tp_id,
                    "name": tp_data.get("name", tp_id),
                    "linked_to": tp_data.get("linked_to"),
                    "linked_from": tp_data.get("linked_from")
                })
        
        return linked
    except Exception as e:
        print(f"Error getting linked teachpoints: {e}")
        return []


# ============== DEVICE COLLECTIONS (Workflow) ==============

def get_all_device_collections() -> List[Dict[str, Any]]:
    """Get all device collections."""
    try:
        db = get_db()
        collections = list(db.device_collections.find({}).sort("name", 1))
        for c in collections:
            c["_id"] = str(c["_id"])
        return collections
    except Exception as e:
        print(f"Error getting device collections: {e}")
        return []


def get_device_collection(collection_id: str) -> Optional[Dict[str, Any]]:
    """Get a device collection by ID."""
    try:
        db = get_db()
        doc = db.device_collections.find_one({"collection_id": collection_id})
        if doc:
            doc["_id"] = str(doc["_id"])
        return doc
    except Exception as e:
        print(f"Error getting device collection {collection_id}: {e}")
        return None


def create_device_collection(data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Create a new device collection."""
    try:
        db = get_db()
        data = dict(data or {})
        if not data.get("collection_id"):
            data["collection_id"] = _new_ulid_str()
        data["created_at"] = datetime.utcnow()
        data["updated_at"] = datetime.utcnow()
        result = db.device_collections.insert_one(data)
        if not result.inserted_id:
            return None
        created = db.device_collections.find_one({"_id": result.inserted_id})
        if created:
            created["_id"] = str(created["_id"])
        return created
    except Exception as e:
        print(f"Error creating device collection: {e}")
        return None


def update_device_collection(collection_id: str, updates: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Update a device collection."""
    try:
        db = get_db()
        updates = dict(updates or {})
        updates["updated_at"] = datetime.utcnow()
        result = db.device_collections.update_one(
            {"collection_id": collection_id},
            {"$set": updates}
        )
        if result.matched_count == 0:
            return None
        return get_device_collection(collection_id)
    except Exception as e:
        print(f"Error updating device collection {collection_id}: {e}")
        return None


def delete_device_collection(collection_id: str) -> bool:
    """Delete a device collection."""
    try:
        db = get_db()
        result = db.device_collections.delete_one({"collection_id": collection_id})
        return result.deleted_count > 0
    except Exception as e:
        print(f"Error deleting device collection {collection_id}: {e}")
        return False


# ============== WORKFLOWS ==============

def get_all_workflows() -> List[Dict[str, Any]]:
    """Get all workflows."""
    try:
        db = get_db()
        workflows = list(db.workflows.find({}).sort([("name", 1), ("created_at", -1)]))
        for w in workflows:
            w["_id"] = str(w["_id"])
        return workflows
    except Exception as e:
        print(f"Error getting workflows: {e}")
        return []


def get_workflow(workflow_id: str) -> Optional[Dict[str, Any]]:
    """Get a workflow by ID."""
    try:
        db = get_db()
        doc = db.workflows.find_one({"workflow_id": workflow_id})
        if doc:
            doc["_id"] = str(doc["_id"])
        return doc
    except Exception as e:
        print(f"Error getting workflow {workflow_id}: {e}")
        return None


def create_workflow(data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Create a new workflow."""
    try:
        db = get_db()
        data = dict(data or {})
        if not data.get("workflow_id"):
            data["workflow_id"] = _new_ulid_str()
        data["created_at"] = datetime.utcnow()
        data["updated_at"] = datetime.utcnow()
        # Initialize empty nodes/edges if not provided
        if "nodes" not in data:
            data["nodes"] = []
        if "edges" not in data:
            data["edges"] = []
        result = db.workflows.insert_one(data)
        if not result.inserted_id:
            return None
        created = db.workflows.find_one({"_id": result.inserted_id})
        if created:
            created["_id"] = str(created["_id"])
        return created
    except Exception as e:
        print(f"Error creating workflow: {e}")
        return None


def update_workflow(workflow_id: str, updates: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Update a workflow."""
    try:
        db = get_db()
        updates = dict(updates or {})
        updates["updated_at"] = datetime.utcnow()
        result = db.workflows.update_one(
            {"workflow_id": workflow_id},
            {"$set": updates}
        )
        if result.matched_count == 0:
            return None
        return get_workflow(workflow_id)
    except Exception as e:
        print(f"Error updating workflow {workflow_id}: {e}")
        return None


def delete_workflow(workflow_id: str) -> bool:
    """Delete a workflow."""
    try:
        db = get_db()
        result = db.workflows.delete_one({"workflow_id": workflow_id})
        return result.deleted_count > 0
    except Exception as e:
        print(f"Error deleting workflow {workflow_id}: {e}")
        return False


# ============== WORKFLOW RUNS ==============

def get_workflow_runs(workflow_id: Optional[str] = None, limit: int = 50) -> List[Dict[str, Any]]:
    """Get workflow runs, optionally filtered by workflow_id."""
    try:
        db = get_db()
        query = {}
        if workflow_id:
            query["workflow_id"] = workflow_id
        runs = list(db.workflow_runs.find(query).sort("started_at", -1).limit(limit))
        for r in runs:
            r["_id"] = str(r["_id"])
        return runs
    except Exception as e:
        print(f"Error getting workflow runs: {e}")
        return []


def get_workflow_run(run_id: str) -> Optional[Dict[str, Any]]:
    """Get a workflow run by ID."""
    try:
        db = get_db()
        doc = db.workflow_runs.find_one({"run_id": run_id})
        if doc:
            doc["_id"] = str(doc["_id"])
        return doc
    except Exception as e:
        print(f"Error getting workflow run {run_id}: {e}")
        return None


def create_workflow_run(data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Create a new workflow run."""
    try:
        db = get_db()
        data = dict(data or {})
        if not data.get("run_id"):
            data["run_id"] = _new_ulid_str()
        data["started_at"] = datetime.utcnow()
        data["updated_at"] = datetime.utcnow()
        # Default state
        if "state" not in data:
            data["state"] = "pending"
        if "step_states" not in data:
            data["step_states"] = {}
        result = db.workflow_runs.insert_one(data)
        if not result.inserted_id:
            return None
        created = db.workflow_runs.find_one({"_id": result.inserted_id})
        if created:
            created["_id"] = str(created["_id"])
        return created
    except Exception as e:
        print(f"Error creating workflow run: {e}")
        return None


def update_workflow_run(run_id: str, updates: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Update a workflow run."""
    try:
        db = get_db()
        updates = dict(updates or {})
        updates["updated_at"] = datetime.utcnow()
        result = db.workflow_runs.update_one(
            {"run_id": run_id},
            {"$set": updates}
        )
        if result.matched_count == 0:
            return None
        return get_workflow_run(run_id)
    except Exception as e:
        print(f"Error updating workflow run {run_id}: {e}")
        return None


def delete_workflow_run(run_id: str) -> bool:
    """Delete a workflow run."""
    try:
        db = get_db()
        result = db.workflow_runs.delete_one({"run_id": run_id})
        return result.deleted_count > 0
    except Exception as e:
        print(f"Error deleting workflow run {run_id}: {e}")
        return False


# ============== CODE MODULES ==============

def get_all_code_modules() -> List[Dict[str, Any]]:
    """Get all code modules."""
    try:
        db = get_db()
        modules = list(db.code_modules.find({}).sort("name", 1))
        for m in modules:
            m["_id"] = str(m["_id"])
        return modules
    except Exception as e:
        print(f"Error getting code modules: {e}")
        return []


def get_code_module(module_id: str) -> Optional[Dict[str, Any]]:
    """Get a code module by ID."""
    try:
        db = get_db()
        doc = db.code_modules.find_one({"module_id": module_id})
        if doc:
            doc["_id"] = str(doc["_id"])
        return doc
    except Exception as e:
        print(f"Error getting code module {module_id}: {e}")
        return None


def create_code_module(data: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Create a new code module."""
    try:
        db = get_db()
        data = dict(data or {})
        if not data.get("module_id"):
            data["module_id"] = _new_ulid_str()
        data["created_at"] = datetime.utcnow()
        data["updated_at"] = datetime.utcnow()
        result = db.code_modules.insert_one(data)
        if not result.inserted_id:
            return None
        created = db.code_modules.find_one({"_id": result.inserted_id})
        if created:
            created["_id"] = str(created["_id"])
        return created
    except Exception as e:
        print(f"Error creating code module: {e}")
        return None


def update_code_module(module_id: str, updates: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """Update a code module."""
    try:
        db = get_db()
        updates = dict(updates or {})
        updates["updated_at"] = datetime.utcnow()
        result = db.code_modules.update_one(
            {"module_id": module_id},
            {"$set": updates}
        )
        if result.matched_count == 0:
            return None
        return get_code_module(module_id)
    except Exception as e:
        print(f"Error updating code module {module_id}: {e}")
        return None


def delete_code_module(module_id: str) -> bool:
    """Delete a code module."""
    try:
        db = get_db()
        result = db.code_modules.delete_one({"module_id": module_id})
        return result.deleted_count > 0
    except Exception as e:
        print(f"Error deleting code module {module_id}: {e}")
        return False

