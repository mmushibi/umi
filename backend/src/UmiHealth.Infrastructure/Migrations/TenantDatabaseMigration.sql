-- UmiHealth Tenant Database Migration
-- This script creates the tenant-specific database schema for individual tenant databases

-- Create tenant schema tables
-- Patients table
CREATE TABLE IF NOT EXISTS tenant_patients (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id UUID NOT NULL,
    patient_number VARCHAR(50) NOT NULL UNIQUE,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    date_of_birth DATE,
    gender VARCHAR(20),
    phone VARCHAR(50),
    email VARCHAR(100),
    address TEXT,
    emergency_contact JSONB DEFAULT '{}',
    medical_history JSONB DEFAULT '{}',
    allergies JSONB DEFAULT '{}',
    insurance_info JSONB DEFAULT '{}',
    blood_type VARCHAR(10),
    national_id VARCHAR(50),
    status VARCHAR(20) DEFAULT 'active',
    notes TEXT,
    created_by UUID,
    updated_by UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL
);

-- Products table
CREATE TABLE IF NOT EXISTS tenant_products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id UUID NOT NULL,
    sku VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    generic_name VARCHAR(255),
    description TEXT,
    category VARCHAR(100),
    subcategory VARCHAR(100),
    manufacturer VARCHAR(100),
    brand VARCHAR(100),
    strength VARCHAR(50),
    form VARCHAR(50) DEFAULT 'tablet',
    dosage_form VARCHAR(50),
    route_of_administration VARCHAR(50),
    storage_requirements JSONB DEFAULT '{}',
    pricing JSONB DEFAULT '{}',
    barcode VARCHAR(100),
    images JSONB DEFAULT '[]',
    tags TEXT[] DEFAULT '{}',
    is_prescription_required BOOLEAN DEFAULT false,
    is_controlled_substance BOOLEAN DEFAULT false,
    requires_refrigeration BOOLEAN DEFAULT false,
    shelf_life_months INTEGER,
    reorder_level INTEGER DEFAULT 10,
    max_level INTEGER DEFAULT 100,
    unit_of_measure VARCHAR(20) DEFAULT 'units',
    supplier VARCHAR(255),
    supplier_code VARCHAR(100),
    cost_price DECIMAL(10,2),
    selling_price DECIMAL(10,2),
    status VARCHAR(20) DEFAULT 'active',
    notes TEXT,
    created_by UUID,
    updated_by UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL
);

-- Inventory table
CREATE TABLE IF NOT EXISTS tenant_inventory (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id UUID NOT NULL,
    product_id UUID NOT NULL REFERENCES tenant_products(id) ON DELETE CASCADE,
    batch_number VARCHAR(100),
    expiry_date DATE,
    quantity_on_hand INTEGER DEFAULT 0,
    quantity_reserved INTEGER DEFAULT 0,
    quantity_available INTEGER GENERATED ALWAYS AS (quantity_on_hand - quantity_reserved) STORED,
    unit_cost DECIMAL(10,2),
    location VARCHAR(100),
    rack VARCHAR(50),
    shelf VARCHAR(50),
    bin VARCHAR(50),
    supplier VARCHAR(255),
    date_received DATE,
    condition VARCHAR(20) DEFAULT 'good',
    storage_temperature DECIMAL(5,2),
    last_count_date DATE,
    variance_quantity INTEGER DEFAULT 0,
    variance_reason TEXT,
    notes TEXT,
    created_by UUID,
    updated_by UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL,
    UNIQUE(branch_id, product_id, batch_number)
);

-- Prescriptions table
CREATE TABLE IF NOT EXISTS tenant_prescriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id UUID NOT NULL,
    patient_id UUID NOT NULL REFERENCES tenant_patients(id) ON DELETE CASCADE,
    prescription_number VARCHAR(50) NOT NULL UNIQUE,
    prescriber_id UUID,
    prescriber_name VARCHAR(255),
    prescriber_license VARCHAR(100),
    prescriber_phone VARCHAR(50),
    date_prescribed DATE NOT NULL,
    date_dispensed DATE,
    expiry_date DATE,
    status VARCHAR(20) DEFAULT 'pending',
    priority VARCHAR(20) DEFAULT 'normal',
    diagnosis TEXT,
    clinical_notes TEXT,
    items JSONB DEFAULT '[]',
    dispensed_items JSONB DEFAULT '[]',
    total_cost DECIMAL(10,2),
    patient_paid DECIMAL(10,2),
    insurance_covered DECIMAL(10,2),
    payment_method VARCHAR(50),
    pharmacist_id UUID,
    pharmacist_notes TEXT,
    verified_by UUID,
    verified_at TIMESTAMP WITH TIME ZONE,
    created_by UUID,
    updated_by UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL
);

