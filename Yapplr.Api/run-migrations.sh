#!/bin/bash

# Standalone Database Migration Script for Yapplr
# This script applies pending database migrations to production

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🗄️ Yapplr Database Migration Script${NC}"

# Check if .env file exists
if [ ! -f .env ]; then
    echo -e "${RED}❌ Error: .env file not found${NC}"
    echo -e "${YELLOW}Please ensure you're in the Yapplr.Api directory and .env file exists${NC}"
    exit 1
fi

# Load environment variables
source .env

# Validate required environment variables
if [ -z "$DATABASE_CONNECTION_STRING" ]; then
    echo -e "${RED}❌ Error: DATABASE_CONNECTION_STRING not set in .env${NC}"
    exit 1
fi

echo -e "${GREEN}📋 Checking current migration status...${NC}"

# Check current migrations
echo -e "${YELLOW}Current migrations in codebase:${NC}"
docker run --rm \
  -v $(pwd):/app \
  -w /app \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  sh -c "dotnet tool install --global dotnet-ef > /dev/null 2>&1 && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef migrations list"

echo -e "${YELLOW}Checking database migration history...${NC}"
docker run --rm \
  -v $(pwd):/app \
  -w /app \
  -e "ConnectionStrings__DefaultConnection=${DATABASE_CONNECTION_STRING}" \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  sh -c "dotnet tool install --global dotnet-ef > /dev/null 2>&1 && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef migrations list --connection \"${DATABASE_CONNECTION_STRING}\"" || echo "Could not connect to database or no migrations applied yet"

echo -e "${GREEN}🚀 Applying database migrations...${NC}"
echo -e "${YELLOW}Using connection string: ${DATABASE_CONNECTION_STRING}${NC}"

# Apply migrations
echo -e "${YELLOW}Running migration command...${NC}"
if docker run --rm \
  -v $(pwd):/app \
  -w /app \
  -e "ConnectionStrings__DefaultConnection=${DATABASE_CONNECTION_STRING}" \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  sh -c "dotnet tool install --global dotnet-ef > /dev/null 2>&1 && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef database update"; then
  echo -e "${GREEN}✅ Database migrations completed successfully!${NC}"
else
  echo -e "${RED}❌ Database migrations failed!${NC}"
  echo -e "${YELLOW}Trying with explicit connection string parameter...${NC}"

  # Try with explicit connection string parameter as fallback
  if docker run --rm \
    -v $(pwd):/app \
    -w /app \
    mcr.microsoft.com/dotnet/sdk:9.0 \
    sh -c "dotnet tool install --global dotnet-ef > /dev/null 2>&1 && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef database update --connection \"${DATABASE_CONNECTION_STRING}\""; then
    echo -e "${GREEN}✅ Database migrations completed successfully with explicit connection!${NC}"
  else
    echo -e "${RED}❌ Database migrations failed completely!${NC}"
    echo -e "${YELLOW}Please check the error messages above and verify:${NC}"
    echo -e "${YELLOW}  1. Database connection string is correct${NC}"
    echo -e "${YELLOW}  2. Database server is accessible${NC}"
    echo -e "${YELLOW}  3. Database user has sufficient permissions${NC}"
    echo -e "${YELLOW}  4. Database exists and is accessible${NC}"
    exit 1
  fi
fi

echo -e "${GREEN}📋 Verifying migration status...${NC}"

# Verify migrations were applied
docker run --rm \
  -v $(pwd):/app \
  -w /app \
  -e "ConnectionStrings__DefaultConnection=${DATABASE_CONNECTION_STRING}" \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  sh -c "dotnet tool install --global dotnet-ef > /dev/null 2>&1 && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef migrations list --connection \"${DATABASE_CONNECTION_STRING}\""

echo -e "${GREEN}🎉 Migration script completed!${NC}"
echo -e "${GREEN}The EmailVerification migration should now be applied to your database.${NC}"
