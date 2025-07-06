#!/bin/bash

# Yapplr Health Check Script
# Comprehensive health monitoring for all services

set -e

# Configuration
COMPOSE_FILE="docker-compose.production.yml"
API_URL="http://localhost"
TIMEOUT=10

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Counters
TOTAL_CHECKS=0
PASSED_CHECKS=0
FAILED_CHECKS=0

log_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
    ((PASSED_CHECKS++))
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
    ((FAILED_CHECKS++))
}

check_service() {
    local service_name="$1"
    local description="$2"
    
    ((TOTAL_CHECKS++))
    
    if docker-compose -f $COMPOSE_FILE ps "$service_name" | grep -q "Up"; then
        log_success "$description is running"
    else
        log_error "$description is not running"
    fi
}

check_health() {
    local service_name="$1"
    local description="$2"
    
    ((TOTAL_CHECKS++))
    
    local health_status=$(docker-compose -f $COMPOSE_FILE ps "$service_name" | grep "$service_name" | awk '{print $4}')
    
    if [[ "$health_status" == *"healthy"* ]]; then
        log_success "$description health check passed"
    elif [[ "$health_status" == *"starting"* ]]; then
        log_warning "$description is starting up"
    else
        log_error "$description health check failed"
    fi
}

check_url() {
    local url="$1"
    local description="$2"
    local expected_status="${3:-200}"
    
    ((TOTAL_CHECKS++))
    
    local status_code=$(curl -s -o /dev/null -w "%{http_code}" --max-time $TIMEOUT "$url" 2>/dev/null || echo "000")
    
    if [ "$status_code" = "$expected_status" ]; then
        log_success "$description endpoint responding ($status_code)"
    else
        log_error "$description endpoint failed ($status_code)"
    fi
}

check_database() {
    ((TOTAL_CHECKS++))
    
    if docker-compose -f $COMPOSE_FILE exec -T postgres pg_isready -q; then
        log_success "Database connection successful"
    else
        log_error "Database connection failed"
    fi
}

check_redis() {
    ((TOTAL_CHECKS++))
    
    if docker-compose -f $COMPOSE_FILE exec -T redis redis-cli ping | grep -q "PONG"; then
        log_success "Redis connection successful"
    else
        log_error "Redis connection failed"
    fi
}

check_ffmpeg() {
    ((TOTAL_CHECKS++))
    
    if docker-compose -f $COMPOSE_FILE exec -T yapplr-video-processor ffmpeg -version >/dev/null 2>&1; then
        log_success "FFmpeg is available"
    else
        log_error "FFmpeg is not available"
    fi
}

check_disk_space() {
    ((TOTAL_CHECKS++))
    
    local usage=$(df / | awk 'NR==2 {print $5}' | sed 's/%//')
    
    if [ "$usage" -lt 80 ]; then
        log_success "Disk space usage: ${usage}%"
    elif [ "$usage" -lt 90 ]; then
        log_warning "Disk space usage: ${usage}% (getting high)"
    else
        log_error "Disk space usage: ${usage}% (critically high)"
    fi
}

check_memory() {
    ((TOTAL_CHECKS++))
    
    local usage=$(free | awk 'NR==2{printf "%.0f", $3*100/$2}')
    
    if [ "$usage" -lt 80 ]; then
        log_success "Memory usage: ${usage}%"
    elif [ "$usage" -lt 90 ]; then
        log_warning "Memory usage: ${usage}% (getting high)"
    else
        log_error "Memory usage: ${usage}% (critically high)"
    fi
}

check_video_processing() {
    ((TOTAL_CHECKS++))
    
    # Check if video processing jobs are being processed
    local pending_jobs=$(docker-compose -f $COMPOSE_FILE exec -T yapplr-api \
        dotnet ef dbcontext scaffold "Host=postgres;Database=yapplr;Username=postgres;Password=password" \
        Npgsql.EntityFrameworkCore.PostgreSQL \
        --force --no-build 2>/dev/null | grep -c "VideoProcessingJob" || echo "0")
    
    if [ "$pending_jobs" -eq 0 ]; then
        log_success "No pending video processing jobs"
    else
        log_info "Video processing jobs in queue: $pending_jobs"
    fi
}

