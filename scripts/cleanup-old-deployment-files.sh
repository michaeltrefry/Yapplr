#!/bin/bash

# Cleanup script to remove old deployment files from Yapplr.Api directory
# Run this after confirming the new deployment system works

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}🧹 Yapplr Deployment Cleanup Script${NC}"
echo "======================================"
echo ""
echo "This script will remove old deployment files from Yapplr.Api/"
echo "Make sure the new deployment system is working before running this!"
echo ""

# List files that will be removed
echo -e "${YELLOW}Files that will be removed:${NC}"
echo "- Yapplr.Api/deploy.sh"
echo "- Yapplr.Api/backup.sh"
echo "- Yapplr.Api/cleanup-containers.sh"
echo "- Yapplr.Api/local-test.sh"
echo "- Yapplr.Api/monitor.sh"
echo "- Yapplr.Api/setup-db.sh"
echo "- Yapplr.Api/docker-compose.prod.yml"
echo "- Yapplr.Api/nginx.conf"
echo ""

# Ask for confirmation
read -p "Are you sure you want to remove these files? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Cleanup cancelled.${NC}"
    exit 0
fi

echo -e "${GREEN}🗑️ Removing old deployment files...${NC}"

# Remove old deployment scripts
rm -f Yapplr.Api/deploy.sh
rm -f Yapplr.Api/backup.sh
rm -f Yapplr.Api/cleanup-containers.sh
rm -f Yapplr.Api/local-test.sh
rm -f Yapplr.Api/monitor.sh
rm -f Yapplr.Api/setup-db.sh

# Remove old docker compose file
rm -f Yapplr.Api/docker-compose.prod.yml

# Remove old nginx config (now in root nginx/ directory)
rm -f Yapplr.Api/nginx.conf

echo -e "${GREEN}✅ Cleanup completed!${NC}"
echo ""
echo -e "${GREEN}Current deployment structure:${NC}"
echo "- 📁 scripts/deploy-production.sh (main deployment script)"
echo "- 📁 scripts/backup.sh (backup script)"
echo "- 📁 scripts/health-check.sh (health monitoring)"
echo "- 📄 docker-compose.production.yml (production services)"
echo "- 📄 .env.production (environment configuration)"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Use './scripts/deploy-production.sh deploy' for deployments"
echo "2. Update any documentation that references old file paths"
echo "3. Update any CI/CD scripts if needed"
