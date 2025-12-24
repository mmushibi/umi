-- Database Triggers for Umi Health System
-- Automated timestamp updates and audit logging

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply updated_at triggers to all relevant tables
CREATE TRIGGER update_tenants_updated_at
    BEFORE UPDATE ON tenants
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_branches_updated_at
    BEFORE UPDATE ON branches
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_suppliers_updated_at
    BEFORE UPDATE ON suppliers
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_products_updated_at
    BEFORE UPDATE ON products
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_patients_updated_at
    BEFORE UPDATE ON patients
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_prescriptions_updated_at
    BEFORE UPDATE ON prescriptions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_sales_updated_at
    BEFORE UPDATE ON sales
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_payments_updated_at
    BEFORE UPDATE ON payments
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Audit logging function
CREATE OR REPLACE FUNCTION audit_trigger_function()
RETURNS TRIGGER AS $$
DECLARE
    user_uuid UUID;
    tenant_uuid UUID;
BEGIN
    -- Get current user and tenant from session
    user_uuid = current_setting('app.current_user_id', true)::UUID;
    tenant_uuid = current_setting('app.current_tenant_id', true)::UUID;
    
    IF TG_OP = 'DELETE' THEN
        INSERT INTO audit_logs (
            tenant_id, 
            user_id, 
            table_name, 
            record_id, 
            action, 
            old_values,
            ip_address,
            user_agent
        ) VALUES (
            tenant_uuid,
            user_uuid,
            TG_TABLE_NAME,
            OLD.id,
            TG_OP,
            to_jsonb(OLD),
            inet_client_addr(),
            current_setting('request.user_agent', true)
        );
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit_logs (
            tenant_id, 
            user_id, 
            table_name, 
            record_id, 
            action, 
            old_values,
            new_values,
            ip_address,
            user_agent
        ) VALUES (
            tenant_uuid,
            user_uuid,
            TG_TABLE_NAME,
            NEW.id,
            TG_OP,
            to_jsonb(OLD),
            to_jsonb(NEW),
            inet_client_addr(),
            current_setting('request.user_agent', true)
        );
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO audit_logs (
            tenant_id, 
            user_id, 
            table_name, 
            record_id, 
            action, 
            new_values,
            ip_address,
            user_agent
        ) VALUES (
            tenant_uuid,
            user_uuid,
            TG_TABLE_NAME,
            NEW.id,
            TG_OP,
            to_jsonb(NEW),
            inet_client_addr(),
            current_setting('request.user_agent', true)
        );
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Apply audit triggers to sensitive tables
CREATE TRIGGER audit_branches
    AFTER INSERT OR UPDATE OR DELETE ON branches
    FOR EACH ROW
    EXECUTE FUNCTION audit_trigger_function();

CREATE TRIGGER audit_users
    AFTER INSERT OR UPDATE OR DELETE ON users
    FOR EACH ROW
    EXECUTE FUNCTION audit_trigger_function();

CREATE TRIGGER audit_products
    AFTER INSERT OR UPDATE OR DELETE ON products
    FOR EACH ROW
    EXECUTE FUNCTION audit_trigger_function();

CREATE TRIGGER audit_patients
    AFTER INSERT OR UPDATE OR DELETE ON patients
    FOR EACH ROW
    EXECUTE FUNCTION audit_trigger_function();

CREATE TRIGGER audit_prescriptions
    AFTER INSERT OR UPDATE OR DELETE ON prescriptions
    FOR EACH ROW
    EXECUTE FUNCTION audit_trigger_function();

CREATE TRIGGER audit_sales
    AFTER INSERT OR UPDATE OR DELETE ON sales
    FOR EACH ROW
    EXECUTE FUNCTION audit_trigger_function();

CREATE TRIGGER audit_payments
    AFTER INSERT OR UPDATE OR DELETE ON payments
    FOR EACH ROW
    EXECUTE FUNCTION audit_trigger_function();

-- Inventory management triggers
-- Trigger to update inventory when sales are made
CREATE OR REPLACE FUNCTION update_inventory_on_sale()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE inventory 
        SET quantity_reserved = quantity_reserved + NEW.quantity,
            last_stock_update = NOW()
        WHERE product_id = NEW.product_id
        AND branch_id = (SELECT branch_id FROM sales WHERE id = NEW.sale_id);
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        UPDATE inventory 
        SET quantity_reserved = quantity_reserved - OLD.quantity + NEW.quantity,
            last_stock_update = NOW()
        WHERE product_id = NEW.product_id
        AND branch_id = (SELECT branch_id FROM sales WHERE id = NEW.sale_id);
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE inventory 
        SET quantity_reserved = quantity_reserved - OLD.quantity,
            last_stock_update = NOW()
        WHERE product_id = OLD.product_id
        AND branch_id = (SELECT branch_id FROM sales WHERE id = OLD.sale_id);
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_inventory_on_sale
    AFTER INSERT OR UPDATE OR DELETE ON sale_items
    FOR EACH ROW
    EXECUTE FUNCTION update_inventory_on_sale();

