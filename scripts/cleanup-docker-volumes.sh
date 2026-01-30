#!/bin/bash

# Umi Health Docker Volumes Cleanup Script
# This script removes Docker volumes and containers for Umi Health

echo "Starting Docker cleanup for Umi Health..."

# Function to check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        echo "Error: Docker is not running"
        exit 1
    fi
}

# Function to remove containers
remove_containers() {
    echo "Stopping and removing Umi Health containers..."
    
    # Stop containers
    docker stop umihealth-api-gateway 2>/dev/null || true
    docker stop umihealth-api 2>/dev/null || true
    docker stop umihealth-identity 2>/dev/null || true
    docker stop umihealth-postgres 2>/dev/null || true
    docker stop umihealth-redis 2>/dev/null || true
    docker stop umihealth-jobs 2>/dev/null || true
    docker stop umihealth-prometheus 2>/dev/null || true
    docker stop umihealth-grafana 2>/dev/null || true
    docker stop umihealth-nginx 2>/dev/null || true
    
    # Remove containers
    docker rm umihealth-api-gateway 2>/dev/null || true
    docker rm umihealth-api 2>/dev/null || true
    docker rm umihealth-identity 2>/dev/null || true
    docker rm umihealth-postgres 2>/dev/null || true
    docker rm umihealth-redis 2>/dev/null || true
    docker rm umihealth-jobs 2>/dev/null || true
    docker rm umihealth-prometheus 2>/dev/null || true
    docker rm umihealth-grafana 2>/dev/null || true
    docker rm umihealth-nginx 2>/dev/null || true
    
    echo "✅ Containers removed"
}

# Function to remove volumes
remove_volumes() {
    echo "Removing Docker volumes..."
    
    # Remove named volumes
    docker volume rm umihealth_postgres_data 2>/dev/null || true
    docker volume rm umihealth_redis_data 2>/dev/null || true
    docker volume rm umihealth_prometheus_data 2>/dev/null || true
    docker volume rm umihealth_grafana_data 2>/dev/null || true
    
    # Remove any anonymous volumes
    docker volume prune -f
    
    echo "✅ Volumes removed"
}

# Function to remove networks
remove_networks() {
    echo "Removing Docker networks..."
    
    docker network rm umihealth_umihealth-network 2>/dev/null || true
    docker network prune -f
    
    echo "✅ Networks removed"
}

# Function to remove images (optional)
remove_images() {
    if [ "$1" = "--remove-images" ]; then
        echo "Removing Umi Health Docker images..."
        
        docker rmi umihealth/api-gateway:latest 2>/dev/null || true
        docker rmi umihealth/api:latest 2>/dev/null || true
        docker rmi umihealth/identity:latest 2>/dev/null || true
        docker rmi umihealth/jobs:latest 2>/dev/null || true
        
        echo "✅ Images removed"
    fi
}

# Function to clean up system
cleanup_system() {
    echo "Performing system cleanup..."
    
    # Remove unused containers, networks, images (both dangling and unreferenced)
    docker system prune -f
    
    # Remove unused volumes (including anonymous volumes)
    docker volume prune -f
    
    echo "✅ System cleanup completed"
}

# Main execution
check_docker

# Ask for confirmation
echo "This will remove ALL Umi Health Docker containers, volumes, and data."
read -p "Are you sure you want to continue? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cleanup cancelled"
    exit 0
fi

remove_containers
remove_volumes
remove_networks
remove_images "$1"
cleanup_system

echo ""
echo "🎉 Umi Health Docker cleanup completed!"
echo "All containers, volumes, and networks have been removed."
echo ""
echo "To restart the application, run:"
echo "  docker-compose up -d"
echo ""
