#!/bin/bash
# Setup script for PM2 installation and configuration

set -e

echo "=========================================="
echo "PM2 Setup for PF400 GUI"
echo "=========================================="
echo ""

# Check if we're in the right directory
if [ ! -d "backend" ] || [ ! -d "frontend" ]; then
    echo "❌ Error: Must run from pf400_gui directory"
    exit 1
fi

# 1. Check for Node.js
echo "1. Checking for Node.js..."
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    echo "   ✓ Node.js found: $NODE_VERSION"
else
    echo "   ⚠ Node.js not found. Installing via Homebrew..."
    if command -v brew &> /dev/null; then
        brew install node
        echo "   ✓ Node.js installed"
    else
        echo "   ❌ Homebrew not found. Please install Node.js manually:"
        echo "      Visit: https://nodejs.org/"
        exit 1
    fi
fi

# 2. Check for npm
echo ""
echo "2. Checking for npm..."
if command -v npm &> /dev/null; then
    NPM_VERSION=$(npm --version)
    echo "   ✓ npm found: $NPM_VERSION"
else
    echo "   ❌ npm not found. This should come with Node.js."
    exit 1
fi

# 3. Install PM2 globally
echo ""
echo "3. Installing PM2 globally..."
if command -v pm2 &> /dev/null; then
    PM2_VERSION=$(pm2 --version)
    echo "   ✓ PM2 already installed: v$PM2_VERSION"
else
    echo "   Installing PM2..."
    npm install -g pm2
    echo "   ✓ PM2 installed"
fi

# 4. Install frontend dependencies
echo ""
echo "4. Installing frontend dependencies..."
cd frontend
if [ -d "node_modules" ]; then
    echo "   ✓ node_modules already exists"
    echo "   Running npm install to ensure dependencies are up to date..."
    npm install
else
    echo "   Installing dependencies..."
    npm install
fi
cd ..
echo "   ✓ Frontend dependencies installed"

# 5. Create logs directory
echo ""
echo "5. Creating logs directory..."
mkdir -p logs
echo "   ✓ Logs directory ready"

# 6. Fix port mismatch (frontend expects 3062, backend uses 3061)
echo ""
echo "6. Checking port configuration..."
if grep -q "localhost:3062" frontend/src/App.jsx; then
    echo "   ⚠ Frontend is configured for port 3062, but backend uses 3061"
    echo "   Updating frontend to use port 3061..."
    # Backup original
    cp frontend/src/App.jsx frontend/src/App.jsx.bak
    # Update port
    sed -i '' 's|http://localhost:3062|http://localhost:3061|g' frontend/src/App.jsx
    echo "   ✓ Frontend updated to use port 3061"
fi

# 7. Make scripts executable
echo ""
echo "7. Making scripts executable..."
chmod +x backend/run_sxl.sh
chmod +x backend/start_backend.sh
echo "   ✓ Scripts are executable"

# 8. Summary
echo ""
echo "=========================================="
echo "Setup Complete!"
echo "=========================================="
echo ""
echo "Next steps:"
echo ""
echo "1. Start both services with PM2:"
echo "   pm2 start ecosystem.dev.config.js"
echo ""
echo "2. View status:"
echo "   pm2 status"
echo ""
echo "3. View logs:"
echo "   pm2 logs"
echo ""
echo "4. Stop services:"
echo "   pm2 stop all"
echo ""
echo "5. Save PM2 configuration:"
echo "   pm2 save"
echo "   pm2 startup  # (to start on system boot)"
echo ""
echo "For more commands, see: pm2 --help"
echo ""



