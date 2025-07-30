#!/bin/bash

if [ $# -ne 1 ]; then
    echo "Usage: $0 <container_name>"
    exit 1
fi

container_name=$1

if [ $1 == "frontend" ]; then
    container_name="yapplr-frontend"
elif [ $1 == "api" ]; then
    container_name="yapplr-api"
elif [ $1 == "video" ]; then
    container_name="yapplr-video-processor"
elif [ $1 == "moderation" ]; then
    container_name="content-moderation"
elif [ $1 == "all" ]; then
    echo "🔄 Rebuilding all services..."
    echo "🛑 Stopping and removing all containers..."
    docker compose -f docker-compose.local.yml down -rmi local
    echo "📦 Building all containers..."
    docker compose -f docker-compose.local.yml build --no-cache
    echo "🚀 Restarting all services..."
    docker compose -f docker-compose.local.yml up -d
    echo "Pruning unused Docker resources..."
    docker system prune -f
    echo "✅ All services rebuilt and restarted!"
    exit 1
elif [ $1 == "mobile" ]; then
    echo "🔄 Rebuilding mobile app..."
    cd YapplrMobile
    npm install
    npm run build
    echo "✅ Mobile app rebuilt!"
    npx expo start
    exit 1
else
    echo "Invalid container name. Must be yapplr-frontend or yapplr-api"
    exit 1
fi

echo "🔄 Rebuilding $container_name with cache busting..."
# Build with no cache and current timestamp
echo "Stopping and removing existing $container_name container..."
docker compose -f docker-compose.local.yml stop $container_name
docker compose -f docker-compose.local.yml rm -f $container_name

echo "📦 Building $container_name container..."
docker compose -f docker-compose.local.yml build --no-cache $container_name

echo "🚀 Restarting $container_name service..."
docker compose -f docker-compose.local.yml up -d $container_name
echo "Pruning unused Docker resources..."
docker system prune -f
echo "✅ $container_name rebuilt and restarted!"