check_ssl_certificate() {
    ((TOTAL_CHECKS++))
    
    if [ -f "nginx/ssl/yapplr.com.crt" ]; then
        local expiry_date=$(openssl x509 -enddate -noout -in nginx/ssl/yapplr.com.crt | cut -d= -f2)
        local expiry_epoch=$(date -d "$expiry_date" +%s)
        local current_epoch=$(date +%s)
        local days_until_expiry=$(( (expiry_epoch - current_epoch) / 86400 ))
        
        if [ "$days_until_expiry" -gt 30 ]; then
            log_success "SSL certificate valid for $days_until_expiry days"
        elif [ "$days_until_expiry" -gt 7 ]; then
            log_warning "SSL certificate expires in $days_until_expiry days"
        else
            log_error "SSL certificate expires in $days_until_expiry days (urgent renewal needed)"
        fi
    else
        log_error "SSL certificate not found"
    fi
}

# Main health check function
main() {
    echo "🏥 Yapplr Health Check"
    echo "====================="
    echo ""
    
    log_info "Checking service status..."
    check_service "yapplr-api" "API Service"
    check_service "yapplr-video-processor" "Video Processor"
    check_service "yapplr-frontend" "Frontend Service"
    check_service "postgres" "Database"
    check_service "redis" "Redis Cache"
    check_service "nginx" "Nginx Proxy"
    
    echo ""
    log_info "Checking service health..."
    check_health "yapplr-api" "API Service"
    check_health "yapplr-video-processor" "Video Processor"
    check_health "postgres" "Database"
    check_health "redis" "Redis Cache"
    
    echo ""
    log_info "Checking endpoints..."
    check_url "$API_URL/health" "Health endpoint"
    check_url "$API_URL/api/auth/health" "Auth endpoint"
    check_url "http://localhost:3001" "Grafana dashboard"
    check_url "http://localhost:9090" "Prometheus metrics"
    
    echo ""
    log_info "Checking database connectivity..."
    check_database
    check_redis
    
    echo ""
    log_info "Checking video processing..."
    check_ffmpeg
    check_video_processing
    
    echo ""
    log_info "Checking system resources..."
    check_disk_space
    check_memory
    
    echo ""
    log_info "Checking security..."
    check_ssl_certificate
    
    echo ""
    echo "📊 Health Check Summary"
    echo "======================"
    echo "Total checks: $TOTAL_CHECKS"
    echo -e "Passed: ${GREEN}$PASSED_CHECKS${NC}"
    echo -e "Failed: ${RED}$FAILED_CHECKS${NC}"
    
    if [ "$FAILED_CHECKS" -eq 0 ]; then
        echo ""
        log_success "🎉 All health checks passed!"
        exit 0
    else
        echo ""
        log_error "❌ $FAILED_CHECKS health check(s) failed"
        exit 1
    fi
}

# Parse command line arguments
case "${1:-check}" in
    "check")
        main
        ;;
    "quick")
        log_info "Quick health check..."
        check_url "$API_URL/health" "API Health"
        check_service "yapplr-api" "API Service"
        check_service "yapplr-video-processor" "Video Processor"
        ;;
    "services")
        log_info "Checking services only..."
        check_service "yapplr-api" "API Service"
        check_service "yapplr-video-processor" "Video Processor"
        check_service "yapplr-frontend" "Frontend Service"
        check_service "postgres" "Database"
        check_service "redis" "Redis Cache"
        check_service "nginx" "Nginx Proxy"
        ;;
    *)
        echo "Usage: $0 {check|quick|services}"
        echo ""
        echo "Commands:"
        echo "  check    - Full comprehensive health check"
        echo "  quick    - Quick essential checks only"
        echo "  services - Check service status only"
        exit 1
        ;;
esac
