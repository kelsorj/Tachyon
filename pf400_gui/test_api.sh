#!/bin/bash
# Quick API testing script

BASE_URL="http://localhost:3061"
FRONTEND_URL="http://localhost:5173"

echo "=========================================="
echo "PF400 GUI API Testing"
echo "=========================================="
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test function
test_endpoint() {
    local name=$1
    local method=$2
    local url=$3
    local data=$4
    
    echo -n "Testing $name... "
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" "$url")
    else
        response=$(curl -s -w "\n%{http_code}" -X POST "$url" \
            -H "Content-Type: application/json" \
            -d "$data")
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        echo -e "${GREEN}✓${NC} (HTTP $http_code)"
        if [ -n "$body" ] && [ "$body" != "null" ]; then
            echo "$body" | head -5 | sed 's/^/  /'
        fi
        return 0
    else
        echo -e "${RED}✗${NC} (HTTP $http_code)"
        echo "$body" | head -3 | sed 's/^/  /'
        return 1
    fi
}

# Check if services are running
echo "1. Checking PM2 services..."
if command -v pm2 &> /dev/null; then
    pm2_status=$(pm2 jlist 2>/dev/null)
    if echo "$pm2_status" | grep -q "pf400-backend"; then
        echo -e "   ${GREEN}✓${NC} Backend service found"
    else
        echo -e "   ${RED}✗${NC} Backend service not running"
        echo "   Run: pm2 start ecosystem.dev.config.js"
        exit 1
    fi
    
    if echo "$pm2_status" | grep -q "pf400-frontend"; then
        echo -e "   ${GREEN}✓${NC} Frontend service found"
    else
        echo -e "   ${YELLOW}⚠${NC} Frontend service not running"
    fi
else
    echo -e "   ${YELLOW}⚠${NC} PM2 not found"
fi

echo ""
echo "2. Testing Backend API..."
echo ""

# Basic endpoints
test_endpoint "State" "GET" "$BASE_URL/state"
test_endpoint "Description" "GET" "$BASE_URL/description"
test_endpoint "Joints" "GET" "$BASE_URL/joints"
test_endpoint "Device Info" "GET" "$BASE_URL/device"

echo ""
echo "3. Testing Diagnostics (SXL)..."
echo ""

# Diagnostics endpoints
test_endpoint "Diagnostics" "GET" "$BASE_URL/diagnostics"
test_endpoint "System State" "GET" "$BASE_URL/diagnostics/system-state"
test_endpoint "Joint States" "GET" "$BASE_URL/diagnostics/joints"

# Rail endpoint (may fail if not SXL)
echo -n "Testing Rail Status... "
rail_response=$(curl -s -w "\n%{http_code}" "$BASE_URL/diagnostics/rail")
rail_code=$(echo "$rail_response" | tail -n1)
if [ "$rail_code" -eq 200 ]; then
    echo -e "${GREEN}✓${NC} (Rail available - SXL model)"
    echo "$rail_response" | sed '$d' | head -3 | sed 's/^/  /'
elif [ "$rail_code" -eq 400 ]; then
    echo -e "${YELLOW}⚠${NC} (Rail not available - may be SX model)"
else
    echo -e "${RED}✗${NC} (HTTP $rail_code)"
fi

echo ""
echo "4. Testing Frontend..."
echo ""

# Check frontend
echo -n "Testing Frontend... "
frontend_response=$(curl -s -w "\n%{http_code}" "$FRONTEND_URL")
frontend_code=$(echo "$frontend_response" | tail -n1)
if [ "$frontend_code" -eq 200 ]; then
    echo -e "${GREEN}✓${NC} (HTTP $frontend_code)"
    echo "   Open in browser: $FRONTEND_URL"
else
    echo -e "${RED}✗${NC} (HTTP $frontend_code)"
fi

echo ""
echo "=========================================="
echo "Testing Complete"
echo "=========================================="
echo ""
echo "Next steps:"
echo "  1. Open frontend: $FRONTEND_URL"
echo "  2. Check PM2 logs: pm2 logs"
echo "  3. Test movement (if robot connected):"
echo "     curl -X POST \"$BASE_URL/jog\" \\"
echo "       -H \"Content-Type: application/json\" \\"
echo "       -d '{\"joint\": 1, \"distance\": 0.001, \"speed_profile\": 1}'"
echo ""


