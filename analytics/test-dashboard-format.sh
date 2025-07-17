#!/bin/bash

# Quick test to verify dashboard JSON format is correct for Grafana import

set -e

echo "🧪 Testing Dashboard JSON Format"
echo "================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

DASHBOARD_DIR="analytics/grafana/dashboards"

echo -e "${BLUE}Checking dashboard JSON files...${NC}"
echo ""

# Test each dashboard file
for dashboard in "$DASHBOARD_DIR"/*.json; do
    if [ -f "$dashboard" ]; then
        filename=$(basename "$dashboard")
        echo -e "${BLUE}Testing: $filename${NC}"
        
        # Check if file exists and is readable
        if [ ! -r "$dashboard" ]; then
            echo -e "${RED}  ❌ File not readable${NC}"
            continue
        fi
        
        # Check JSON syntax
        if ! jq empty "$dashboard" 2>/dev/null; then
            echo -e "${RED}  ❌ Invalid JSON syntax${NC}"
            continue
        fi
        
        # Check for required dashboard fields
        title=$(jq -r '.title // "MISSING"' "$dashboard" 2>/dev/null)
        panels_count=$(jq '.panels | length' "$dashboard" 2>/dev/null || echo "0")
        has_id=$(jq 'has("id")' "$dashboard" 2>/dev/null || echo "false")
        has_nested_dashboard=$(jq 'has("dashboard")' "$dashboard" 2>/dev/null || echo "false")
        
        # Validate structure
        echo "  📊 Title: $title"
        echo "  📈 Panels: $panels_count"
        
        # Check for issues
        issues=0
        
        if [ "$title" = "MISSING" ] || [ "$title" = "null" ] || [ "$title" = "" ]; then
            echo -e "${RED}  ❌ Missing or empty title${NC}"
            ((issues++))
        else
            echo -e "${GREEN}  ✅ Has title${NC}"
        fi
        
        if [ "$panels_count" -eq 0 ]; then
            echo -e "${RED}  ❌ No panels found${NC}"
            ((issues++))
        else
            echo -e "${GREEN}  ✅ Has $panels_count panels${NC}"
        fi
        
        if [ "$has_nested_dashboard" = "true" ]; then
            echo -e "${RED}  ❌ Has nested 'dashboard' object (wrong format)${NC}"
            ((issues++))
        else
            echo -e "${GREEN}  ✅ Correct root structure${NC}"
        fi
        
        if [ "$has_id" = "false" ]; then
            echo -e "${YELLOW}  ⚠️  Missing 'id' field (will be auto-generated)${NC}"
        else
            echo -e "${GREEN}  ✅ Has id field${NC}"
        fi
        
        # Check data source references
        datasource_refs=$(jq '[.panels[]?.targets[]?.datasource?.type // empty] | unique' "$dashboard" 2>/dev/null || echo "[]")
        if [ "$datasource_refs" != "[]" ]; then
            echo "  🔗 Data sources: $datasource_refs"
            if echo "$datasource_refs" | grep -q "influxdb"; then
                echo -e "${GREEN}  ✅ Uses InfluxDB data source${NC}"
            else
                echo -e "${YELLOW}  ⚠️  No InfluxDB data source found${NC}"
            fi
        fi
        
        # Overall status
        if [ $issues -eq 0 ]; then
            echo -e "${GREEN}  🎉 Dashboard format is correct!${NC}"
        else
            echo -e "${RED}  ❌ Found $issues issues${NC}"
        fi
        
        echo ""
    fi
done

echo -e "${BLUE}Testing import readiness...${NC}"
echo ""

# Test if Grafana is accessible
if curl -s -f "http://localhost:3001/api/health" > /dev/null; then
    echo -e "${GREEN}✅ Grafana is accessible at http://localhost:3001${NC}"
else
    echo -e "${YELLOW}⚠️  Grafana not accessible - start with: docker-compose -f docker-compose.local.yml up -d${NC}"
fi

# Test if InfluxDB is accessible
if curl -s -f "http://localhost:8086/ping" > /dev/null; then
    echo -e "${GREEN}✅ InfluxDB is accessible at http://localhost:8086${NC}"
else
    echo -e "${YELLOW}⚠️  InfluxDB not accessible - start with: docker-compose -f docker-compose.local.yml up -d${NC}"
fi

echo ""
echo -e "${GREEN}🎯 Dashboard Import Instructions:${NC}"
echo "=================================="
echo ""
echo "1. 🌐 Open Grafana: http://localhost:3001"
echo "2. 🔑 Login: admin / yapplr123"
echo "3. ➕ Click '+' → Import"
echo "4. 📁 Upload JSON file"
echo "5. 🔗 Select 'InfluxDB' as data source"
echo "6. ✅ Click 'Import'"
echo ""
echo -e "${BLUE}📊 Recommended import order:${NC}"
echo "1. yapplr-test-working.json (start here)"
echo "2. yapplr-simple-working.json"
echo "3. yapplr-comprehensive-analytics.json"
echo "4. yapplr-admin-analytics.json"
echo "5. yapplr-realtime-monitoring.json"
echo ""
echo -e "${YELLOW}💡 If dashboards are empty after import:${NC}"
echo "   - Enable InfluxDB: ./analytics/enable-influxdb.sh"
echo "   - Generate test data in your application"
echo "   - Check debug: ./analytics/debug-influxdb-data.sh"
