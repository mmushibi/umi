#!/bin/bash

# Umi Health Redis Cleanup Script
# This script clears all Redis cache data

REDIS_HOST=${REDIS_HOST:-localhost}
REDIS_PORT=${REDIS_PORT:-6379}
REDIS_PASSWORD=${REDIS_PASSWORD:-}

echo "Starting Redis cleanup..."

# Function to execute Redis command
redis_cmd() {
    if [ -n "$REDIS_PASSWORD" ]; then
        redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" -a "$REDIS_PASSWORD" "$@"
    else
        redis-cli -h "$REDIS_HOST" -p "$REDIS_PORT" "$@"
    fi
}

# Check Redis connection
if ! redis_cmd ping > /dev/null 2>&1; then
    echo "Error: Cannot connect to Redis at $REDIS_HOST:$REDIS_PORT"
    exit 1
fi

echo "Connected to Redis successfully"

# Get database info before cleanup
echo "Redis info before cleanup:"
redis_cmd info keyspace

# Flush all databases (this will delete ALL data)
echo "Flushing all Redis databases..."
redis_cmd flushall

# Alternative: Flush only current database
# redis_cmd flushdb

# Clear specific patterns if needed (uncomment if you want selective cleanup)
# echo "Clearing specific patterns..."
# redis_cmd --scan --pattern "umihealth:*" | xargs redis_cmd del
# redis_cmd --scan --pattern "session:*" | xargs redis_cmd del
# redis_cmd --scan --pattern "cache:*" | xargs redis_cmd del

# Verify cleanup
echo "Verifying cleanup..."
db_count=$(redis_cmd info keyspace | grep -c "db" || echo "0")
echo "Number of databases with keys: $db_count"

if [ "$db_count" -eq 0 ]; then
    echo "✅ Redis cleanup completed successfully - all databases are empty"
else
    echo "⚠️  Some databases still contain keys"
    redis_cmd info keyspace
fi

# Restart Redis if running in Docker (optional)
if command -v docker > /dev/null 2>&1; then
    redis_container=$(docker ps -q --filter "name=redis" --filter "name=umihealth")
    if [ -n "$redis_container" ]; then
        echo "Restarting Redis container..."
        docker restart "$redis_container"
        echo "Redis container restarted"
    fi
fi

echo "Redis cleanup completed!"
