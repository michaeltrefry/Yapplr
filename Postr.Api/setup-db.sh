#!/bin/bash

# Setup script for Postr API database

echo "Setting up Postr database..."

# Check if PostgreSQL is running
if ! pg_isready -h localhost -p 5432 > /dev/null 2>&1; then
    echo "PostgreSQL is not running. Please start PostgreSQL first."
    echo "On macOS with Homebrew: brew services start postgresql"
    echo "On Ubuntu: sudo systemctl start postgresql"
    exit 1
fi

# Create database if it doesn't exist
echo "Creating database 'postr_db' if it doesn't exist..."
createdb postr_db 2>/dev/null || echo "Database 'postr_db' already exists or could not be created."

# Run migrations
echo "Running Entity Framework migrations..."
dotnet ef database update

echo "Database setup complete!"
echo ""
echo "You can now run the API with: dotnet run"
echo "The API will be available at: https://localhost:7000 and http://localhost:5000"
