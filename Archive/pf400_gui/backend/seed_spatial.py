#!/usr/bin/env python3
"""Seed script to populate spatial data for Millbrae site."""

import db

def seed_millbrae():
    """Create the Millbrae site with all floors."""
    
    # Create Millbrae site
    site_data = {
        "site_id": "millbrae",
        "name": "Millbrae, CA",
        "address": "1000 Gateway Blvd, Millbrae, CA 94030",
        "coordinates": {"lat": 37.5996, "lng": -122.3865}
    }
    
    existing = db.get_site("millbrae")
    if existing:
        print("Millbrae site already exists, skipping...")
    else:
        db.create_site(site_data)
        print("Created Millbrae site")
    
    # Create floors
    floors = [
        {"floor_id": "millbrae-b", "name": "Basement", "order": -1, "floorplan_image": "millbrae/basement.png"},
        {"floor_id": "millbrae-1", "name": "1st Floor", "order": 1, "floorplan_image": "millbrae/1st floor.png"},
        {"floor_id": "millbrae-2", "name": "2nd Floor", "order": 2, "floorplan_image": "millbrae/2nd floor.png"},
        {"floor_id": "millbrae-3", "name": "3rd Floor", "order": 3, "floorplan_image": "millbrae/3rd floor.png"},
        {"floor_id": "millbrae-4", "name": "4th Floor", "order": 4, "floorplan_image": "millbrae/4th floor.png"},
        {"floor_id": "millbrae-5", "name": "5th Floor", "order": 5, "floorplan_image": "millbrae/5th floor.png"},
        {"floor_id": "millbrae-6", "name": "6th Floor", "order": 6, "floorplan_image": "millbrae/6th floor.png"},
    ]
    
    for floor_data in floors:
        floor_data["site_id"] = "millbrae"
        # Floorplan is ~4:1 aspect ratio, estimate ~100m x 25m
        floor_data["dimensions"] = {"width": 100, "depth": 25, "height": 3.5}
        floor_data["scale"] = 16  # pixels per meter (1600px / 100m)
        
        existing = db.get_floor(floor_data["floor_id"])
        if existing:
            print(f"Floor {floor_data['name']} already exists, skipping...")
        else:
            db.create_floor(floor_data)
            print(f"Created floor: {floor_data['name']}")
    
    # Create key spaces on 3rd floor (where the Automation lab is)
    spaces_3rd = [
        {
            "space_id": "millbrae-3-automation",
            "name": "Automation",
            "room_number": "3-L-032",
            "color": "#4fc3f7",  # Light blue
            "height": 3.5
        },
        {
            "space_id": "millbrae-3-htc-tc",
            "name": "HTC TC Lab",
            "room_number": "3-L-030",
            "color": "#4fc3f7",
            "height": 3.5
        },
        {
            "space_id": "millbrae-3-htp-tc",
            "name": "HTP TC Lab",
            "room_number": "3-L-033",
            "color": "#4fc3f7",
            "height": 3.5
        },
        {
            "space_id": "millbrae-3-compound",
            "name": "Compound Management",
            "room_number": "3-L-035",
            "color": "#81c784",  # Light green
            "height": 3.5
        },
        {
            "space_id": "millbrae-3-consumables",
            "name": "Consumables",
            "room_number": "3-L-034",
            "color": "#81c784",
            "height": 3.5
        },
        {
            "space_id": "millbrae-3-optical",
            "name": "Optical",
            "room_number": "3-L-036",
            "color": "#ffb74d",  # Orange
            "height": 3.5
        },
        {
            "space_id": "millbrae-3-darkroom-37",
            "name": "Dark Room",
            "room_number": "3-L-037",
            "color": "#424242",  # Dark gray
            "height": 3.5
        },
        {
            "space_id": "millbrae-3-darkroom-38",
            "name": "Dark Room",
            "room_number": "3-L-038",
            "color": "#424242",
            "height": 3.5
        },
    ]
    
    for space_data in spaces_3rd:
        space_data["floor_id"] = "millbrae-3"
        space_data["boundary"] = None  # To be defined via UI
        
        existing = db.get_space(space_data["space_id"])
        if existing:
            print(f"Space {space_data['name']} ({space_data['room_number']}) already exists, skipping...")
        else:
            db.create_space(space_data)
            print(f"Created space: {space_data['name']} ({space_data['room_number']})")
    
    # Assign PF400-021 to Automation lab
    print("\nAssigning PF400-021 to Automation lab...")
    db.assign_device_to_space("PF400-021", "millbrae-3-automation")
    
    print("\n✓ Seeding complete!")


def seed_new_york():
    """Create placeholder for New York site."""
    site_data = {
        "site_id": "new-york",
        "name": "New York, NY",
        "address": "",  # TBD
        "coordinates": {"lat": 40.7128, "lng": -74.0060}
    }
    
    existing = db.get_site("new-york")
    if existing:
        print("New York site already exists, skipping...")
    else:
        db.create_site(site_data)
        print("Created New York site (placeholder)")


if __name__ == "__main__":
    print("=== Seeding Spatial Data ===\n")
    seed_millbrae()
    print()
    seed_new_york()

