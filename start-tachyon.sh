#!/bin/bash
# Tachyon Services Startup Script
# Handles PM2 cleanup after hard restarts and starts all Tachyon services
# 
# This script is designed to be run by launchd at login/reboot
# It aggressively cleans up any corrupted PM2 state before starting services

set -e

TACHYON_DIR="/Users/kelsorj/Tachyon"
LOG_DIR="$TACHYON_DIR/logs"
STARTUP_LOG="$LOG_DIR/startup.log"
PM2_HOME="/Users/kelsorj/.pm2"

# Ensure log directory exists
mkdir -p "$LOG_DIR"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$STARTUP_LOG"
}

log "=========================================="
log "Tachyon Startup Script Beginning"
log "=========================================="

# Wait for network connectivity (robot at 192.168.0.20)
# IMPORTANT: Use -b en9 to ping via the robot network interface
# The default routing may use the wrong interface after reboot
log "Waiting for network connectivity..."
MAX_WAIT=120
WAITED=0

# First wait for en9 interface to be up
log "  Waiting for en9 interface..."
while ! ifconfig en9 2>/dev/null | grep -q "inet 192.168.0"; do
    sleep 2
    WAITED=$((WAITED + 2))
    if [ $WAITED -ge $MAX_WAIT ]; then
        log "WARNING: en9 interface timeout after ${MAX_WAIT}s"
        break
    fi
done

# Now wait for robot to be pingable via en9
log "  Waiting for robot on 192.168.0.20..."
WAITED=0
while ! ping -c 1 -W 2 -b en9 192.168.0.20 &>/dev/null; do
    sleep 3
    WAITED=$((WAITED + 3))
    if [ $WAITED -ge $MAX_WAIT ]; then
        log "WARNING: Robot connectivity timeout after ${MAX_WAIT}s, continuing anyway..."
        break
    fi
done
if [ $WAITED -lt $MAX_WAIT ]; then
    log "Network connectivity confirmed (robot reachable via en9)"
fi

# Give network stack a moment to stabilize routing tables
sleep 2

# Step 1: Aggressively kill any existing PM2 processes
log "Step 1: Cleaning up existing PM2 processes..."
pkill -9 -f "PM2" 2>/dev/null || true
pkill -9 -f "pm2" 2>/dev/null || true
sleep 2

# Step 2: Remove potentially corrupted PM2 socket and PID files
log "Step 2: Removing potentially corrupted PM2 files..."
rm -f "$PM2_HOME/rpc.sock" 2>/dev/null || true
rm -f "$PM2_HOME/pub.sock" 2>/dev/null || true
rm -f "$PM2_HOME/pm2.pid" 2>/dev/null || true
rm -f "$PM2_HOME/agent.sock" 2>/dev/null || true
rm -f "$PM2_HOME/interactor.sock" 2>/dev/null || true

# Step 3: Remove corrupted log files (PM2 logs, not app logs)
log "Step 3: Cleaning PM2 system logs..."
rm -f "$PM2_HOME/pm2.log" 2>/dev/null || true

# Step 4: Start PM2 daemon fresh
log "Step 4: Starting PM2 daemon..."
export PATH="/opt/homebrew/bin:/opt/homebrew/sbin:/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin:$PATH"

# Kill PM2 and start fresh (ignore errors)
pm2 kill 2>/dev/null || true
sleep 2

# Step 5: Kill any orphan processes on our ports
log "Step 5: Killing orphan processes on ports 5173, 8091, 3062..."
kill -9 $(lsof -t -i :5173) 2>/dev/null || true
kill -9 $(lsof -t -i :8091) 2>/dev/null || true
kill -9 $(lsof -t -i :3062) 2>/dev/null || true
sleep 1

# Step 6: Start Tachyon services
log "Step 6: Starting Tachyon services..."

# PF400 Backend
# IMPORTANT: DEVICE_NAME must be set to the correct robot (PF400-021, not the default PF400-015)
# This controls which device's teachpoints are loaded from MongoDB
log "  Starting pf400-backend..."
cd "$TACHYON_DIR/pf400_gui"
DEVICE_NAME=PF400-021 PF400_PORT=8091 pm2 start backend/run_sxl.sh \
    --name pf400-backend \
    --interpreter bash \
    --output "$LOG_DIR/backend-out.log" \
    --error "$LOG_DIR/backend-error.log" \
    --time

# Wait for backend to initialize
sleep 3

# PF400 Frontend
log "  Starting pf400-frontend..."
cd "$TACHYON_DIR/pf400_gui/frontend"
pm2 start npm \
    --name pf400-frontend \
    -- run dev

# Planar Motor Backend
log "  Starting planar-motor-backend..."
cd "$TACHYON_DIR/planar_motor"
pm2 start backend/main.py \
    --name planar-motor-backend \
    --interpreter "$TACHYON_DIR/planar_motor/venv313/bin/python" \
    --output "$LOG_DIR/planar-out.log" \
    --error "$LOG_DIR/planar-error.log" \
    --time \
    -- --port 3062 --pmc-ip 192.168.10.100

# Step 7: Save PM2 process list
log "Step 7: Saving PM2 process list..."
pm2 save

# Step 8: Verify services are running
log "Step 8: Verifying services..."
sleep 5

BACKEND_STATUS=$(pm2 jlist 2>/dev/null | python3 -c "import sys,json; procs=json.load(sys.stdin); print([p['pm2_env']['status'] for p in procs if p['name']=='pf400-backend'][0])" 2>/dev/null || echo "unknown")
FRONTEND_STATUS=$(pm2 jlist 2>/dev/null | python3 -c "import sys,json; procs=json.load(sys.stdin); print([p['pm2_env']['status'] for p in procs if p['name']=='pf400-frontend'][0])" 2>/dev/null || echo "unknown")
PLANAR_STATUS=$(pm2 jlist 2>/dev/null | python3 -c "import sys,json; procs=json.load(sys.stdin); print([p['pm2_env']['status'] for p in procs if p['name']=='planar-motor-backend'][0])" 2>/dev/null || echo "unknown")

log "  pf400-backend: $BACKEND_STATUS"
log "  pf400-frontend: $FRONTEND_STATUS"
log "  planar-motor-backend: $PLANAR_STATUS"

# Check if all services are online
if [ "$BACKEND_STATUS" = "online" ] && [ "$FRONTEND_STATUS" = "online" ] && [ "$PLANAR_STATUS" = "online" ]; then
    log "=========================================="
    log "✅ All Tachyon services started successfully!"
    log "=========================================="
else
    log "=========================================="
    log "⚠️  Some services may not be running correctly"
    log "  Check logs in $LOG_DIR"
    log "=========================================="
fi

# Step 9: Test robot connectivity
log "Step 9: Testing robot connectivity..."
sleep 2
ROBOT_TEST=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8091/joints 2>/dev/null || echo "000")
if [ "$ROBOT_TEST" = "200" ]; then
    log "✅ Robot API responding (HTTP 200)"
else
    log "⚠️  Robot API not responding yet (HTTP $ROBOT_TEST) - may still be initializing"
fi

log "Tachyon Startup Script Complete"
log ""
