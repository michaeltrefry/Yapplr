#!/bin/bash

# Yapplr API Backup Script
# This script creates backups of the database and uploaded files

set -e

# Configuration
BACKUP_DIR="/opt/backups/yapplr"
DATE=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=7

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}🔄 Starting Yapplr backup process...${NC}"

# Create backup directory
mkdir -p $BACKUP_DIR

# Load environment variables
if [ -f .env ]; then
    source .env
else
    echo -e "${YELLOW}⚠️ Warning: .env file not found${NC}"
fi

# Backup database (if using external database)
if [ ! -z "$PROD_DATABASE_CONNECTION_STRING" ]; then
    echo -e "${GREEN}📊 Backing up database...${NC}"
    
    # Extract database details from connection string
    # This is a simplified parser - adjust based on your connection string format
    DB_HOST=$(echo $PROD_DATABASE_CONNECTION_STRING | grep -oP 'Host=\K[^;]+')
    DB_NAME=$(echo $PROD_DATABASE_CONNECTION_STRING | grep -oP 'Database=\K[^;]+')
    DB_USER=$(echo $PROD_DATABASE_CONNECTION_STRING | grep -oP 'Username=\K[^;]+')
    DB_PASS=$(echo $PROD_DATABASE_CONNECTION_STRING | grep -oP 'Password=\K[^;]+')
    
    # Create database backup
    PGPASSWORD=$DB_PASS pg_dump -h $DB_HOST -U $DB_USER -d $DB_NAME > $BACKUP_DIR/database_$DATE.sql
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Database backup completed${NC}"
        gzip $BACKUP_DIR/database_$DATE.sql
    else
        echo -e "${YELLOW}⚠️ Database backup failed${NC}"
    fi
fi

# Backup uploaded files
echo -e "${GREEN}📁 Backing up uploaded files...${NC}"
if [ -d "/opt/yapplr/Yapplr.Api/uploads" ]; then
    tar -czf $BACKUP_DIR/uploads_$DATE.tar.gz -C /opt/yapplr/Yapplr.Api uploads/
    echo -e "${GREEN}✅ Files backup completed${NC}"
else
    echo -e "${YELLOW}⚠️ Uploads directory not found${NC}"
fi

# Backup configuration files
echo -e "${GREEN}⚙️ Backing up configuration...${NC}"
tar -czf $BACKUP_DIR/config_$DATE.tar.gz .env docker-compose.prod.yml nginx.conf

# Clean up old backups
echo -e "${GREEN}🧹 Cleaning up old backups...${NC}"
find $BACKUP_DIR -name "*.sql.gz" -mtime +$RETENTION_DAYS -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +$RETENTION_DAYS -delete

# List current backups
echo -e "${GREEN}📋 Current backups:${NC}"
ls -lh $BACKUP_DIR/

echo -e "${GREEN}✅ Backup process completed successfully!${NC}"

# Optional: Upload to cloud storage (uncomment and configure as needed)
# echo -e "${GREEN}☁️ Uploading to cloud storage...${NC}"
# aws s3 sync $BACKUP_DIR s3://your-backup-bucket/yapplr/ --delete
