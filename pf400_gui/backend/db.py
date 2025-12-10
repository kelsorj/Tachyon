"""MongoDB integration for device configuration, teachpoints, and spatial management.

Collections:
- devices: Individual devices (robots, instruments, etc.)
- sites: Physical locations (Millbrae CA, New York NY, etc.)
- floors: Floors within a site
- spaces: Rooms/areas within a floor
"""

from pymongo import MongoClient
from datetime import datetime
from typing import Optional, Dict, Any, List
import os

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


def get_device_connection(name: str) -> Optional[Dict[str, Any]]:
    """Get connection info for a device."""
    device = get_device_by_name(name)
    if device:
        return device.get("connection", {})
    return None


def get_device_teachpoints(name: str) -> Dict[str, Any]:
    """Get all teachpoints for a device.
    
    Uses 'gui_teachpoints' field to avoid conflict with legacy array-style teachpoints.
    """
    device = get_device_by_name(name)
    if device:
        # Use gui_teachpoints field (dict style) for this GUI
        teachpoints = device.get("gui_teachpoints", {})
        if isinstance(teachpoints, dict):
            return teachpoints
    return {}


def save_teachpoint(device_name: str, teachpoint_id: str, teachpoint_data: Dict[str, Any]) -> bool:
    """Save or update a teachpoint for a device.
    
    Handles both array-style teachpoints (legacy) and dict-style teachpoints (new).
    For this GUI, we use a separate field 'gui_teachpoints' to avoid conflicts.
    """
    try:
        db = get_db()
        print(f"save_teachpoint: device={device_name}, id={teachpoint_id}")
        
        # Add timestamp and id to the data
        teachpoint_data["updated_at"] = datetime.utcnow()
        teachpoint_data["id"] = teachpoint_id
        if "created_at" not in teachpoint_data:
            teachpoint_data["created_at"] = datetime.utcnow()
        
        # Use gui_teachpoints field to avoid conflict with legacy array-style teachpoints
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
            print(f"Saved teachpoint '{teachpoint_id}' for {device_name}")
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
    """Delete a teachpoint from a device."""
    try:
        db = get_db()
        
        # Use gui_teachpoints field to match save_teachpoint
        result = db.devices.update_one(
            {"name": device_name},
            {
                "$unset": {f"gui_teachpoints.{teachpoint_id}": ""},
                "$set": {"updated_at": datetime.utcnow()}
            }
        )
        
        if result.modified_count > 0:
            print(f"Deleted teachpoint '{teachpoint_id}' from {device_name}")
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

