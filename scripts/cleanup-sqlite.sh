#!/bin/bash

# Umi Health SQLite Cleanup Script
# This script cleans SQLite database files

echo "Starting SQLite cleanup..."

# Find and remove SQLite database files
SQLITE_FILES=(
    "./UmiHealth.MinimalApi/bin/Debug/net8.0/UmiHealth.db"
    "./UmiHealth.MinimalApi/bin/Release/net8.0/UmiHealth.db"
    "./UmiHealth.MinimalApi/UmiHealth.db"
    "./data/UmiHealth.db"
    "./app_data/UmiHealth.db"
    "./UmiHealth.db"
)

# Remove SQLite files
for db_file in "${SQLITE_FILES[@]}"; do
    if [ -f "$db_file" ]; then
        echo "Removing SQLite database: $db_file"
        rm -f "$db_file"
        rm -f "$db_file-journal" 2>/dev/null || true
        rm -f "$db_file-wal" 2>/dev/null || true
        rm -f "$db_file-shm" 2>/dev/null || true
    fi
done

# Find any other SQLite files in the project
echo "Searching for additional SQLite files..."
find . -name "*.db" -type f ! -path "./node_modules/*" ! -path "./.git/*" | while read file; do
    echo "Found SQLite file: $file"
    read -p "Remove this file? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        rm -f "$file"
        rm -f "$file-journal" 2>/dev/null || true
        rm -f "$file-wal" 2>/dev/null || true
        rm -f "$file-shm" 2>/dev/null || true
        echo "✅ Removed: $file"
    fi
done

# Clean connection string files that might contain cached data
echo "Cleaning connection string cache files..."
find . -name "*.cache" -type f ! -path "./node_modules/*" ! -path "./.git/*" -delete 2>/dev/null || true

echo "✅ SQLite cleanup completed!"
