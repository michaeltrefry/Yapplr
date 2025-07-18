#!/bin/bash

# Diagnose RabbitMQ disk space issues
# This script helps identify why RabbitMQ thinks it's out of disk space

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}🔍 RabbitMQ Disk Space Diagnostic${NC}"
echo -e "${BLUE}==================================${NC}"

# Check if we're in the right directory
if [ ! -f "docker-compose.stage.yml" ]; then
    echo -e "${RED}❌ Error: Must be run from the project root directory${NC}"
    exit 1
fi

# Check host disk space
echo -e "\n${BLUE}1. Host Machine Disk Space:${NC}"
df -h

# Check Docker disk usage
echo -e "\n${BLUE}2. Docker System Disk Usage:${NC}"
docker system df

# Check if RabbitMQ container is running
echo -e "\n${BLUE}3. RabbitMQ Container Status:${NC}"
if docker compose -f docker-compose.stage.yml ps rabbitmq | grep -q "Up"; then
    echo -e "${GREEN}✅ RabbitMQ container is running${NC}"
    
    # Check disk space inside RabbitMQ container
    echo -e "\n${BLUE}4. Disk Space Inside RabbitMQ Container:${NC}"
    docker compose -f docker-compose.stage.yml exec rabbitmq df -h || echo "Could not check container disk space"
    
    # Check RabbitMQ data directory
    echo -e "\n${BLUE}5. RabbitMQ Data Directory:${NC}"
    docker compose -f docker-compose.stage.yml exec rabbitmq ls -la /var/lib/rabbitmq/ || echo "Could not list RabbitMQ data directory"
    
    # Check RabbitMQ disk space status
    echo -e "\n${BLUE}6. RabbitMQ Disk Space Status:${NC}"
    docker compose -f docker-compose.stage.yml exec rabbitmq rabbitmq-diagnostics disk_usage || echo "Could not check RabbitMQ disk usage"
    
    # Check RabbitMQ alarms
    echo -e "\n${BLUE}7. RabbitMQ Active Alarms:${NC}"
    docker compose -f docker-compose.stage.yml exec rabbitmq rabbitmq-diagnostics alarms || echo "Could not check RabbitMQ alarms"
    
    # Check RabbitMQ memory usage
    echo -e "\n${BLUE}8. RabbitMQ Memory Usage:${NC}"
    docker compose -f docker-compose.stage.yml exec rabbitmq rabbitmq-diagnostics memory_usage || echo "Could not check RabbitMQ memory usage"
    
    # Check tmpfs mounts
    echo -e "\n${BLUE}9. tmpfs Mounts in Container:${NC}"
    docker compose -f docker-compose.stage.yml exec rabbitmq mount | grep tmpfs || echo "No tmpfs mounts found"
    
else
    echo -e "${RED}❌ RabbitMQ container is not running${NC}"
    
    # Check RabbitMQ logs
    echo -e "\n${BLUE}4. Recent RabbitMQ Logs:${NC}"
    docker compose -f docker-compose.stage.yml logs --tail=20 rabbitmq || echo "Could not get RabbitMQ logs"
fi

# Check Docker volumes
echo -e "\n${BLUE}10. Docker Volumes:${NC}"
docker volume ls | grep rabbitmq || echo "No RabbitMQ volumes found"

# Check volume disk usage
echo -e "\n${BLUE}11. RabbitMQ Volume Disk Usage:${NC}"
rabbitmq_volumes=$(docker volume ls -q | grep rabbitmq || echo "")
if [ -n "$rabbitmq_volumes" ]; then
    for volume in $rabbitmq_volumes; do
        echo "Volume: $volume"
        docker run --rm -v "$volume":/data alpine du -sh /data 2>/dev/null || echo "Could not check volume size"
    done
else
    echo "No RabbitMQ volumes found"
fi

# Check container resource limits
echo -e "\n${BLUE}12. Container Resource Limits:${NC}"
if docker compose -f docker-compose.stage.yml ps rabbitmq | grep -q "Up"; then
    container_id=$(docker compose -f docker-compose.stage.yml ps -q rabbitmq)
    if [ -n "$container_id" ]; then
        echo "Memory limit:"
        docker inspect "$container_id" | grep -A 5 "Memory" || echo "Could not check memory limits"
        echo "Disk usage:"
        docker exec "$container_id" df -h /var/lib/rabbitmq 2>/dev/null || echo "Could not check container disk usage"
    fi
fi

# Check system memory
echo -e "\n${BLUE}13. System Memory Usage:${NC}"
free -h

# Summary and recommendations
echo -e "\n${BLUE}==================================${NC}"
echo -e "${GREEN}🎯 Analysis Summary${NC}"
echo -e "\n${YELLOW}Common Causes of RabbitMQ Disk Alarms:${NC}"
echo -e "1. ${BLUE}tmpfs mount running out of RAM${NC} (most likely with 35GB free)"
echo -e "2. ${BLUE}Docker volume on different partition${NC}"
echo -e "3. ${BLUE}Container memory limits${NC}"
echo -e "4. ${BLUE}RabbitMQ checking wrong filesystem${NC}"

echo -e "\n${YELLOW}Solutions to Try:${NC}"
echo -e "1. ${GREEN}Remove tmpfs mount${NC} (already done in latest config)"
echo -e "2. ${GREEN}Restart RabbitMQ${NC} with new configuration"
echo -e "3. ${GREEN}Clear RabbitMQ data${NC} if issue persists"
echo -e "4. ${GREEN}Check Docker daemon storage driver${NC}"

echo -e "\n${BLUE}Next Steps:${NC}"
echo -e "- Deploy the updated configuration (tmpfs removed)"
echo -e "- Restart RabbitMQ: docker compose -f docker-compose.stage.yml restart rabbitmq"
echo -e "- Monitor for disk alarms: docker compose -f docker-compose.stage.yml logs rabbitmq -f"
