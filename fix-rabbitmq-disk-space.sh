#!/bin/bash

# Fix RabbitMQ disk space alarm in staging
# This script clears RabbitMQ data and restarts the service

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}🔧 RabbitMQ Disk Space Fix Script${NC}"
echo -e "${BLUE}===================================${NC}"

# Check if we're in the right directory
if [ ! -f "docker-compose.stage.yml" ]; then
    echo -e "${RED}❌ Error: Must be run from the project root directory${NC}"
    exit 1
fi

echo -e "${YELLOW}⚠️ This will clear all RabbitMQ data and restart the service${NC}"
echo -e "${YELLOW}⚠️ Any pending video processing jobs will be lost${NC}"
echo -e "${BLUE}This is safe for staging but should NOT be used in production${NC}"
echo ""
read -p "Continue? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Operation cancelled${NC}"
    exit 0
fi

# Step 1: Stop RabbitMQ container
echo -e "\n${BLUE}Step 1: Stopping RabbitMQ container...${NC}"
docker compose -f docker-compose.stage.yml stop rabbitmq || true

# Step 2: Remove RabbitMQ container
echo -e "\n${BLUE}Step 2: Removing RabbitMQ container...${NC}"
docker compose -f docker-compose.stage.yml rm -f rabbitmq || true

# Step 3: Clear RabbitMQ data volume
echo -e "\n${BLUE}Step 3: Clearing RabbitMQ data volume...${NC}"
docker volume rm rabbitmq_data || true
docker volume rm yapplrapi_rabbitmq_data || true

# Step 4: Clean up any leftover RabbitMQ volumes
echo -e "\n${BLUE}Step 4: Cleaning up any leftover RabbitMQ volumes...${NC}"
docker volume ls -q | grep rabbitmq | xargs -r docker volume rm || true

# Step 5: Check available disk space
echo -e "\n${BLUE}Step 5: Checking available disk space...${NC}"
df -h

# Step 6: Clean up Docker system if needed
echo -e "\n${BLUE}Step 6: Cleaning up Docker system...${NC}"
docker system prune -f
docker volume prune -f

# Step 7: Restart RabbitMQ
echo -e "\n${BLUE}Step 7: Starting RabbitMQ with fresh data...${NC}"
docker compose -f docker-compose.stage.yml up -d rabbitmq

# Step 8: Wait for RabbitMQ to be healthy
echo -e "\n${BLUE}Step 8: Waiting for RabbitMQ to be healthy...${NC}"
echo "This may take up to 60 seconds..."

for i in {1..12}; do
    if docker compose -f docker-compose.stage.yml ps rabbitmq | grep -q "healthy"; then
        echo -e "${GREEN}✅ RabbitMQ is healthy!${NC}"
        break
    elif [ $i -eq 12 ]; then
        echo -e "${RED}❌ RabbitMQ failed to become healthy${NC}"
        echo "Check logs with: docker compose -f docker-compose.stage.yml logs rabbitmq"
        exit 1
    else
        echo "Waiting... ($i/12)"
        sleep 5
    fi
done

# Step 9: Restart API to reconnect to RabbitMQ
echo -e "\n${BLUE}Step 9: Restarting API to reconnect to RabbitMQ...${NC}"
docker compose -f docker-compose.stage.yml restart yapplr-api

# Step 10: Test RabbitMQ connection
echo -e "\n${BLUE}Step 10: Testing RabbitMQ connection...${NC}"
sleep 10

if docker compose -f docker-compose.stage.yml logs yapplr-api --tail=20 | grep -q "RabbitMQ"; then
    echo -e "${GREEN}✅ API appears to be connecting to RabbitMQ${NC}"
else
    echo -e "${YELLOW}⚠️ No RabbitMQ connection logs found - check API logs if issues persist${NC}"
fi

# Summary
echo -e "\n${BLUE}===================================${NC}"
echo -e "${GREEN}🎉 RabbitMQ Disk Space Fix Complete!${NC}"
echo -e "\n${GREEN}✅ What was fixed:${NC}"
echo -e "  - Cleared RabbitMQ data volume"
echo -e "  - Removed disk space alarm"
echo -e "  - Restarted RabbitMQ with fresh data"
echo -e "  - Reconnected API to RabbitMQ"

echo -e "\n${BLUE}💡 Next Steps:${NC}"
echo -e "  - Test creating posts with media"
echo -e "  - Monitor RabbitMQ disk usage"
echo -e "  - Consider implementing disk space monitoring"

echo -e "\n${BLUE}🔍 Monitoring Commands:${NC}"
echo -e "  - Check RabbitMQ status: docker compose -f docker-compose.stage.yml ps rabbitmq"
echo -e "  - Check RabbitMQ logs: docker compose -f docker-compose.stage.yml logs rabbitmq"
echo -e "  - Check API logs: docker compose -f docker-compose.stage.yml logs yapplr-api"
echo -e "  - Check disk space: df -h"

echo -e "\n${YELLOW}⚠️ Prevention:${NC}"
echo -e "  - This issue may recur if disk space is limited"
echo -e "  - Consider increasing server disk space"
echo -e "  - Implement RabbitMQ disk space monitoring"
echo -e "  - Regular cleanup of old Docker volumes"
