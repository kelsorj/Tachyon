import mujoco
import mujoco.viewer
import time
import os

def simulate():
    urdf_path = '/Users/kelsorj/My Drive/Code/Aikon/models/pf400_urdf/pf400Complete.urdf'
    
    # Load model
    try:
        model = mujoco.MjModel.from_xml_path(urdf_path)
    except Exception as e:
        print(f"Failed to load URDF: {e}")
        return

    data = mujoco.MjData(model)

    print(f"Model loaded successfully. nq={model.nq}, nv={model.nv}")
    
    # Launch viewer
    try:
        with mujoco.viewer.launch_passive(model, data) as viewer:
            start = time.time()
            while viewer.is_running() and time.time() - start < 10: # Run for 10 seconds for test
                step_start = time.time()
                
                mujoco.mj_step(model, data)
                viewer.sync()
                
                time_until_next_step = model.opt.timestep - (time.time() - step_start)
                if time_until_next_step > 0:
                    time.sleep(time_until_next_step)
    except Exception as e:
        print(f"Viewer failed (expected in headless env): {e}")
        # Fallback to headless stepping
        print("Running headless simulation for 100 steps...")
        for _ in range(100):
            mujoco.mj_step(model, data)
        print("Headless simulation completed.")

if __name__ == "__main__":
    simulate()
