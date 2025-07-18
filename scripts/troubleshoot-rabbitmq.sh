#!/bin/bash

# RabbitMQ Troubleshooting Script for Staging Environment
# Usage: ./scripts/troubleshoot-rabbitmq.sh

echo "🐰 RabbitMQ Troubleshooting Script"
echo "=================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running or not accessible"
    exit 1
fi

# Function to check RabbitMQ container status
check_rabbitmq_status() {
    echo "📊 Checking RabbitMQ container status..."
    
    CONTAINER_ID=$(docker ps -q -f name=rabbitmq)
    if [ -z "$CONTAINER_ID" ]; then
        echo "❌ RabbitMQ container is not running"
        
        # Check if container exists but is stopped
        STOPPED_CONTAINER=$(docker ps -a -q -f name=rabbitmq)
        if [ -n "$STOPPED_CONTAINER" ]; then
            echo "🔍 Found stopped RabbitMQ container: $STOPPED_CONTAINER"
            echo "📋 Container logs (last 50 lines):"
            docker logs --tail 50 $STOPPED_CONTAINER
        fi
        return 1
    else
        echo "✅ RabbitMQ container is running: $CONTAINER_ID"
        
        # Check container health
        HEALTH_STATUS=$(docker inspect --format='{{.State.Health.Status}}' $CONTAINER_ID 2>/dev/null)
        if [ "$HEALTH_STATUS" = "healthy" ]; then
            echo "✅ RabbitMQ container is healthy"
        elif [ "$HEALTH_STATUS" = "unhealthy" ]; then
            echo "❌ RabbitMQ container is unhealthy"
            echo "📋 Health check logs:"
            docker inspect --format='{{range .State.Health.Log}}{{.Output}}{{end}}' $CONTAINER_ID
        else
            echo "⚠️  RabbitMQ container health status: $HEALTH_STATUS"
        fi
        
        return 0
    fi
}

# Function to check RabbitMQ logs
check_rabbitmq_logs() {
    echo "📋 Checking RabbitMQ logs..."
    
    CONTAINER_ID=$(docker ps -q -f name=rabbitmq)
    if [ -n "$CONTAINER_ID" ]; then
        echo "📋 Recent RabbitMQ logs (last 100 lines):"
        docker logs --tail 100 $CONTAINER_ID
    else
        echo "❌ No running RabbitMQ container found"
    fi
}

# Function to check RabbitMQ connectivity
check_rabbitmq_connectivity() {
    echo "🔌 Checking RabbitMQ connectivity..."
    
    CONTAINER_ID=$(docker ps -q -f name=rabbitmq)
    if [ -n "$CONTAINER_ID" ]; then
        echo "🔍 Testing RabbitMQ diagnostics..."
        docker exec $CONTAINER_ID rabbitmq-diagnostics ping
        
        echo "🔍 Checking RabbitMQ status..."
        docker exec $CONTAINER_ID rabbitmq-diagnostics status
        
        echo "🔍 Checking RabbitMQ cluster status..."
        docker exec $CONTAINER_ID rabbitmq-diagnostics cluster_status
        
        echo "🔍 Checking RabbitMQ memory usage..."
        docker exec $CONTAINER_ID rabbitmq-diagnostics memory_breakdown
    else
        echo "❌ No running RabbitMQ container found"
    fi
}

# Function to check Docker resources
check_docker_resources() {
    echo "💾 Checking Docker resources..."
    
    echo "📊 Docker system info:"
    docker system df
    
    echo "📊 Container resource usage:"
    docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"
}

# Function to check RabbitMQ volumes
check_rabbitmq_volumes() {
    echo "💾 Checking RabbitMQ volumes..."
    
    echo "📊 RabbitMQ volume info:"
    docker volume ls | grep rabbitmq
    
    RABBITMQ_VOLUME=$(docker volume ls -q | grep rabbitmq | head -1)
    if [ -n "$RABBITMQ_VOLUME" ]; then
        echo "📊 Volume details for $RABBITMQ_VOLUME:"
        docker volume inspect $RABBITMQ_VOLUME
    fi
}

# Function to restart RabbitMQ
restart_rabbitmq() {
    echo "🔄 Restarting RabbitMQ..."
    
    read -p "Are you sure you want to restart RabbitMQ? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        CONTAINER_ID=$(docker ps -q -f name=rabbitmq)
        if [ -n "$CONTAINER_ID" ]; then
            echo "🔄 Restarting RabbitMQ container..."
            docker restart $CONTAINER_ID
            
            echo "⏳ Waiting for RabbitMQ to start..."
            sleep 10
            
            check_rabbitmq_status
        else
            echo "❌ No running RabbitMQ container found"
        fi
    else
        echo "❌ Restart cancelled"
    fi
}

# Function to clean RabbitMQ data
clean_rabbitmq_data() {
    echo "🧹 Cleaning RabbitMQ data..."
    
    read -p "⚠️  This will DELETE ALL RabbitMQ data! Are you sure? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "🛑 Stopping RabbitMQ container..."
        docker stop $(docker ps -q -f name=rabbitmq) 2>/dev/null
        
        echo "🗑️  Removing RabbitMQ volume..."
        docker volume rm $(docker volume ls -q | grep rabbitmq) 2>/dev/null
        
        echo "🔄 Starting RabbitMQ with fresh data..."
        # This assumes you're using docker-compose
        docker-compose up -d rabbitmq
        
        echo "⏳ Waiting for RabbitMQ to initialize..."
        sleep 30
        
        check_rabbitmq_status
    else
        echo "❌ Clean cancelled"
    fi
}

# Main menu
show_menu() {
    echo ""
    echo "🛠️  RabbitMQ Troubleshooting Options:"
    echo "1. Check RabbitMQ status"
    echo "2. Check RabbitMQ logs"
    echo "3. Check RabbitMQ connectivity"
    echo "4. Check Docker resources"
    echo "5. Check RabbitMQ volumes"
    echo "6. Restart RabbitMQ"
    echo "7. Clean RabbitMQ data (DESTRUCTIVE)"
    echo "8. Run all checks"
    echo "9. Exit"
    echo ""
}

# Run all checks
run_all_checks() {
    check_rabbitmq_status
    echo ""
    check_rabbitmq_logs
    echo ""
    check_rabbitmq_connectivity
    echo ""
    check_docker_resources
    echo ""
    check_rabbitmq_volumes
}

# Main script
if [ "$1" = "--auto" ]; then
    echo "🤖 Running automatic diagnostics..."
    run_all_checks
    exit 0
fi

while true; do
    show_menu
    read -p "Select an option (1-9): " choice
    
    case $choice in
        1) check_rabbitmq_status ;;
        2) check_rabbitmq_logs ;;
        3) check_rabbitmq_connectivity ;;
        4) check_docker_resources ;;
        5) check_rabbitmq_volumes ;;
        6) restart_rabbitmq ;;
        7) clean_rabbitmq_data ;;
        8) run_all_checks ;;
        9) echo "👋 Goodbye!"; exit 0 ;;
        *) echo "❌ Invalid option. Please try again." ;;
    esac
    
    echo ""
    read -p "Press Enter to continue..."
done
