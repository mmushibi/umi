-- Migration 002: Subscription Plans Update and Approval Workflow
-- Umi Health Pharmacy Management System
-- Updated subscription plan structure and approval system

-- Begin transaction
BEGIN;

-- Update existing subscription plans to new naming convention
UPDATE tenants 
SET subscription_plan = CASE 
    WHEN subscription_plan = 'Go' THEN 'Care'
    WHEN subscription_plan = 'Grow' THEN 'Care Plus'
    WHEN subscription_plan = 'Pro' THEN 'Care Pro'
    WHEN subscription_plan = 'basic' THEN 'Care'
    WHEN subscription_plan = 'professional' THEN 'Care Plus'
    WHEN subscription_plan = 'enterprise' THEN 'Care Pro'
    ELSE subscription_plan
END,
updated_at = NOW()
WHERE subscription_plan IN ('Go', 'Grow', 'Pro', 'basic', 'professional', 'enterprise');

-- Create subscription plans reference table
CREATE TABLE subscription_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) UNIQUE NOT NULL,
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    price_monthly DECIMAL(10,2) NOT NULL,
    price_annually DECIMAL(10,2) NOT NULL,
    features JSONB DEFAULT '[]',
    max_users INTEGER DEFAULT 5,
    max_branches INTEGER DEFAULT 1,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Insert new subscription plans
INSERT INTO subscription_plans (name, display_name, description, price_monthly, price_annually, features, max_users, max_branches) VALUES
('Care', 'Care', 'Essential pharmacy management features for small pharmacies', 250.00, 2500.00, 
 '["Patient Management", "Basic Inventory", "Point of Sale", "Basic Reports"]', 5, 1),
('Care Plus', 'Care Plus', 'Enhanced features for growing pharmacies with multiple branches', 450.00, 4500.00,
 '["Patient Management", "Advanced Inventory", "Point of Sale", "Advanced Reports", "Multi-Branch Support", "Prescription Management"]', 20, 5),
('Care Pro', 'Care Pro', 'Complete solution for large pharmacy chains with advanced features', 700.00, 7000.00,
 '["Patient Management", "Advanced Inventory", "Point of Sale", "Advanced Reports", "Multi-Branch Support", "Prescription Management", "API Access", "Advanced Analytics", "Priority Support"]', -1, -1);

-- Create subscription transactions table for approval workflow
CREATE TABLE subscription_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id VARCHAR(50) UNIQUE NOT NULL,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    plan_from VARCHAR(50) NOT NULL,
    plan_to VARCHAR(50) NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'ZMW',
    billing_cycle VARCHAR(20) DEFAULT 'monthly',
    status VARCHAR(50) DEFAULT 'pending_approval',
    requested_by UUID NOT NULL REFERENCES users(id),
    approved_by UUID REFERENCES users(id),
    approved_at TIMESTAMP WITH TIME ZONE,
    rejection_reason TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create subscription history table for tracking changes
CREATE TABLE subscription_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    plan VARCHAR(50) NOT NULL,
    billing_cycle VARCHAR(20) NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    status VARCHAR(20) NOT NULL,
    effective_from TIMESTAMP WITH TIME ZONE NOT NULL,
    effective_to TIMESTAMP WITH TIME ZONE,
    transaction_id UUID REFERENCES subscription_transactions(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Add indexes for performance
CREATE INDEX idx_subscription_transactions_tenant_id ON subscription_transactions(tenant_id);
CREATE INDEX idx_subscription_transactions_status ON subscription_transactions(status);
CREATE INDEX idx_subscription_transactions_created_at ON subscription_transactions(created_at);
CREATE INDEX idx_subscription_history_tenant_id ON subscription_history(tenant_id);
CREATE INDEX idx_subscription_history_effective_from ON subscription_history(effective_from);

-- Add constraint for subscription transactions
ALTER TABLE subscription_transactions 
ADD CONSTRAINT check_status 
CHECK (status IN ('pending_approval', 'approved', 'rejected', 'completed', 'cancelled'));

-- Add constraint for billing cycle
ALTER TABLE subscription_transactions 
ADD CONSTRAINT check_billing_cycle 
CHECK (billing_cycle IN ('monthly', 'quarterly', 'annually'));

-- Update comment on tenants.subscription_plan column
COMMENT ON COLUMN tenants.subscription_plan IS 'Available plans: Care, Care Plus, Care Pro';

-- Create function to generate transaction IDs
CREATE OR REPLACE FUNCTION generate_subscription_transaction_id()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.transaction_id IS NULL THEN
        NEW.transaction_id := 'SUB' || TO_CHAR(NOW(), 'YYYYMMDDHH24MISS') || LPAD(EXTRACT(MICROSECONDS FROM NOW())::text, 6, '0');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for auto-generating transaction IDs
CREATE TRIGGER trigger_generate_subscription_transaction_id
    BEFORE INSERT ON subscription_transactions
    FOR EACH ROW
    EXECUTE FUNCTION generate_subscription_transaction_id();

-- Create function to log subscription changes
CREATE OR REPLACE FUNCTION log_subscription_change()
RETURNS TRIGGER AS $$
BEGIN
    -- Log old subscription if it exists
    IF OLD.subscription_plan IS NOT NULL THEN
        INSERT INTO subscription_history (tenant_id, plan, billing_cycle, price, status, effective_from, effective_to)
        VALUES (
            OLD.id,
            OLD.subscription_plan,
            COALESCE(OLD.billing_cycle, 'monthly'),
            COALESCE(OLD.price, 0.00),
            OLD.status,
            OLD.created_at,
            NOW()
        );
    END IF;
    
    -- Log new subscription
    INSERT INTO subscription_history (tenant_id, plan, billing_cycle, price, status, effective_from)
    VALUES (
        NEW.id,
        NEW.subscription_plan,
        COALESCE(NEW.billing_cycle, 'monthly'),
        COALESCE(NEW.price, 0.00),
        NEW.status,
        NOW()
    );
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for logging subscription changes
CREATE TRIGGER trigger_log_subscription_change
    AFTER UPDATE ON tenants
    FOR EACH ROW
    WHEN (OLD.subscription_plan IS DISTINCT FROM NEW.subscription_plan)
    EXECUTE FUNCTION log_subscription_change();

-- Update existing tenants with pricing based on their new plans
UPDATE tenants 
SET 
    price = CASE 
        WHEN subscription_plan = 'Care' THEN 250.00
        WHEN subscription_plan = 'Care Plus' THEN 450.00
        WHEN subscription_plan = 'Care Pro' THEN 700.00
        ELSE 0.00
    END,
    billing_cycle = COALESCE(billing_cycle, 'monthly'),
    updated_at = NOW()
WHERE subscription_plan IN ('Care', 'Care Plus', 'Care Pro');

-- Commit transaction
COMMIT;

-- Migration completed successfully
-- Note: Run \i indexes.sql to update indexes if needed
