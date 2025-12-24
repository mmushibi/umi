-- Migration 001: Initial Schema Creation
-- Umi Health Pharmacy Management System
-- Multi-Tenant PostgreSQL Database

-- Run this migration first to create the initial database structure

-- Create extensions
\i ../schema.sql

-- Create indexes
\i ../indexes.sql

-- Create triggers
\i ../triggers.sql

-- Create row-level security
\i ../row_level_security.sql

-- Insert initial data for system configuration
INSERT INTO tenants (name, slug, domain, subscription_plan, status, settings) VALUES
('System Administrator', 'system', NULL, 'enterprise', 'active', '{"is_system": true}');

-- Create default admin user for system management
INSERT INTO users (
    tenant_id, email, username, password_hash, first_name, last_name, role, permissions, is_active
) VALUES (
    (SELECT id FROM tenants WHERE slug = 'system'),
    'admin@umihealth.com',
    'admin',
    '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj6ukx.LFvO.', -- password: admin123
    'System',
    'Administrator',
    'super_admin',
    '["tenant_management", "user_management", "system_configuration"]',
    true
);

-- Create default subscription plans reference data
-- This can be moved to a separate subscription management table later
COMMENT ON COLUMN tenants.subscription_plan IS 'Available plans: basic, professional, enterprise';
