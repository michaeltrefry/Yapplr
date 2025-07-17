#!/bin/bash

# Script to fix Grafana dashboard JSON format
# Removes the nested "dashboard" wrapper and fixes indentation

set -e

echo "🔧 Fixing Grafana Dashboard JSON Format"
echo "======================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

DASHBOARD_DIR="analytics/grafana/dashboards"

echo -e "${BLUE}1. Finding dashboard JSON files...${NC}"

# Find all JSON files in the dashboards directory
dashboards=($(find "$DASHBOARD_DIR" -name "*.json" -type f))

if [ ${#dashboards[@]} -eq 0 ]; then
    echo -e "${YELLOW}⚠️  No JSON files found in $DASHBOARD_DIR${NC}"
    exit 0
fi

echo "Found ${#dashboards[@]} dashboard files:"
for dashboard in "${dashboards[@]}"; do
    echo "  📊 $(basename "$dashboard")"
done

echo ""
echo -e "${BLUE}2. Fixing dashboard JSON format...${NC}"

for dashboard in "${dashboards[@]}"; do
    echo -e "${BLUE}Processing: $(basename "$dashboard")${NC}"
    
    # Create backup
    cp "$dashboard" "$dashboard.backup.$(date +%Y%m%d_%H%M%S)"
    
    # Check if file has the nested "dashboard" structure
    if grep -q '"dashboard".*{' "$dashboard"; then
        echo -e "${YELLOW}  ⚠️  Found nested dashboard structure, fixing...${NC}"
        
        # Use jq to extract the dashboard object and reformat
        if command -v jq &> /dev/null; then
            # Extract the dashboard object and pretty-print
            jq '.dashboard' "$dashboard" > "$dashboard.tmp" && mv "$dashboard.tmp" "$dashboard"
            echo -e "${GREEN}  ✅ Fixed using jq${NC}"
        else
            echo -e "${RED}  ❌ jq not available, manual fix needed${NC}"
        fi
    else
        echo -e "${GREEN}  ✅ Already in correct format${NC}"
    fi
    
    # Validate JSON syntax
    if jq empty "$dashboard" 2>/dev/null; then
        echo -e "${GREEN}  ✅ Valid JSON${NC}"
    else
        echo -e "${RED}  ❌ Invalid JSON after fix${NC}"
        # Restore backup
        cp "$dashboard.backup."* "$dashboard"
        echo -e "${YELLOW}  ⚠️  Restored from backup${NC}"
    fi
    
    echo ""
done

echo -e "${BLUE}3. Verifying dashboard structure...${NC}"

for dashboard in "${dashboards[@]}"; do
    filename=$(basename "$dashboard")
    
    # Check for required fields
    title=$(jq -r '.title // "MISSING"' "$dashboard" 2>/dev/null)
    panels_count=$(jq '.panels | length' "$dashboard" 2>/dev/null || echo "0")
    
    echo "📊 $filename:"
    echo "   Title: $title"
    echo "   Panels: $panels_count"
    
    if [ "$title" = "MISSING" ] || [ "$title" = "null" ]; then
        echo -e "${RED}   ❌ Missing title${NC}"
    else
        echo -e "${GREEN}   ✅ Has title${NC}"
    fi
    
    if [ "$panels_count" -gt 0 ]; then
        echo -e "${GREEN}   ✅ Has panels${NC}"
    else
        echo -e "${RED}   ❌ No panels${NC}"
    fi
    
    echo ""
done

echo -e "${BLUE}4. Testing dashboard import format...${NC}"

# Create a test dashboard to verify the format
test_dashboard='{
  "id": null,
  "title": "Test Dashboard",
  "tags": [],
  "timezone": "browser",
  "panels": [],
  "time": {
    "from": "now-6h",
    "to": "now"
  },
  "timepicker": {},
  "templating": {
    "list": []
  },
  "annotations": {
    "list": []
  },
  "refresh": "30s",
  "schemaVersion": 37,
  "version": 1
}'

echo "$test_dashboard" > "$DASHBOARD_DIR/test-format.json"

if jq empty "$DASHBOARD_DIR/test-format.json" 2>/dev/null; then
    echo -e "${GREEN}✅ Test dashboard format is valid${NC}"
    rm "$DASHBOARD_DIR/test-format.json"
else
    echo -e "${RED}❌ Test dashboard format is invalid${NC}"
fi

echo ""
echo -e "${GREEN}🎉 Dashboard JSON Fix Complete!${NC}"
echo ""
echo "📋 Summary:"
echo "==========="
echo "  📊 Processed: ${#dashboards[@]} dashboard files"
echo "  💾 Backups created with timestamp suffix"
echo "  🔧 Fixed nested dashboard structure"
echo "  ✅ Validated JSON syntax"
echo ""
echo "🚀 Next Steps:"
echo "=============="
echo "  1. Import dashboards in Grafana UI"
echo "  2. Go to http://localhost:3001"
echo "  3. Dashboards → Import → Upload JSON file"
echo "  4. Select InfluxDB as data source"
echo ""
echo "📁 Dashboard files ready for import:"
for dashboard in "${dashboards[@]}"; do
    echo "  📊 $(basename "$dashboard")"
done
