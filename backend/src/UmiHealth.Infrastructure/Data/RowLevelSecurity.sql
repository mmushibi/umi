-- PostgreSQL Row-Level Security (RLS) Policies for Multi-Tenancy
-- This script implements hybrid multi-tenancy with shared database, shared schema

-- Enable RLS on all tenant-specific tables
ALTER TABLE shared.branches ENABLE ROW LEVEL SECURITY;
ALTER TABLE shared.users ENABLE ROW LEVEL SECURITY;
ALTER TABLE shared.patients ENABLE ROW LEVEL SECURITY;
ALTER TABLE shared.products ENABLE ROW LEVEL SECURITY;
ALTER TABLE shared.inventory ENABLE ROW LEVEL SECURITY;
ALTER TABLE shared.prescriptions ENABLE ROW LEVEL SECURITY;
ALTER TABLE shared.sales ENABLE ROW LEVEL SECURITY;
ALTER TABLE shared.payments ENABLE ROW LEVEL SECURITY;
ALTER TABLE shared.audit_logs ENABLE ROW LEVEL SECURITY;

-- Create schema for application session variables
CREATE SCHEMA IF NOT EXISTS app;

-- Create function to get current tenant ID from session
CREATE OR REPLACE FUNCTION app.current_tenant_id() 
RETURNS UUID AS $$
BEGIN
    RETURN current_setting('app.current_tenant_id', true)::UUID;
EXCEPTION
    WHEN others THEN RETURN NULL;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Create function to get current branch ID from session
CREATE OR REPLACE FUNCTION app.current_branch_id() 
RETURNS UUID AS $$
BEGIN
    RETURN current_setting('app.current_branch_id', true)::UUID;
EXCEPTION
    WHEN others THEN RETURN NULL;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Create function to check if user has access to specific branch
CREATE OR REPLACE FUNCTION app.has_branch_access(branch_id UUID) 
RETURNS BOOLEAN AS $$
DECLARE
    current_user_id UUID;
    user_branch_access UUID[];
BEGIN
    -- Get current user ID from JWT claims (would need to be set in session)
    current_user_id := current_setting('app.current_user_id', true)::UUID;
    
    -- Get user's branch access list
    SELECT branch_access INTO user_branch_access 
    FROM shared.users 
    WHERE id = current_user_id AND tenant_id = app.current_tenant_id();
    
    -- Check if user has access to the requested branch
    RETURN branch_id = ANY(user_branch_access) OR branch_id = app.current_branch_id();
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Branch-specific RLS policies
CREATE POLICY tenant_isolation_branches ON shared.branches
    FOR ALL TO authenticated_role
    USING (tenant_id = app.current_tenant_id())
    WITH CHECK (tenant_id = app.current_tenant_id());

CREATE POLICY branch_access_branches ON shared.branches
    FOR SELECT TO authenticated_role
    USING (app.has_branch_access(id));

-- User-specific RLS policies
CREATE POLICY tenant_isolation_users ON shared.users
    FOR ALL TO authenticated_role
    USING (tenant_id = app.current_tenant_id())
    WITH CHECK (tenant_id = app.current_tenant_id());

CREATE POLICY branch_access_users ON shared.users
    FOR SELECT TO authenticated_role
    USING (app.has_branch_access(branch_id) OR branch_id IS NULL);

-- Patient-specific RLS policies
CREATE POLICY tenant_isolation_patients ON shared.patients
    FOR ALL TO authenticated_role
    USING (branch_id IN (
        SELECT id FROM shared.branches 
        WHERE tenant_id = app.current_tenant_id() 
        AND app.has_branch_access(id)
    ))
    WITH CHECK (branch_id IN (
        SELECT id FROM shared.branches 
        WHERE tenant_id = app.current_tenant_id() 
        AND app.has_branch_access(id)
    ));

-- Product-specific RLS policies
CREATE POLICY tenant_isolation_products ON shared.products
    FOR ALL TO authenticated_role
    USING (tenant_id = app.current_tenant_id())
    WITH CHECK (tenant_id = app.current_tenant_id());

CREATE POLICY branch_access_products ON shared.products
    FOR SELECT TO authenticated_role
    USING (
        is_global_product = true OR 
        branch_ids && ARRAY(
            SELECT id FROM shared.branches 
            WHERE tenant_id = app.current_tenant_id() 
            AND app.has_branch_access(id)
        )
    );

