#!/bin/bash

# Script to rebuild frontend with cache busting
echo "🔄 Rebuilding frontend with cache busting..."

# Build with no cache and current timestamp
echo "📦 Building frontend container..."
docker-compose -f docker-compose.local.yml build --no-cache yapplr-frontend

# Restart the frontend service
echo "🚀 Restarting frontend service..."
docker-compose -f docker-compose.local.yml up -d yapplr-frontend

echo "✅ Frontend rebuilt and restarted!"
echo "💡 Hard refresh your browser (Cmd+Shift+R or Ctrl+Shift+R) to see changes"
