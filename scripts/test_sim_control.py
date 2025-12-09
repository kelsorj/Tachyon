from pf400_sim_driver import PF400Simulator
import time
import math

def test_control():
    urdf_path = '/Users/kelsorj/My Drive/Code/Aikon/models/pf400_urdf/pf400Complete.urdf'
    
    print("Initializing Simulator...")
    sim = PF400Simulator(urdf_path)
    
    print("Initial State:")
    print(sim.get_joint_positions())
    
    # Define some target poses (joint angles in radians)
    # Based on joint limits from URDF:
    # j1 (Z-rail): -0.1 to 0.44
    # j2 (Shoulder): -1.39 to 1.74
    # j3 (Elbow): -12.2 to 0.3 (Wait, -12? That's huge. Let's stick to small moves)
    # j4 (Wrist): -7.5 to 5.0
    # j5 (Gripper): 0 to 0.025
    
    targets = [
        {
            "name": "Move 1: Lift and Turn",
            "joints": {
                "j1": 0.2,       # Lift up
                "j2": 1.0,       # Rotate shoulder
                "j3": -1.57,     # Bend elbow 90 deg
                "j4": 0.0,
                "j5left": 0.0,
                "j5right": 0.0
            },
            "duration": 2.0
        },
        {
            "name": "Move 2: Extend",
            "joints": {
                "j1": 0.3,
                "j2": 0.0,
                "j3": 0.0,       # Straighten elbow
                "j4": 1.57,      # Rotate wrist
                "j5left": 0.01,  # Open gripper
                "j5right": 0.01
            },
            "duration": 2.0
        },
        {
            "name": "Move 3: Home",
            "joints": {
                "j1": 0.0,
                "j2": 0.0,
                "j3": 0.0,
                "j4": 0.0,
                "j5left": 0.0,
                "j5right": 0.0
            },
            "duration": 2.0
        }
    ]
    
    for target in targets:
        print(f"\nExecuting: {target['name']}")
        sim.move_to_joints(target['joints'], duration=target['duration'])
        
        current = sim.get_joint_positions()
        print("Reached State:")
        # Print only relevant joints
        for k in target['joints']:
            if k in current:
                print(f"  {k}: {current[k]:.4f}")
                
    print("\nTest Complete!")

if __name__ == "__main__":
    test_control()
