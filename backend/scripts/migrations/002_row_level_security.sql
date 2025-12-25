-- UmiHealth Multi-Tenant Pharmacy POS System
-- Row-Level Security (RLS) Implementation
-- PostgreSQL 15+

-- Enable Row Level Security on all tenant-scoped tables
ALTER TABLE branches ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE role_claims ENABLE ROW LEVEL SECURITY;
ALTER TABLE user_claims ENABLE ROW LEVEL SECURITY;
ALTER TABLE refresh_tokens ENABLE ROW LEVEL SECURITY;
ALTER TABLE products ENABLE ROW LEVEL SECURITY;
ALTER TABLE suppliers ENABLE ROW LEVEL SECURITY;
ALTER TABLE inventory ENABLE ROW LEVEL SECURITY;
ALTER TABLE stock_transactions ENABLE ROW LEVEL SECURITY;
ALTER TABLE purchase_orders ENABLE ROW LEVEL SECURITY;
ALTER TABLE purchase_order_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE patients ENABLE ROW LEVEL SECURITY;
ALTER TABLE prescriptions ENABLE ROW LEVEL SECURITY;
ALTER TABLE prescription_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE sales ENABLE ROW LEVEL SECURITY;
ALTER TABLE sale_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE payments ENABLE ROW LEVEL SECURITY;
ALTER TABLE sale_returns ENABLE ROW LEVEL SECURITY;

-- Create authenticated_users role for RLS policies
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated_users') THEN
        CREATE ROLE authenticated_users;
    END IF;
END
$$;

-- Grant necessary permissions to authenticated_users
GRANT USAGE ON SCHEMA public TO authenticated_users;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO authenticated_users;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO authenticated_users;

-- Default RLS policies - Deny all access by default
CREATE POLICY deny_all ON branches FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON users FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON roles FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON user_roles FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON role_claims FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON user_claims FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON refresh_tokens FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON products FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON suppliers FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON inventory FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON stock_transactions FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON purchase_orders FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON purchase_order_items FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON patients FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON prescriptions FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON prescription_items FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON sales FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON sale_items FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON payments FOR ALL TO authenticated_users USING (false);
CREATE POLICY deny_all ON sale_returns FOR ALL TO authenticated_users USING (false);

-- Tenant isolation policies
CREATE POLICY tenant_isolation ON branches
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON users
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON roles
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON user_roles
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON role_claims
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON user_claims
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON refresh_tokens
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON products
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON suppliers
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON inventory
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON stock_transactions
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON purchase_orders
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON purchase_order_items
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON patients
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON prescriptions
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON prescription_items
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON sales
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON sale_items
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON payments
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

CREATE POLICY tenant_isolation ON sale_returns
    FOR ALL TO authenticated_users
    USING (tenant_id = current_setting('app.current_tenant_id', true)::UUID)
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::UUID);

-- Branch-level access control policies
CREATE POLICY branch_access ON users
    FOR SELECT TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID AND
        (
            -- Users can see their own record
            id = current_setting('app.current_user_id', true)::UUID OR
            -- Admin can see all users in tenant
            current_setting('app.user_role', true) IN ('Admin', 'SuperAdmin') OR
            -- Users can see users in their branch
            (branch_id = current_setting('app.current_branch_id', true)::UUID AND branch_id IS NOT NULL)
        )
    );

CREATE POLICY branch_access ON inventory
    FOR ALL TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID AND
        (
            -- Admin can access all branches
            current_setting('app.user_role', true) IN ('Admin', 'SuperAdmin') OR
            -- Users can only access their branch inventory
            branch_id = current_setting('app.current_branch_id', true)::UUID
        )
    );

CREATE POLICY branch_access ON sales
    FOR ALL TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID AND
        (
            -- Admin can access all branches
            current_setting('app.user_role', true) IN ('Admin', 'SuperAdmin') OR
            -- Users can only access their branch sales
            branch_id = current_setting('app.current_branch_id', true)::UUID
        )
    );

