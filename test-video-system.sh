#!/bin/bash

echo "🎥 Testing Yapplr Video Processing System"
echo "========================================"

# Check if FFmpeg is installed
echo "1. Checking FFmpeg installation..."
if command -v ffmpeg &> /dev/null; then
    echo "✅ FFmpeg is installed"
    ffmpeg -version | head -1
else
    echo "❌ FFmpeg is not installed. Please install FFmpeg first:"
    echo "   macOS: brew install ffmpeg"
    echo "   Ubuntu: sudo apt install ffmpeg"
    echo "   Windows: Download from https://ffmpeg.org/download.html"
    exit 1
fi

if command -v ffprobe &> /dev/null; then
    echo "✅ FFprobe is available"
else
    echo "❌ FFprobe is not available"
    exit 1
fi

echo ""

# Check if directories exist
echo "2. Checking upload directories..."
mkdir -p uploads/videos
mkdir -p uploads/thumbnails
mkdir -p uploads/temp
echo "✅ Upload directories created/verified"

echo ""

# Build the projects
echo "3. Building projects..."
echo "Building API..."
if dotnet build Yapplr.Api --verbosity quiet; then
    echo "✅ API build successful"
else
    echo "❌ API build failed"
    exit 1
fi

echo "Building Video Processor..."
if dotnet build Yapplr.VideoProcessor --verbosity quiet; then
    echo "✅ Video Processor build successful"
else
    echo "❌ Video Processor build failed"
    exit 1
fi

echo ""

# Check database migration
echo "4. Checking database migration..."
cd Yapplr.Api
if dotnet ef migrations list | grep -q "AddVideoSupport"; then
    echo "✅ Video support migration exists"
else
    echo "❌ Video support migration not found"
    exit 1
fi
cd ..

echo ""

# Test video processing configuration
echo "5. Testing video processing configuration..."
cd Yapplr.VideoProcessor
if dotnet run --no-build -- --help &> /dev/null; then
    echo "✅ Video processor can start"
else
    echo "⚠️  Video processor may have configuration issues"
fi
cd ..

echo ""

echo "🎉 Video Processing System Test Complete!"
echo ""
echo "Next steps:"
echo "1. Apply database migration: cd Yapplr.Api && dotnet ef database update"
echo "2. Start the API: cd Yapplr.Api && dotnet run"
echo "3. Start the Video Processor: cd Yapplr.VideoProcessor && dotnet run"
echo "4. Test video upload via API endpoints"
echo ""
echo "Docker deployment:"
echo "docker-compose -f docker-compose.video.yml up --build"
echo ""
echo "API Endpoints for video:"
echo "- POST /api/videos/upload - Upload video"
echo "- GET /api/videos/{fileName} - Stream video"
echo "- GET /api/videos/thumbnails/{fileName} - Get thumbnail"
echo "- GET /api/videos/processing-status/{jobId} - Check processing status"
