#!/bin/bash

# Yapplr API Deployment Script for Linode
# This script builds and deploys the API service using docker-compose.prod.yml
# Frontend can be deployed independently using the same compose file

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

# Note: Frontend Firebase variables are not needed for API-only deployment
# Frontend deployment is handled separately with its own configuration

echo -e "${GREEN}✅ Environment variables validated${NC}"

# Build Docker image
echo -e "${GREEN}🔨 Building Docker image...${NC}"
docker build -t $IMAGE_NAME:$TAG .

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Docker image built successfully${NC}"
else
    echo -e "${RED}❌ Docker build failed${NC}"
    exit 1
fi

# Stop and rebuild only the API service
echo -e "${GREEN}🛑 Stopping API service...${NC}"
docker-compose -f docker-compose.prod.yml stop yapplr-api || true

# Remove old API container and image
echo -e "${GREEN}🗑️ Removing old API container and image...${NC}"
docker-compose -f docker-compose.prod.yml rm -f yapplr-api || true
docker image rm yapplrapi_yapplr-api:latest || true

# Build and start API service
echo -e "${GREEN}🔨 Building new API image...${NC}"
docker-compose -f docker-compose.prod.yml build yapplr-api

echo -e "${GREEN}🚀 Starting API service...${NC}"
docker-compose -f docker-compose.prod.yml up -d yapplr-api

# Ensure nginx is running (it may already be running from frontend deployment)
echo -e "${GREEN}🌐 Ensuring nginx is running...${NC}"
docker-compose -f docker-compose.prod.yml up -d nginx

# Wait for services to be ready
echo -e "${GREEN}⏳ Waiting for services to be ready...${NC}"
sleep 30

# Check if API is responding
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
echo -e "${GREEN}Your API is now running at: https://$API_DOMAIN_NAME${NC}"

# Clean up old images
echo -e "${GREEN}🧹 Cleaning up old Docker images...${NC}"
docker image prune -f

echo -e "${GREEN}✅ Deployment script finished${NC}"
