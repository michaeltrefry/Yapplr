#!/bin/bash

# Check deployment status and what would be rebuilt
# Shows current hashes vs stored hashes for each service

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

HASH_FILE=".deployment_hashes"

echo -e "${GREEN}📊 Deployment Status Check${NC}"
echo -e "${BLUE}================================${NC}"

# Function to calculate directory hash
calculate_hash() {
    local dir=$1
    if [ -d "$dir" ]; then
        find "$dir" -type f \( -name "*.cs" -o -name "*.csproj" -o -name "*.json" -o -name "*.js" -o -name "*.ts" -o -name "*.tsx" -o -name "*.py" -o -name "Dockerfile*" \) -exec md5sum {} \; | sort | md5sum | cut -d' ' -f1
    else
        echo "missing"
    fi
}

# Function to get stored hash
get_stored_hash() {
    local service=$1
    if [ -f "$HASH_FILE" ]; then
        grep "^${service}:" "$HASH_FILE" 2>/dev/null | cut -d':' -f2 || echo "none"
    else
        echo "none"
    fi
}

# Function to check service status
check_service() {
    local service=$1
    local dir=$2
    local current_hash=$(calculate_hash "$dir")
    local stored_hash=$(get_stored_hash "$service")
    
    echo -e "${BLUE}Service: $service${NC}"
    echo -e "  Directory: $dir"
    echo -e "  Current hash: ${current_hash:0:12}..."
    echo -e "  Stored hash:  ${stored_hash:0:12}..."
    
    if [ "$current_hash" = "$stored_hash" ] && [ "$stored_hash" != "none" ]; then
        echo -e "  Status: ${GREEN}✅ UP TO DATE${NC}"
        return 0
    else
        echo -e "  Status: ${YELLOW}🔄 NEEDS REBUILD${NC}"
        return 1
    fi
}

# Check if hash file exists
if [ ! -f "$HASH_FILE" ]; then
    echo -e "${YELLOW}⚠️ No deployment hash file found - all services will be rebuilt${NC}"
    echo ""
fi

# Check each service
total_services=0
needs_rebuild=0

echo -e "${BLUE}Checking API Service...${NC}"
if ! check_service "yapplr-api" "Yapplr.Api"; then
    ((needs_rebuild++))
fi
((total_services++))
echo ""

echo -e "${BLUE}Checking Video Processor Service...${NC}"
if ! check_service "yapplr-video-processor" "Yapplr.VideoProcessor"; then
    ((needs_rebuild++))
fi
((total_services++))
echo ""

echo -e "${BLUE}Checking Frontend Service...${NC}"
if ! check_service "yapplr-frontend" "yapplr-frontend"; then
    ((needs_rebuild++))
fi
((total_services++))
echo ""

echo -e "${BLUE}Checking Content Moderation Service...${NC}"
if ! check_service "content-moderation" "sentiment-analysis"; then
    ((needs_rebuild++))
fi
((total_services++))
echo ""

# Summary
up_to_date=$((total_services - needs_rebuild))
echo -e "${BLUE}================================${NC}"
echo -e "${GREEN}📈 Summary${NC}"
echo -e "  Total services: $total_services"
echo -e "  Up to date: ${GREEN}$up_to_date${NC}"
echo -e "  Need rebuild: ${YELLOW}$needs_rebuild${NC}"

if [ $needs_rebuild -eq 0 ]; then
    echo -e "  ${GREEN}⚡ Next deployment will be FAST (no rebuilds needed)${NC}"
    echo -e "  ${GREEN}⏱️ Estimated time: ~2-3 minutes${NC}"
else
    echo -e "  ${YELLOW}🔨 Next deployment will rebuild $needs_rebuild service(s)${NC}"
    echo -e "  ${YELLOW}⏱️ Estimated time: ~$((2 + needs_rebuild * 3)) minutes${NC}"
fi

echo ""
echo -e "${BLUE}💡 Tips:${NC}"
echo -e "  - Run ${GREEN}./deploy-stage-optimized.sh${NC} to deploy with optimizations"
echo -e "  - Run ${GREEN}./force-rebuild.sh [service]${NC} to force rebuild specific services"
echo -e "  - Run ${GREEN}./force-rebuild.sh all${NC} to force rebuild everything"

# Check running containers
echo ""
echo -e "${BLUE}🐳 Current Running Containers:${NC}"
if command -v docker &> /dev/null; then
    if docker compose -f docker-compose.stage.yml ps 2>/dev/null | grep -q "Up"; then
        docker compose -f docker-compose.stage.yml ps
    else
        echo -e "${YELLOW}  No containers currently running${NC}"
    fi
else
    echo -e "${RED}  Docker not available${NC}"
fi