-- Trigger to actually deduct inventory when payment is completed
CREATE OR REPLACE FUNCTION deduct_inventory_on_payment()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'UPDATE' AND NEW.status = 'completed' AND OLD.status != 'completed' THEN
        UPDATE inventory 
        SET quantity_on_hand = quantity_on_hand - (
            SELECT COALESCE(SUM(si.quantity), 0) 
            FROM sale_items si 
            WHERE si.sale_id = NEW.sale_id
        ),
        quantity_reserved = quantity_reserved - (
            SELECT COALESCE(SUM(si.quantity), 0) 
            FROM sale_items si 
            WHERE si.sale_id = NEW.sale_id
        ),
        last_stock_update = NOW()
        WHERE branch_id = (SELECT branch_id FROM sales WHERE id = NEW.sale_id)
        AND product_id IN (
            SELECT product_id FROM sale_items WHERE sale_id = NEW.sale_id
        );
        
        -- Create stock movement records
        INSERT INTO stock_movements (
            tenant_id, branch_id, product_id, movement_type, quantity, 
            reference_type, reference_id, created_by, created_at
        )
        SELECT 
            s.tenant_id, s.branch_id, si.product_id, 'out', si.quantity,
            'sale', s.id, current_setting('app.current_user_id', true)::UUID, NOW()
        FROM sales s
        JOIN sale_items si ON s.id = si.sale_id
        WHERE s.id = NEW.sale_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_deduct_inventory_on_payment
    AFTER UPDATE ON payments
    FOR EACH ROW
    EXECUTE FUNCTION deduct_inventory_on_payment();

-- Trigger to generate patient numbers
CREATE OR REPLACE FUNCTION generate_patient_number()
RETURNS TRIGGER AS $$
DECLARE
    new_number TEXT;
    tenant_prefix TEXT;
BEGIN
    -- Generate patient number with tenant prefix
    tenant_prefix := SUBSTRING(NEW.tenant_id::TEXT, 1, 8);
    
    SELECT 'PAT-' || tenant_prefix || '-' || LPAD((COUNT(*) + 1)::TEXT, 6, '0')
    INTO new_number
    FROM patients
    WHERE tenant_id = NEW.tenant_id;
    
    NEW.patient_number := new_number;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_generate_patient_number
    BEFORE INSERT ON patients
    FOR EACH ROW
    EXECUTE FUNCTION generate_patient_number();

-- Trigger to generate prescription numbers
CREATE OR REPLACE FUNCTION generate_prescription_number()
RETURNS TRIGGER AS $$
DECLARE
    new_number TEXT;
    tenant_prefix TEXT;
BEGIN
    tenant_prefix := SUBSTRING(NEW.tenant_id::TEXT, 1, 8);
    
    SELECT 'RX-' || tenant_prefix || '-' || LPAD((COUNT(*) + 1)::TEXT, 6, '0')
    INTO new_number
    FROM prescriptions
    WHERE tenant_id = NEW.tenant_id
    AND DATE(created_at) = CURRENT_DATE;
    
    NEW.prescription_number := new_number;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_generate_prescription_number
    BEFORE INSERT ON prescriptions
    FOR EACH ROW
    EXECUTE FUNCTION generate_prescription_number();

-- Trigger to generate sale numbers
CREATE OR REPLACE FUNCTION generate_sale_number()
RETURNS TRIGGER AS $$
DECLARE
    new_number TEXT;
    tenant_prefix TEXT;
BEGIN
    tenant_prefix := SUBSTRING(NEW.tenant_id::TEXT, 1, 8);
    
    SELECT 'SALE-' || tenant_prefix || '-' || LPAD((COUNT(*) + 1)::TEXT, 6, '0')
    INTO new_number
    FROM sales
    WHERE tenant_id = NEW.tenant_id
    AND DATE(created_at) = CURRENT_DATE;
    
    NEW.sale_number := new_number;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_generate_sale_number
    BEFORE INSERT ON sales
    FOR EACH ROW
    EXECUTE FUNCTION generate_sale_number();
