-- Umi Health Database Cleanup Script
-- This script cleans all data from PostgreSQL databases
-- Run with: psql -h localhost -U umihealth -d UmiHealth -f cleanup-databases.sql

-- Disable foreign key constraints temporarily
SET session_replication_role = replica;

-- Clean main application data in correct order (respecting foreign keys)

-- 1. Clean audit logs and system logs first
DELETE FROM audit_logs;
DELETE FROM system_logs;

-- 2. Clean transactional data
DELETE FROM sale_items;
DELETE FROM sales;
DELETE FROM prescription_items;
DELETE FROM prescriptions;
DELETE FROM inventory_transactions;
DELETE FROM stock_adjustments;

-- 3. Clean medical records
DELETE FROM patient_allergies;
DELETE FROM patient_medications;
DELETE FROM patient_visits;
DELETE FROM patients;

-- 4. Clean inventory data
DELETE FROM inventory;
DELETE FROM suppliers;
DELETE FROM categories;

-- 5. Clean user sessions and tokens
DELETE FROM user_sessions;
DELETE FROM refresh_tokens;
DELETE FROM blacklisted_tokens;

-- 6. Clean user accounts (keep system admin if exists)
DELETE FROM users WHERE role != 'superadmin';

-- 7. Clean tenant data
DELETE FROM tenant_settings;
DELETE FROM tenant_subscriptions;
DELETE FROM tenants WHERE name != 'System';

-- 8. Clean any remaining system data
DELETE FROM notifications;
DELETE FROM background_jobs;
DELETE FROM email_queue;

-- Re-enable foreign key constraints
SET session_replication_role = DEFAULT;

-- Reset sequences
DO $$
DECLARE
    seq_name text;
BEGIN
    FOR seq_name IN 
        SELECT sequence_name 
        FROM information_schema.sequences 
        WHERE sequence_schema = 'public'
    LOOP
        EXECUTE 'ALTER SEQUENCE ' || seq_name || ' RESTART WITH 1';
    END LOOP;
END $$;

-- Vacuum and analyze for performance
VACUUM FULL;
ANALYZE;

-- Log cleanup completion
INSERT INTO audit_logs (id, user_id, action, entity_type, details, created_at)
VALUES (
    gen_random_uuid(),
    'system',
    'DATABASE_CLEANUP',
    'system',
    'All data cleaned from database',
    NOW()
);

SELECT 'Database cleanup completed successfully!' as status;
