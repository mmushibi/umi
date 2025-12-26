#!/bin/bash
set -e

# Database initialization script
echo "Initializing database..."

# Wait for PostgreSQL to be fully ready
sleep 5

# Create additional databases if needed
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    -- Create additional schemas or databases if needed
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    
    -- You can add initial schema setup here
    -- Example: CREATE SCHEMA IF NOT EXISTS pharmacy;
    
    -- Grant permissions
    GRANT ALL PRIVILEGES ON DATABASE $POSTGRES_DB TO $POSTGRES_USER;
EOSQL

echo "Database initialization completed"