-- Sales table
CREATE TABLE IF NOT EXISTS tenant_sales (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id UUID NOT NULL,
    sale_number VARCHAR(50) NOT NULL UNIQUE,
    patient_id UUID REFERENCES tenant_patients(id) ON DELETE SET NULL,
    prescription_id UUID REFERENCES tenant_prescriptions(id) ON DELETE SET NULL,
    cashier_id UUID,
    date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    subtotal DECIMAL(10,2) NOT NULL,
    tax_amount DECIMAL(10,2) DEFAULT 0,
    discount_amount DECIMAL(10,2) DEFAULT 0,
    total_amount DECIMAL(10,2) NOT NULL,
    amount_paid DECIMAL(10,2) DEFAULT 0,
    change_amount DECIMAL(10,2) DEFAULT 0,
    payment_method VARCHAR(50),
    payment_status VARCHAR(20) DEFAULT 'pending',
    customer_name VARCHAR(255),
    customer_phone VARCHAR(50),
    customer_email VARCHAR(100),
    items JSONB DEFAULT '[]',
    discounts JSONB DEFAULT '[]',
    taxes JSONB DEFAULT '[]',
    notes TEXT,
    receipt_number VARCHAR(50),
    invoice_number VARCHAR(50),
    is_return BOOLEAN DEFAULT false,
    original_sale_id UUID,
    created_by UUID,
    updated_by UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL
);

-- Payments table
CREATE TABLE IF NOT EXISTS tenant_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id UUID NOT NULL,
    sale_id UUID REFERENCES tenant_sales(id) ON DELETE CASCADE,
    payment_method VARCHAR(50) NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'ZMW',
    transaction_reference VARCHAR(100),
    payment_gateway VARCHAR(50),
    gateway_response JSONB DEFAULT '{}',
    status VARCHAR(20) DEFAULT 'pending',
    payment_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    processed_by UUID,
    notes TEXT,
    refund_amount DECIMAL(10,2) DEFAULT 0,
    refund_reason TEXT,
    refund_date TIMESTAMP WITH TIME ZONE,
    created_by UUID,
    updated_by UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL
);

-- Audit logs table
CREATE TABLE IF NOT EXISTS tenant_audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    branch_id UUID,
    user_id UUID,
    entity_type VARCHAR(50) NOT NULL,
    entity_id UUID,
    action VARCHAR(100) NOT NULL,
    old_values JSONB DEFAULT '{}',
    new_values JSONB DEFAULT '{}',
    ip_address INET,
    user_agent TEXT,
    session_id VARCHAR(255),
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    metadata JSONB DEFAULT '{}'
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_patients_branch_id ON tenant_patients(branch_id);
CREATE INDEX IF NOT EXISTS idx_patients_patient_number ON tenant_patients(patient_number);
CREATE INDEX IF NOT EXISTS idx_patients_phone ON tenant_patients(phone);
CREATE INDEX IF NOT EXISTS idx_patients_email ON tenant_patients(email);
CREATE INDEX IF NOT EXISTS idx_patients_status ON tenant_patients(status);

CREATE INDEX IF NOT EXISTS idx_products_branch_id ON tenant_products(branch_id);
CREATE INDEX IF NOT EXISTS idx_products_sku ON tenant_products(sku);
CREATE INDEX IF NOT EXISTS idx_products_category ON tenant_products(category);
CREATE INDEX IF NOT EXISTS idx_products_barcode ON tenant_products(barcode);
CREATE INDEX IF NOT EXISTS idx_products_status ON tenant_products(status);
CREATE INDEX IF NOT EXISTS idx_products_is_prescription_required ON tenant_products(is_prescription_required);

CREATE INDEX IF NOT EXISTS idx_inventory_branch_id ON tenant_inventory(branch_id);
CREATE INDEX IF NOT EXISTS idx_inventory_product_id ON tenant_inventory(product_id);
CREATE INDEX IF NOT EXISTS idx_inventory_batch_number ON tenant_inventory(batch_number);
CREATE INDEX IF NOT EXISTS idx_inventory_expiry_date ON tenant_inventory(expiry_date);
CREATE INDEX IF NOT EXISTS idx_inventory_quantity_available ON tenant_inventory(quantity_available);

CREATE INDEX IF NOT EXISTS idx_prescriptions_branch_id ON tenant_prescriptions(branch_id);
CREATE INDEX IF NOT EXISTS idx_prescriptions_patient_id ON tenant_prescriptions(patient_id);
CREATE INDEX IF NOT EXISTS idx_prescriptions_prescription_number ON tenant_prescriptions(prescription_number);
CREATE INDEX IF NOT EXISTS idx_prescriptions_date_prescribed ON tenant_prescriptions(date_prescribed);
CREATE INDEX IF NOT EXISTS idx_prescriptions_status ON tenant_prescriptions(status);

