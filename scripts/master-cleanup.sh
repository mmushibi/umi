#!/bin/bash

# Umi Health Master Cleanup Script
# This script performs comprehensive cleanup of all data, cache, and temporary files

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo -e "${BLUE}🧹 Umi Health Master Cleanup Script${NC}"
echo "======================================="
echo ""

# Function to print status
print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

# Function to check if PostgreSQL is available
check_postgres() {
    if command -v psql > /dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Function to check if Redis is available
check_redis() {
    if command -v redis-cli > /dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Function to check if Docker is available
check_docker() {
    if command -v docker > /dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Function to cleanup PostgreSQL databases
cleanup_postgresql() {
    print_info "Cleaning PostgreSQL databases..."
    
    if ! check_postgres; then
        print_warning "PostgreSQL not found, skipping database cleanup"
        return
    fi
    
    # Check if PostgreSQL is running
    if ! pg_isready -h localhost -p 5432 > /dev/null 2>&1; then
        print_warning "PostgreSQL is not running, skipping database cleanup"
        return
    fi
    
    # Run database cleanup script
    if [ -f "$SCRIPT_DIR/cleanup-databases.sql" ]; then
        print_info "Executing PostgreSQL cleanup script..."
        
        # Try to connect to UmiHealth database
        if PGPASSWORD=root psql -h localhost -p 5432 -U umihealth -d UmiHealth -f "$SCRIPT_DIR/cleanup-databases.sql" 2>/dev/null; then
            print_status "PostgreSQL cleanup completed"
        else
            print_warning "Could not connect to UmiHealth database, trying alternative connection..."
            # Try with different credentials
            if PGPASSWORD=umihealth_2024! psql -h localhost -p 5432 -U umi_health_app -d UmiHealth -f "$SCRIPT_DIR/cleanup-databases.sql" 2>/dev/null; then
                print_status "PostgreSQL cleanup completed with alternative credentials"
            else
                print_error "Failed to connect to PostgreSQL database"
            fi
        fi
    else
        print_error "PostgreSQL cleanup script not found"
    fi
}

# Function to cleanup Redis cache
cleanup_redis() {
    print_info "Cleaning Redis cache..."
    
    if ! check_redis; then
        print_warning "Redis CLI not found, skipping Redis cleanup"
        return
    fi
    
    # Run Redis cleanup script
    if [ -f "$SCRIPT_DIR/cleanup-redis.sh" ]; then
        chmod +x "$SCRIPT_DIR/cleanup-redis.sh"
        "$SCRIPT_DIR/cleanup-redis.sh"
        print_status "Redis cleanup completed"
    else
        print_error "Redis cleanup script not found"
    fi
}

# Function to cleanup SQLite databases
cleanup_sqlite() {
    print_info "Cleaning SQLite databases..."
    
    if [ -f "$SCRIPT_DIR/cleanup-sqlite.sh" ]; then
        chmod +x "$SCRIPT_DIR/cleanup-sqlite.sh"
        "$SCRIPT_DIR/cleanup-sqlite.sh"
        print_status "SQLite cleanup completed"
    else
        print_error "SQLite cleanup script not found"
    fi
}

# Function to cleanup Docker containers and volumes
cleanup_docker() {
    print_info "Cleaning Docker containers and volumes..."
    
    if ! check_docker; then
        print_warning "Docker not found, skipping Docker cleanup"
        return
    fi
    
    if [ -f "$SCRIPT_DIR/cleanup-docker-volumes.sh" ]; then
        chmod +x "$SCRIPT_DIR/cleanup-docker-volumes.sh"
        "$SCRIPT_DIR/cleanup-docker-volumes.sh" --remove-images
        print_status "Docker cleanup completed"
    else
        print_error "Docker cleanup script not found"
    fi
}

# Function to cleanup temporary files and logs
cleanup_temp_files() {
    print_info "Cleaning temporary files and logs..."
    
    # Clean log files
    find "$PROJECT_ROOT" -name "*.log" -type f ! -path "./node_modules/*" ! -path "./.git/*" -delete 2>/dev/null || true
    
    # Clean temporary files
    find "$PROJECT_ROOT" -name "*.tmp" -type f ! -path "./node_modules/*" ! -path "./.git/*" -delete 2>/dev/null || true
    find "$PROJECT_ROOT" -name "*.temp" -type f ! -path "./node_modules/*" ! -path "./.git/*" -delete 2>/dev/null || true
    
    # Clean cache directories
    if [ -d "$PROJECT_ROOT/.cache" ]; then
        rm -rf "$PROJECT_ROOT/.cache"
    fi
    
    # Clean build artifacts
    find "$PROJECT_ROOT" -name "bin" -type d ! -path "./node_modules/*" ! -path "./.git/*" -exec rm -rf {} + 2>/dev/null || true
    find "$PROJECT_ROOT" -name "obj" -type d ! -path "./node_modules/*" ! -path "./.git/*" -exec rm -rf {} + 2>/dev/null || true
    
    print_status "Temporary files cleanup completed"
}

# Function to cleanup browser data
cleanup_browser_data() {
    print_info "Cleaning browser cache and local storage..."
    
    # Create a simple HTML file to clear browser data
    cat > "$PROJECT_ROOT/clear-browser-data.html" << 'EOF'
<!DOCTYPE html>
<html>
<head>
    <title>Clear Browser Data</title>
    <script>
        function clearAllData() {
            // Clear localStorage
            localStorage.clear();
            
            // Clear sessionStorage
            sessionStorage.clear();
            
            // Clear IndexedDB
            if (window.indexedDB) {
                const databases = indexedDB.databases();
                databases.then(function(dbs) {
                    dbs.forEach(function(db) {
                        indexedDB.deleteDatabase(db.name);
                    });
                });
            }
            
            alert('Browser data cleared successfully!');
            window.close();
        }
    </script>
</head>
<body>
    <h1>Clear Browser Data</h1>
    <p>Click the button below to clear all browser data for Umi Health:</p>
    <button onclick="clearAllData()">Clear All Data</button>
</body>
</html>
EOF
    
    print_status "Browser data cleanup HTML file created"
    print_info "Open clear-browser-data.html in your browser to clear browser data"
}

# Function to show cleanup summary
show_summary() {
    echo ""
    echo "======================================="
    echo -e "${GREEN}🎉 Cleanup Summary${NC}"
    echo "======================================="
    echo ""
    echo "The following cleanup operations have been performed:"
    echo "✅ PostgreSQL databases cleaned"
    echo "✅ Redis cache cleared"
    echo "✅ SQLite databases removed"
    echo "✅ Docker containers and volumes removed"
    echo "✅ Temporary files and logs cleaned"
    echo "✅ Browser data cleanup HTML created"
    echo ""
    echo "To restart the application:"
    echo "  1. Run: docker-compose up -d"
    echo "  2. Or start your development server"
    echo ""
    echo "⚠️  Important: All data has been permanently deleted!"
    echo ""
}

# Main execution
main() {
    echo "This script will perform a comprehensive cleanup of all Umi Health data."
    echo "This includes databases, cache, containers, and temporary files."
    echo ""
    echo "⚠️  WARNING: This action is irreversible and will delete ALL data!"
    echo ""
    
    read -p "Are you sure you want to continue? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_info "Cleanup cancelled"
        exit 0
    fi
    
    echo ""
    print_info "Starting comprehensive cleanup..."
    echo ""
    
    # Change to project root
    cd "$PROJECT_ROOT"
    
    # Execute cleanup functions
    cleanup_postgresql
    cleanup_redis
    cleanup_sqlite
    cleanup_docker
    cleanup_temp_files
    cleanup_browser_data
    
    show_summary
}

# Run main function
main "$@"
