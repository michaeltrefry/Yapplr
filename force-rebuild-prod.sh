#!/bin/bash

# Force rebuild specific production services or all services
# Usage: ./force-rebuild-prod.sh [service1] [service2] ... or ./force-rebuild-prod.sh all

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

HASH_FILE=".deployment_hashes_prod"
VALID_SERVICES=("yapplr-api" "yapplr-video-processor" "yapplr-frontend" "content-moderation")

echo -e "${GREEN}🔨 Production Force Rebuild Utility${NC}"
echo -e "${BLUE}🛡️ Note: This only affects container rebuilding - ALL DATA IS PRESERVED${NC}"

if [ $# -eq 0 ]; then
    echo -e "${YELLOW}Usage: $0 [service1] [service2] ... or $0 all${NC}"
    echo -e "${BLUE}Valid services:${NC}"
    for service in "${VALID_SERVICES[@]}"; do
        echo -e "  - $service"
    done
    echo -e "  - all (rebuild everything)"
    exit 1
fi

# Function to remove hash for a service
remove_service_hash() {
    local service=$1
    if [ -f "$HASH_FILE" ]; then
        echo -e "${YELLOW}🗑️ Removing hash for $service${NC}"
        grep -v "^${service}:" "$HASH_FILE" > "${HASH_FILE}.tmp" || true
        mv "${HASH_FILE}.tmp" "$HASH_FILE"
    fi
}

# Function to validate service name
is_valid_service() {
    local service=$1
    for valid in "${VALID_SERVICES[@]}"; do
        if [ "$service" = "$valid" ]; then
            return 0
        fi
    done
    return 1
}

# Process arguments
if [ "$1" = "all" ]; then
    echo -e "${YELLOW}🔥 Forcing rebuild of ALL production services${NC}"
    for service in "${VALID_SERVICES[@]}"; do
        remove_service_hash "$service"
    done
    echo -e "${GREEN}✅ All service hashes cleared${NC}"
else
    # Process individual services
    for service in "$@"; do
        if is_valid_service "$service"; then
            remove_service_hash "$service"
        else
            echo -e "${RED}❌ Invalid service: $service${NC}"
            echo -e "${BLUE}Valid services: ${VALID_SERVICES[*]}${NC}"
            exit 1
        fi
    done
    echo -e "${GREEN}✅ Selected service hashes cleared${NC}"
fi

echo -e "${BLUE}💡 Next production deployment will rebuild the selected services${NC}"
echo -e "${BLUE}Run ./deploy-prod-optimized.sh to deploy with forced rebuilds${NC}"
echo -e "${GREEN}🛡️ Remember: ALL PRODUCTION DATA IS ALWAYS PRESERVED${NC}"
