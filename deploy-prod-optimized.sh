#!/bin/bash

# Optimized Yapplr Production Deployment Script
# Only rebuilds containers when source code has changed
# PRESERVES ALL DATA - never clears production volumes

set -e  # Exit on any error

# Configuration
HASH_FILE=".deployment_hashes_prod"
FORCE_REBUILD=${FORCE_REBUILD:-false}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 Starting Optimized Yapplr Production Deployment${NC}"

# Check if .env file exists
if [ ! -f .env ]; then
    echo -e "${RED}❌ Error: .env file not found${NC}"
    echo -e "${YELLOW}Please copy .env.example to .env and configure your settings${NC}"
    exit 1
fi

# Load environment variables (with proper handling of quoted values)
set -a  # automatically export all variables
if [ -f .env ]; then
    # Use source instead of xargs to properly handle complex connection strings
    source .env
fi
set +a  # turn off automatic export

# Validate required environment variables
required_vars=("PROD_DATABASE_CONNECTION_STRING" "PROD_JWT_SECRET_KEY" "PROD_API_DOMAIN_NAME" "PROD_FIREBASE_PROJECT_ID" "PROD_FIREBASE_SERVICE_ACCOUNT_KEY")
for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo -e "${RED}❌ Error: $var is not set in .env file${NC}"
        exit 1
    fi
done

echo -e "${GREEN}✅ Environment variables validated${NC}"

# Create required storage directories
echo -e "${GREEN}📁 Ensuring production storage directories exist...${NC}"
sudo mkdir -p /mnt/yapplr-prod-storage/{uploads,logs,rabbitmq,redis,seq,influxdb,influxdb-config,prometheus,grafana}
sudo chown -R $USER:$USER /mnt/yapplr-prod-storage/
echo -e "${GREEN}✅ Storage directories ready${NC}"

# Function to calculate directory hash
calculate_hash() {
    local dir=$1
    if [ -d "$dir" ]; then
        find "$dir" -type f \( -name "*.cs" -o -name "*.csproj" -o -name "*.json" -o -name "*.js" -o -name "*.ts" -o -name "*.tsx" -o -name "*.py" -o -name "Dockerfile*" \) -exec md5sum {} \; | sort | md5sum | cut -d' ' -f1
    else
        echo "missing"
    fi
}

# Function to check if rebuild is needed
needs_rebuild() {
    local service=$1
    local current_hash=$2
    local stored_hash=""
    
    if [ -f "$HASH_FILE" ] && [ "$FORCE_REBUILD" != "true" ]; then
        stored_hash=$(grep "^${service}:" "$HASH_FILE" 2>/dev/null | cut -d':' -f2 || echo "")
    fi
    
    if [ "$current_hash" != "$stored_hash" ] || [ "$FORCE_REBUILD" = "true" ]; then
        echo "true"
    else
        echo "false"
    fi
}

# Function to update hash
update_hash() {
    local service=$1
    local hash=$2
    
    # Remove old entry and add new one
    if [ -f "$HASH_FILE" ]; then
        grep -v "^${service}:" "$HASH_FILE" > "${HASH_FILE}.tmp" || true
        mv "${HASH_FILE}.tmp" "$HASH_FILE"
    fi
    echo "${service}:${hash}" >> "$HASH_FILE"
}

# Calculate hashes for each service
echo -e "${BLUE}🔍 Checking for changes in source code...${NC}"

api_hash=$(calculate_hash "Yapplr.Api")
video_processor_hash=$(calculate_hash "Yapplr.VideoProcessor")
frontend_hash=$(calculate_hash "yapplr-frontend")
content_moderation_hash=$(calculate_hash "sentiment-analysis")

# Check what needs rebuilding
api_rebuild=$(needs_rebuild "yapplr-api" "$api_hash")
video_processor_rebuild=$(needs_rebuild "yapplr-video-processor" "$video_processor_hash")
frontend_rebuild=$(needs_rebuild "yapplr-frontend" "$frontend_hash")
content_moderation_rebuild=$(needs_rebuild "content-moderation" "$content_moderation_hash")

echo -e "${BLUE}📊 Rebuild Status:${NC}"
echo -e "  API: $([ "$api_rebuild" = "true" ] && echo -e "${YELLOW}REBUILD${NC}" || echo -e "${GREEN}SKIP${NC}")"
echo -e "  Video Processor: $([ "$video_processor_rebuild" = "true" ] && echo -e "${YELLOW}REBUILD${NC}" || echo -e "${GREEN}SKIP${NC}")"
echo -e "  Frontend: $([ "$frontend_rebuild" = "true" ] && echo -e "${YELLOW}REBUILD${NC}" || echo -e "${GREEN}SKIP${NC}")"
echo -e "  Content Moderation: $([ "$content_moderation_rebuild" = "true" ] && echo -e "${YELLOW}REBUILD${NC}" || echo -e "${GREEN}SKIP${NC}")"

