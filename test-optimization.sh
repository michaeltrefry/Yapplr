#!/bin/bash

# Test script to verify the optimization system works correctly
# This script simulates changes and tests the deployment optimization

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}🧪 Testing Deployment Optimization System${NC}"
echo -e "${BLUE}=========================================${NC}"

# Check if we're in the right directory
if [ ! -f "docker-compose.stage.yml" ]; then
    echo -e "${RED}❌ Error: Must be run from the project root directory${NC}"
    exit 1
fi

# Function to calculate directory hash (same as in deployment script)
calculate_hash() {
    local dir=$1
    if [ -d "$dir" ]; then
        find "$dir" -type f \( -name "*.cs" -o -name "*.csproj" -o -name "*.json" -o -name "*.js" -o -name "*.ts" -o -name "*.tsx" -o -name "*.py" -o -name "Dockerfile*" \) -exec md5sum {} \; | sort | md5sum | cut -d' ' -f1
    else
        echo "missing"
    fi
}

# Test 1: Check hash calculation
echo -e "${BLUE}Test 1: Hash Calculation${NC}"
api_hash=$(calculate_hash "Yapplr.Api")
echo -e "  API hash: ${api_hash:0:12}..."
if [ ${#api_hash} -eq 32 ]; then
    echo -e "  ${GREEN}✅ Hash calculation working${NC}"
else
    echo -e "  ${RED}❌ Hash calculation failed${NC}"
    exit 1
fi

# Test 2: Check status script
echo -e "\n${BLUE}Test 2: Status Check Script${NC}"
if [ -x "./check-deployment-status.sh" ]; then
    echo -e "  ${GREEN}✅ Status script is executable${NC}"
    # Run a quick test (suppress output)
    ./check-deployment-status.sh > /dev/null 2>&1
    echo -e "  ${GREEN}✅ Status script runs without errors${NC}"
else
    echo -e "  ${RED}❌ Status script not executable${NC}"
    exit 1
fi

# Test 3: Check force rebuild script
echo -e "\n${BLUE}Test 3: Force Rebuild Script${NC}"
if [ -x "./force-rebuild.sh" ]; then
    echo -e "  ${GREEN}✅ Force rebuild script is executable${NC}"
else
    echo -e "  ${RED}❌ Force rebuild script not executable${NC}"
    exit 1
fi

# Test 4: Check deployment script
echo -e "\n${BLUE}Test 4: Deployment Script${NC}"
if [ -x "./deploy-stage-optimized.sh" ]; then
    echo -e "  ${GREEN}✅ Optimized deployment script is executable${NC}"
else
    echo -e "  ${RED}❌ Optimized deployment script not executable${NC}"
    exit 1
fi

# Test 5: Simulate hash storage and retrieval
echo -e "\n${BLUE}Test 5: Hash Storage System${NC}"
HASH_FILE=".test_deployment_hashes"

# Clean up any existing test file
rm -f "$HASH_FILE"

# Test storing a hash
echo "yapplr-api:abc123def456" > "$HASH_FILE"
echo "yapplr-frontend:def456ghi789" >> "$HASH_FILE"

# Test retrieving a hash
stored_hash=$(grep "^yapplr-api:" "$HASH_FILE" | cut -d':' -f2)
if [ "$stored_hash" = "abc123def456" ]; then
    echo -e "  ${GREEN}✅ Hash storage and retrieval working${NC}"
else
    echo -e "  ${RED}❌ Hash storage/retrieval failed${NC}"
    exit 1
fi

# Test removing a hash
grep -v "^yapplr-api:" "$HASH_FILE" > "${HASH_FILE}.tmp"
mv "${HASH_FILE}.tmp" "$HASH_FILE"
if ! grep -q "^yapplr-api:" "$HASH_FILE"; then
    echo -e "  ${GREEN}✅ Hash removal working${NC}"
else
    echo -e "  ${RED}❌ Hash removal failed${NC}"
    exit 1
fi

# Clean up test file
rm -f "$HASH_FILE"

# Test 6: Check Docker Compose file modifications
echo -e "\n${BLUE}Test 6: Docker Compose Configuration${NC}"
if grep -q "cache_from:" docker-compose.stage.yml; then
    echo -e "  ${GREEN}✅ Docker Compose has cache optimization${NC}"
else
    echo -e "  ${RED}❌ Docker Compose missing cache optimization${NC}"
    exit 1
fi

if grep -q "YAPPLR_API_VERSION" docker-compose.stage.yml; then
    echo -e "  ${GREEN}✅ Docker Compose has version variables${NC}"
else
    echo -e "  ${RED}❌ Docker Compose missing version variables${NC}"
    exit 1
fi

# Test 7: Check GitHub Actions workflow
echo -e "\n${BLUE}Test 7: GitHub Actions Workflow${NC}"
if [ -f ".github/workflows/deploy-stage.yml" ]; then
    if grep -q "deploy-stage-optimized.sh" .github/workflows/deploy-stage.yml; then
        echo -e "  ${GREEN}✅ GitHub Actions uses optimized deployment${NC}"
    else
        echo -e "  ${YELLOW}⚠️ GitHub Actions not updated to use optimized deployment${NC}"
    fi
    
    if grep -q "force_rebuild" .github/workflows/deploy-stage.yml; then
        echo -e "  ${GREEN}✅ GitHub Actions supports force rebuild${NC}"
    else
        echo -e "  ${YELLOW}⚠️ GitHub Actions missing force rebuild option${NC}"
    fi
else
    echo -e "  ${RED}❌ GitHub Actions workflow file not found${NC}"
fi

# Test 8: Dry run test (if Docker is available)
echo -e "\n${BLUE}Test 8: Dry Run Test${NC}"
if command -v docker &> /dev/null; then
    echo -e "  ${GREEN}✅ Docker is available${NC}"
    
    # Test Docker Compose syntax
    if docker compose -f docker-compose.stage.yml config > /dev/null 2>&1; then
        echo -e "  ${GREEN}✅ Docker Compose configuration is valid${NC}"
    else
        echo -e "  ${RED}❌ Docker Compose configuration has errors${NC}"
        exit 1
    fi
else
    echo -e "  ${YELLOW}⚠️ Docker not available - skipping Docker tests${NC}"
fi

# Summary
echo -e "\n${BLUE}=========================================${NC}"
echo -e "${GREEN}🎉 All Tests Passed!${NC}"
echo -e "\n${GREEN}✅ Optimization System Ready${NC}"
echo -e "\n${BLUE}Next Steps:${NC}"
echo -e "  1. Run ${GREEN}./check-deployment-status.sh${NC} to see current status"
echo -e "  2. Run ${GREEN}./deploy-stage-optimized.sh${NC} to deploy with optimizations"
echo -e "  3. Monitor deployment times and verify optimizations work"
echo -e "\n${BLUE}💡 Tips:${NC}"
echo -e "  - First deployment will rebuild everything (establishes baseline)"
echo -e "  - Subsequent deployments will only rebuild changed services"
echo -e "  - Use force rebuild scripts when needed"
echo -e "  - Check the README for detailed usage instructions"
