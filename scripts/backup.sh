#!/bin/bash

# Yapplr Backup Script
# Creates backups of database and uploaded files

set -e

# Configuration
BACKUP_DIR="./backups"
DATE=$(date +%Y%m%d_%H%M%S)
COMPOSE_FILE="docker-compose.production.yml"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

log_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Create backup directory
mkdir -p "$BACKUP_DIR"

# Load environment variables
if [ -f ".env.production" ]; then
    export $(cat .env.production | xargs)
fi

# Database backup
backup_database() {
    log_info "Creating database backup..."
    
    local backup_file="$BACKUP_DIR/database_$DATE.sql"
    
    docker-compose -f $COMPOSE_FILE exec -T postgres pg_dump \
        -U "$POSTGRES_USER" \
        -d "$POSTGRES_DB" \
        --no-owner \
        --no-privileges \
        --clean \
        --if-exists > "$backup_file"
    
    # Compress the backup
    gzip "$backup_file"
    
    log_success "Database backup created: ${backup_file}.gz"
}

# Files backup
backup_files() {
    log_info "Creating files backup..."
    
    local backup_file="$BACKUP_DIR/uploads_$DATE.tar.gz"
    
    tar -czf "$backup_file" uploads/
    
    log_success "Files backup created: $backup_file"
}

# Configuration backup
backup_config() {
    log_info "Creating configuration backup..."
    
    local backup_file="$BACKUP_DIR/config_$DATE.tar.gz"
    
    tar -czf "$backup_file" \
        .env.production \
        nginx/ \
        monitoring/ \
        scripts/ \
        docker-compose.production.yml \
        2>/dev/null || true
    
    log_success "Configuration backup created: $backup_file"
}

# Cleanup old backups
cleanup_old_backups() {
    log_info "Cleaning up old backups..."
    
    local retention_days=${BACKUP_RETENTION_DAYS:-30}
    
    find "$BACKUP_DIR" -name "*.gz" -mtime +$retention_days -delete
    find "$BACKUP_DIR" -name "*.tar.gz" -mtime +$retention_days -delete
    
    log_success "Old backups cleaned up (retention: $retention_days days)"
}

# Upload to S3 (if configured)
upload_to_s3() {
    if [ -n "$BACKUP_S3_BUCKET" ] && [ -n "$AWS_ACCESS_KEY_ID" ]; then
        log_info "Uploading backups to S3..."
        
        if command -v aws &> /dev/null; then
            aws s3 sync "$BACKUP_DIR" "s3://$BACKUP_S3_BUCKET/yapplr-backups/" \
                --exclude "*" \
                --include "*$DATE*"
            
            log_success "Backups uploaded to S3"
        else
            log_error "AWS CLI not found, skipping S3 upload"
        fi
    fi
}

# Verify backups
verify_backups() {
    log_info "Verifying backups..."
    
    local db_backup="$BACKUP_DIR/database_$DATE.sql.gz"
    local files_backup="$BACKUP_DIR/uploads_$DATE.tar.gz"
    local config_backup="$BACKUP_DIR/config_$DATE.tar.gz"
    
    if [ -f "$db_backup" ] && [ -s "$db_backup" ]; then
        log_success "Database backup verified"
    else
        log_error "Database backup verification failed"
        return 1
    fi
    
    if [ -f "$files_backup" ] && [ -s "$files_backup" ]; then
        log_success "Files backup verified"
    else
        log_error "Files backup verification failed"
        return 1
    fi
    
    if [ -f "$config_backup" ] && [ -s "$config_backup" ]; then
        log_success "Configuration backup verified"
    else
        log_error "Configuration backup verification failed"
        return 1
    fi
}

# Main backup function
main() {
    log_info "Starting Yapplr backup process..."
    
    backup_database
    backup_files
    backup_config
    verify_backups
    cleanup_old_backups
    upload_to_s3
    
    log_success "🎉 Backup process completed successfully!"
    
    # Show backup summary
    echo ""
    log_info "Backup Summary:"
    echo "==============="
    ls -lh "$BACKUP_DIR"/*$DATE*
}

# Parse command line arguments
case "${1:-backup}" in
    "backup")
        main
        ;;
    "restore")
        if [ -z "$2" ]; then
            log_error "Please specify backup date (YYYYMMDD_HHMMSS)"
            echo "Available backups:"
            ls -1 "$BACKUP_DIR"/database_*.sql.gz | sed 's/.*database_\(.*\)\.sql\.gz/\1/'
            exit 1
        fi
        
        RESTORE_DATE="$2"
        log_info "Restoring from backup: $RESTORE_DATE"
        
        # Restore database
        if [ -f "$BACKUP_DIR/database_$RESTORE_DATE.sql.gz" ]; then
            log_info "Restoring database..."
            gunzip -c "$BACKUP_DIR/database_$RESTORE_DATE.sql.gz" | \
                docker-compose -f $COMPOSE_FILE exec -T postgres psql \
                    -U "$POSTGRES_USER" \
                    -d "$POSTGRES_DB"
            log_success "Database restored"
        fi
        
        # Restore files
        if [ -f "$BACKUP_DIR/uploads_$RESTORE_DATE.tar.gz" ]; then
            log_info "Restoring files..."
            tar -xzf "$BACKUP_DIR/uploads_$RESTORE_DATE.tar.gz"
            log_success "Files restored"
        fi
        ;;
    "list")
        echo "Available backups:"
        ls -1 "$BACKUP_DIR"/database_*.sql.gz 2>/dev/null | sed 's/.*database_\(.*\)\.sql\.gz/\1/' || echo "No backups found"
        ;;
    *)
        echo "Usage: $0 {backup|restore|list}"
        echo ""
        echo "Commands:"
        echo "  backup           - Create a new backup"
        echo "  restore <date>   - Restore from backup (format: YYYYMMDD_HHMMSS)"
        echo "  list             - List available backups"
        exit 1
        ;;
esac
