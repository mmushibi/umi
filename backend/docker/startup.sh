#!/bin/bash
set -e

# Startup script for unified container
echo "Starting UmiHealth Unified Container..."

# Start PostgreSQL in background
echo "Starting PostgreSQL..."
docker-entrypoint.sh postgres &

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to start..."
until pg_isready -h localhost -p 5432 -U "$POSTGRES_USER"
do
  echo "PostgreSQL is unavailable - sleeping"
  sleep 2
done

echo "PostgreSQL is ready - initializing database..."
/app/scripts/init-db.sh

echo "Starting API..."
cd /app/api
dotnet UmiHealth.Api.dll