CREATE POLICY branch_access ON prescriptions
    FOR ALL TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID AND
        (
            -- Admin can access all branches
            current_setting('app.user_role', true) IN ('Admin', 'SuperAdmin') OR
            -- Pharmacists can access prescriptions they created or dispensed
            (doctor_id = current_setting('app.current_user_id', true)::UUID OR 
             dispensed_by = current_setting('app.current_user_id', true)::UUID) OR
            -- Users can access prescriptions in their branch
            (EXISTS (
                SELECT 1 FROM branches b 
                WHERE b.id = prescriptions.branch_id 
                AND b.id = current_setting('app.current_branch_id', true)::UUID
            ) AND current_setting('app.user_role', true) IN ('Pharmacist', 'Cashier'))
        )
    );

-- User-specific policies for sensitive data
CREATE POLICY user_self_access ON refresh_tokens
    FOR ALL TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID AND
        user_id = current_setting('app.current_user_id', true)::UUID
    );

CREATE POLICY user_self_access ON user_claims
    FOR ALL TO authenticated_users
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::UUID AND
        user_id = current_setting('app.current_user_id', true)::UUID
    );

-- Create function to set tenant context
CREATE OR REPLACE FUNCTION set_tenant_context(tenant_uuid UUID, user_uuid UUID, user_role TEXT, branch_uuid UUID DEFAULT NULL)
RETURNS VOID AS $$
BEGIN
    -- Set session variables for RLS policies
    PERFORM set_config('app.current_tenant_id', tenant_uuid::TEXT, true);
    PERFORM set_config('app.current_user_id', user_uuid::TEXT, true);
    PERFORM set_config('app.user_role', user_role, true);
    
    IF branch_uuid IS NOT NULL THEN
        PERFORM set_config('app.current_branch_id', branch_uuid::TEXT, true);
    ELSE
        PERFORM set_config('app.current_branch_id', '', true);
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Create function to clear tenant context
CREATE OR REPLACE FUNCTION clear_tenant_context()
RETURNS VOID AS $$
BEGIN
    PERFORM set_config('app.current_tenant_id', '', true);
    PERFORM set_config('app.current_user_id', '', true);
    PERFORM set_config('app.user_role', '', true);
    PERFORM set_config('app.current_branch_id', '', true);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Create function to validate tenant access
CREATE OR REPLACE FUNCTION validate_tenant_access(tenant_uuid UUID)
RETURNS BOOLEAN AS $$
DECLARE
    current_tenant UUID;
BEGIN
    current_tenant := current_setting('app.current_tenant_id', true)::UUID;
    
    -- If no tenant context set, deny access
    IF current_tenant IS NULL THEN
        RETURN FALSE;
    END IF;
    
    -- Check if the requested tenant matches the current tenant context
    RETURN current_tenant = tenant_uuid;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Create function to validate branch access
CREATE OR REPLACE FUNCTION validate_branch_access(branch_uuid UUID)
RETURNS BOOLEAN AS $$
DECLARE
    current_tenant UUID;
    current_branch UUID;
    user_role TEXT;
BEGIN
    current_tenant := current_setting('app.current_tenant_id', true)::UUID;
    current_branch := current_setting('app.current_branch_id', true)::UUID;
    user_role := current_setting('app.user_role', true);
    
    -- Admin and SuperAdmin can access any branch in their tenant
    IF user_role IN ('Admin', 'SuperAdmin') THEN
        -- Verify branch belongs to current tenant
        RETURN EXISTS (
            SELECT 1 FROM branches 
            WHERE id = branch_uuid AND tenant_id = current_tenant
        );
    END IF;
    
    -- Other users can only access their assigned branch
    RETURN current_branch = branch_uuid;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Create audit trigger function for tenant security
