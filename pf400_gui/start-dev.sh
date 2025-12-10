#!/bin/bash
# Quick start script for development mode

cd "$(dirname "$0")"

echo "Starting PF400 GUI in development mode..."
echo ""

# Check if PM2 is installed
if ! command -v pm2 &> /dev/null; then
    echo "❌ PM2 not found. Run ./setup_pm2.sh first"
    exit 1
fi

# Stop any existing processes
pm2 delete all 2>/dev/null

# Start services
echo "Starting services with PM2..."
pm2 start ecosystem.dev.config.js

# Show status
echo ""
echo "Services started! Status:"
pm2 status

echo ""
echo "View logs with: pm2 logs"
echo "Stop services with: pm2 stop all"
echo ""


