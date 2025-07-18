#!/bin/bash

# Debug script to test staging API connectivity and health
# This helps diagnose CORS and timeout issues

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}🔍 Staging API Diagnostic Script${NC}"
echo -e "${BLUE}=================================${NC}"

# Test 1: Check if containers are running
echo -e "\n${BLUE}Test 1: Container Status${NC}"
if command -v docker &> /dev/null; then
    echo "Checking Docker containers..."
    docker compose -f docker-compose.stage.yml ps
else
    echo -e "${YELLOW}Docker not available locally${NC}"
fi

# Test 2: Direct health check to API container (bypassing nginx)
echo -e "\n${BLUE}Test 2: Direct API Health Check${NC}"
echo "Testing direct connection to API container..."

# Try to connect directly to the API container
if curl -f -m 10 http://localhost:8080/health 2>/dev/null; then
    echo -e "${GREEN}✅ Direct API connection successful${NC}"
else
    echo -e "${RED}❌ Direct API connection failed${NC}"
    echo "This suggests the API container is not responding"
fi

# Test 3: Health check through nginx
echo -e "\n${BLUE}Test 3: Health Check via Nginx${NC}"
echo "Testing connection through nginx proxy..."

if curl -f -m 10 http://localhost/health 2>/dev/null; then
    echo -e "${GREEN}✅ Nginx proxy to API successful${NC}"
else
    echo -e "${RED}❌ Nginx proxy to API failed${NC}"
    echo "This suggests nginx routing issues"
fi

# Test 4: HTTPS health check
echo -e "\n${BLUE}Test 4: HTTPS Health Check${NC}"
echo "Testing HTTPS connection..."

if curl -f -k -m 10 https://localhost/health 2>/dev/null; then
    echo -e "${GREEN}✅ HTTPS connection successful${NC}"
else
    echo -e "${RED}❌ HTTPS connection failed${NC}"
    echo "This suggests SSL/TLS issues"
fi

# Test 5: External domain health check
echo -e "\n${BLUE}Test 5: External Domain Health Check${NC}"
echo "Testing external staging domain..."

if curl -f -m 10 https://stg-api.yapplr.com/health 2>/dev/null; then
    echo -e "${GREEN}✅ External domain connection successful${NC}"
else
    echo -e "${RED}❌ External domain connection failed${NC}"
    echo "This suggests DNS or external routing issues"
fi

# Test 6: CORS preflight test
echo -e "\n${BLUE}Test 6: CORS Preflight Test${NC}"
echo "Testing CORS preflight request..."

cors_response=$(curl -s -o /dev/null -w "%{http_code}" -X OPTIONS \
    -H "Origin: https://stg.yapplr.com" \
    -H "Access-Control-Request-Method: POST" \
    -H "Access-Control-Request-Headers: Content-Type,Authorization" \
    https://stg-api.yapplr.com/api/posts/with-media 2>/dev/null || echo "000")

if [ "$cors_response" = "200" ] || [ "$cors_response" = "204" ]; then
    echo -e "${GREEN}✅ CORS preflight successful (HTTP $cors_response)${NC}"
else
    echo -e "${RED}❌ CORS preflight failed (HTTP $cors_response)${NC}"
    echo "This suggests CORS configuration issues"
fi

# Test 7: API endpoint timeout test
echo -e "\n${BLUE}Test 7: API Endpoint Timeout Test${NC}"
echo "Testing /api/posts/with-media endpoint with timeout..."

start_time=$(date +%s)
response_code=$(curl -s -o /dev/null -w "%{http_code}" -m 30 \
    -X POST \
    -H "Content-Type: application/json" \
    -H "Origin: https://stg.yapplr.com" \
    -d '{"content":"test","mediaFiles":[]}' \
    https://stg-api.yapplr.com/api/posts/with-media 2>/dev/null || echo "000")
end_time=$(date +%s)
duration=$((end_time - start_time))

echo "Response code: $response_code"
echo "Duration: ${duration}s"

if [ "$response_code" = "401" ]; then
    echo -e "${YELLOW}⚠️ Unauthorized (expected without auth token)${NC}"
elif [ "$response_code" = "504" ]; then
    echo -e "${RED}❌ Gateway timeout - API is taking too long to respond${NC}"
elif [ "$response_code" = "000" ]; then
    echo -e "${RED}❌ Connection failed or timeout${NC}"
else
    echo -e "${GREEN}✅ API endpoint responding (HTTP $response_code)${NC}"
fi

# Test 8: Check API logs for errors
echo -e "\n${BLUE}Test 8: Recent API Logs${NC}"
if command -v docker &> /dev/null; then
    echo "Checking recent API logs for errors..."
    docker compose -f docker-compose.stage.yml logs --tail=20 yapplr-api | grep -i error || echo "No recent errors found"
else
    echo -e "${YELLOW}Docker not available - cannot check logs${NC}"
fi

# Summary
echo -e "\n${BLUE}=================================${NC}"
echo -e "${GREEN}🎯 Diagnostic Summary${NC}"
echo -e "\n${BLUE}Common Issues and Solutions:${NC}"
echo -e "1. ${YELLOW}504 Gateway Timeout${NC}: API taking too long - check database/processing"
echo -e "2. ${YELLOW}CORS Error${NC}: Usually secondary to 504 - nginx doesn't add CORS headers on errors"
echo -e "3. ${YELLOW}Container Not Running${NC}: Check Docker container status"
echo -e "4. ${YELLOW}Database Issues${NC}: Check database connectivity and migrations"
echo -e "5. ${YELLOW}Memory/CPU Issues${NC}: Check container resource usage"

echo -e "\n${BLUE}Next Steps:${NC}"
echo -e "- If 504 errors: Check API container logs and database performance"
echo -e "- If CORS errors: Usually resolve when 504 is fixed"
echo -e "- If container issues: Restart containers with optimized deployment"
echo -e "- If persistent: Check server resources and database health"
