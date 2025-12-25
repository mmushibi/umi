-- Migration 003: Additional User Charges and Approval Workflow
-- Umi Health Pharmacy Management System
-- Adds support for charging K50 per additional user beyond subscription limits

-- Begin transaction
BEGIN;

-- Create additional user charges table
CREATE TABLE additional_user_charges (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    user_id UUID NOT NULL REFERENCES users(id),
    charge_amount DECIMAL(10,2) NOT NULL DEFAULT 50.00,
    currency VARCHAR(3) DEFAULT 'ZMW',
    billing_month DATE NOT NULL,
    status VARCHAR(50) DEFAULT 'pending_payment',
    payment_reference VARCHAR(100),
    payment_method VARCHAR(50),
    payment_date TIMESTAMP WITH TIME ZONE,
    approved_by UUID REFERENCES users(id),
    approved_at TIMESTAMP WITH TIME ZONE,
    rejection_reason TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(tenant_id, user_id, billing_month)
);

-- Create additional user requests table for tracking requests beyond limits
CREATE TABLE additional_user_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id VARCHAR(50) UNIQUE NOT NULL,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    user_email VARCHAR(255) NOT NULL,
    user_first_name VARCHAR(100) NOT NULL,
    user_last_name VARCHAR(100) NOT NULL,
    user_role VARCHAR(50) NOT NULL,
    branch_id UUID REFERENCES branches(id),
    requested_by UUID NOT NULL REFERENCES users(id),
    subscription_plan_at_request VARCHAR(50) NOT NULL,
    current_user_count INTEGER NOT NULL,
    max_allowed_users INTEGER NOT NULL,
    charge_amount DECIMAL(10,2) NOT NULL DEFAULT 50.00,
    status VARCHAR(50) DEFAULT 'pending_approval',
    approved_by UUID REFERENCES users(id),
    approved_at TIMESTAMP WITH TIME ZONE,
    rejection_reason TEXT,
    user_created_id UUID REFERENCES users(id),
    user_created_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Add indexes for performance
CREATE INDEX idx_additional_user_charges_tenant_id ON additional_user_charges(tenant_id);
CREATE INDEX idx_additional_user_charges_user_id ON additional_user_charges(user_id);
CREATE INDEX idx_additional_user_charges_billing_month ON additional_user_charges(billing_month);
CREATE INDEX idx_additional_user_charges_status ON additional_user_charges(status);

CREATE INDEX idx_additional_user_requests_tenant_id ON additional_user_requests(tenant_id);
CREATE INDEX idx_additional_user_requests_requested_by ON additional_user_requests(requested_by);
CREATE INDEX idx_additional_user_requests_status ON additional_user_requests(status);
CREATE INDEX idx_additional_user_requests_created_at ON additional_user_requests(created_at);

-- Add constraints for status values
ALTER TABLE additional_user_charges 
ADD CONSTRAINT check_charge_status 
CHECK (status IN ('pending_payment', 'paid', 'approved', 'rejected', 'cancelled'));

ALTER TABLE additional_user_requests 
ADD CONSTRAINT check_request_status 
CHECK (status IN ('pending_approval', 'approved', 'rejected', 'cancelled'));

-- Create function to generate request IDs
CREATE OR REPLACE FUNCTION generate_additional_user_request_id()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.request_id IS NULL THEN
        NEW.request_id := 'AUR' || TO_CHAR(NOW(), 'YYYYMMDDHH24MISS') || LPAD(EXTRACT(MICROSECONDS FROM NOW())::text, 6, '0');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for auto-generating request IDs
CREATE TRIGGER trigger_generate_additional_user_request_id
    BEFORE INSERT ON additional_user_requests
    FOR EACH ROW
    EXECUTE FUNCTION generate_additional_user_request_id();

-- Create function to check if adding a user requires additional charge
CREATE OR REPLACE FUNCTION check_additional_user_charge(
    p_tenant_id UUID,
    p_current_users INTEGER
)
RETURNS TABLE(
    requires_charge BOOLEAN,
    additional_users INTEGER,
    total_charge DECIMAL(10,2)
) AS $$
DECLARE
    v_max_users INTEGER;
    v_subscription_plan VARCHAR(50);
BEGIN
    -- Get tenant's subscription plan and max users
    SELECT t.subscription_plan, sp.max_users
    INTO v_subscription_plan, v_max_users
    FROM tenants t
    JOIN subscription_plans sp ON t.subscription_plan = sp.name
    WHERE t.id = p_tenant_id;
    
    -- Handle unlimited plans (-1 means unlimited)
    IF v_max_users = -1 THEN
        requires_charge := FALSE;
        additional_users := 0;
        total_charge := 0.00;
        RETURN NEXT;
    END IF;
    
    -- Calculate if additional users are needed
    IF p_current_users > v_max_users THEN
        requires_charge := TRUE;
        additional_users := p_current_users - v_max_users;
        total_charge := additional_users * 50.00; -- K50 per additional user
    ELSE
        requires_charge := FALSE;
        additional_users := 0;
        total_charge := 0.00;
    END IF;
    
    RETURN NEXT;
END;
$$ LANGUAGE plpgsql;

-- Create function to create monthly additional user charges
CREATE OR REPLACE FUNCTION create_monthly_additional_charges()
RETURNS VOID AS $$
DECLARE
    v_tenant RECORD;
    v_user_count INTEGER;
    v_max_users INTEGER;
    v_additional_users INTEGER;
    v_billing_month DATE;
