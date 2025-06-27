#!/bin/bash

# Local Docker Testing Script for Yapplr API
# This script builds and runs the API locally for testing

set -e

echo "🚀 Starting local Yapplr API test..."

# Build the Docker image
echo "🔨 Building Docker image..."
docker build -t yapplr-api:local .

# Stop any existing containers
echo "🛑 Stopping existing containers..."
docker-compose down || true

# Start the services
echo "🚀 Starting services..."
docker-compose up -d

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 15

# Check if API is responding
echo "🔍 Testing API health..."
if curl -f http://localhost:8080/health > /dev/null 2>&1; then
    echo "✅ API is healthy and responding at http://localhost:8080"
    echo "📊 API Health: $(curl -s http://localhost:8080/health)"
else
    echo "❌ API health check failed"
    echo "📋 Checking logs..."
    docker-compose logs yapplr-api
    exit 1
fi

echo "🎉 Local test completed successfully!"
echo "🌐 API is running at: http://localhost:8080"
echo "📖 API Documentation: http://localhost:8080/swagger"
echo "🗄️ Database is running on: localhost:5432"

echo ""
echo "Useful commands:"
echo "  View logs: docker-compose logs -f"
echo "  Stop services: docker-compose down"
echo "  Restart: docker-compose restart"
