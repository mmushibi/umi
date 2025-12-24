-- Additional User Charges Migration Execution Script
-- Umi Health Pharmacy Management System
-- This script executes the migration for additional user charging functionality

-- Check if migration has already been applied
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'additional_user_charges' 
        AND table_schema = 'shared'
    ) THEN
        RAISE NOTICE 'Additional user tables already exist. Migration may have been applied already.';
        RETURN;
    END IF;
END $$;

-- Execute the migration
\i database/migrations/003_additional_user_charges.sql

-- Verify tables were created
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'additional_user_charges' 
        AND table_schema = 'shared'
    ) THEN
        RAISE EXCEPTION 'Failed to create additional_user_charges table';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'additional_user_requests' 
        AND table_schema = 'shared'
    ) THEN
        RAISE EXCEPTION 'Failed to create additional_user_requests table';
    END IF;
    
    RAISE NOTICE 'Migration 003_additional_user_charges.sql executed successfully';
END $$;

-- Create initial subscription plans if they don't exist
INSERT INTO subscription_plans (name, display_name, description, price_monthly, price_annually, features, max_users, max_branches)
VALUES 
    ('Care', 'Care', 'Essential pharmacy management features for small pharmacies', 250.00, 2500.00, 
     '["Patient Management", "Basic Inventory", "Point of Sale", "Basic Reports"]', 5, 1),
    ('Care Plus', 'Care Plus', 'Enhanced features for growing pharmacies with multiple branches', 450.00, 4500.00,
     '["Patient Management", "Advanced Inventory", "Point of Sale", "Advanced Reports", "Multi-Branch Support", "Prescription Management"]', 20, 5),
    ('Care Pro', 'Care Pro', 'Complete solution for large pharmacy chains with advanced features', 700.00, 7000.00,
     '["Patient Management", "Advanced Inventory", "Point of Sale", "Advanced Reports", "Multi-Branch Support", "Prescription Management", "API Access", "Advanced Analytics", "Priority Support"]', -1, -1)
ON CONFLICT (name) DO NOTHING;

-- Grant necessary permissions to authenticated_users role
GRANT SELECT, INSERT, UPDATE, DELETE ON additional_user_charges TO authenticated_users;
GRANT SELECT, INSERT, UPDATE, DELETE ON additional_user_requests TO authenticated_users;
GRANT SELECT, INSERT, UPDATE, DELETE ON payment_transactions TO authenticated_users;
GRANT SELECT, INSERT, UPDATE, DELETE ON notifications TO authenticated_users;
GRANT SELECT, INSERT, UPDATE, DELETE ON notification_settings TO authenticated_users;

-- Grant usage on sequences
GRANT USAGE ON ALL SEQUENCES IN SCHEMA shared TO authenticated_users;

-- Create indexes for performance (if not already created by migration)
CREATE INDEX IF NOT EXISTS idx_additional_user_charges_tenant_billing_month 
ON additional_user_charges(tenant_id, billing_month);

CREATE INDEX IF NOT EXISTS idx_additional_user_requests_status_created 
ON additional_user_requests(status, created_at);

CREATE INDEX IF NOT EXISTS idx_payment_transactions_tenant_date 
ON payment_transactions(tenant_id, transaction_date);

-- Create function to automatically create monthly charges (cron job function)
CREATE OR REPLACE FUNCTION create_monthly_additional_charges_job()
RETURNS VOID AS $$
BEGIN
    PERFORM create_monthly_additional_charges();
    RAISE NOTICE 'Monthly additional user charges created successfully';
END;
$$ LANGUAGE plpgsql;

-- Add comment explaining the function
COMMENT ON FUNCTION create_monthly_additional_charges_job() IS 
'Job function to be called monthly to create charges for additional users beyond subscription limits';

-- Verify the system is ready
DO $$
DECLARE
    v_plan_count INTEGER;
    v_table_count INTEGER;
BEGIN
    -- Count subscription plans
    SELECT COUNT(*) INTO v_plan_count FROM subscription_plans WHERE is_active = TRUE;
    
    -- Count new tables
    SELECT COUNT(*) INTO v_table_count 
    FROM information_schema.tables 
    WHERE table_schema = 'shared' 
    AND table_name IN ('additional_user_charges', 'additional_user_requests', 'payment_transactions');
    
    RAISE NOTICE 'System verification complete:';
    RAISE NOTICE '- Active subscription plans: %', v_plan_count;
    RAISE NOTICE '- New tables created: %/3', v_table_count;
    
    IF v_plan_count >= 3 AND v_table_count = 3 THEN
        RAISE NOTICE 'Additional user charging system is ready for use!';
    ELSE
        RAISE WARNING 'System may not be fully configured. Please check the migration.';
    END IF;
END $$;

-- Sample data for testing (optional - remove in production)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM tenants WHERE name = 'Test Pharmacy') THEN
        -- Create a sample additional user request for testing
        INSERT INTO additional_user_requests (
            request_id, tenant_id, user_email, user_first_name, user_last_name, 
            user_role, requested_by, subscription_plan_at_request, 
            current_user_count, max_allowed_users, charge_amount, status
        )
        SELECT 
            'AURTEST001',
            t.id,
            'additional.user@testpharmacy.com',
            'Additional',
            'User',
            'pharmacist',
            u.id,
            t.subscription_plan,
            6, -- Exceeding Care plan limit of 5
            5,
            50.00,
            'pending_approval'
        FROM tenants t
        JOIN users u ON u.tenant_id = t.id AND u.role = 'admin'
        WHERE t.name = 'Test Pharmacy'
        LIMIT 1
        ON CONFLICT (request_id) DO NOTHING;
        
        RAISE NOTICE 'Sample test data created for Test Pharmacy';
    END IF;
END $$;

COMMIT;

-- Migration completed successfully
RAISE NOTICE '=== ADDITIONAL USER CHARGES MIGRATION COMPLETED ===';
RAISE NOTICE 'The system now supports:';
RAISE NOTICE '- K50 charging per additional user beyond subscription limits';
RAISE NOTICE '- Automatic request creation when users exceed limits';
RAISE NOTICE '- Approval workflow for operations and super admin';
RAISE NOTICE '- Payment verification and processing';
RAISE NOTICE '- Notifications to relevant stakeholders';
RAISE NOTICE '- Monthly billing reports and summaries';
