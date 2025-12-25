-- Database Setup Script for Umi Health
-- Run this script to create the database and set up initial configuration

-- Create database if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT FROM pg_database WHERE datname = 'Umi_db') THEN
        CREATE DATABASE Umi_db
            WITH 
            OWNER = postgres
            ENCODING = 'UTF8'
            LC_COLLATE = 'en_US.UTF-8'
            LC_CTYPE = 'en_US.UTF-8'
            TABLESPACE = pg_default
            CONNECTION LIMIT = -1;
    END IF;
END
$$;

-- Connect to the database
\c Umi_db;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create application user
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'umi_health_app') THEN
        CREATE ROLE umi_health_app WITH LOGIN PASSWORD 'umi_health_2024!';
    END IF;
END
$$;

-- Grant basic permissions
GRANT CONNECT ON DATABASE Umi_db TO umi_health_app;
GRANT USAGE ON SCHEMA public TO umi_health_app;
GRANT CREATE ON SCHEMA public TO umi_health_app;

-- Output setup completion message
SELECT 'Database setup completed successfully!' as status;
