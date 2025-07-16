#!/bin/bash

# Analytics Data Migration Script
# Migrates existing analytics data from PostgreSQL to InfluxDB

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
API_URL="http://localhost:8080"
ADMIN_TOKEN=""  # Set this to your admin JWT token

echo -e "${BLUE}📊 Yapplr Analytics Data Migration${NC}"
echo "===================================="

# Function to check if admin token is set
check_admin_token() {
    if [ -z "$ADMIN_TOKEN" ]; then
        echo -e "${RED}❌ Error: ADMIN_TOKEN environment variable is not set${NC}"
        echo "Please set your admin JWT token:"
        echo "export ADMIN_TOKEN=\"your-jwt-token-here\""
        exit 1
    fi
}

# Function to make authenticated API calls
api_call() {
    local method=$1
    local endpoint=$2
    local data=${3:-""}
    
    if [ -n "$data" ]; then
        curl -s -X "$method" "$API_URL$endpoint" \
            -H "Authorization: Bearer $ADMIN_TOKEN" \
            -H "Content-Type: application/json" \
            -d "$data"
    else
        curl -s -X "$method" "$API_URL$endpoint" \
            -H "Authorization: Bearer $ADMIN_TOKEN"
    fi
}

# Function to check migration status
check_status() {
    echo -e "\n${BLUE}📋 Checking Migration Status${NC}"
    local status=$(api_call GET "/api/admin/analytics/migration/status")
    echo "$status" | jq '.'
}

# Function to check data source
check_data_source() {
    echo -e "\n${BLUE}🔍 Checking Analytics Data Source${NC}"
    local source=$(api_call GET "/api/admin/analytics/data-source")
    echo "$source" | jq '.'
    
    local influx_available=$(echo "$source" | jq -r '.influx_available')
    if [ "$influx_available" != "true" ]; then
        echo -e "${RED}❌ InfluxDB is not available. Please ensure InfluxDB is running.${NC}"
        exit 1
    fi
}

# Function to migrate all data
migrate_all() {
    local from_date=${1:-""}
    local to_date=${2:-""}
    local batch_size=${3:-1000}
    
    echo -e "\n${BLUE}🚀 Starting Full Migration${NC}"
    echo "From Date: ${from_date:-"beginning"}"
    echo "To Date: ${to_date:-"now"}"
    echo "Batch Size: $batch_size"
    
    local data="{\"fromDate\":\"$from_date\",\"toDate\":\"$to_date\",\"batchSize\":$batch_size}"
    local result=$(api_call POST "/api/admin/analytics/migrate" "$data")
    
    echo -e "\n${YELLOW}Migration Result:${NC}"
    echo "$result" | jq '.'
    
    local success=$(echo "$result" | jq -r '.success')
    if [ "$success" = "true" ]; then
        echo -e "\n${GREEN}✅ Migration completed successfully!${NC}"
    else
        echo -e "\n${RED}❌ Migration failed!${NC}"
        local error=$(echo "$result" | jq -r '.errorMessage // "Unknown error"')
        echo "Error: $error"
        exit 1
    fi
}

# Function to validate migration
validate_migration() {
    local from_date=${1:-""}
    local to_date=${2:-""}
    
    echo -e "\n${BLUE}🔍 Validating Migration${NC}"
    
    local data="{\"fromDate\":\"$from_date\",\"toDate\":\"$to_date\"}"
    local result=$(api_call POST "/api/admin/analytics/migration/validate" "$data")
    
    echo -e "\n${YELLOW}Validation Result:${NC}"
    echo "$result" | jq '.'
    
    local is_valid=$(echo "$result" | jq -r '.isValid')
    if [ "$is_valid" = "true" ]; then
        echo -e "\n${GREEN}✅ Migration validation passed!${NC}"
    else
        echo -e "\n${RED}❌ Migration validation failed!${NC}"
        echo "Please check the table validations above for details."
        exit 1
    fi
}

# Function to migrate specific table
migrate_table() {
    local table=$1
    local from_date=${2:-""}
    local to_date=${3:-""}
    local batch_size=${4:-1000}
    
    echo -e "\n${BLUE}🚀 Migrating $table${NC}"
    
    local endpoint=""
    case $table in
        "user-activities")
            endpoint="/api/admin/analytics/migrate/user-activities"
            ;;
        "content-engagements")
            endpoint="/api/admin/analytics/migrate/content-engagements"
            ;;
        *)
            echo -e "${RED}❌ Unknown table: $table${NC}"
            echo "Available tables: user-activities, content-engagements"
            exit 1
            ;;
    esac
    
    local data="{\"fromDate\":\"$from_date\",\"toDate\":\"$to_date\",\"batchSize\":$batch_size}"
    local result=$(api_call POST "$endpoint" "$data")
    
    echo -e "\n${YELLOW}Migration Result for $table:${NC}"
    echo "$result" | jq '.'
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  status                    - Check migration status"
    echo "  check                     - Check data source availability"
    echo "  migrate [from] [to] [batch] - Migrate all data"
    echo "  validate [from] [to]      - Validate migrated data"
    echo "  migrate-table <table> [from] [to] [batch] - Migrate specific table"
    echo ""
    echo "Options:"
    echo "  from     - Start date (YYYY-MM-DD format, optional)"
    echo "  to       - End date (YYYY-MM-DD format, optional)"
    echo "  batch    - Batch size (default: 1000)"
    echo "  table    - Table name (user-activities, content-engagements)"
    echo ""
    echo "Examples:"
    echo "  $0 migrate                           # Migrate all data"
    echo "  $0 migrate 2024-01-01 2024-12-31    # Migrate data for 2024"
    echo "  $0 migrate-table user-activities     # Migrate only user activities"
    echo "  $0 validate                          # Validate all migrated data"
    echo ""
    echo "Environment Variables:"
    echo "  ADMIN_TOKEN - Your admin JWT token (required)"
}

# Main script logic
main() {
    check_admin_token
    
    case ${1:-""} in
        "status")
            check_status
            ;;
        "check")
            check_data_source
            ;;
        "migrate")
            check_data_source
            migrate_all "$2" "$3" "$4"
            ;;
        "validate")
            validate_migration "$2" "$3"
            ;;
        "migrate-table")
            if [ -z "$2" ]; then
                echo -e "${RED}❌ Table name is required${NC}"
                show_usage
                exit 1
            fi
            check_data_source
            migrate_table "$2" "$3" "$4" "$5"
            ;;
        "help"|"-h"|"--help"|"")
            show_usage
            ;;
        *)
            echo -e "${RED}❌ Unknown command: $1${NC}"
            show_usage
            exit 1
            ;;
    esac
}

# Check dependencies
if ! command -v curl &> /dev/null; then
    echo -e "${RED}❌ curl is required but not installed${NC}"
    exit 1
fi

if ! command -v jq &> /dev/null; then
    echo -e "${RED}❌ jq is required but not installed${NC}"
    echo "Install with: brew install jq (macOS) or apt-get install jq (Ubuntu)"
    exit 1
fi

# Run main function
main "$@"