-- Inventory-specific RLS policies
CREATE POLICY tenant_isolation_inventory ON shared.inventory
    FOR ALL TO authenticated_role
    USING (tenant_id = app.current_tenant_id())
    WITH CHECK (tenant_id = app.current_tenant_id());

CREATE POLICY branch_access_inventory ON shared.inventory
    FOR ALL TO authenticated_role
    USING (app.has_branch_access(branch_id))
    WITH CHECK (app.has_branch_access(branch_id));

-- Prescription-specific RLS policies
CREATE POLICY tenant_isolation_prescriptions ON shared.prescriptions
    FOR ALL TO authenticated_role
    USING (branch_id IN (
        SELECT id FROM shared.branches 
        WHERE tenant_id = app.current_tenant_id() 
        AND app.has_branch_access(id)
    ))
    WITH CHECK (branch_id IN (
        SELECT id FROM shared.branches 
        WHERE tenant_id = app.current_tenant_id() 
        AND app.has_branch_access(id)
    ));

-- Sales-specific RLS policies
CREATE POLICY tenant_isolation_sales ON shared.sales
    FOR ALL TO authenticated_role
    USING (branch_id IN (
        SELECT id FROM shared.branches 
        WHERE tenant_id = app.current_tenant_id() 
        AND app.has_branch_access(id)
    ))
    WITH CHECK (branch_id IN (
        SELECT id FROM shared.branches 
        WHERE tenant_id = app.current_tenant_id() 
        AND app.has_branch_access(id)
    ));

-- Payment-specific RLS policies
CREATE POLICY tenant_isolation_payments ON shared.payments
    FOR ALL TO authenticated_role
    USING (branch_id IN (
        SELECT id FROM shared.branches 
        WHERE tenant_id = app.current_tenant_id() 
        AND app.has_branch_access(id)
    ))
    WITH CHECK (branch_id IN (
        SELECT id FROM shared.branches 
        WHERE tenant_id = app.current_tenant_id() 
        AND app.has_branch_access(id)
    ));

-- Audit Log-specific RLS policies
CREATE POLICY tenant_isolation_audit_logs ON shared.audit_logs
    FOR ALL TO authenticated_role
    USING (tenant_id = app.current_tenant_id() OR 
           branch_id IN (
               SELECT id FROM shared.branches 
               WHERE tenant_id = app.current_tenant_id() 
               AND app.has_branch_access(id)
           ))
    WITH CHECK (tenant_id = app.current_tenant_id() OR 
                branch_id IN (
                    SELECT id FROM shared.branches 
                    WHERE tenant_id = app.current_tenant_id() 
                    AND app.has_branch_access(id)
                ));

-- Stock Transfer-specific RLS policies
CREATE POLICY tenant_isolation_stock_transfers ON shared.stock_transfers
    FOR ALL TO authenticated_role
    USING (tenant_id = app.current_tenant_id())
    WITH CHECK (tenant_id = app.current_tenant_id());

CREATE POLICY branch_access_stock_transfers ON shared.stock_transfers
    FOR SELECT TO authenticated_role
    USING (
        app.has_branch_access(source_branch_id) OR 
        app.has_branch_access(destination_branch_id)
    );

-- Procurement Request-specific RLS policies
CREATE POLICY tenant_isolation_procurement_requests ON shared.procurement_requests
    FOR ALL TO authenticated_role
    USING (tenant_id = app.current_tenant_id())
    WITH CHECK (tenant_id = app.current_tenant_id());

CREATE POLICY branch_access_procurement_requests ON shared.procurement_requests
    FOR SELECT TO authenticated_role
    USING (
        app.has_branch_access(requesting_branch_id) OR 
        (approving_branch_id IS NOT NULL AND app.has_branch_access(approving_branch_id))
    );

-- Branch Report-specific RLS policies
CREATE POLICY tenant_isolation_branch_reports ON shared.branch_reports
    FOR ALL TO authenticated_role
    USING (tenant_id = app.current_tenant_id())
    WITH CHECK (tenant_id = app.current_tenant_id());

CREATE POLICY branch_access_branch_reports ON shared.branch_reports
    FOR ALL TO authenticated_role
    USING (app.has_branch_access(branch_id))
    WITH CHECK (app.has_branch_access(branch_id));

