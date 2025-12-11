# Planar Motor System Documentation

This document contains information about the Planar Motor Controller (PMC) API and system commands, compiled from the [Planar Motor Technical Portal](https://docs.planarmotor.com/tech-portal/system-commands) and existing code examples.

## Table of Contents

1. [System Commands Overview](#system-commands-overview)
2. [Connection & Mastership](#connection--mastership)
3. [PMC Information](#pmc-information)
4. [System Configuration](#system-configuration)
5. [System Startup](#system-startup)
6. [XBOT Commands](#xbot-commands)
7. [Error Handling](#error-handling)

---

## System Commands Overview

The Planar Motor system commands are organized into several categories:

### 1. Activation & Deactivation
- Activate XBOTs
- Deactivate XBOTs

### 2. PMC Information
- Get PMC Version
- Get PMC Serial Number
- Get PMC Status
- Read PMC State
- Get PMC Warning Code
- Get PMC Warning Log
- Get PMC Error Code
- Get PMC Error Log
- Set PMC Date and Time
- Read TimeStamp

### 3. System Configuration
- Get PMC Configuration
- Set PMC Configuration

### 4. System Startup
- Start Up Planar Motor System
- Reboot PMC
- Set XBot Orientation
- Get Spaced XBot Positions

### 5. System Connection
- Auto Connect to PMC
- Connect to Specified PMC
- Gain Mastership

### 6. Service Mode Control
- Service mode commands (details to be added)

### 7. Auto Refresh
- Auto refresh commands (details to be added)

---

## Connection & Mastership

### Auto Connect to PMC
```python
from pmclib import system_commands as sys

# Automatically search and connect to the PMC
is_connected = sys.auto_search_and_connect_to_pmc()
if not is_connected:
    print("Failed to connect to Planar Motor Controller")
```

### Connect to Specific PMC
```python
# Connect to a PMC at a specific IP address
is_connected = sys.connect_to_specific_pmc("127.0.0.1")
```

### Gain Mastership
```python
# Gain mastership of the system
# Only 1 program can have mastership at once
sys.gain_mastership()
```

---

## PMC Information

### Get PMC Status
```python
from pmclib import system_commands as sys
from pmclib import pmc_types as pm

pmc_stat = sys.get_pmc_status()

# PMC Status Values (from pmc_types):
# - pm.PMCSTATUS.PMC_FULLCTRL - Full control mode
# - pm.PMCSTATUS.PMC_INTELLIGENTCTRL - Intelligent control mode
# - pm.PMCSTATUS.PMC_ACTIVATING - Activating
# - pm.PMCSTATUS.PMC_BOOTING - Booting
# - pm.PMCSTATUS.PMC_DEACTIVATING - Deactivating
# - pm.PMCSTATUS.PMC_ERRORHANDLING - Error handling
# - pm.PMCSTATUS.PMC_ERROR - Error state
# - pm.PMCSTATUS.PMC_INACTIVE - Inactive
```

### Get PMC Version
```python
# TODO: Add implementation when documentation is available
# version = sys.get_pmc_version()
```

### Get PMC Serial Number
```python
# TODO: Add implementation when documentation is available
# serial = sys.get_pmc_serial_number()
```

### Read PMC State
```python
# TODO: Add implementation when documentation is available
# state = sys.read_pmc_state()
```

### Get PMC Warning Code
```python
# TODO: Add implementation when documentation is available
# warning_code = sys.get_pmc_warning_code()
```

### Get PMC Warning Log
```python
# TODO: Add implementation when documentation is available
# warning_log = sys.get_pmc_warning_log()
```

### Get PMC Error Code
```python
# TODO: Add implementation when documentation is available
# error_code = sys.get_pmc_error_code()
```

### Get PMC Error Log
```python
# TODO: Add implementation when documentation is available
# error_log = sys.get_pmc_error_log()
```

### Set PMC Date and Time
```python
# TODO: Add implementation when documentation is available
# sys.set_pmc_date_time(datetime)
```

### Read TimeStamp
```python
# TODO: Add implementation when documentation is available
# timestamp = sys.read_timestamp()
```

---

## System Configuration

### Get PMC Configuration
```python
# TODO: Add implementation when documentation is available
# config = sys.get_pmc_configuration()
```

### Set PMC Configuration
```python
# TODO: Add implementation when documentation is available
# sys.set_pmc_configuration(config)
```

---

## System Startup

### Start Up Planar Motor System
The startup routine typically involves:
1. Connect to PMC
2. Gain mastership
3. Check PMC status
4. Activate XBOTs if needed
5. Get XBOT IDs
6. Stop any existing motion
7. Levitate XBOTs

See `python_demo.py` for a complete startup routine implementation.

### Reboot PMC
```python
# TODO: Add implementation when documentation is available
# sys.reboot_pmc()
```

### Set XBot Orientation
```python
# TODO: Add implementation when documentation is available
# sys.set_xbot_orientation(xbot_id, orientation)
```

### Get Spaced XBot Positions
```python
# TODO: Add implementation when documentation is available
# positions = sys.get_spaced_xbot_positions()
```

---

## XBOT Commands

### Get XBOT IDs
```python
from pmclib import xbot_commands as bot
from pmclib import pmc_types as pm

xbot_ids = bot.get_xbot_ids()
if xbot_ids.PmcRtn == pm.PMCRTN.ALLOK:
    print(f"Found {xbot_ids.xbot_count} XBOT(s)")
    for xbot_id in xbot_ids.xbot_ids_array:
        print(f"  XBOT ID: {xbot_id}")
```

### Activate XBOTs
```python
# Activate all XBOTs everywhere (in all zones) on the flyway
bot.activate_xbots()
```

### Get XBOT Status
```python
xbot_status = bot.get_xbot_status(xbot_id)
xbot_state = xbot_status.xbot_state

# XBOT States (from pmc_types):
# - pm.XBOTSTATE.XBOT_IDLE - Idle and ready
# - pm.XBOTSTATE.XBOT_STOPPED - Stopped
# - pm.XBOTSTATE.XBOT_LANDED - Landed (needs levitation)
# - pm.XBOTSTATE.XBOT_STOPPING - Stopping
# - pm.XBOTSTATE.XBOT_DISCOVERING - Discovering
# - pm.XBOTSTATE.XBOT_MOTION - In motion
# - pm.XBOTSTATE.XBOT_WAIT - Waiting
# - pm.XBOTSTATE.XBOT_OBSTACLE_DETECTED - Obstacle detected
# - pm.XBOTSTATE.XBOT_HOLDPOSITION - Holding position
# - pm.XBOTSTATE.XBOT_DISABLED - Disabled
```

### Levitation Command
```python
# Levitate all XBOTs (xbot_id = 0 means all)
bot.levitation_command(0, pm.LEVITATEOPTIONS.LEVITATE)

# Land XBOTs
bot.levitation_command(xbot_id, pm.LEVITATEOPTIONS.LAND)
```

### Stop Motion
```python
# Stop all XBOTs (xbot_id = 0 means all)
bot.stop_motion(0)

# Stop specific XBOT
bot.stop_motion(xbot_id)
```

### Linear Motion
```python
# Linear motion command
# Parameters:
# - cmd_label: Command label (for tracking)
# - xbot_id: XBOT ID
# - position_mode: pm.POSITIONMODE.ABSOLUTE or pm.POSITIONMODE.RELATIVE
# - path_type: pm.LINEARPATHTYPE.DIRECT, XTHENY, or YTHENX
# - target_x: Target X position in meters
# - target_y: Target Y position in meters
# - final_speed: Final speed in m/s (0 to stop)
# - max_speed: Maximum speed in m/s
# - max_acceleration: Maximum acceleration in m/s²

travel_time = bot.linear_motion_si(
    cmd_label=100,
    xbot_id=xbot_id,
    position_mode=pm.POSITIONMODE.ABSOLUTE,
    path_type=pm.LINEARPATHTYPE.DIRECT,
    target_x=0.120,  # 120mm
    target_y=0.180,  # 180mm
    final_speed=0.0,
    max_speed=1.0,
    max_acceleration=10.0
)
```

### Arc Motion
```python
import math

# Arc motion using center and angle
bot.arc_motion_meters_radians(
    cmd_label=101,
    xbot_id=xbot_id,
    arc_mode=pm.ARCMODE.CENTERANGLE,
    arc_type=pm.ARCTYPE.MAJORARC,
    arc_direction=pm.ARCDIRECTION.COUNTERCLOCKWISE,
    position_mode=pm.POSITIONMODE.ABSOLUTE,
    center_x=0.120,
    center_y=0.120,
    final_speed=0.0,
    max_speed=1.0,
    max_acceleration=10.0,
    radius=0.0,  # Not used for CENTERANGLE mode
    rotation_angle=math.pi  # 180 degrees
)

# Arc motion using target and radius
bot.arc_motion_meters_radians(
    cmd_label=103,
    xbot_id=xbot_id,
    arc_mode=pm.ARCMODE.TARGETRADIUS,
    arc_type=pm.ARCTYPE.MINORARC,
    arc_direction=pm.ARCDIRECTION.COUNTERCLOCKWISE,
    position_mode=pm.POSITIONMODE.ABSOLUTE,
    target_x=0.360,
    target_y=0.180,
    final_speed=0.0,
    max_speed=1.0,
    max_acceleration=10.0,
    radius=0.060,  # 60mm radius
    rotation_angle=0.0  # Not used for TARGETRADIUS mode
)
```

### Short Axes Motion (Z, Rx, Ry, Rz)
```python
# Short axes motion for Z, Rx, Ry, Rz
# Parameters:
# - target_z: Target Z position in meters
# - target_rx: Target Rx rotation in radians
# - target_ry: Target Ry rotation in radians
# - target_rz: Target Rz rotation in radians
# - z_speed: Z travel speed in m/s
# - rx_speed: Rx travel speed in rad/s
# - ry_speed: Ry travel speed in rad/s
# - rz_speed: Rz travel speed in rad/s

bot.short_axes_motion_si(
    cmd_label=101,
    xbot_id=xbot_id,
    position_mode=pm.POSITIONMODE.ABSOLUTE,
    target_z=0.002,  # 2mm
    target_rx=0.0,
    target_ry=0.0,
    target_rz=0.0,
    z_speed=0.005,
    rx_speed=0.1,
    ry_speed=0.1,
    rz_speed=0.1
)
```

### Six DOF Motion
```python
# Six DOF motion (X, Y, Z, Rx, Ry, Rz)
bot.six_dof_motion_si(
    cmd_label=200,
    xbot_id=xbot_id,
    target_x=0.080,
    target_y=0.100,
    target_z=0.002,
    target_rx=0.01,  # 10 mrad
    target_ry=0.01,  # 10 mrad
    target_rz=0.080  # 80 mrad
)

# With custom speeds
bot.six_dof_motion_si(
    cmd_label=200,
    xbot_id=xbot_id,
    target_x=0.12,
    target_y=0.12,
    target_z=0.001,
    target_rx=0.0,
    target_ry=0.0,
    target_rz=0.0,
    xy_speed=0.1,
    xy_accel=1.0,
    z_speed=0.001,
    rx_speed=0.001,
    ry_speed=0.001,
    rz_speed=0.001
)
```

### Motion Buffer Control
```python
# Block buffer to queue commands without executing
bot.motion_buffer_control(xbot_id, pm.MOTIONBUFFEROPTIONS.BLOCKBUFFER)

# Release buffer to execute queued commands
bot.motion_buffer_control(xbot_id, pm.MOTIONBUFFEROPTIONS.RELEASEBUFFER)

# Clear buffer
bot.motion_buffer_control(xbot_id, pm.MOTIONBUFFEROPTIONS.CLEARBUFFER)
```

### Motion Macros
```python
# Clear a macro
bot.edit_motion_macro(pm.MOTIONMACROOPTIONS.CLEARMACRO, macro_id)

# Send commands to macro (use macro_id as xbot_id)
bot.linear_motion_si(100, macro_id, ...)

# Save macro
bot.edit_motion_macro(pm.MOTIONMACROOPTIONS.SAVEMACRO, macro_id)

# Run macro on an XBOT
bot.run_motion_macro(cmd_label=200, macro_id=macro_id, xbot_id=xbot_id)
```

### Group Control
```python
# Create a group
bot.group_control(
    pm.GROUPOPTIONS.CREATEGROUP,
    group_id=1,
    xbot_count=2,
    xbot_ids=[xbot_id1, xbot_id2]
)

# Bond group (lock relative positions)
bot.group_control(pm.GROUPOPTIONS.BONDGROUP, group_id=1, ...)

# Unbond group
bot.group_control(pm.GROUPOPTIONS.UNBONDGROUP, group_id=1, ...)

# Block group members' buffers
bot.group_control(pm.GROUPOPTIONS.BLOCKMEMBERSBUFFER, group_id=1, ...)

# Release group members' buffers
bot.group_control(pm.GROUPOPTIONS.RELEASEMEMBERSBUFFER, group_id=1, ...)

# Delete group
bot.group_control(pm.GROUPOPTIONS.DELETEGROUP, group_id=0, ...)  # 0 = all groups
```

### Asynchronous Motion
```python
# Move multiple XBOTs asynchronously
bot.async_motion_si(
    xbot_count=2,
    option=pm.ASYNCOPTIONS.MOVEALL,
    xbot_ids=[xbot_id1, xbot_id2],
    target_x_coords=[0.060, 0.060],
    target_y_coords=[0.060, 0.180]
)
```

### Auto Driving Motion
```python
# Auto driving motion (for multiple XBOTs)
bot.auto_driving_motion_si(
    xbot_count=4,
    option=pm.ASYNCOPTIONS.MOVEALL,
    xbot_ids=[xbot_id1, xbot_id2, xbot_id3, xbot_id4],
    target_x_coords=[0.06, 0.18, 0.30, 0.420],
    target_y_coords=[0.24, 0.24, 0.24, 0.24]
)
```

### Wait Until
```python
# Wait until command label is executing
trigger_params = pm.WaitUntilTriggerParams(
    triggerxbot_id=xbot_id2,
    trigger_cmd_label=1234,
    cmd_label_trigger_type=pm.TRIGGERCMDLABELTYPE.CMD_EXECUTING,
    trigger_cmd_type=pm.TRIGGERCMDTYPE.MOTION_COMMAND
)
bot.wait_until(
    cmd_label=101,
    xbot_id=xbot_id1,
    trigger_source=pm.TRIGGERSOURCE.CMD_LABEL,
    trigger_parameters=trigger_params
)

# Wait until displacement threshold
trigger_params = pm.WaitUntilTriggerParams(
    triggerxbot_id=xbot_id2,
    displacement_threshold_meters=0.29999,
    displacement_trigger_mode=pm.TRIGGERDISPLACEMENTMODE.X_ONLY,
    displacement_trigger_type=pm.TRIGGERDISPLACEMENTTYPE.GREATER_THAN
)
bot.wait_until(
    cmd_label=200,
    xbot_id=xbot_id1,
    trigger_source=pm.TRIGGERSOURCE.DISPLACEMENT,
    trigger_parameters=trigger_params
)

# Wait until time delay
trigger_params = pm.WaitUntilTriggerParams(delay_secs=2.0)
bot.wait_until(
    cmd_label=0,
    xbot_id=xbot_id,
    trigger_source=pm.TRIGGERSOURCE.TIME_DELAY,
    trigger_parameters=trigger_params
)
```

### Rotary Motion
```python
# Point-to-point rotary motion
xbot_status = bot.get_xbot_status(xbot_id)
bot.rotary_motion_p2p(
    cmd_label=0,
    xbot_id=xbot_id,
    rotation_mode=pm.ROTATIONMODE.WRAP_TO_2PI_CCW,
    target_rz=xbot_status.feedback_position_si[5] + math.pi,
    max_speed=1.0,  # rad/s
    max_acceleration=10.0,  # rad/s²
    position_mode=pm.POSITIONMODE.ABSOLUTE
)

# Timed spin rotary motion
bot.rotary_motion_timed_spin(
    cmd_label=0,
    xbot_id=xbot_id,
    target_rz=0,
    speed=2.0,  # rad/s
    acceleration=10.0,  # rad/s²
    duration=5  # seconds
)
```

---

## Error Handling

### Return Codes
```python
from pmclib import pmc_types as pm

# Check return codes
if return_value == pm.PMCRTN.ALLOK:
    print("Command succeeded")
else:
    print(f"Command failed with code: {return_value}")

# Common return codes:
# - pm.PMCRTN.ALLOK - Success
# - pm.PMCRTN.INVALIDPARAMS - Invalid parameters
# - Additional error codes to be documented
```

---

## Notes

- All position values are in meters (SI units)
- All angle values are in radians
- Command labels are arbitrary but useful for debugging
- XBOT ID = 0 means "all XBOTs" for most commands
- Only one program can have mastership at a time
- XBOTs must be levitated before motion commands
- Motion commands can be buffered for smooth motion

---

## References

- [Planar Motor Technical Portal - System Commands](https://docs.planarmotor.com/tech-portal/system-commands)
- [Planar Motor Software Manual](https://docs.planarmotor.com/tech-portal/software-manual)
- Existing code examples in `planar_motor/shaker/python_demo.py`

---

## TODO

This documentation is incomplete. The following items need to be added from the official documentation:

1. Complete parameter lists for all system commands
2. Error code definitions and meanings
3. Warning code definitions
4. Configuration structure details
5. Service mode commands
6. Auto refresh commands
7. Additional XBOT command details
8. Best practices and safety guidelines

