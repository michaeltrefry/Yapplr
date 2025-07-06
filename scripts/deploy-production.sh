#!/bin/bash

# Yapplr Production Deployment Script
# This script deploys the complete Yapplr system with video processing

set -e

echo "🚀 Starting Yapplr Production Deployment"
echo "========================================"

# Configuration
COMPOSE_FILE="docker-compose.production.yml"
ENV_FILE=".env.production"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

log_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

log_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

log_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose is not installed"
        exit 1
    fi
    
    if [ ! -f "$ENV_FILE" ]; then
        log_error "Environment file $ENV_FILE not found"
        log_info "Please create $ENV_FILE with required environment variables"
        exit 1
    fi
    
    log_success "Prerequisites check passed"
}

# Create required directories
create_directories() {
    log_info "Creating required directories..."
    
    mkdir -p uploads/{videos,thumbnails,temp}
    mkdir -p logs/{api,video-processor}
    mkdir -p backups
    mkdir -p nginx/ssl
    
    # Set proper permissions
    chmod 755 uploads/{videos,thumbnails,temp}
    chmod 755 logs/{api,video-processor}
    chmod 755 backups
    
    log_success "Directories created"
}

# Build and start services
deploy_services() {
    log_info "Building and starting services..."
    
    # Load environment variables
    export $(cat $ENV_FILE | xargs)
    
    # Build images
    log_info "Building Docker images..."
    docker-compose -f $COMPOSE_FILE build --no-cache
    
    # Start services
    log_info "Starting services..."
    docker-compose -f $COMPOSE_FILE up -d
    
    log_success "Services started"
}

# Wait for services to be healthy
wait_for_services() {
    log_info "Waiting for services to be healthy..."
    
    local max_attempts=30
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if docker-compose -f $COMPOSE_FILE ps | grep -q "healthy"; then
            log_success "Services are healthy"
            return 0
        fi
        
        log_info "Attempt $attempt/$max_attempts - waiting for services..."
        sleep 10
        ((attempt++))
    done
    
    log_error "Services failed to become healthy within timeout"
    return 1
}

# Run database migrations
run_migrations() {
    log_info "Running database migrations..."
    
    docker-compose -f $COMPOSE_FILE exec -T yapplr-api dotnet ef database update
    
    log_success "Database migrations completed"
}

# Verify deployment
verify_deployment() {
    log_info "Verifying deployment..."
    
    # Check API health
    if curl -f http://localhost/health > /dev/null 2>&1; then
        log_success "API health check passed"
    else
        log_error "API health check failed"
        return 1
    fi
    
    # Check video processor
    if docker-compose -f $COMPOSE_FILE exec -T yapplr-video-processor ffmpeg -version > /dev/null 2>&1; then
        log_success "Video processor check passed"
    else
        log_error "Video processor check failed"
        return 1
    fi
    
    log_success "Deployment verification completed"
}

# Show deployment status
show_status() {
    echo ""
    log_info "Deployment Status:"
    echo "=================="
    
    docker-compose -f $COMPOSE_FILE ps
    
    echo ""
    log_info "Service URLs:"
    echo "============="
    echo "🌐 Frontend: https://yapplr.com"
    echo "🔧 API: https://api.yapplr.com"
    echo "📊 Grafana: http://localhost:3001"
    echo "📈 Prometheus: http://localhost:9090"
    echo ""
    
    log_info "Logs:"
    echo "====="
    echo "📋 View all logs: docker-compose -f $COMPOSE_FILE logs -f"
    echo "🔍 API logs: docker-compose -f $COMPOSE_FILE logs -f yapplr-api"
    echo "🎥 Video processor logs: docker-compose -f $COMPOSE_FILE logs -f yapplr-video-processor"
}

# Cleanup function
cleanup() {
    if [ $? -ne 0 ]; then
        log_error "Deployment failed. Cleaning up..."
        docker-compose -f $COMPOSE_FILE down
    fi
}

# Set trap for cleanup
trap cleanup EXIT

# Main deployment flow
main() {
    check_prerequisites
    create_directories
    deploy_services
    wait_for_services
    run_migrations
    verify_deployment
    show_status
    
    log_success "🎉 Yapplr deployment completed successfully!"
}

# Parse command line arguments
case "${1:-deploy}" in
    "deploy")
        main
        ;;
    "stop")
        log_info "Stopping services..."
        docker-compose -f $COMPOSE_FILE down
        log_success "Services stopped"
        ;;
    "restart")
        log_info "Restarting services..."
        docker-compose -f $COMPOSE_FILE restart
        log_success "Services restarted"
        ;;
    "logs")
        docker-compose -f $COMPOSE_FILE logs -f "${2:-}"
        ;;
    "status")
        show_status
        ;;
    *)
        echo "Usage: $0 {deploy|stop|restart|logs|status}"
        echo ""
        echo "Commands:"
        echo "  deploy  - Deploy the complete system"
        echo "  stop    - Stop all services"
        echo "  restart - Restart all services"
        echo "  logs    - Show logs (optionally specify service name)"
        echo "  status  - Show deployment status"
        exit 1
        ;;
esac
