-- Performance Indexes for Umi Health Database
-- Optimized for multi-tenant queries

-- Tenant-based indexes (composite indexes for tenant isolation)
CREATE INDEX idx_branches_tenant_id ON branches(tenant_id);
CREATE INDEX idx_users_tenant_id ON users(tenant_id);
CREATE INDEX idx_users_tenant_branch ON users(tenant_id, branch_id);
CREATE INDEX idx_products_tenant_id ON products(tenant_id);
CREATE INDEX idx_suppliers_tenant_id ON suppliers(tenant_id);
CREATE INDEX idx_inventory_tenant_branch ON inventory(tenant_id, branch_id);
CREATE INDEX idx_patients_tenant_id ON patients(tenant_id);
CREATE INDEX idx_prescriptions_tenant_id ON prescriptions(tenant_id);
CREATE INDEX idx_prescriptions_tenant_branch ON prescriptions(tenant_id, branch_id);
CREATE INDEX idx_sales_tenant_id ON sales(tenant_id);
CREATE INDEX idx_sales_tenant_branch ON sales(tenant_id, branch_id);
CREATE INDEX idx_payments_tenant_id ON payments(tenant_id);
CREATE INDEX idx_stock_movements_tenant_id ON stock_movements(tenant_id);
CREATE INDEX idx_audit_logs_tenant_id ON audit_logs(tenant_id);

-- Business logic indexes
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_active ON users(is_active) WHERE is_active = TRUE;
CREATE INDEX idx_users_role ON users(role);

CREATE INDEX idx_products_barcode ON products(barcode) WHERE barcode IS NOT NULL;
CREATE INDEX idx_products_category ON products(category);
CREATE INDEX idx_products_status ON products(status) WHERE status = 'active';
CREATE INDEX idx_products_supplier ON products(supplier_id) WHERE supplier_id IS NOT NULL;
CREATE INDEX idx_products_controlled ON products(is_controlled_substance) WHERE is_controlled_substance = TRUE;

CREATE INDEX idx_inventory_product ON inventory(product_id);
CREATE INDEX idx_inventory_branch_product ON inventory(branch_id, product_id);
CREATE INDEX idx_inventory_expiry ON inventory(expiry_date) WHERE expiry_date IS NOT NULL;
CREATE INDEX idx_inventory_low_stock ON inventory(tenant_id, branch_id) 
    WHERE quantity_on_hand <= reorder_level;

CREATE INDEX idx_patients_number ON patients(patient_number);
CREATE INDEX idx_patients_name ON patients(first_name, last_name);
CREATE INDEX idx_patients_phone ON patients(phone) WHERE phone IS NOT NULL;

CREATE INDEX idx_prescriptions_patient ON prescriptions(patient_id);
CREATE INDEX idx_prescriptions_prescriber ON prescriptions(prescriber_id);
CREATE INDEX idx_prescriptions_status ON prescriptions(status);
CREATE INDEX idx_prescriptions_number ON prescriptions(prescription_number);
CREATE INDEX idx_prescriptions_date ON prescriptions(created_at);

CREATE INDEX idx_prescription_items_prescription ON prescription_items(prescription_id);
CREATE INDEX idx_prescription_items_product ON prescription_items(product_id);

CREATE INDEX idx_sales_patient ON sales(patient_id) WHERE patient_id IS NOT NULL;
CREATE INDEX idx_sales_cashier ON sales(cashier_id);
CREATE INDEX idx_sales_status ON sales(status);
CREATE INDEX idx_sales_payment_status ON sales(payment_status);
CREATE INDEX idx_sales_date ON sales(created_at);
CREATE INDEX idx_sales_number ON sales(sale_number);

CREATE INDEX idx_sale_items_sale ON sale_items(sale_id);
CREATE INDEX idx_sale_items_product ON sale_items(product_id);

CREATE INDEX idx_payments_sale ON payments(sale_id);
CREATE INDEX idx_payments_status ON payments(status);
CREATE INDEX idx_payments_method ON payments(payment_method);
CREATE INDEX idx_payments_date ON payments(created_at);

CREATE INDEX idx_stock_movements_product ON stock_movements(product_id);
CREATE INDEX idx_stock_movements_type ON stock_movements(movement_type);
CREATE INDEX idx_stock_movements_date ON stock_movements(created_at);
CREATE INDEX idx_stock_movements_reference ON stock_movements(reference_type, reference_id);

CREATE INDEX idx_audit_logs_table ON audit_logs(table_name);
CREATE INDEX idx_audit_logs_user ON audit_logs(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_audit_logs_action ON audit_logs(action);
CREATE INDEX idx_audit_logs_date ON audit_logs(created_at);

-- Full-text search indexes
CREATE INDEX idx_products_search ON products USING gin(to_tsvector('english', name || ' ' || COALESCE(generic_name, '') || ' ' || COALESCE(brand, '')));
CREATE INDEX idx_patients_search ON patients USING gin(to_tsvector('english', first_name || ' ' || last_name || ' ' || COALESCE(patient_number, '')));

-- Unique constraints for business rules
ALTER TABLE branches ADD CONSTRAINT uk_branches_tenant_code UNIQUE(tenant_id, code);
ALTER TABLE products ADD CONSTRAINT uk_products_tenant_barcode UNIQUE(tenant_id, barcode) WHERE barcode IS NOT NULL;
ALTER TABLE patients ADD CONSTRAINT uk_patients_tenant_number UNIQUE(tenant_id, patient_number);
ALTER TABLE prescriptions ADD CONSTRAINT uk_prescriptions_tenant_number UNIQUE(tenant_id, prescription_number);
ALTER TABLE sales ADD CONSTRAINT uk_sales_tenant_number UNIQUE(tenant_id, sale_number);