# Set version tags based on rebuild status
export YAPPLR_API_VERSION=$([ "$api_rebuild" = "true" ] && echo "$(date +%Y%m%d-%H%M%S)" || echo "latest")
export YAPPLR_VIDEO_PROCESSOR_VERSION=$([ "$video_processor_rebuild" = "true" ] && echo "$(date +%Y%m%d-%H%M%S)" || echo "latest")
export YAPPLR_FRONTEND_VERSION=$([ "$frontend_rebuild" = "true" ] && echo "$(date +%Y%m%d-%H%M%S)" || echo "latest")
export CONTENT_MODERATION_VERSION=$([ "$content_moderation_rebuild" = "true" ] && echo "$(date +%Y%m%d-%H%M%S)" || echo "latest")

# Stop existing containers (but preserve all volumes)
echo -e "${GREEN}🛑 Stopping existing containers (preserving all data)...${NC}"
docker compose -f docker-compose.prod.yml down --remove-orphans || true

# Clean up any stuck containers
echo -e "${GREEN}🧹 Cleaning up any stuck containers...${NC}"
docker ps -a --filter "name=yapplr" --format "{{.Names}}" | xargs -r docker stop || true
docker ps -a --filter "name=yapplr" --format "{{.Names}}" | xargs -r docker rm -f || true
docker container prune -f || true

# Clean up networks that might be left behind
echo -e "${GREEN}🌐 Cleaning up networks...${NC}"
docker network rm yapplrapi_yapplr-network || true
docker network rm yapplr-network || true

echo -e "${GREEN}🔄 Cache bust value: $(date +%s)${NC}"
export CACHE_BUST=$(date +%s)

# Build services that need rebuilding
services_to_build=""
if [ "$api_rebuild" = "true" ]; then
    services_to_build="$services_to_build yapplr-api"
fi
if [ "$video_processor_rebuild" = "true" ]; then
    services_to_build="$services_to_build yapplr-video-processor"
fi
if [ "$frontend_rebuild" = "true" ]; then
    services_to_build="$services_to_build yapplr-frontend"
fi
if [ "$content_moderation_rebuild" = "true" ]; then
    services_to_build="$services_to_build content-moderation"
fi

if [ -n "$services_to_build" ]; then
    echo -e "${GREEN}🔨 Building changed services:$services_to_build${NC}"
    docker compose -f docker-compose.prod.yml build $services_to_build
else
    echo -e "${GREEN}⚡ No services need rebuilding - using existing images${NC}"
fi

# Start all services
echo -e "${GREEN}🚀 Starting all services...${NC}"
docker compose -f docker-compose.prod.yml up -d

# Update hashes for rebuilt services
if [ "$api_rebuild" = "true" ]; then
    update_hash "yapplr-api" "$api_hash"
fi
if [ "$video_processor_rebuild" = "true" ]; then
    update_hash "yapplr-video-processor" "$video_processor_hash"
fi
if [ "$frontend_rebuild" = "true" ]; then
    update_hash "yapplr-frontend" "$frontend_hash"
fi
if [ "$content_moderation_rebuild" = "true" ]; then
    update_hash "content-moderation" "$content_moderation_hash"
fi

# Wait for services to be ready
echo -e "${GREEN}⏳ Waiting for services to be ready...${NC}"
sleep 30

# Health checks
echo -e "${GREEN}🔍 Checking service health...${NC}"
docker compose -f docker-compose.prod.yml ps

# Check API health
if curl -f http://localhost/health > /dev/null 2>&1; then
    echo -e "${GREEN}✅ API is healthy and responding${NC}"
else
    echo -e "${RED}❌ API health check failed${NC}"
    docker compose -f docker-compose.prod.yml logs yapplr-api
    exit 1
fi

echo -e "${GREEN}🎉 Optimized production deployment completed successfully!${NC}"
echo -e "${GREEN}📈 Performance Summary:${NC}"
total_services=4
rebuilt_services=$(echo "$services_to_build" | wc -w)
skipped_services=$((total_services - rebuilt_services))
echo -e "  Services rebuilt: ${YELLOW}$rebuilt_services${NC}"
echo -e "  Services skipped: ${GREEN}$skipped_services${NC}"
echo -e "  Time saved: ~$(($skipped_services * 3)) minutes${NC}"
echo -e "  ${GREEN}🛡️ ALL DATA PRESERVED${NC}"

echo -e "${GREEN}✅ Production deployment script finished${NC}"
