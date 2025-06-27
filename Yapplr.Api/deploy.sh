#!/bin/bash

# Yapplr API Deployment Script for Linode
# This script builds and deploys the Yapplr API to a Linode server

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

# Load environment variables
source .env

# Validate required environment variables
required_vars=("DATABASE_CONNECTION_STRING" "JWT_SECRET_KEY" "DOMAIN_NAME")
for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo -e "${RED}❌ Error: $var is not set in .env file${NC}"
        exit 1
    fi
done

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

# Stop existing containers
echo -e "${GREEN}🛑 Stopping existing containers...${NC}"
docker-compose -f docker-compose.prod.yml down || true

# Start new containers
echo -e "${GREEN}🚀 Starting new containers...${NC}"
docker-compose -f docker-compose.prod.yml up -d

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

# Run database migrations
echo -e "${GREEN}🗄️ Running database migrations...${NC}"
docker-compose -f docker-compose.prod.yml exec yapplr-api dotnet ef database update || true

echo -e "${GREEN}🎉 Deployment completed successfully!${NC}"
echo -e "${GREEN}Your API is now running at: https://$DOMAIN_NAME${NC}"

# Clean up old images
echo -e "${GREEN}🧹 Cleaning up old Docker images...${NC}"
docker image prune -f

echo -e "${GREEN}✅ Deployment script finished${NC}"
