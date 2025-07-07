#!/bin/bash

# Yapplr Unified Deployment Script for Linode
# This script builds and deploys both API and Frontend services together
# Single nginx handles all routing for both services

set -e  # Exit on any error

# Configuration
IMAGE_NAME="yapplr-api"
TAG="latest"
REGISTRY_URL=""  # Set this if using a container registry

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 Starting Yapplr API Deployment${NC}"

# Check if .env file exists
if [ ! -f .env ]; then
    echo -e "${RED}❌ Error: .env file not found${NC}"
    echo -e "${YELLOW}Please copy .env.example to .env and configure your settings${NC}"
    exit 1
fi

# Load environment variables (with proper handling of quoted values)
set -a  # automatically export all variables
source .env
set +a  # turn off automatic export

# Validate required environment variables
required_vars=("PROD_DATABASE_CONNECTION_STRING" "PROD_JWT_SECRET_KEY" "PROD_API_DOMAIN_NAME" "PROD_FIREBASE_PROJECT_ID" "PROD_FIREBASE_SERVICE_ACCOUNT_KEY")
for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo -e "${RED}❌ Error: $var is not set in .env file${NC}"
        exit 1
    fi
done

# Note: Frontend now uses SignalR-only for notifications
# Firebase is only used in the API for mobile push notifications

echo -e "${GREEN}✅ Environment variables validated${NC}"

# Note: Docker images will be built by docker compose with --build flag

# Stop existing containers
echo -e "${GREEN}🛑 Stopping existing containers...${NC}"
docker compose -f docker-compose.prod.yml down --volumes --remove-orphans || true

# Force remove specific containers that might be stuck
echo -e "${GREEN}🧹 Force removing any stuck containers...${NC}"
docker rm -f yapplrapi_certbot_1 || true
docker rm -f yapplrapi_yapplr-api_1 || true
docker rm -f yapplrapi_nginx_1 || true
docker rm -f yapplrapi_yapplr-frontend_1 || true

# Stop and remove all containers with yapplr in the name
docker ps -a --filter "name=yapplr" --format "{{.Names}}" | xargs -r docker stop || true
docker ps -a --filter "name=yapplr" --format "{{.Names}}" | xargs -r docker rm -f || true

# Additional cleanup to ensure ports are free
echo -e "${GREEN}🧹 Cleaning up any remaining containers...${NC}"
docker container prune -f || true

# Clean up networks that might be left behind
echo -e "${GREEN}🌐 Cleaning up networks...${NC}"
docker network rm yapplrapi_yapplr-network || true
docker network rm yapplr-network || true

# Remove old images to force complete rebuild
echo -e "${GREEN}🗑️ Removing old Docker images...${NC}"
docker image rm yapplr-api:latest || true
docker image rm yapplr-frontend:latest || true
docker image prune -f || true

# Set cache bust variable to force frontend rebuild
export CACHE_BUST=$(date +%s)
echo -e "${GREEN}🔄 Cache bust value: $CACHE_BUST${NC}"

# Start new containers (both API and frontend) with forced rebuild
echo -e "${GREEN}🚀 Starting all services (API + Frontend + nginx)...${NC}"
docker compose -f docker-compose.prod.yml up -d --build --force-recreate

# Wait for services to be ready
echo -e "${GREEN}⏳ Waiting for services to be ready...${NC}"
sleep 30

# Check if API is responding through nginx
echo -e "${GREEN}🔍 Checking API health...${NC}"
if curl -f http://localhost/health > /dev/null 2>&1; then
    echo -e "${GREEN}✅ API is healthy and responding${NC}"
else
    echo -e "${RED}❌ API health check failed${NC}"
    echo -e "${YELLOW}Checking logs...${NC}"
    docker compose -f docker-compose.prod.yml logs yapplr-api
    exit 1
fi

# Database migrations will run automatically at application startup
echo -e "${GREEN}🗄️ Database migrations will run automatically when the API starts${NC}"

echo -e "${GREEN}🎉 Deployment completed successfully!${NC}"
echo -e "${GREEN}Your application is now running at:${NC}"
echo -e "${GREEN}  Frontend: https://yapplr.com${NC}"
echo -e "${GREEN}  API: https://$PROD_API_DOMAIN_NAME${NC}"

# Clean up old images
echo -e "${GREEN}🧹 Cleaning up old Docker images...${NC}"
docker image prune -f

echo -e "${GREEN}✅ Deployment script finished${NC}"
