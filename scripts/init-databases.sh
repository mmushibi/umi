#!/bin/bash
set -e

# Function to create a new database
create_database() {
    local db_name=$1
    echo "Creating database: $db_name"
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
        CREATE DATABASE $db_name;
        GRANT ALL PRIVILEGES ON DATABASE $db_name TO $POSTGRES_USER;
EOSQL
    echo "Database $db_name created successfully"
}

# Create additional databases
create_database "UmiHealthIdentity"

echo "All databases initialized successfully"
