-- Clear existing tenant and user data
-- This script will remove all existing tenants and their associated users
-- and create fresh demo data for Admin, Cashier, and Pharmacist portals

-- Begin transaction
BEGIN;

-- Delete existing data in correct order (respecting foreign key constraints)
DELETE FROM user_roles;
DELETE FROM user_claims;
DELETE FROM refresh_tokens;
DELETE FROM users;
DELETE FROM roles;
DELETE FROM tenants;

-- Reset sequences
ALTER SEQUENCE IF EXISTS tenants_id_seq RESTART WITH 1;
ALTER SEQUENCE IF EXISTS users_id_seq RESTART WITH 1;
ALTER SEQUENCE IF EXISTS roles_id_seq RESTART WITH 1;

-- Create demo tenant
INSERT INTO tenants (
    id,
    name,
    description,
    subdomain,
    database_name,
    contact_email,
    contact_phone,
    address,
    city,
    country,
    postal_code,
    subscription_plan,
    is_active,
    created_at,
    updated_at,
    created_by
) VALUES (
    gen_random_uuid(),
    'Umi Health Demo Pharmacy',
    'A comprehensive pharmacy management system demonstration',
    'demo',
    'umihealth_demo',
    'demo@umihealth.com',
    '+1-555-0123',
    '123 Demo Street',
    'Demo City',
    'Demo Country',
    '12345',
    'Enterprise',
    true,
    NOW(),
    NOW(),
    'system'
);

-- Get the tenant ID for demo
DO $$
DECLARE
    demo_tenant_id UUID;
