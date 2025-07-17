#!/bin/bash

# Script to enable InfluxDB in application configuration
# This fixes the main reason why dashboards are empty

set -e

echo "🔧 Enabling InfluxDB Analytics"
echo "============================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

CONFIG_FILE="Yapplr.Api/appsettings.json"

echo -e "${BLUE}1. Checking current configuration...${NC}"

if [ ! -f "$CONFIG_FILE" ]; then
    echo -e "${RED}❌ Configuration file not found: $CONFIG_FILE${NC}"
    exit 1
fi

# Check current InfluxDB enabled status
current_enabled=$(grep -A 5 '"InfluxDB"' "$CONFIG_FILE" | grep '"Enabled"' | grep -o 'true\|false' || echo "not found")
echo "Current InfluxDB enabled status: $current_enabled"

if [ "$current_enabled" = "true" ]; then
    echo -e "${GREEN}✅ InfluxDB is already enabled${NC}"
else
    echo -e "${YELLOW}⚠️  InfluxDB is currently disabled${NC}"
    
    echo ""
    echo -e "${BLUE}2. Creating backup of configuration...${NC}"
    
    # Create backup
    cp "$CONFIG_FILE" "$CONFIG_FILE.backup.$(date +%Y%m%d_%H%M%S)"
    echo -e "${GREEN}✅ Backup created${NC}"
    
    echo ""
    echo -e "${BLUE}3. Enabling InfluxDB...${NC}"
    
    # Enable InfluxDB
    if command -v jq &> /dev/null; then
        # Use jq if available for safer JSON editing
        jq '.InfluxDB.Enabled = true | .Analytics.EnableDualWrite = true | .Analytics.UseInfluxForAdminDashboard = true' "$CONFIG_FILE" > "$CONFIG_FILE.tmp" && mv "$CONFIG_FILE.tmp" "$CONFIG_FILE"
        echo -e "${GREEN}✅ InfluxDB enabled using jq${NC}"
    else
        # Fallback to sed (less safe but works)
        sed -i.bak 's/"Enabled": false/"Enabled": true/g' "$CONFIG_FILE"
        sed -i.bak 's/"EnableDualWrite": false/"EnableDualWrite": true/g' "$CONFIG_FILE"
        sed -i.bak 's/"UseInfluxForAdminDashboard": false/"UseInfluxForAdminDashboard": true/g' "$CONFIG_FILE"
        echo -e "${GREEN}✅ InfluxDB enabled using sed${NC}"
    fi
fi

echo ""
echo -e "${BLUE}4. Verifying configuration...${NC}"

# Verify the changes
new_enabled=$(grep -A 5 '"InfluxDB"' "$CONFIG_FILE" | grep '"Enabled"' | grep -o 'true\|false' || echo "not found")
dual_write=$(grep -A 5 '"Analytics"' "$CONFIG_FILE" | grep '"EnableDualWrite"' | grep -o 'true\|false' || echo "not found")

echo "InfluxDB Enabled: $new_enabled"
echo "Dual Write Enabled: $dual_write"

if [ "$new_enabled" = "true" ]; then
    echo -e "${GREEN}✅ InfluxDB is now enabled${NC}"
else
    echo -e "${RED}❌ Failed to enable InfluxDB${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}5. Configuration summary...${NC}"

echo ""
echo "📋 Current InfluxDB Configuration:"
echo "=================================="
grep -A 10 '"InfluxDB"' "$CONFIG_FILE" | head -11
echo ""
echo "📋 Current Analytics Configuration:"
echo "=================================="
grep -A 5 '"Analytics"' "$CONFIG_FILE" | head -6

echo ""
echo -e "${GREEN}🎉 InfluxDB Analytics Enabled Successfully!${NC}"
echo ""
echo "📋 Next Steps:"
echo "=============="
echo ""
echo -e "${BLUE}1. Restart your application:${NC}"
echo "   docker-compose -f docker-compose.local.yml restart yapplr-api"
echo "   # OR if running directly:"
echo "   # dotnet run --project Yapplr.Api"
echo ""
echo -e "${BLUE}2. Generate test data:${NC}"
echo "   - Login to your application"
echo "   - Create posts, like content, use hashtags"
echo "   - Navigate around to generate activity"
echo ""
echo -e "${BLUE}3. Check if data is being written:${NC}"
echo "   ./analytics/debug-influxdb-data.sh"
echo ""
echo -e "${BLUE}4. Import the working dashboard:${NC}"
echo "   - Open Grafana: http://localhost:3001"
echo "   - Import: analytics/grafana/dashboards/yapplr-simple-working.json"
echo ""
echo -e "${BLUE}5. Verify dashboards show data:${NC}"
echo "   - Check stat panels show non-zero values"
echo "   - Time series should show activity lines"
echo "   - Raw data table should show recent records"
echo ""
echo "🔧 Configuration files modified:"
echo "  - $CONFIG_FILE (InfluxDB enabled)"
echo "  - Backup saved as: $CONFIG_FILE.backup.*"
echo ""
echo "📊 Your analytics dashboards should now start working!"
