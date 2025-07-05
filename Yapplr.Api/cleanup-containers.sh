#!/bin/bash

# Yapplr Container Cleanup Script
# This script forcefully removes all Yapplr-related containers and images

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🧹 Starting Yapplr container cleanup...${NC}"

# Stop and remove all containers with yapplr in the name
echo -e "${GREEN}🛑 Stopping all Yapplr containers...${NC}"
docker ps -a --filter "name=yapplr" --format "{{.Names}}" | xargs -r docker stop || true
docker ps -a --filter "name=yapplr" --format "{{.Names}}" | xargs -r docker rm -f || true

# Also stop and remove the specific containers mentioned in the error
echo -e "${GREEN}🛑 Removing specific conflicting containers...${NC}"
docker rm -f yapplrapi_certbot_1 || true
docker rm -f yapplrapi_yapplr-api_1 || true
docker rm -f yapplrapi_nginx_1 || true
docker rm -f yapplrapi_yapplr-frontend_1 || true

# Remove containers by ID if they exist
docker rm -f 495ce9beb368af04a23e03f8d3e1899200b65d9b2de8b6d768c78c18706772d6 || true
docker rm -f db366e2aa7ce61d25ff342624c213d25bf410f416e8a506831ab0fc8871248f5 || true
docker rm -f 6468565bbcc1ab6a9a046d31c6d9b49c0a0ad7070e0f903fda235a9fd87b8aff || true

# Stop any docker-compose services in this directory
echo -e "${GREEN}🛑 Stopping docker-compose services...${NC}"
docker-compose -f docker-compose.prod.yml down --volumes --remove-orphans || true
docker-compose -f docker-compose.yml down --volumes --remove-orphans || true

# Remove Yapplr-related images
echo -e "${GREEN}🗑️ Removing Yapplr images...${NC}"
docker image rm yapplr-api:latest || true
docker image rm yapplr-frontend:latest || true
docker image rm yapplrapi_yapplr-api:latest || true
docker image rm yapplrapi_yapplr-frontend:latest || true

# Clean up dangling images and containers
echo -e "${GREEN}🧹 Cleaning up dangling resources...${NC}"
docker container prune -f || true
docker image prune -f || true

# Remove any networks that might be left
echo -e "${GREEN}🌐 Cleaning up networks...${NC}"
docker network rm yapplrapi_yapplr-network || true
docker network rm yapplr-network || true

echo -e "${GREEN}✅ Cleanup completed!${NC}"
echo -e "${GREEN}You can now run the deployment script again.${NC}"
