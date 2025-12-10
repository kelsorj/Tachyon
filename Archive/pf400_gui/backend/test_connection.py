import sys
import os
import time

# Add current directory to path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from pf400_driver import PF400Driver

def test_connection():
    print("Testing connection to PF400 at 192.168.10.69...")
    driver = PF400Driver(ip="192.168.10.69", port=10100)
    
    if driver.connect():
        print("Successfully connected!")
        
        print("\nSending 'WhereJ' command...")
        try:
            # Raw command test
            response = driver.send_command("WhereJ")
            print(f"Raw Response: '{response}'")
            
            # Parsed test
            print("\nParsing joints...")
            joints = driver.get_joint_positions()
            print(f"Parsed Joints: {joints}")
            
            print("\nMonitoring joints for 5 seconds (move the robot manually if safe)...")
            for _ in range(10):
                joints = driver.get_joint_positions()
                # Print nicely formatted
                if joints:
                    j_str = ", ".join([f"{k}: {v:.4f}" for k,v in joints.items() if isinstance(v, float)])
                    print(f"Joints: {j_str}")
                time.sleep(0.5)
                
        except Exception as e:
            print(f"Error during communication: {e}")
            
        driver.disconnect()
        print("\nDisconnected.")
    else:
        print("Failed to connect. Please check IP, port, and network connection.")
        print("Ensure you are on the same subnet (192.168.10.x)")

if __name__ == "__main__":
    test_connection()