CREATE OR REPLACE FUNCTION tenant_audit_trigger()
RETURNS TRIGGER AS $$
DECLARE
    current_tenant UUID;
    current_user UUID;
BEGIN
    current_tenant := current_setting('app.current_tenant_id', true)::UUID;
    current_user := current_setting('app.current_user_id', true)::UUID;
    
    -- Ensure tenant_id is set and matches current tenant
    IF TG_OP = 'INSERT' THEN
        IF NEW.tenant_id IS NULL THEN
            NEW.tenant_id := current_tenant;
        ELSIF NEW.tenant_id != current_tenant THEN
            RAISE EXCEPTION 'Tenant ID mismatch: cannot insert data for different tenant';
        END IF;
        
        -- Set audit fields
        NEW.created_by := current_user::TEXT;
        NEW.updated_by := current_user::TEXT;
        RETURN NEW;
    END IF;
    
    IF TG_OP = 'UPDATE' THEN
        -- Prevent tenant_id changes
        IF OLD.tenant_id != NEW.tenant_id THEN
            RAISE EXCEPTION 'Tenant ID cannot be changed';
        END IF;
        
        -- Update audit fields
        NEW.updated_by := current_user::TEXT;
        RETURN NEW;
    END IF;
    
    IF TG_OP = 'DELETE' THEN
        -- Soft delete with audit
        UPDATE TG_TABLE_NAME SET 
            is_deleted = TRUE,
            deleted_at = NOW(),
            deleted_by = current_user::TEXT
        WHERE id = OLD.id;
        RETURN NULL;
    END IF;
    
    RETURN NULL;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Apply audit triggers to tenant-scoped tables
CREATE TRIGGER tenant_audit_branches
    BEFORE INSERT OR UPDATE OR DELETE ON branches
    FOR EACH ROW EXECUTE FUNCTION tenant_audit_trigger();

CREATE TRIGGER tenant_audit_users
    BEFORE INSERT OR UPDATE OR DELETE ON users
    FOR EACH ROW EXECUTE FUNCTION tenant_audit_trigger();

CREATE TRIGGER tenant_audit_roles
    BEFORE INSERT OR UPDATE OR DELETE ON roles
    FOR EACH ROW EXECUTE FUNCTION tenant_audit_trigger();

CREATE TRIGGER tenant_audit_products
    BEFORE INSERT OR UPDATE OR DELETE ON products
    FOR EACH ROW EXECUTE FUNCTION tenant_audit_trigger();

CREATE TRIGGER tenant_audit_suppliers
    BEFORE INSERT OR UPDATE OR DELETE ON suppliers
    FOR EACH ROW EXECUTE FUNCTION tenant_audit_trigger();

CREATE TRIGGER tenant_audit_inventory
    BEFORE INSERT OR UPDATE OR DELETE ON inventory
    FOR EACH ROW EXECUTE FUNCTION tenant_audit_trigger();

CREATE TRIGGER tenant_audit_patients
    BEFORE INSERT OR UPDATE OR DELETE ON patients
    FOR EACH ROW EXECUTE FUNCTION tenant_audit_trigger();

CREATE TRIGGER tenant_audit_prescriptions
    BEFORE INSERT OR UPDATE OR DELETE ON prescriptions
    FOR EACH ROW EXECUTE FUNCTION tenant_audit_trigger();

CREATE TRIGGER tenant_audit_sales
    BEFORE INSERT OR UPDATE OR DELETE ON sales
    FOR EACH ROW EXECUTE FUNCTION tenant_audit_trigger();

-- Grant execute permissions on security functions to authenticated_users
GRANT EXECUTE ON FUNCTION set_tenant_context TO authenticated_users;
GRANT EXECUTE ON FUNCTION clear_tenant_context TO authenticated_users;
GRANT EXECUTE ON FUNCTION validate_tenant_access TO authenticated_users;
GRANT EXECUTE ON FUNCTION validate_branch_access TO authenticated_users;