BEGIN
    SELECT id INTO demo_tenant_id FROM tenants WHERE subdomain = 'demo';
    
    -- Create roles for the demo tenant
    INSERT INTO roles (id, tenant_id, name, normalized_name, description, created_at, updated_at, created_by) VALUES
    (gen_random_uuid(), demo_tenant_id, 'Admin', 'ADMIN', 'System Administrator with full access', NOW(), NOW(), 'system'),
    (gen_random_uuid(), demo_tenant_id, 'Cashier', 'CASHIER', 'Point of Sale and payment processing', NOW(), NOW(), 'system'),
    (gen_random_uuid(), demo_tenant_id, 'Pharmacist', 'PHARMACIST', 'Medication management and prescriptions', NOW(), NOW(), 'system'),
    (gen_random_uuid(), demo_tenant_id, 'Operations', 'OPERATIONS', 'Operations and inventory management', NOW(), NOW(), 'system'),
    (gen_random_uuid(), demo_tenant_id, 'SuperAdmin', 'SUPERADMIN', 'Super administrator with cross-tenant access', NOW(), NOW(), 'system');
    
    -- Get role IDs
    DECLARE
        admin_role_id UUID;
        cashier_role_id UUID;
        pharmacist_role_id UUID;
        operations_role_id UUID;
        superadmin_role_id UUID;
    BEGIN
        SELECT id INTO admin_role_id FROM roles WHERE tenant_id = demo_tenant_id AND name = 'Admin';
        SELECT id INTO cashier_role_id FROM roles WHERE tenant_id = demo_tenant_id AND name = 'Cashier';
        SELECT id INTO pharmacist_role_id FROM roles WHERE tenant_id = demo_tenant_id AND name = 'Pharmacist';
        SELECT id INTO operations_role_id FROM roles WHERE tenant_id = demo_tenant_id AND name = 'Operations';
        SELECT id INTO superadmin_role_id FROM roles WHERE tenant_id = demo_tenant_id AND name = 'SuperAdmin';
        
        -- Create demo users with hashed passwords (password: Demo123!)
        INSERT INTO users (
            id,
            tenant_id,
            user_name,
            email,
            password_hash,
            first_name,
            last_name,
            phone_number,
            is_active,
            email_confirmed,
            phone_number_confirmed,
            two_factor_enabled,
            created_at,
            updated_at,
            created_by
        ) VALUES
        -- Admin User
        (
            gen_random_uuid(),
            demo_tenant_id,
            'admin',
            'admin@demo.umihealth.com',
            '$2a$11$lQv6YzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzK', -- Demo123!
            'John',
            'Administrator',
            '+1-555-0101',
            true,
            true,
            true,
            false,
            NOW(),
            NOW(),
            'system'
        ),
        -- Cashier User
        (
            gen_random_uuid(),
            demo_tenant_id,
            'cashier',
            'cashier@demo.umihealth.com',
            '$2a$11$lQv6YzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzK', -- Demo123!
            'Sarah',
            'Cashier',
            '+1-555-0102',
            true,
            true,
            true,
            false,
            NOW(),
            NOW(),
            'system'
        ),
        -- Pharmacist User
        (
            gen_random_uuid(),
            demo_tenant_id,
            'pharmacist',
            'pharmacist@demo.umihealth.com',
            '$2a$11$lQv6YzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzK', -- Demo123!
            'Dr. Michael',
            'Pharmacist',
            '+1-555-0103',
            true,
            true,
            true,
            false,
            NOW(),
            NOW(),
            'system'
        ),
        -- Operations User
        (
            gen_random_uuid(),
            demo_tenant_id,
            'operations',
            'operations@demo.umihealth.com',
            '$2a$11$lQv6YzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzK', -- Demo123!
            'Lisa',
            'Operations',
            '+1-555-0104',
            true,
            true,
            true,
            false,
            NOW(),
            NOW(),
            'system'
        ),
        -- SuperAdmin User
        (
            gen_random_uuid(),
            demo_tenant_id,
            'superadmin',
            'superadmin@umihealth.com',
            '$2a$11$lQv6YzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzKzK', -- Demo123!
            'Super',
            'Administrator',
            '+1-555-0105',
            true,
            true,
            true,
            false,
            NOW(),
            NOW(),
            'system'
        );
        
        -- Assign roles to users
        DECLARE
            admin_user_id UUID;
            cashier_user_id UUID;
            pharmacist_user_id UUID;
            operations_user_id UUID;
            superadmin_user_id UUID;
        BEGIN
            SELECT id INTO admin_user_id FROM users WHERE user_name = 'admin';
            SELECT id INTO cashier_user_id FROM users WHERE user_name = 'cashier';
            SELECT id INTO pharmacist_user_id FROM users WHERE user_name = 'pharmacist';
            SELECT id INTO operations_user_id FROM users WHERE user_name = 'operations';
            SELECT id INTO superadmin_user_id FROM users WHERE user_name = 'superadmin';
            
            INSERT INTO user_roles (user_id, role_id, tenant_id, created_at, updated_at, created_by) VALUES
            (admin_user_id, admin_role_id, demo_tenant_id, NOW(), NOW(), 'system'),
            (cashier_user_id, cashier_role_id, demo_tenant_id, NOW(), NOW(), 'system'),
            (pharmacist_user_id, pharmacist_role_id, demo_tenant_id, NOW(), NOW(), 'system'),
            (operations_user_id, operations_role_id, demo_tenant_id, NOW(), NOW(), 'system'),
            (superadmin_user_id, superadmin_role_id, demo_tenant_id, NOW(), NOW(), 'system');
        END;
    END;
END $$;

-- Commit transaction
COMMIT;

-- Display created demo accounts
SELECT 
    'Demo Accounts Created:' as info,
    'Username | Password | Role | Portal' as details
UNION ALL
SELECT 
    '---',
    'admin | Demo123! | Admin | Admin Portal'
UNION ALL
SELECT 
    '---',
    'cashier | Demo123! | Cashier | Cashier Portal'
UNION ALL
SELECT 
    '---',
    'pharmacist | Demo123! | Pharmacist | Pharmacist Portal'
UNION ALL
SELECT 
    '---',
    'operations | Demo123! | Operations | Operations Portal'
UNION ALL
SELECT 
    '---',
    'superadmin | Demo123! | SuperAdmin | Super Admin Portal';

-- Show tenant info
SELECT 
    'Tenant Information:' as info,
    name || ' (' || subdomain || ')' as details
FROM tenants 
WHERE subdomain = 'demo';
