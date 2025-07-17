#!/bin/bash

# Debug script to check InfluxDB data structure and content
# This helps diagnose why Grafana dashboards are empty

set -e

echo "🔍 Debugging InfluxDB Data Structure"
echo "===================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# InfluxDB configuration
INFLUX_URL="http://localhost:8086"
INFLUX_ORG="yapplr"
INFLUX_BUCKET="analytics"
INFLUX_TOKEN="yapplr-analytics-token-local-dev-only"

echo -e "${BLUE}1. Checking InfluxDB connectivity...${NC}"

# Check if InfluxDB is running
if curl -s -f "$INFLUX_URL/ping" > /dev/null; then
    echo -e "${GREEN}✅ InfluxDB is accessible at $INFLUX_URL${NC}"
else
    echo -e "${RED}❌ InfluxDB not accessible at $INFLUX_URL${NC}"
    echo "   Start with: docker-compose -f docker-compose.local.yml up -d"
    exit 1
fi

echo ""
echo -e "${BLUE}2. Checking InfluxDB health...${NC}"

health_response=$(curl -s "$INFLUX_URL/health" || echo "failed")
if [[ "$health_response" == *"pass"* ]]; then
    echo -e "${GREEN}✅ InfluxDB health check passed${NC}"
else
    echo -e "${YELLOW}⚠️  InfluxDB health check failed or pending${NC}"
    echo "Response: $health_response"
fi

echo ""
echo -e "${BLUE}3. Checking if analytics bucket exists...${NC}"

# Check buckets
buckets_response=$(curl -s -H "Authorization: Token $INFLUX_TOKEN" "$INFLUX_URL/api/v2/buckets?org=$INFLUX_ORG" || echo "failed")

if [[ "$buckets_response" == *"analytics"* ]]; then
    echo -e "${GREEN}✅ Analytics bucket exists${NC}"
else
    echo -e "${YELLOW}⚠️  Analytics bucket not found${NC}"
    echo "Available buckets:"
    echo "$buckets_response" | jq -r '.buckets[]?.name // "Could not parse buckets"' 2>/dev/null || echo "   Could not parse bucket list"
fi

echo ""
echo -e "${BLUE}4. Checking for data in the last 24 hours...${NC}"

# Query for any data in the analytics bucket
query_all_data='from(bucket: "analytics")
  |> range(start: -24h)
  |> limit(n: 10)'

echo "Running query: $query_all_data"
echo ""

all_data_response=$(curl -s -X POST "$INFLUX_URL/api/v2/query?org=$INFLUX_ORG" \
  -H "Authorization: Token $INFLUX_TOKEN" \
  -H "Content-Type: application/vnd.flux" \
  -d "$query_all_data" || echo "failed")

if [[ "$all_data_response" == *"_measurement"* ]]; then
    echo -e "${GREEN}✅ Found data in analytics bucket${NC}"
    echo "Sample data:"
    echo "$all_data_response" | head -20
else
    echo -e "${YELLOW}⚠️  No data found in analytics bucket in the last 24 hours${NC}"
    echo "Response: $all_data_response"
fi

echo ""
echo -e "${BLUE}5. Checking specific measurements...${NC}"

measurements=("user_activities" "content_engagement" "tag_actions" "performance_metrics" "events" "metrics")

for measurement in "${measurements[@]}"; do
    echo -e "${BLUE}Checking measurement: $measurement${NC}"
    
    query_measurement="from(bucket: \"analytics\")
  |> range(start: -24h)
  |> filter(fn: (r) => r._measurement == \"$measurement\")
  |> limit(n: 5)"
    
    measurement_response=$(curl -s -X POST "$INFLUX_URL/api/v2/query?org=$INFLUX_ORG" \
      -H "Authorization: Token $INFLUX_TOKEN" \
      -H "Content-Type: application/vnd.flux" \
      -d "$query_measurement" || echo "failed")
    
    if [[ "$measurement_response" == *"_measurement"* ]]; then
        echo -e "${GREEN}  ✅ Found data in $measurement${NC}"
        # Count records
        record_count=$(echo "$measurement_response" | grep -c "_measurement" || echo "0")
        echo -e "${GREEN}     Records found: $record_count${NC}"
    else
        echo -e "${YELLOW}  ⚠️  No data in $measurement${NC}"
    fi
done

echo ""
echo -e "${BLUE}6. Checking field and tag structure...${NC}"

# Get schema information
schema_query='import "influxdata/influxdb/schema"
schema.measurements(bucket: "analytics")'

echo "Getting measurement schema..."
schema_response=$(curl -s -X POST "$INFLUX_URL/api/v2/query?org=$INFLUX_ORG" \
  -H "Authorization: Token $INFLUX_TOKEN" \
  -H "Content-Type: application/vnd.flux" \
  -d "$schema_query" || echo "failed")

if [[ "$schema_response" == *"_value"* ]]; then
    echo -e "${GREEN}✅ Schema information retrieved${NC}"
    echo "Available measurements:"
    echo "$schema_response" | grep -o '"[^"]*"' | sort | uniq || echo "Could not parse measurements"
else
    echo -e "${YELLOW}⚠️  Could not retrieve schema information${NC}"
fi

echo ""
echo -e "${BLUE}7. Application configuration check...${NC}"

# Check if InfluxDB is enabled in the application
if [ -f "Yapplr.Api/appsettings.json" ]; then
    influx_enabled=$(grep -A 5 '"InfluxDB"' Yapplr.Api/appsettings.json | grep '"Enabled"' | grep -o 'true\|false' || echo "not found")
    echo "InfluxDB enabled in appsettings.json: $influx_enabled"
    
    if [ "$influx_enabled" = "false" ]; then
        echo -e "${RED}❌ InfluxDB is disabled in application configuration!${NC}"
        echo -e "${YELLOW}   To enable: Set InfluxDB:Enabled to true in appsettings.json${NC}"
    else
        echo -e "${GREEN}✅ InfluxDB is enabled in application configuration${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  Could not find appsettings.json${NC}"
fi

echo ""
echo -e "${BLUE}8. Recommendations...${NC}"

echo ""
echo "📋 Troubleshooting Steps:"
echo ""

if [ "$influx_enabled" = "false" ]; then
    echo -e "${YELLOW}1. Enable InfluxDB in your application:${NC}"
    echo "   - Edit Yapplr.Api/appsettings.json"
    echo "   - Set \"InfluxDB\": { \"Enabled\": true }"
    echo "   - Restart your application"
    echo ""
fi

echo -e "${BLUE}2. Generate test data:${NC}"
echo "   - Start your application"
echo "   - Login, create posts, interact with content"
echo "   - Check this script again to see if data appears"
echo ""

echo -e "${BLUE}3. Check application logs:${NC}"
echo "   - Look for InfluxDB connection errors"
echo "   - Verify analytics service is working"
echo ""

echo -e "${BLUE}4. Verify Grafana data source:${NC}"
echo "   - URL: http://influxdb:8086 (or http://localhost:8086)"
echo "   - Organization: yapplr"
echo "   - Token: yapplr-analytics-token-local-dev-only"
echo "   - Default Bucket: analytics"
echo ""

echo "🔍 Debug complete! Check the results above to identify issues."
