#!/bin/bash

# Yapplr Staging Deployment Script
# This script deploys the complete Yapplr system for staging with local PostgreSQL

set -e

echo "🚀 Starting Yapplr Staging Deployment"
echo "====================================="

# Configuration
COMPOSE_FILE="docker-compose.staging.yml"
ENV_FILE=".env.staging"

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
    
    # Validate required environment variables
    log_info "Validating environment variables..."
    source "$ENV_FILE"
    required_vars=("POSTGRES_DB" "POSTGRES_USER" "POSTGRES_PASSWORD" "JWT_SECRET_KEY" "SENDGRID_API_KEY")
    for var in "${required_vars[@]}"; do
        if [ -z "${!var}" ]; then
            log_error "$var is not set in $ENV_FILE"
            exit 1
        fi
    done
    
    log_success "Prerequisites check passed"
}

# Aggressive cleanup function
aggressive_cleanup() {
    log_info "Performing aggressive cleanup..."
    
    # Stop existing containers
    log_info "Stopping existing containers..."
    docker-compose -f "$COMPOSE_FILE" down --volumes --remove-orphans || true
    
    # Force remove specific containers that might be stuck
    log_info "Force removing any stuck containers..."
    docker rm -f yapplr_nginx_1 yapplr_yapplr-api_1 yapplr_yapplr-frontend_1 yapplr_yapplr-video-processor_1 yapplr_postgres_1 || true
    docker rm -f yapplr-nginx-1 yapplr-yapplr-api-1 yapplr-yapplr-frontend-1 yapplr-yapplr-video-processor-1 yapplr-postgres-1 || true
    
    # Stop and remove all containers with yapplr in the name
    docker ps -a --filter "name=yapplr" --format "{{.Names}}" | xargs -r docker stop || true
    docker ps -a --filter "name=yapplr" --format "{{.Names}}" | xargs -r docker rm -f || true
    
    # Additional cleanup to ensure ports are free
    log_info "Cleaning up any remaining containers..."
    docker container prune -f || true
    
    # Clean up networks that might be left behind
    log_info "Cleaning up networks..."
    docker network rm yapplr_yapplr-network yapplr-network || true
    
    # Remove old images to force complete rebuild
    log_info "Removing old Docker images..."
    docker image rm yapplr-api:latest yapplr-frontend:latest yapplr-video-processor:latest || true
    docker image rm yapplr_yapplr-api:latest yapplr_yapplr-frontend:latest yapplr_yapplr-video-processor:latest || true
    docker image prune -f || true
    
    log_success "Aggressive cleanup completed"
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
    export $(cat "$ENV_FILE" | xargs)
    
    # Set cache bust variable to force frontend rebuild
    export CACHE_BUST=$(date +%s)
    log_info "Cache bust value: $CACHE_BUST"
    
    # Build images
    log_info "Building Docker images..."
    docker-compose -f "$COMPOSE_FILE" build --no-cache
    
    # Start services with forced recreation
    log_info "Starting services..."
    docker-compose -f "$COMPOSE_FILE" up -d --force-recreate
    
    log_success "Services started"
}

# Wait for services to be healthy
wait_for_services() {
    log_info "Waiting for services to be healthy..."
    
    local max_attempts=30
    local attempt=1
    
    # Wait for services to start
    sleep 30
    
    while [ $attempt -le $max_attempts ]; do
        # Check if API is responding
        if curl -f http://localhost/health > /dev/null 2>&1; then
            log_success "API is healthy and responding"
            return 0
        fi
        
        log_info "Attempt $attempt/$max_attempts - waiting for API to be ready..."
        sleep 10
        ((attempt++))
    done
    
    log_error "Services failed to become healthy within timeout"
    log_info "Checking logs for debugging..."
    docker-compose -f "$COMPOSE_FILE" logs --tail=50 yapplr-api
    return 1
}

# Run database migrations
run_migrations() {
    log_info "Database migrations will run automatically when the API starts..."
    
    # Database migrations are handled automatically in Program.cs
    # No manual intervention needed
    
    log_success "Database migrations are handled automatically"
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
    if docker-compose -f "$COMPOSE_FILE" exec -T yapplr-video-processor ffmpeg -version > /dev/null 2>&1; then
        log_success "Video processor check passed"
    else
        log_warning "Video processor check failed (this is expected if video processing is disabled)"
    fi
    
    # Check database connectivity
    if docker-compose -f "$COMPOSE_FILE" exec -T postgres pg_isready > /dev/null 2>&1; then
        log_success "Database connectivity check passed"
    else
        log_error "Database connectivity check failed"
        return 1
    fi
    
    log_success "Deployment verification completed"
}

# Show deployment status
show_status() {
    echo ""
    log_info "Staging Deployment Status:"
    echo "=========================="
    
    docker-compose -f "$COMPOSE_FILE" ps
    
    echo ""
    log_info "Service URLs:"
    echo "============="
    echo "🌐 Frontend: http://localhost (or your staging domain)"
    echo "🔧 API: http://localhost/api"
    echo "🗄️ Database: localhost:5432 (PostgreSQL)"
    echo ""
    
    log_info "Useful Commands:"
    echo "================"
    echo "📋 View all logs: docker-compose -f $COMPOSE_FILE logs -f"
    echo "🔍 API logs: docker-compose -f $COMPOSE_FILE logs -f yapplr-api"
    echo "🎥 Video processor logs: docker-compose -f $COMPOSE_FILE logs -f yapplr-video-processor"
    echo "🗄️ Database logs: docker-compose -f $COMPOSE_FILE logs -f postgres"
    echo "🔄 Restart services: ./scripts/deploy-staging.sh restart"
    echo "🛑 Stop services: ./scripts/deploy-staging.sh stop"
    echo "📊 Health check: ./scripts/health-check.sh staging"
    
    # Clean up old images
    log_info "Cleaning up old Docker images..."
    docker image prune -f > /dev/null 2>&1 || true
}

# Cleanup function
cleanup() {
    if [ $? -ne 0 ]; then
        log_error "Deployment failed. Cleaning up..."
        docker-compose -f "$COMPOSE_FILE" down
    fi
}

# Set trap for cleanup
trap cleanup EXIT

# Main deployment flow
main() {
    check_prerequisites
    aggressive_cleanup
    create_directories
    deploy_services
    wait_for_services
    run_migrations
    verify_deployment
    show_status
    
    log_success "🎉 Yapplr staging deployment completed successfully!"
}

# Parse command line arguments
case "${1:-deploy}" in
    "deploy")
        main
        ;;
    "stop")
        log_info "Stopping services..."
        docker-compose -f "$COMPOSE_FILE" down
        log_success "Services stopped"
        ;;
    "restart")
        log_info "Restarting services..."
        docker-compose -f "$COMPOSE_FILE" restart
        log_success "Services restarted"
        ;;
    "logs")
        docker-compose -f "$COMPOSE_FILE" logs -f "${2:-}"
        ;;
    "status")
        show_status
        ;;
    "cleanup")
        log_info "Performing cleanup only..."
        aggressive_cleanup
        log_success "Cleanup completed"
        ;;
    *)
        echo "Usage: $0 {deploy|stop|restart|logs|status|cleanup}"
        echo ""
        echo "Commands:"
        echo "  deploy  - Deploy the complete staging system"
        echo "  stop    - Stop all services"
        echo "  restart - Restart all services"
        echo "  logs    - Show logs (optionally specify service name)"
        echo "  status  - Show deployment status"
        echo "  cleanup - Perform aggressive cleanup only"
        exit 1
        ;;
esac
