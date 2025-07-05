#!/bin/bash

# Yapplr API-Only Deployment Script for Linode
# This script builds and deploys ONLY the Yapplr API to a Linode server
# Frontend deployment is handled separately by deploy-frontend.yml workflow

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

# Note: Firebase frontend variables are not needed for API-only deployment
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

# Stop existing API containers
echo -e "${GREEN}🛑 Stopping existing API containers...${NC}"
docker-compose -f docker-compose.api.yml down --volumes --remove-orphans || true

# Additional cleanup to ensure ports are free
echo -e "${GREEN}🧹 Cleaning up any remaining containers...${NC}"
docker container prune -f || true

# Start new API containers
echo -e "${GREEN}🚀 Starting new API containers...${NC}"
docker-compose -f docker-compose.api.yml up -d

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
    docker-compose -f docker-compose.api.yml logs yapplr-api
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
