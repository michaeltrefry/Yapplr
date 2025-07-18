#!/bin/bash

# Debug script to verify connection string handling
# Run this on your production server to check if the connection string is properly loaded

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}🔍 Connection String Debug Script${NC}"
echo -e "${BLUE}=================================${NC}"

# Check if we're in the right directory
if [ ! -f "docker-compose.prod.yml" ]; then
    echo -e "${RED}❌ Error: Must be run from the project root directory (/opt/Yapplr)${NC}"
    exit 1
fi

# Check if .env file exists
if [ ! -f ".env" ]; then
    echo -e "${RED}❌ Error: .env file not found${NC}"
    echo -e "${YELLOW}This script should be run after deployment${NC}"
    exit 1
fi

echo -e "\n${BLUE}1. Checking .env file format:${NC}"
echo -e "File exists: ${GREEN}✅${NC}"
echo -e "File size: $(wc -c < .env) bytes"
echo -e "Number of lines: $(wc -l < .env)"

echo -e "\n${BLUE}2. Checking for connection string in .env:${NC}"
if grep -q "PROD_DATABASE_CONNECTION_STRING" .env; then
    echo -e "${GREEN}✅ PROD_DATABASE_CONNECTION_STRING found in .env${NC}"
    # Show the line but mask the password
    masked_line=$(grep "PROD_DATABASE_CONNECTION_STRING" .env | sed 's/Password=[^;"]*/Password=****/g')
    echo -e "Line content (masked): ${masked_line}"
else
    echo -e "${RED}❌ PROD_DATABASE_CONNECTION_STRING not found in .env${NC}"
    exit 1
fi

echo -e "\n${BLUE}3. Testing environment variable loading:${NC}"
# Load environment variables the same way the deployment script does
set -a
source .env
set +a

if [ -n "$PROD_DATABASE_CONNECTION_STRING" ]; then
    echo -e "${GREEN}✅ Connection string loaded successfully${NC}"
    # Parse and display components (masked)
    host=$(echo "$PROD_DATABASE_CONNECTION_STRING" | grep -oP 'Host=\K[^;]*' || echo "")
    port=$(echo "$PROD_DATABASE_CONNECTION_STRING" | grep -oP 'Port=\K[^;]*' || echo "5432")
    database=$(echo "$PROD_DATABASE_CONNECTION_STRING" | grep -oP 'Database=\K[^;]*' || echo "")
    username=$(echo "$PROD_DATABASE_CONNECTION_STRING" | grep -oP 'Username=\K[^;]*' || echo "")
    
    echo -e "  Host: ${host}"
    echo -e "  Port: ${port}"
    echo -e "  Database: ${database}"
    echo -e "  Username: ${username}"
    echo -e "  Password: ****"
else
    echo -e "${RED}❌ Connection string not loaded into environment${NC}"
    exit 1
fi

echo -e "\n${BLUE}4. Checking Docker container environment:${NC}"
if docker ps | grep -q yapplr-api; then
    echo -e "${GREEN}✅ yapplr-api container is running${NC}"
    
    # Check if the connection string is properly passed to the container
    echo -e "\n${BLUE}5. Checking container environment variables:${NC}"
    if docker exec yapplr-api env | grep -q "ConnectionStrings__DefaultConnection"; then
        echo -e "${GREEN}✅ ConnectionStrings__DefaultConnection is set in container${NC}"
        # Show masked version
        masked_container_conn=$(docker exec yapplr-api env | grep "ConnectionStrings__DefaultConnection" | sed 's/Password=[^;"]*/Password=****/g')
        echo -e "Container value (masked): ${masked_container_conn}"
    else
        echo -e "${RED}❌ ConnectionStrings__DefaultConnection not found in container${NC}"
    fi
    
    echo -e "\n${BLUE}6. Checking API logs for database connection issues:${NC}"
    echo -e "${YELLOW}Recent API logs (last 20 lines):${NC}"
    docker logs yapplr-api --tail 20
else
    echo -e "${RED}❌ yapplr-api container is not running${NC}"
    echo -e "\n${BLUE}Container status:${NC}"
    docker ps -a | grep yapplr || echo "No yapplr containers found"
fi

echo -e "\n${GREEN}🎉 Debug script completed${NC}"
echo -e "${YELLOW}If you see any issues above, the connection string may not be properly formatted or loaded.${NC}"
