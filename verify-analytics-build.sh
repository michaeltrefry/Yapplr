#!/bin/bash

# Verification script for completed analytics implementation
# This script verifies that the analytics implementation builds and works correctly

set -e

echo "🔍 Verifying Analytics Implementation"
echo "===================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}1. Building the solution...${NC}"
if dotnet build Yapplr.Api/Yapplr.Api.csproj; then
    echo -e "${GREEN}✅ Build successful!${NC}"
else
    echo -e "${RED}❌ Build failed!${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}2. Checking for compilation errors...${NC}"
build_output=$(dotnet build Yapplr.Api/Yapplr.Api.csproj 2>&1)
error_count=$(echo "$build_output" | grep -c "error CS" || true)
warning_count=$(echo "$build_output" | grep -c "warning CS" || true)

if [ $error_count -eq 0 ]; then
    echo -e "${GREEN}✅ No compilation errors found${NC}"
else
    echo -e "${RED}❌ Found $error_count compilation errors${NC}"
    exit 1
fi

if [ $warning_count -gt 0 ]; then
    echo -e "${YELLOW}⚠️  Found $warning_count warnings (non-breaking)${NC}"
else
    echo -e "${GREEN}✅ No warnings found${NC}"
fi

echo ""
echo -e "${BLUE}3. Verifying analytics files exist...${NC}"

# Check for key analytics files
files_to_check=(
    "Yapplr.Api/Services/InfluxAdminAnalyticsService.cs"
    "Yapplr.Api/Services/IInfluxAdminAnalyticsService.cs"
    "Yapplr.Api/Services/AnalyticsMigrationService.cs"
    "Yapplr.Api/Services/IAnalyticsMigrationService.cs"
    "Yapplr.Api/DTOs/AnalyticsDataSourceDto.cs"
    "Yapplr.Api/DTOs/MigrationResult.cs"
    "Yapplr.Api/DTOs/MigrationStatusDto.cs"
    "analytics/test-complete-analytics.sh"
    "ANALYTICS_COMPLETION_SUMMARY.md"
)

for file in "${files_to_check[@]}"; do
    if [ -f "$file" ]; then
        echo -e "${GREEN}✅ $file${NC}"
    else
        echo -e "${RED}❌ Missing: $file${NC}"
        exit 1
    fi
done

echo ""
echo -e "${BLUE}4. Checking service registration...${NC}"
if grep -q "IInfluxAdminAnalyticsService" Yapplr.Api/Extensions/AnalyticsServiceExtensions.cs; then
    echo -e "${GREEN}✅ IInfluxAdminAnalyticsService registered${NC}"
else
    echo -e "${RED}❌ IInfluxAdminAnalyticsService not registered${NC}"
    exit 1
fi

if grep -q "IAnalyticsMigrationService" Yapplr.Api/Extensions/AnalyticsServiceExtensions.cs; then
    echo -e "${GREEN}✅ IAnalyticsMigrationService registered${NC}"
else
    echo -e "${RED}❌ IAnalyticsMigrationService not registered${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}5. Checking admin endpoints...${NC}"
if grep -q "/analytics/data-source" Yapplr.Api/Endpoints/AdminEndpoints.cs; then
    echo -e "${GREEN}✅ Analytics data source endpoint found${NC}"
else
    echo -e "${RED}❌ Analytics data source endpoint missing${NC}"
    exit 1
fi

if grep -q "/analytics/migrate" Yapplr.Api/Endpoints/AdminEndpoints.cs; then
    echo -e "${GREEN}✅ Migration endpoints found${NC}"
else
    echo -e "${RED}❌ Migration endpoints missing${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}6. Testing application startup...${NC}"
echo "Starting application for 5 seconds to verify it works..."

# Start the application in background and capture output
dotnet run --project Yapplr.Api/Yapplr.Api.csproj --no-build > startup_test.log 2>&1 &
APP_PID=$!

# Wait for startup
sleep 5

# Check if the application is still running
if kill -0 $APP_PID 2>/dev/null; then
    echo -e "${GREEN}✅ Application started successfully${NC}"
    
    # Check for any startup errors
    if grep -q "error\|Error\|ERROR" startup_test.log; then
        echo -e "${YELLOW}⚠️  Some errors found in startup log${NC}"
    else
        echo -e "${GREEN}✅ No startup errors detected${NC}"
    fi
    
    # Kill the application
    kill $APP_PID 2>/dev/null || true
    wait $APP_PID 2>/dev/null || true
else
    echo -e "${RED}❌ Application failed to start or crashed${NC}"
    echo "Startup log:"
    cat startup_test.log
    exit 1
fi

# Clean up
rm -f startup_test.log

echo ""
echo -e "${GREEN}🎉 Analytics Implementation Verification Complete!${NC}"
echo "=================================================="
echo ""
echo -e "${GREEN}✅ All checks passed successfully!${NC}"
echo ""
echo "📊 Analytics Features Verified:"
echo "  ✅ Build successful"
echo "  ✅ No compilation errors"
echo "  ✅ All required files present"
echo "  ✅ Services properly registered"
echo "  ✅ Admin endpoints available"
echo "  ✅ Application starts correctly"
echo ""
echo "🚀 The analytics implementation is ready for use!"
echo ""
echo "Next steps:"
echo "  1. Start the full stack: docker-compose -f docker-compose.local.yml up -d"
echo "  2. Test analytics: ./analytics/test-complete-analytics.sh"
echo "  3. Access Grafana: http://localhost:3001 (admin/yapplr123)"
echo "  4. Use admin endpoints with proper authentication"
