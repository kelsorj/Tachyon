
import socket
import time

def test_cartesian():
    ip = "192.168.10.69"
    port = 10100
    
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.settimeout(2.0)
        s.connect((ip, port))
        print(f"Connected to {ip}:{port}")
        
        # Clear buffer
        try:
            s.recv(1024)
        except:
            pass
            
        # Test "Where" (Cartesian)
        print("\nSending 'Where'...")
        s.sendall(b"Where\r\n")
        response = s.recv(4096).decode('ascii').strip()
        print(f"Response: '{response}'")
        
        parts = response.split(' ')
        if len(parts) > 6:
            x = float(parts[1])
            y = float(parts[2])
            z = float(parts[3])
            yaw = float(parts[4])
            pitch = float(parts[5])
            roll = float(parts[6])
            
            print(f"Parsed: X={x}, Y={y}, Z={z}, Yaw={yaw}, Pitch={pitch}, Roll={roll}")
            
            # Try a small move in Z (safe)
            # Move <profile> <X> <Y> <Z> <Yaw> <Pitch> <Roll>
            # Note: Z is typically J1 for this robot, so ensure it's within limits
            new_z = z + 1.0 # Move up 1mm
            
            
            # Try syntax: Move 1 Location(X, Y, Z, Yaw, Pitch, Roll)
            cmd = f"Move 1 Location({x}, {y}, {new_z}, {yaw}, {pitch}, {roll})\r\n"
            print(f"Sending Move Location: {cmd.strip()}")
            s.sendall(cmd.encode('ascii'))
            resp = s.recv(4096).decode('ascii').strip()
            print(f"Move Response: '{resp}'")

            
            # Return
            time.sleep(1)
            cmd = f"Move 1 {x} {y} {z} {yaw} {pitch} {roll}\r\n"
            s.sendall(cmd.encode('ascii'))
            s.recv(4096)
            print("Returned to start")

        # Test "WhereJ" (Joints) for comparison
        print("\nSending 'WhereJ'...")
        s.sendall(b"WhereJ\r\n")
        response = s.recv(4096).decode('ascii').strip()
        print(f"Response: '{response}'")

        s.close()
        
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    test_cartesian()