BEGIN
    v_billing_month := DATE_TRUNC('month', CURRENT_DATE);
    
    -- Loop through all active tenants
    FOR v_tenant IN 
        SELECT t.id, t.name, t.subscription_plan, sp.max_users
        FROM tenants t
        JOIN subscription_plans sp ON t.subscription_plan = sp.name
        WHERE t.status = 'active' AND sp.max_users != -1
    LOOP
        -- Count current active users for the tenant
        SELECT COUNT(*) INTO v_user_count
        FROM users
        WHERE tenant_id = v_tenant.id AND is_active = TRUE;
        
        -- Skip if no additional users needed
        IF v_user_count <= v_tenant.max_users THEN
            CONTINUE;
        END IF;
        
        v_additional_users := v_user_count - v_tenant.max_users;
        
        -- Create charges for additional users that don't already have charges for this month
        INSERT INTO additional_user_charges (tenant_id, user_id, charge_amount, billing_month, status)
        SELECT 
            v_tenant.id,
            u.id,
            50.00,
            v_billing_month,
            'pending_payment'
        FROM users u
        WHERE u.tenant_id = v_tenant.id 
          AND u.is_active = TRUE
          AND NOT EXISTS (
              SELECT 1 FROM additional_user_charges auc 
              WHERE auc.tenant_id = v_tenant.id 
                AND auc.user_id = u.id 
                AND auc.billing_month = v_billing_month
          )
        LIMIT v_additional_users;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- Create view for operations and super admin to monitor additional user requests
CREATE VIEW additional_user_requests_view AS
SELECT 
    aur.request_id,
    aur.tenant_id,
    t.name as tenant_name,
    aur.user_email,
    aur.user_first_name,
    aur.user_last_name,
    aur.user_role,
    b.name as branch_name,
    aur.requested_by,
    req_user.email as requested_by_email,
    aur.subscription_plan_at_request,
    aur.current_user_count,
    aur.max_allowed_users,
    aur.charge_amount,
    aur.status,
    aur.approved_by,
    approved_user.email as approved_by_email,
    aur.approved_at,
    aur.rejection_reason,
    aur.created_at,
    aur.updated_at
FROM additional_user_requests aur
JOIN tenants t ON aur.tenant_id = t.id
LEFT JOIN branches b ON aur.branch_id = b.id
LEFT JOIN users req_user ON aur.requested_by = req_user.id
LEFT JOIN users approved_user ON aur.approved_by = approved_user.id;

-- Create view for monthly billing reports
CREATE VIEW monthly_additional_user_charges_view AS
SELECT 
    auc.tenant_id,
    t.name as tenant_name,
    auc.billing_month,
    COUNT(*) as additional_users_count,
    SUM(auc.charge_amount) as total_monthly_charge,
    COUNT(CASE WHEN auc.status = 'paid' THEN 1 END) as paid_users,
    COUNT(CASE WHEN auc.status = 'pending_payment' THEN 1 END) as pending_users,
    COUNT(CASE WHEN auc.status = 'rejected' THEN 1 END) as rejected_users
FROM additional_user_charges auc
JOIN tenants t ON auc.tenant_id = t.id
GROUP BY auc.tenant_id, t.name, auc.billing_month
ORDER BY auc.billing_month DESC, t.name;

-- Grant permissions to authenticated users
GRANT SELECT ON additional_user_requests_view TO authenticated_users;
GRANT SELECT ON monthly_additional_user_charges_view TO authenticated_users;

-- Create trigger to automatically create additional user request when user exceeds limit
CREATE OR REPLACE FUNCTION trigger_additional_user_request()
RETURNS TRIGGER AS $$
DECLARE
    v_user_count INTEGER;
    v_max_users INTEGER;
    v_subscription_plan VARCHAR(50);
    v_requires_charge BOOLEAN;
    v_additional_users INTEGER;
    v_total_charge DECIMAL(10,2);
BEGIN
    -- Only trigger for new active users
    IF TG_OP = 'INSERT' AND NEW.is_active = TRUE THEN
        -- Check if this user requires additional charge
        SELECT * INTO v_requires_charge, v_additional_users, v_total_charge
        FROM check_additional_user_charge(NEW.tenant_id, 
            (SELECT COUNT(*) FROM users WHERE tenant_id = NEW.tenant_id AND is_active = TRUE)
        );
        
        -- If additional charge is required, create a request
        IF v_requires_charge THEN
            INSERT INTO additional_user_requests (
                tenant_id,
                user_email,
                user_first_name,
                user_last_name,
                user_role,
                branch_id,
                requested_by,
                subscription_plan_at_request,
                current_user_count,
                max_allowed_users,
                charge_amount,
                status
            ) VALUES (
                NEW.tenant_id,
                NEW.email,
                NEW.first_name,
                NEW.last_name,
                NEW.role,
                NEW.branch_id,
                NEW.id, -- Self-requested
                (SELECT subscription_plan FROM tenants WHERE id = NEW.tenant_id),
                (SELECT COUNT(*) FROM users WHERE tenant_id = NEW.tenant_id AND is_active = TRUE),
                (SELECT sp.max_users FROM tenants t JOIN subscription_plans sp ON t.subscription_plan = sp.name WHERE t.id = NEW.tenant_id),
                v_total_charge,
                'pending_approval'
            );
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger on users table
CREATE TRIGGER trigger_additional_user_request_check
    AFTER INSERT ON users
    FOR EACH ROW
    EXECUTE FUNCTION trigger_additional_user_request();

-- Commit transaction
COMMIT;

-- Migration completed successfully
-- Note: This migration adds support for charging K50 per additional user beyond subscription limits
-- and creates approval workflows for operations and super admin notifications