-- Create indexes to support RLS performance
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_branches_tenant_id ON shared.branches(tenant_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_users_tenant_id ON shared.users(tenant_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_patients_branch_id ON shared.patients(branch_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_products_tenant_id ON shared.products(tenant_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_inventory_tenant_branch ON shared.inventory(tenant_id, branch_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_prescriptions_branch_id ON shared.prescriptions(branch_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_sales_branch_id ON shared.sales(branch_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_payments_branch_id ON shared.payments(branch_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_tenant_branch ON shared.audit_logs(tenant_id, branch_id);

-- Create trigger to automatically set tenant and branch context
CREATE OR REPLACE FUNCTION app.set_tenant_context() 
RETURNS TRIGGER AS $$
BEGIN
    -- Set tenant_id for new records if not provided
    IF NEW.tenant_id IS NULL AND TG_OP = 'INSERT' THEN
        NEW.tenant_id := app.current_tenant_id();
    END IF;
    
    -- Set branch_id for new records if not provided and applicable
    IF TG_TABLE_NAME IN ('patients', 'inventory', 'prescriptions', 'sales', 'payments') 
       AND NEW.branch_id IS NULL AND TG_OP = 'INSERT' THEN
        NEW.branch_id := app.current_branch_id();
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Create triggers for automatic context setting
CREATE TRIGGER trigger_set_tenant_context_patients
    BEFORE INSERT OR UPDATE ON shared.patients
    FOR EACH ROW EXECUTE FUNCTION app.set_tenant_context();

CREATE TRIGGER trigger_set_tenant_context_inventory
    BEFORE INSERT OR UPDATE ON shared.inventory
    FOR EACH ROW EXECUTE FUNCTION app.set_tenant_context();

CREATE TRIGGER trigger_set_tenant_context_prescriptions
    BEFORE INSERT OR UPDATE ON shared.prescriptions
    FOR EACH ROW EXECUTE FUNCTION app.set_tenant_context();

CREATE TRIGGER trigger_set_tenant_context_sales
    BEFORE INSERT OR UPDATE ON shared.sales
    FOR EACH ROW EXECUTE FUNCTION app.set_tenant_context();

CREATE TRIGGER trigger_set_tenant_context_payments
    BEFORE INSERT OR UPDATE ON shared.payments
    FOR EACH ROW EXECUTE FUNCTION app.set_tenant_context();

-- Grant necessary permissions
GRANT USAGE ON SCHEMA app TO authenticated_role;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA app TO authenticated_role;

-- Create view for cross-branch reporting (for users with appropriate permissions)
CREATE OR REPLACE VIEW shared.cross_branch_inventory AS
SELECT 
    i.id,
    i.tenant_id,
    i.branch_id,
    b.name as branch_name,
    i.product_id,
    p.name as product_name,
    p.sku,
    i.quantity_on_hand,
    i.quantity_reserved,
    i.quantity_available,
    i.cost_price,
    i.selling_price,
    i.last_counted,
    i.updated_at
FROM shared.inventory i
JOIN shared.branches b ON i.branch_id = b.id
JOIN shared.products p ON i.product_id = p.id
WHERE i.tenant_id = app.current_tenant_id();

-- Grant access to cross-branch view for users with reporting permissions
CREATE POLICY cross_branch_inventory_access ON shared.cross_branch_inventory
    FOR SELECT TO authenticated_role
    USING (
        EXISTS (
            SELECT 1 FROM shared.branch_permissions bp
            WHERE bp.user_id = current_setting('app.current_user_id', true)::UUID
            AND bp.tenant_id = app.current_tenant_id()
            AND bp.can_view_reports = true
        )
    );

-- Create function to validate branch hierarchy access
CREATE OR REPLACE FUNCTION app.validate_branch_hierarchy_access(branch_id UUID) 
RETURNS BOOLEAN AS $$
DECLARE
    current_user_branch UUID;
    user_permissions JSONB;
BEGIN
    current_user_branch := app.current_branch_id();
    
    -- Get user permissions
    SELECT permissions INTO user_permissions
    FROM shared.users 
    WHERE id = current_setting('app.current_user_id', true)::UUID 
    AND tenant_id = app.current_tenant_id();
    
    -- Super admin can access all branches
    IF user_permissions ? 'super_admin' AND user_permissions->>'super_admin' = 'true' THEN
        RETURN TRUE;
    END IF;
    
    -- Branch manager can access child branches
    IF user_permissions ? 'branch_manager' AND user_permissions->>'branch_manager' = 'true' THEN
        RETURN EXISTS (
            SELECT 1 FROM shared.branches 
            WHERE id = branch_id 
            AND (id = current_user_branch OR parent_branch_id = current_user_branch)
            AND tenant_id = app.current_tenant_id()
        );
    END IF;
    
    -- Regular users can only access their assigned branch
    RETURN branch_id = current_user_branch;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