CREATE INDEX IF NOT EXISTS idx_sales_branch_id ON tenant_sales(branch_id);
CREATE INDEX IF NOT EXISTS idx_sales_patient_id ON tenant_sales(patient_id);
CREATE INDEX IF NOT EXISTS idx_sales_prescription_id ON tenant_sales(prescription_id);
CREATE INDEX IF NOT EXISTS idx_sales_sale_number ON tenant_sales(sale_number);
CREATE INDEX IF NOT EXISTS idx_sales_date ON tenant_sales(date);
CREATE INDEX IF NOT EXISTS idx_sales_payment_status ON tenant_sales(payment_status);

CREATE INDEX IF NOT EXISTS idx_payments_branch_id ON tenant_payments(branch_id);
CREATE INDEX IF NOT EXISTS idx_payments_sale_id ON tenant_payments(sale_id);
CREATE INDEX IF NOT EXISTS idx_payments_transaction_reference ON tenant_payments(transaction_reference);
CREATE INDEX IF NOT EXISTS idx_payments_status ON tenant_payments(status);
CREATE INDEX IF NOT EXISTS idx_payments_payment_date ON tenant_payments(payment_date);

CREATE INDEX IF NOT EXISTS idx_audit_logs_branch_id ON tenant_audit_logs(branch_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_user_id ON tenant_audit_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity_type ON tenant_audit_logs(entity_type);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity_id ON tenant_audit_logs(entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON tenant_audit_logs(action);
CREATE INDEX IF NOT EXISTS idx_audit_logs_timestamp ON tenant_audit_logs(timestamp);

-- Create updated_at trigger function (if not exists)
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updated_at columns
CREATE TRIGGER update_patients_updated_at BEFORE UPDATE ON tenant_patients 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_products_updated_at BEFORE UPDATE ON tenant_products 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_inventory_updated_at BEFORE UPDATE ON tenant_inventory 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_prescriptions_updated_at BEFORE UPDATE ON tenant_prescriptions 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_sales_updated_at BEFORE UPDATE ON tenant_sales 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_payments_updated_at BEFORE UPDATE ON tenant_payments 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Create function to generate patient numbers
CREATE OR REPLACE FUNCTION generate_patient_number()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.patient_number IS NULL OR NEW.patient_number = '' THEN
        NEW.patient_number := 'PT' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || LPAD(NEXTVAL('patient_number_seq')::TEXT, 4, '0');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create sequence for patient numbers
CREATE SEQUENCE IF NOT EXISTS patient_number_seq START 1;

-- Create trigger for patient number generation
CREATE TRIGGER generate_patient_number_trigger
    BEFORE INSERT ON tenant_patients
    FOR EACH ROW
    EXECUTE FUNCTION generate_patient_number();

-- Create function to generate sale numbers
CREATE OR REPLACE FUNCTION generate_sale_number()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.sale_number IS NULL OR NEW.sale_number = '' THEN
        NEW.sale_number := 'SL' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || LPAD(NEXTVAL('sale_number_seq')::TEXT, 4, '0');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create sequence for sale numbers
CREATE SEQUENCE IF NOT EXISTS sale_number_seq START 1;

-- Create trigger for sale number generation
CREATE TRIGGER generate_sale_number_trigger
    BEFORE INSERT ON tenant_sales
    FOR EACH ROW
    EXECUTE FUNCTION generate_sale_number();

-- Create function to generate prescription numbers
CREATE OR REPLACE FUNCTION generate_prescription_number()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.prescription_number IS NULL OR NEW.prescription_number = '' THEN
        NEW.prescription_number := 'RX' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || LPAD(NEXTVAL('prescription_number_seq')::TEXT, 4, '0');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create sequence for prescription numbers
CREATE SEQUENCE IF NOT EXISTS prescription_number_seq START 1;

-- Create trigger for prescription number generation
CREATE TRIGGER generate_prescription_number_trigger
    BEFORE INSERT ON tenant_prescriptions
    FOR EACH ROW
    EXECUTE FUNCTION generate_prescription_number();

-- Create function to check inventory levels
CREATE OR REPLACE FUNCTION check_inventory_levels()
RETURNS TRIGGER AS $$
BEGIN
    -- Check if quantity is below reorder level
    IF NEW.quantity_available < (SELECT reorder_level FROM tenant_products WHERE id = NEW.product_id) THEN
        -- Here you could insert a notification or log entry
        -- For now, we'll just log a warning
        RAISE WARNING 'Low inventory alert: Product % has only % units remaining', NEW.product_id, NEW.quantity_available;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for inventory level checking
CREATE TRIGGER check_inventory_levels_trigger
    AFTER UPDATE ON tenant_inventory
    FOR EACH ROW
    EXECUTE FUNCTION check_inventory_levels();

COMMIT;
