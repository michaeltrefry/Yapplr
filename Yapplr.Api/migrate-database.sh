#!/bin/bash

# Migration script to rename database from postr_db to yapplr_db
# This script helps users migrate from the old database name to the new one

echo "Yapplr Database Migration Script"
echo "================================"
echo ""
echo "This script will help you migrate from 'postr_db' to 'yapplr_db'"
echo ""

# Check if PostgreSQL is running
if ! pg_isready -h localhost -p 5432 > /dev/null 2>&1; then
    echo "PostgreSQL is not running. Please start PostgreSQL first."
    echo "On macOS with Homebrew: brew services start postgresql"
    echo "On Ubuntu: sudo systemctl start postgresql"
    exit 1
fi

# Check if old database exists
if psql -lqt | cut -d \| -f 1 | grep -qw postr_db; then
    echo "Found existing 'postr_db' database."
    echo ""
    echo "Options:"
    echo "1. Rename 'postr_db' to 'yapplr_db' (preserves all data)"
    echo "2. Create new 'yapplr_db' and keep 'postr_db' (fresh start)"
    echo "3. Exit and handle manually"
    echo ""
    read -p "Choose option (1/2/3): " choice
    
    case $choice in
        1)
            echo "Renaming database from 'postr_db' to 'yapplr_db'..."
            
            # Terminate connections to the database
            psql -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'postr_db';" postgres
            
            # Rename the database
            psql -c "ALTER DATABASE postr_db RENAME TO yapplr_db;" postgres
            
            if [ $? -eq 0 ]; then
                echo "✅ Database successfully renamed to 'yapplr_db'"
                echo "Your existing data has been preserved."
            else
                echo "❌ Failed to rename database. Please check for active connections."
                exit 1
            fi
            ;;
        2)
            echo "Creating new 'yapplr_db' database..."
            createdb yapplr_db 2>/dev/null || echo "Database 'yapplr_db' already exists."
            
            echo "Running migrations on new database..."
            dotnet ef database update
            
            echo "✅ New 'yapplr_db' database created."
            echo "Note: Your old 'postr_db' database still exists and can be removed manually if desired."
            ;;
        3)
            echo "Exiting. No changes made."
            exit 0
            ;;
        *)
            echo "Invalid option. Exiting."
            exit 1
            ;;
    esac
else
    echo "No existing 'postr_db' database found."
    echo "Creating new 'yapplr_db' database..."
    createdb yapplr_db 2>/dev/null || echo "Database 'yapplr_db' already exists."
    
    echo "Running migrations..."
    dotnet ef database update
    
    echo "✅ New 'yapplr_db' database created and configured."
fi

echo ""
echo "Database migration complete!"
echo ""
echo "Next steps:"
echo "1. Update your connection string to use 'yapplr_db'"
echo "2. Run the API with: dotnet run"
echo "3. The API will be available at: https://localhost:7000 and http://localhost:5000"
