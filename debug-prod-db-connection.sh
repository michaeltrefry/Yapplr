#!/bin/bash

# Debug production database connection
# This script helps diagnose database connection issues in production

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}🔍 Production Database Connection Debug${NC}"
echo -e "${BLUE}======================================${NC}"

# Check if we're in the right directory
if [ ! -f "docker-compose.prod.yml" ]; then
    echo -e "${RED}❌ Error: Must be run from the project root directory${NC}"
    exit 1
fi

# Check if .env file exists
if [ ! -f ".env" ]; then
    echo -e "${RED}❌ Error: .env file not found${NC}"
    echo -e "${YELLOW}This script should be run on the production server after deployment${NC}"
    exit 1
fi

# Load environment variables
source .env

echo -e "\n${BLUE}1. Environment Variables Check:${NC}"
if [ -n "$PROD_DATABASE_CONNECTION_STRING" ]; then
    echo -e "${GREEN}✅ PROD_DATABASE_CONNECTION_STRING is set${NC}"
    # Mask the password for security
    masked_conn_string=$(echo "$PROD_DATABASE_CONNECTION_STRING" | sed 's/Password=[^;]*/Password=****/g')
    echo -e "${BLUE}Connection string (masked): ${masked_conn_string}${NC}"
else
    echo -e "${RED}❌ PROD_DATABASE_CONNECTION_STRING is not set${NC}"
    exit 1
fi

echo -e "\n${BLUE}2. Parse Connection String Components:${NC}"
# Extract components from connection string
host=$(echo "$PROD_DATABASE_CONNECTION_STRING" | grep -oP 'Host=\K[^;]*' || echo "")
port=$(echo "$PROD_DATABASE_CONNECTION_STRING" | grep -oP 'Port=\K[^;]*' || echo "5432")
database=$(echo "$PROD_DATABASE_CONNECTION_STRING" | grep -oP 'Database=\K[^;]*' || echo "")
username=$(echo "$PROD_DATABASE_CONNECTION_STRING" | grep -oP 'Username=\K[^;]*' || echo "")

echo -e "Host: ${host}"
echo -e "Port: ${port}"
echo -e "Database: ${database}"
echo -e "Username: ${username}"

echo -e "\n${BLUE}3. Network Connectivity Test:${NC}"
if command -v nc >/dev/null 2>&1; then
    if nc -z "$host" "$port" 2>/dev/null; then
        echo -e "${GREEN}✅ Can reach database server at ${host}:${port}${NC}"
    else
        echo -e "${RED}❌ Cannot reach database server at ${host}:${port}${NC}"
        echo -e "${YELLOW}This could indicate network issues or server downtime${NC}"
    fi
else
    echo -e "${YELLOW}⚠️ netcat (nc) not available, skipping connectivity test${NC}"
fi

echo -e "\n${BLUE}4. Docker Container Database Connection Test:${NC}"
if docker compose -f docker-compose.prod.yml ps yapplr-api | grep -q "Up"; then
    echo -e "${GREEN}✅ yapplr-api container is running${NC}"
    
    # Test connection from within the container
    echo -e "\n${BLUE}Testing database connection from yapplr-api container:${NC}"
    docker compose -f docker-compose.prod.yml exec yapplr-api sh -c "
        echo 'Testing connection with environment variables:'
        echo 'ConnectionStrings__DefaultConnection: '\$ConnectionStrings__DefaultConnection
        
        # Try to connect using psql if available
        if command -v psql >/dev/null 2>&1; then
            echo 'Testing with psql...'
            psql \"\$ConnectionStrings__DefaultConnection\" -c 'SELECT version();' 2>&1 || echo 'psql connection failed'
        else
            echo 'psql not available in container'
        fi
    " || echo "Could not test from container"
else
    echo -e "${RED}❌ yapplr-api container is not running${NC}"
    echo -e "\n${BLUE}Container status:${NC}"
    docker compose -f docker-compose.prod.yml ps yapplr-api || echo "Could not get container status"
fi

echo -e "\n${BLUE}5. Recent Container Logs:${NC}"
echo -e "${YELLOW}Last 20 lines of yapplr-api logs:${NC}"
docker compose -f docker-compose.prod.yml logs --tail=20 yapplr-api || echo "Could not get logs"

echo -e "\n${BLUE}6. Environment Variable in Container:${NC}"
if docker compose -f docker-compose.prod.yml ps yapplr-api | grep -q "Up"; then
    echo -e "${YELLOW}Checking environment variables in container:${NC}"
    docker compose -f docker-compose.prod.yml exec yapplr-api env | grep -E "(ConnectionStrings|ASPNETCORE)" || echo "Could not check environment variables"
fi

echo -e "\n${BLUE}======================================${NC}"
echo -e "${GREEN}🎯 Debug Summary${NC}"
echo -e "\n${YELLOW}Common Issues:${NC}"
echo -e "1. ${BLUE}Incorrect password${NC} - Database password may have changed"
echo -e "2. ${BLUE}Connection string format${NC} - Check for special characters or encoding"
echo -e "3. ${BLUE}Database server issues${NC} - Server may be down or unreachable"
echo -e "4. ${BLUE}User permissions${NC} - Database user may not have proper permissions"
echo -e "5. ${BLUE}SSL/TLS issues${NC} - SSL mode requirements may have changed"

echo -e "\n${YELLOW}Next Steps:${NC}"
echo -e "1. ${GREEN}Verify database server is running${NC}"
echo -e "2. ${GREEN}Test connection string manually${NC}"
echo -e "3. ${GREEN}Check database user permissions${NC}"
echo -e "4. ${GREEN}Verify GitHub secret is correct${NC}"
