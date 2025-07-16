#!/bin/bash

# Test script for complete analytics implementation
# This script tests all analytics components including admin services and migration

set -e

echo "🧪 Testing Complete Analytics Implementation"
echo "=========================================="

# Configuration
API_BASE="http://localhost:8080"
ADMIN_TOKEN=""  # You'll need to set this with a valid admin token

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to make API calls
make_api_call() {
    local method=$1
    local endpoint=$2
    local description=$3
    local auth_header=""
    
    if [ ! -z "$ADMIN_TOKEN" ]; then
        auth_header="-H \"Authorization: Bearer $ADMIN_TOKEN\""
    fi
    
    echo -e "${BLUE}Testing:${NC} $description"
    echo -e "${YELLOW}$method${NC} $API_BASE$endpoint"
    
    if [ "$method" = "GET" ]; then
        response=$(eval "curl -s -w \"HTTPSTATUS:%{http_code}\" $auth_header \"$API_BASE$endpoint\"")
    else
        response=$(eval "curl -s -w \"HTTPSTATUS:%{http_code}\" -X $method $auth_header \"$API_BASE$endpoint\"")
    fi
    
    http_code=$(echo $response | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    body=$(echo $response | sed -e 's/HTTPSTATUS\:.*//g')
    
    if [ $http_code -eq 200 ] || [ $http_code -eq 201 ]; then
        echo -e "${GREEN}✓ Success${NC} (HTTP $http_code)"
        echo "$body" | jq '.' 2>/dev/null || echo "$body"
    else
        echo -e "${RED}✗ Failed${NC} (HTTP $http_code)"
        echo "$body"
    fi
    echo ""
}

echo "1. Testing Basic Health Checks"
echo "=============================="

# Test basic health endpoint
make_api_call "GET" "/health" "Basic application health"

# Test Prometheus metrics endpoint
make_api_call "GET" "/metrics" "Prometheus metrics endpoint"

echo "2. Testing Analytics Data Source"
echo "==============================="

# Test analytics data source info (requires auth)
if [ ! -z "$ADMIN_TOKEN" ]; then
    make_api_call "GET" "/api/admin/analytics/data-source" "Analytics data source information"
    make_api_call "GET" "/api/admin/analytics/health" "Comprehensive analytics health"
else
    echo -e "${YELLOW}⚠ Skipping authenticated endpoints - ADMIN_TOKEN not set${NC}"
fi

echo "3. Testing Migration Services"
echo "============================"

if [ ! -z "$ADMIN_TOKEN" ]; then
    make_api_call "GET" "/api/admin/analytics/migration/status" "Migration status"
    make_api_call "GET" "/api/admin/analytics/migration/stats" "Migration statistics"
    
    echo -e "${BLUE}Note:${NC} Migration endpoints are available but require admin authentication"
    echo "Available migration endpoints:"
    echo "  POST /api/admin/analytics/migrate"
    echo "  POST /api/admin/analytics/migrate/user-activities"
    echo "  POST /api/admin/analytics/migrate/content-engagements"
    echo "  POST /api/admin/analytics/migrate/tag-analytics"
    echo "  POST /api/admin/analytics/migrate/performance-metrics"
    echo "  POST /api/admin/analytics/validate"
fi

echo "4. Testing Analytics Endpoints"
echo "=============================="

if [ ! -z "$ADMIN_TOKEN" ]; then
    make_api_call "GET" "/api/admin/analytics/user-growth?days=7" "User growth stats (database)"
    make_api_call "GET" "/api/admin/analytics/content-stats?days=7" "Content stats (database)"
    make_api_call "GET" "/api/admin/analytics/system-health" "System health (database)"
    
    make_api_call "GET" "/api/admin/analytics/user-growth-influx?days=7" "User growth stats (InfluxDB)"
    make_api_call "GET" "/api/admin/analytics/content-stats-influx?days=7" "Content stats (InfluxDB)"
    make_api_call "GET" "/api/admin/analytics/system-health-influx" "System health (InfluxDB)"
fi

echo "5. Testing Tag Analytics"
echo "======================="

make_api_call "GET" "/api/tags/trending/analytics?days=7&limit=5" "Trending tags analytics"
make_api_call "GET" "/api/tags/top/analytics?limit=10" "Top tags analytics"

echo "6. Testing Metrics Endpoints"
echo "============================"

if [ ! -z "$ADMIN_TOKEN" ]; then
    make_api_call "GET" "/api/metrics/health" "Notification system health"
    make_api_call "GET" "/api/metrics/connections" "Connection pool stats"
    make_api_call "GET" "/api/metrics/queue" "Queue statistics"
fi

echo "7. Testing Docker Services"
echo "=========================="

echo -e "${BLUE}Checking Docker services...${NC}"

# Check if analytics services are running
if command -v docker-compose &> /dev/null; then
    echo "Analytics services status:"
    docker-compose -f docker-compose.local.yml ps influxdb prometheus grafana 2>/dev/null || echo "Docker Compose not running or services not found"
else
    echo "Docker Compose not available"
fi

echo "8. Testing Service URLs"
echo "======================"

# Test InfluxDB
echo -e "${BLUE}Testing InfluxDB...${NC}"
curl -s -f http://localhost:8086/ping && echo -e "${GREEN}✓ InfluxDB is accessible${NC}" || echo -e "${RED}✗ InfluxDB not accessible${NC}"

# Test Prometheus
echo -e "${BLUE}Testing Prometheus...${NC}"
curl -s -f http://localhost:9090/-/healthy && echo -e "${GREEN}✓ Prometheus is accessible${NC}" || echo -e "${RED}✗ Prometheus not accessible${NC}"

# Test Grafana
echo -e "${BLUE}Testing Grafana...${NC}"
curl -s -f http://localhost:3001/api/health && echo -e "${GREEN}✓ Grafana is accessible${NC}" || echo -e "${RED}✗ Grafana not accessible${NC}"

echo ""
echo "🎉 Analytics Testing Complete!"
echo "=============================="
echo ""
echo "📊 Access your analytics:"
echo "  • Grafana Dashboard: http://localhost:3001 (admin/yapplr123)"
echo "  • InfluxDB UI: http://localhost:8086 (yapplr/yapplr123)"
echo "  • Prometheus: http://localhost:9090"
echo ""
echo "🔧 To test authenticated endpoints:"
echo "  1. Login to get an admin token"
echo "  2. Set ADMIN_TOKEN environment variable"
echo "  3. Re-run this script"
echo ""
echo "📈 Available Analytics Features:"
echo "  ✓ Dual-write pattern (Database + InfluxDB)"
echo "  ✓ Admin analytics endpoints"
echo "  ✓ Data migration utilities"
echo "  ✓ Real-time metrics collection"
echo "  ✓ Grafana dashboards"
echo "  ✓ Prometheus monitoring"
echo "  ✓ Health checks and validation"
