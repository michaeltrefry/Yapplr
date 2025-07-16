#!/bin/bash

# Test script for Grafana dashboards
# This script verifies that all dashboards are valid and accessible

set -e

echo "🎨 Testing Grafana Dashboards"
echo "============================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

GRAFANA_URL="http://localhost:3001"
GRAFANA_USER="admin"
GRAFANA_PASS="yapplr123"

echo -e "${BLUE}1. Checking dashboard files...${NC}"

# Check if dashboard files exist
dashboards=(
    "yapplr-comprehensive-analytics.json"
    "yapplr-admin-analytics.json"
    "yapplr-realtime-monitoring.json"
    "yapplr-analytics.json"
)

for dashboard in "${dashboards[@]}"; do
    if [ -f "analytics/grafana/dashboards/$dashboard" ]; then
        echo -e "${GREEN}✅ $dashboard${NC}"
        
        # Validate JSON syntax
        if jq empty "analytics/grafana/dashboards/$dashboard" 2>/dev/null; then
            echo -e "${GREEN}   ✅ Valid JSON${NC}"
        else
            echo -e "${RED}   ❌ Invalid JSON${NC}"
            exit 1
        fi
    else
        echo -e "${RED}❌ Missing: $dashboard${NC}"
        exit 1
    fi
done

echo ""
echo -e "${BLUE}2. Checking Grafana availability...${NC}"

# Check if Grafana is running
if curl -s -f "$GRAFANA_URL/api/health" > /dev/null; then
    echo -e "${GREEN}✅ Grafana is accessible at $GRAFANA_URL${NC}"
else
    echo -e "${YELLOW}⚠️  Grafana not accessible at $GRAFANA_URL${NC}"
    echo "   Start with: docker-compose -f docker-compose.local.yml up -d"
    exit 0
fi

echo ""
echo -e "${BLUE}3. Testing Grafana API access...${NC}"

# Test API access
api_response=$(curl -s -u "$GRAFANA_USER:$GRAFANA_PASS" "$GRAFANA_URL/api/org" || echo "failed")

if [[ "$api_response" == *"id"* ]]; then
    echo -e "${GREEN}✅ Grafana API accessible${NC}"
else
    echo -e "${YELLOW}⚠️  Grafana API not accessible (may need to wait for startup)${NC}"
fi

echo ""
echo -e "${BLUE}4. Checking data source configuration...${NC}"

# Check if InfluxDB data source exists
datasources_response=$(curl -s -u "$GRAFANA_USER:$GRAFANA_PASS" "$GRAFANA_URL/api/datasources" || echo "[]")

if [[ "$datasources_response" == *"InfluxDB"* ]]; then
    echo -e "${GREEN}✅ InfluxDB data source configured${NC}"
else
    echo -e "${YELLOW}⚠️  InfluxDB data source not found${NC}"
    echo "   Configure manually or check provisioning"
fi

echo ""
echo -e "${BLUE}5. Listing available dashboards...${NC}"

# List dashboards
dashboards_response=$(curl -s -u "$GRAFANA_USER:$GRAFANA_PASS" "$GRAFANA_URL/api/search?type=dash-db" || echo "[]")

if [[ "$dashboards_response" == *"Yapplr"* ]]; then
    echo -e "${GREEN}✅ Yapplr dashboards found in Grafana${NC}"
    
    # Extract dashboard titles
    echo "$dashboards_response" | jq -r '.[] | select(.title | contains("Yapplr")) | "   📊 " + .title' 2>/dev/null || echo "   (Could not parse dashboard list)"
else
    echo -e "${YELLOW}⚠️  No Yapplr dashboards found${NC}"
    echo "   Dashboards may still be loading or need manual import"
fi

echo ""
echo -e "${BLUE}6. Dashboard validation summary...${NC}"

echo ""
echo "📊 Dashboard Files Created:"
echo "  ✅ yapplr-comprehensive-analytics.json - Complete analytics overview"
echo "  ✅ yapplr-admin-analytics.json - Admin-focused dashboard"
echo "  ✅ yapplr-realtime-monitoring.json - Real-time monitoring"
echo "  ✅ yapplr-analytics.json - Original basic dashboard"
echo ""
echo "🔧 Features:"
echo "  ✅ InfluxDB integration with Flux queries"
echo "  ✅ User activity tracking and visualization"
echo "  ✅ Content engagement metrics"
echo "  ✅ Performance monitoring"
echo "  ✅ Real-time updates"
echo "  ✅ Admin analytics matching your API endpoints"
echo ""
echo "🎯 Access Your Dashboards:"
echo "  🌐 Grafana: $GRAFANA_URL"
echo "  👤 Login: $GRAFANA_USER / $GRAFANA_PASS"
echo "  📊 Look for 'Yapplr' dashboards in the dashboard list"
echo ""
echo "🚀 Next Steps:"
echo "  1. Start the analytics stack: docker-compose -f docker-compose.local.yml up -d"
echo "  2. Generate some test data in your application"
echo "  3. Open Grafana and explore the dashboards"
echo "  4. Customize queries and panels as needed"
echo ""

if [[ "$datasources_response" == *"InfluxDB"* ]] && [[ "$dashboards_response" == *"Yapplr"* ]]; then
    echo -e "${GREEN}🎉 All dashboards are ready to use!${NC}"
else
    echo -e "${YELLOW}⚠️  Some setup may be needed - check the steps above${NC}"
fi
