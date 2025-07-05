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
required_vars=("DATABASE_CONNECTION_STRING" "JWT_SECRET_KEY" "API_DOMAIN_NAME" "FIREBASE_PROJECT_ID" "FIREBASE_SERVICE_ACCOUNT_KEY")
for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo -e "${RED}❌ Error: $var is not set in .env file${NC}"
        exit 1
    fi
done

# Note: Frontend now uses SignalR-only for notifications
# Firebase is only used in the API for mobile push notifications

echo -e "${GREEN}✅ Environment variables validated${NC}"

# Note: Docker images will be built by docker-compose with --build flag

# Stop existing containers
echo -e "${GREEN}🛑 Stopping existing containers...${NC}"
docker-compose -f docker-compose.prod.yml down --volumes --remove-orphans || true

# Additional cleanup to ensure ports are free
echo -e "${GREEN}🧹 Cleaning up any remaining containers...${NC}"
docker container prune -f || true

# Remove old images to force complete rebuild
echo -e "${GREEN}🗑️ Removing old Docker images...${NC}"
docker image rm yapplr-api:latest || true
docker image rm yapplr-frontend:latest || true
# Also remove docker-compose generated image names
docker image rm yapplr-api_yapplr-api:latest || true
docker image rm yapplr-api_yapplr-frontend:latest || true
docker image prune -f || true

# Set cache bust variable to force frontend rebuild
export CACHE_BUST=$(date +%s)
echo -e "${GREEN}🔄 Cache bust value: $CACHE_BUST${NC}"

# Start new containers (both API and frontend) with forced rebuild
echo -e "${GREEN}🚀 Starting all services (API + Frontend + nginx)...${NC}"
docker-compose -f docker-compose.prod.yml up -d --build --force-recreate

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
    docker-compose -f docker-compose.prod.yml logs yapplr-api
    exit 1
fi

# Run database migrations using SDK container
echo -e "${GREEN}🗄️ Running database migrations...${NC}"
echo -e "${YELLOW}Using connection string: ${DATABASE_CONNECTION_STRING}${NC}"
docker run --rm \
  --network yapplrapi_yapplr-network \
  -v $(pwd):/app \
  -w /app \
  -e "ConnectionStrings__DefaultConnection=${DATABASE_CONNECTION_STRING}" \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  sh -c "dotnet tool install --global dotnet-ef && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef database update" || true

echo -e "${GREEN}🎉 Deployment completed successfully!${NC}"
echo -e "${GREEN}Your application is now running at:${NC}"
echo -e "${GREEN}  Frontend: https://yapplr.com${NC}"
echo -e "${GREEN}  API: https://$API_DOMAIN_NAME${NC}"

# Clean up old images
echo -e "${GREEN}🧹 Cleaning up old Docker images...${NC}"
docker image prune -f

echo -e "${GREEN}✅ Deployment script finished${NC}"
