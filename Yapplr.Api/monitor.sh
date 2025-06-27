#!/bin/bash

# Yapplr API Monitoring Script
# This script monitors the health and performance of the Yapplr API

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🔍 Yapplr API Health Monitor${NC}"
echo "=================================="

# Check if containers are running
echo -e "${BLUE}📦 Container Status:${NC}"
docker-compose -f docker-compose.prod.yml ps

echo ""

# Check API health endpoint
echo -e "${BLUE}🏥 API Health Check:${NC}"
if curl -f -s http://localhost/health > /dev/null 2>&1; then
    HEALTH_RESPONSE=$(curl -s http://localhost/health)
    echo -e "${GREEN}✅ API is healthy${NC}"
    echo "Response: $HEALTH_RESPONSE"
else
    echo -e "${RED}❌ API health check failed${NC}"
fi

echo ""

# Check disk usage
echo -e "${BLUE}💾 Disk Usage:${NC}"
df -h | grep -E "(Filesystem|/dev/)"

echo ""

# Check memory usage
echo -e "${BLUE}🧠 Memory Usage:${NC}"
free -h

echo ""

# Check Docker container resource usage
echo -e "${BLUE}🐳 Container Resource Usage:${NC}"
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}\t{{.BlockIO}}"

echo ""

# Check recent logs for errors
echo -e "${BLUE}📋 Recent Error Logs (last 10):${NC}"
docker-compose -f docker-compose.prod.yml logs --tail=50 yapplr-api | grep -i error | tail -10 || echo "No recent errors found"

echo ""

# Check SSL certificate expiry
echo -e "${BLUE}🔒 SSL Certificate Status:${NC}"
if [ -f "/etc/letsencrypt/live/*/cert.pem" ]; then
    CERT_FILE=$(find /etc/letsencrypt/live -name "cert.pem" | head -1)
    EXPIRY_DATE=$(openssl x509 -enddate -noout -in "$CERT_FILE" | cut -d= -f2)
    EXPIRY_TIMESTAMP=$(date -d "$EXPIRY_DATE" +%s)
    CURRENT_TIMESTAMP=$(date +%s)
    DAYS_UNTIL_EXPIRY=$(( (EXPIRY_TIMESTAMP - CURRENT_TIMESTAMP) / 86400 ))
    
    if [ $DAYS_UNTIL_EXPIRY -gt 30 ]; then
        echo -e "${GREEN}✅ SSL certificate expires in $DAYS_UNTIL_EXPIRY days${NC}"
    elif [ $DAYS_UNTIL_EXPIRY -gt 7 ]; then
        echo -e "${YELLOW}⚠️ SSL certificate expires in $DAYS_UNTIL_EXPIRY days${NC}"
    else
        echo -e "${RED}❌ SSL certificate expires in $DAYS_UNTIL_EXPIRY days - URGENT RENEWAL NEEDED${NC}"
    fi
else
    echo -e "${YELLOW}⚠️ SSL certificate not found${NC}"
fi

echo ""

# Check database connectivity (if using external database)
echo -e "${BLUE}🗄️ Database Connectivity:${NC}"
if docker-compose -f docker-compose.prod.yml exec -T yapplr-api dotnet ef database update --dry-run > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Database connection successful${NC}"
else
    echo -e "${RED}❌ Database connection failed${NC}"
fi

echo ""

# Check backup status
echo -e "${BLUE}💾 Backup Status:${NC}"
if [ -d "/opt/backups/yapplr" ]; then
    LATEST_BACKUP=$(ls -t /opt/backups/yapplr/ | head -1)
    if [ ! -z "$LATEST_BACKUP" ]; then
        BACKUP_DATE=$(stat -c %y "/opt/backups/yapplr/$LATEST_BACKUP" | cut -d' ' -f1)
        echo -e "${GREEN}✅ Latest backup: $LATEST_BACKUP ($BACKUP_DATE)${NC}"
    else
        echo -e "${YELLOW}⚠️ No backups found${NC}"
    fi
else
    echo -e "${YELLOW}⚠️ Backup directory not found${NC}"
fi

echo ""
echo "=================================="
echo -e "${BLUE}📊 Monitor completed at $(date)${NC}"
