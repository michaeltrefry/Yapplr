#!/bin/bash

# Yapplr Analytics Testing Script
# This script helps test the analytics stack and verify data flow

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔍 Yapplr Analytics Stack Test${NC}"
echo "=================================="

# Configuration
INFLUXDB_URL="http://localhost:8086"
INFLUXDB_TOKEN="yapplr-analytics-token-local-dev-only"
INFLUXDB_ORG="yapplr"
INFLUXDB_BUCKET="analytics"
GRAFANA_URL="http://localhost:3001"
PROMETHEUS_URL="http://localhost:9090"

# Function to check service health
check_service() {
    local service_name=$1
    local url=$2
    local expected_status=${3:-200}
    
    echo -n "Checking $service_name... "
    
    if curl -s -o /dev/null -w "%{http_code}" "$url" | grep -q "$expected_status"; then
        echo -e "${GREEN}✅ OK${NC}"
        return 0
    else
        echo -e "${RED}❌ FAILED${NC}"
        return 1
    fi
}

# Function to query InfluxDB
query_influxdb() {
    local query=$1
    local description=$2
    
    echo -e "\n${YELLOW}📊 $description${NC}"
    
    curl -s -X POST "$INFLUXDB_URL/api/v2/query?org=$INFLUXDB_ORG" \
        -H "Authorization: Token $INFLUXDB_TOKEN" \
        -H "Content-Type: application/vnd.flux" \
        -d "$query" | jq -r '.[] | select(.result) | .result | .[] | ._value' 2>/dev/null || echo "No data found"
}

echo -e "\n${BLUE}🏥 Health Checks${NC}"
echo "=================="

# Check all services
check_service "InfluxDB" "$INFLUXDB_URL/ping" "204"
check_service "Grafana" "$GRAFANA_URL/api/health"
check_service "Prometheus" "$PROMETHEUS_URL/-/healthy"

echo -e "\n${BLUE}📈 Data Verification${NC}"
echo "===================="

# Check for analytics data in InfluxDB
echo -e "\n${YELLOW}Checking for analytics data...${NC}"

# Query user activities
query_influxdb 'from(bucket:"analytics") |> range(start:-1h) |> filter(fn:(r) => r._measurement == "user_activities") |> count()' "User Activities (last hour)"

# Query content engagement
query_influxdb 'from(bucket:"analytics") |> range(start:-1h) |> filter(fn:(r) => r._measurement == "content_engagement") |> count()' "Content Engagement (last hour)"

# Query performance metrics
query_influxdb 'from(bucket:"analytics") |> range(start:-1h) |> filter(fn:(r) => r._measurement == "performance_metrics") |> count()' "Performance Metrics (last hour)"

# Query tag actions
query_influxdb 'from(bucket:"analytics") |> range(start:-1h) |> filter(fn:(r) => r._measurement == "tag_actions") |> count()' "Tag Actions (last hour)"

echo -e "\n${BLUE}🔗 Service URLs${NC}"
echo "==============="
echo -e "Grafana Dashboard: ${GREEN}$GRAFANA_URL${NC} (admin/yapplr123)"
echo -e "InfluxDB UI: ${GREEN}$INFLUXDB_URL${NC} (yapplr/yapplr123)"
echo -e "Prometheus UI: ${GREEN}$PROMETHEUS_URL${NC}"

echo -e "\n${BLUE}📝 Sample Queries${NC}"
echo "=================="
echo "To manually query InfluxDB:"
echo ""
echo "curl -X POST \"$INFLUXDB_URL/api/v2/query?org=$INFLUXDB_ORG\" \\"
echo "  -H \"Authorization: Token $INFLUXDB_TOKEN\" \\"
echo "  -H \"Content-Type: application/vnd.flux\" \\"
echo "  -d 'from(bucket:\"analytics\") |> range(start:-1h) |> filter(fn:(r) => r._measurement == \"user_activities\")'"

echo -e "\n${BLUE}🚀 Next Steps${NC}"
echo "=============="
echo "1. Generate some application activity (login, create posts, etc.)"
echo "2. Wait a few minutes for data to appear"
echo "3. Check the Grafana dashboard: $GRAFANA_URL"
echo "4. Run this script again to verify data flow"

echo -e "\n${GREEN}✅ Analytics stack test completed!${NC}"
