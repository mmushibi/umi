#!/bin/bash
set -e

# Health check script for both services
echo "Performing health checks..."

# Check PostgreSQL
if ! pg_isready -h localhost -p 5432 -U "$POSTGRES_USER"; then
    echo "PostgreSQL health check failed"
    exit 1
fi

# Check API
if ! curl -f http://localhost:8080/health >/dev/null 2>&1; then
    echo "API health check failed"
    exit 1
fi

echo "Both services are healthy"
exit 0
