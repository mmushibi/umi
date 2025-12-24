-- Row-Level Security (RLS) Implementation
-- Multi-tenant data isolation

-- Enable RLS on all tenant-scoped tables
ALTER TABLE branches ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE suppliers ENABLE ROW LEVEL SECURITY;
ALTER TABLE products ENABLE ROW LEVEL SECURITY;
ALTER TABLE inventory ENABLE ROW LEVEL SECURITY;
ALTER TABLE patients ENABLE ROW LEVEL SECURITY;
ALTER TABLE prescriptions ENABLE ROW LEVEL SECURITY;
ALTER TABLE prescription_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales ENABLE ROW LEVEL SECURITY;
ALTER TABLE sale_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE payments ENABLE ROW LEVEL SECURITY;
ALTER TABLE stock_movements ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;

-- RLS Policies for Tenant Isolation
-- Branches
CREATE POLICY tenant_isolation_branches ON branches
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Users
CREATE POLICY tenant_isolation_users ON users
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Suppliers
CREATE POLICY tenant_isolation_suppliers ON suppliers
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Products
CREATE POLICY tenant_isolation_products ON products
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Inventory
CREATE POLICY tenant_isolation_inventory ON inventory
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Patients
CREATE POLICY tenant_isolation_patients ON patients
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Prescriptions
CREATE POLICY tenant_isolation_prescriptions ON prescriptions
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Prescription Items (inherits isolation from prescriptions)
CREATE POLICY tenant_isolation_prescription_items ON prescription_items
    FOR ALL TO authenticated_users
    USING (EXISTS (
        SELECT 1 FROM prescriptions 
        WHERE prescriptions.id = prescription_items.prescription_id 
        AND prescriptions.tenant_id = current_setting('app.current_tenant_id', true)::UUID
    ))
    WITH CHECK (EXISTS (
        SELECT 1 FROM prescriptions 
        WHERE prescriptions.id = prescription_items.prescription_id 
        AND prescriptions.tenant_id = current_setting('app.current_tenant_id', true)::UUID
    ));

-- Sales
CREATE POLICY tenant_isolation_sales ON sales
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Sale Items (inherits isolation from sales)
CREATE POLICY tenant_isolation_sale_items ON sale_items
    FOR ALL TO authenticated_users
    USING (EXISTS (
        SELECT 1 FROM sales 
        WHERE sales.id = sale_items.sale_id 
        AND sales.tenant_id = current_setting('app.current_tenant_id', true)::UUID
    ))
    WITH CHECK (EXISTS (
        SELECT 1 FROM sales 
        WHERE sales.id = sale_items.sale_id 
        AND sales.tenant_id = current_setting('app.current_tenant_id', true)::UUID
    ));

-- Payments
CREATE POLICY tenant_isolation_payments ON payments
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Stock Movements
CREATE POLICY tenant_isolation_stock_movements ON stock_movements
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Audit Logs
CREATE POLICY tenant_isolation_audit_logs ON audit_logs
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Branch-level access control for users
-- Users can only see data from their own branch unless they have cross-branch permissions
CREATE POLICY branch_isolation_users ON users
    FOR SELECT TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID
        AND (
            branch_id = current_setting('app.current_branch_id', true)::UUID
            OR current_setting('app.user_permissions', true)::JSONB ? 'cross_branch_access'
        )
    );

CREATE POLICY branch_isolation_inventory ON inventory
    FOR ALL TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID
        AND (
            branch_id = current_setting('app.current_branch_id', true)::UUID
            OR current_setting('app.user_permissions', true)::JSONB ? 'cross_branch_access'
        )
    )
    WITH CHECK (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID
        AND (
            branch_id = current_setting('app.current_branch_id', true)::UUID
            OR current_setting('app.user_permissions', true)::JSONB ? 'cross_branch_access'
        )
    );

-- Role-based access control
-- Pharmacists can access prescriptions and related data
CREATE POLICY pharmacist_access_prescriptions ON prescriptions
    FOR SELECT TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID
        AND current_setting('app.user_role', true) = 'pharmacist'
    );

-- Cashiers can access sales and payment data
CREATE POLICY cashier_access_sales ON sales
    FOR ALL TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID
        AND current_setting('app.user_role', true) = 'cashier'
    )
    WITH CHECK (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID
        AND current_setting('app.user_role', true) = 'cashier'
    );

-- Admin users have full access within their tenant
CREATE POLICY admin_full_access ON branches
    FOR ALL TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID
        AND current_setting('app.user_role', true) = 'admin'
    )
    WITH CHECK (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID
        AND current_setting('app.user_role', true) = 'admin'
    );

-- Function to set tenant context
CREATE OR REPLACE FUNCTION set_tenant_context(tenant_uuid UUID, branch_uuid UUID DEFAULT NULL, user_role TEXT DEFAULT NULL, user_permissions JSONB DEFAULT '[]')
RETURNS void AS $$
BEGIN
    PERFORM set_config('app.current_tenant_id', tenant_uuid::TEXT, true);
    
    IF branch_uuid IS NOT NULL THEN
        PERFORM set_config('app.current_branch_id', branch_uuid::TEXT, true);
    END IF;
    
    IF user_role IS NOT NULL THEN
        PERFORM set_config('app.user_role', user_role, true);
    END IF;
    
    IF user_permissions IS NOT NULL THEN
        PERFORM set_config('app.user_permissions', user_permissions::TEXT, true);
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to clear tenant context
CREATE OR REPLACE FUNCTION clear_tenant_context()
RETURNS void AS $$
BEGIN
    PERFORM set_config('app.current_tenant_id', '', true);
    PERFORM set_config('app.current_branch_id', '', true);
    PERFORM set_config('app.user_role', '', true);
    PERFORM set_config('app.user_permissions', '[]', true);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to verify tenant access
CREATE OR REPLACE FUNCTION verify_tenant_access(table_name TEXT, record_id UUID)
RETURNS BOOLEAN AS $$
DECLARE
    tenant_check BOOLEAN;
BEGIN
    EXECUTE format('SELECT EXISTS(
        SELECT 1 FROM %I 
        WHERE id = $1 
        AND tenant_id = current_setting(''app.current_tenant_id'', true)::UUID
    )', table_name)
    INTO tenant_check
    USING record_id;
    
    RETURN COALESCE(tenant_check, false);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
