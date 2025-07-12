#!/bin/bash

# Video Processor Codec Compatibility Test Script
# This script verifies that all required codecs are available in the Docker container

set -e

echo "🎬 Testing Video Processor Codec Compatibility"
echo "=============================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test FFmpeg installation
echo -e "\n${BLUE}1. Testing FFmpeg Installation${NC}"
if command -v ffmpeg &> /dev/null; then
    echo -e "${GREEN}✓ FFmpeg is installed${NC}"
    ffmpeg -version | head -1
else
    echo -e "${RED}✗ FFmpeg is not installed${NC}"
    exit 1
fi

# Test video codecs
echo -e "\n${BLUE}2. Testing Video Codecs${NC}"
video_codecs=("libx264" "libx265" "libvpx-vp9" "libvpx")

for codec in "${video_codecs[@]}"; do
    if ffmpeg -encoders 2>/dev/null | grep -q "$codec"; then
        echo -e "${GREEN}✓ Video codec $codec is available${NC}"
    else
        echo -e "${YELLOW}⚠ Video codec $codec is not available${NC}"
    fi
done

# Test audio codecs
echo -e "\n${BLUE}3. Testing Audio Codecs${NC}"
audio_codecs=("aac" "libmp3lame" "libvorbis" "libopus")

for codec in "${audio_codecs[@]}"; do
    if ffmpeg -encoders 2>/dev/null | grep -q "$codec"; then
        echo -e "${GREEN}✓ Audio codec $codec is available${NC}"
    else
        echo -e "${YELLOW}⚠ Audio codec $codec is not available${NC}"
    fi
done

# Test input format support
echo -e "\n${BLUE}4. Testing Input Format Support${NC}"
input_formats=("mp4" "avi" "mov" "mkv" "webm" "flv" "wmv" "m4v" "3gp" "ogv")

for format in "${input_formats[@]}"; do
    if ffmpeg -formats 2>/dev/null | grep -q " $format "; then
        echo -e "${GREEN}✓ Input format $format is supported${NC}"
    else
        echo -e "${YELLOW}⚠ Input format $format is not supported${NC}"
    fi
done

# Test basic video processing with a sample
echo -e "\n${BLUE}5. Testing Basic Video Processing${NC}"

# Create a test video (1 second, 320x240, solid color)
echo -e "${BLUE}Creating test video...${NC}"
ffmpeg -f lavfi -i testsrc=duration=1:size=320x240:rate=1 -c:v libx264 -t 1 /tmp/test_input.mp4 -y &>/dev/null

if [ -f "/tmp/test_input.mp4" ]; then
    echo -e "${GREEN}✓ Test video created successfully${NC}"
    
    # Test video processing
    echo -e "${BLUE}Testing video processing...${NC}"
    ffmpeg -i /tmp/test_input.mp4 -c:v libx264 -c:a aac -vf scale=160:120 /tmp/test_output.mp4 -y &>/dev/null
    
    if [ -f "/tmp/test_output.mp4" ]; then
        echo -e "${GREEN}✓ Video processing test successful${NC}"
        
        # Test thumbnail generation
        echo -e "${BLUE}Testing thumbnail generation...${NC}"
        ffmpeg -i /tmp/test_input.mp4 -vf scale=80:60 -vframes 1 /tmp/test_thumb.jpg -y &>/dev/null
        
        if [ -f "/tmp/test_thumb.jpg" ]; then
            echo -e "${GREEN}✓ Thumbnail generation test successful${NC}"
        else
            echo -e "${RED}✗ Thumbnail generation test failed${NC}"
        fi
    else
        echo -e "${RED}✗ Video processing test failed${NC}"
    fi
    
    # Cleanup test files
    rm -f /tmp/test_input.mp4 /tmp/test_output.mp4 /tmp/test_thumb.jpg
else
    echo -e "${RED}✗ Failed to create test video${NC}"
fi

# Test hardware acceleration availability (optional)
echo -e "\n${BLUE}6. Testing Hardware Acceleration (Optional)${NC}"
hw_accels=("vaapi" "vdpau" "qsv" "nvenc")

for hw in "${hw_accels[@]}"; do
    if ffmpeg -hwaccels 2>/dev/null | grep -q "$hw"; then
        echo -e "${GREEN}✓ Hardware acceleration $hw is available${NC}"
    else
        echo -e "${YELLOW}⚠ Hardware acceleration $hw is not available (optional)${NC}"
    fi
done

echo -e "\n${GREEN}🎉 Codec compatibility test completed!${NC}"
echo -e "${BLUE}The video processor should work correctly with the available codecs.${NC}"
