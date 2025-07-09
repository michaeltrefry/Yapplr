#!/bin/bash

# Script to generate secure passwords for staging environment
# This script generates strong passwords that meet security requirements

echo "🔐 Generating secure passwords for staging environment..."
echo ""

# Generate a strong PostgreSQL password
# 24 characters with uppercase, lowercase, numbers, and special characters
POSTGRES_PASSWORD=$(openssl rand -base64 18 | tr -d "=+/" | cut -c1-24)
# Add some special characters for extra security
POSTGRES_PASSWORD="${POSTGRES_PASSWORD}@2024!"

# Generate a strong JWT secret key
# 64 characters for JWT secret (recommended minimum is 32)
JWT_SECRET=$(openssl rand -base64 48 | tr -d "=+/" | cut -c1-64)

echo "Generated secure passwords:"
echo "=========================="
echo ""
echo "PostgreSQL Password (STAGE_POSTGRES_PASSWORD):"
echo "$POSTGRES_PASSWORD"
echo ""
echo "JWT Secret Key (STAGE_JWT_SECRET_KEY):"
echo "$JWT_SECRET"
echo ""
echo "⚠️  IMPORTANT SECURITY NOTES:"
echo "1. Copy these passwords to your .env file"
echo "2. Never share these passwords or commit them to version control"
echo "3. Store them securely (password manager recommended)"
echo "4. Rotate them regularly for better security"
echo ""
echo "📝 To update your .env file:"
echo "   Edit .env and replace the placeholder values with the generated passwords above"
echo ""
