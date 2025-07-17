#!/bin/bash

# Yapplr Logging Stack Startup Script
# This script helps you start the logging infrastructure

set -e

echo "🚀 Starting Yapplr Logging Stack..."

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo "📋 Checking prerequisites..."

if ! command_exists docker; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

if ! command_exists docker-compose; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

echo "✅ Prerequisites check passed"

# Determine environment
ENVIRONMENT=${1:-local}

case $ENVIRONMENT in
    local)
        COMPOSE_FILE="docker-compose.local.yml"
        GRAFANA_URL="http://localhost:3000"
        LOKI_URL="http://localhost:3100"
        ;;
    staging)
        COMPOSE_FILE="docker-compose.stage.yml"
        GRAFANA_URL="https://stg-api.yapplr.com:3000"
        LOKI_URL="https://stg-api.yapplr.com:3100"
        ;;
    production)
        COMPOSE_FILE="docker-compose.prod.yml"
        GRAFANA_URL="https://api.yapplr.com:3000"
        LOKI_URL="https://api.yapplr.com:3100"
        ;;
    *)
        echo "❌ Invalid environment. Use: local, staging, or production"
        exit 1
        ;;
esac

echo "🌍 Environment: $ENVIRONMENT"
echo "📄 Using compose file: $COMPOSE_FILE"

# Create necessary directories for production
if [ "$ENVIRONMENT" = "production" ]; then
    echo "📁 Creating production storage directories..."
    sudo mkdir -p /mnt/yapplr-prod-storage/{logs,loki,grafana}
    sudo chown -R $USER:$USER /mnt/yapplr-prod-storage/
fi

# Start only the logging services
echo "🔄 Starting logging services..."

# Start Loki first
echo "📊 Starting Loki..."
docker-compose -f $COMPOSE_FILE up -d loki

# Wait for Loki to be ready
echo "⏳ Waiting for Loki to be ready..."
for i in {1..30}; do
    if curl -s $LOKI_URL/ready >/dev/null 2>&1; then
        echo "✅ Loki is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "❌ Loki failed to start within 30 seconds"
        exit 1
    fi
    sleep 1
done

# Start Grafana
echo "📈 Starting Grafana..."
docker-compose -f $COMPOSE_FILE up -d grafana

# Wait for Grafana to be ready
echo "⏳ Waiting for Grafana to be ready..."
for i in {1..30}; do
    if curl -s $GRAFANA_URL/api/health >/dev/null 2>&1; then
        echo "✅ Grafana is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "❌ Grafana failed to start within 30 seconds"
        exit 1
    fi
    sleep 1
done

# Start Promtail
echo "📝 Starting Promtail..."
docker-compose -f $COMPOSE_FILE up -d promtail

echo ""
echo "🎉 Logging stack started successfully!"
echo ""
echo "📊 Access URLs:"
echo "   Grafana UI: $GRAFANA_URL"
echo "   Loki API:   $LOKI_URL"
echo ""
echo "🔐 Default Grafana credentials:"
echo "   Username: admin"
if [ "$ENVIRONMENT" = "local" ]; then
    echo "   Password: admin123"
else
    echo "   Password: Check your environment variables"
fi
echo ""
echo "📖 Quick start:"
echo "   1. Open Grafana in your browser"
echo "   2. Go to Explore (compass icon)"
echo "   3. Select 'Loki' as data source"
echo "   4. Try query: {service=\"yapplr-api\"}"
echo ""
echo "📚 For more information, see Documents/LOGGING_SETUP_GUIDE.md"

# Optionally start the full application stack
read -p "🤔 Do you want to start the full application stack now? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "🚀 Starting full application stack..."
    docker-compose -f $COMPOSE_FILE up -d
    echo "✅ Full stack started!"
else
    echo "ℹ️  To start the full stack later, run:"
    echo "   docker-compose -f $COMPOSE_FILE up -d"
fi
