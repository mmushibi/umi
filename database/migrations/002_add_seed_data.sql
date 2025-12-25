-- Migration 002: Add Seed Data
-- Sample data for testing and demonstration

-- Insert sample tenant
INSERT INTO tenants (name, slug, domain, subscription_plan, status, settings) VALUES
('Umi Pharmacy - Main Branch', 'umi-pharmacy', 'umi-pharmacy.umihealth.com', 'Care Plus', 'active', '{
    "business_hours": {"monday": "08:00-18:00", "tuesday": "08:00-18:00", "wednesday": "08:00-18:00", "thursday": "08:00-18:00", "friday": "08:00-18:00", "saturday": "09:00-14:00", "sunday": "closed"},
    "currency": "KES",
    "tax_rate": 16.0,
    "low_stock_alerts": true,
    "expiry_alert_days": 90
}') RETURNING id;

-- Insert sample branch
INSERT INTO branches (tenant_id, name, code, address, phone, email, is_main_branch, status) VALUES
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Main Branch', 'MAIN', '123 Nairobi Road, Nairobi, Kenya', '+254-20-1234567', 'main@umi-pharmacy.com', true, 'active');

-- Insert sample supplier
INSERT INTO suppliers (tenant_id, name, contact_person, phone, email, address, license_number, status) VALUES
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Kenya Medical Supplies', 'John Mwangi', '+254-20-7654321', 'info@kms.co.ke', '456 Industrial Area, Nairobi', 'SUP-2023-001', 'active'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Global Pharma Ltd', 'Sarah Ochieng', '+254-20-9876543', 'sales@globalpharma.co.ke', '789 Export Processing Zone, Nairobi', 'SUP-2023-002', 'active');

-- Insert sample products
INSERT INTO products (tenant_id, name, generic_name, brand, category, description, strength, dosage_form, requires_prescription, is_controlled_substance, barcode, manufacturer, supplier_id, reorder_level, max_level, unit_cost, selling_price, tax_rate, status) VALUES
-- Prescription medications
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Amoxicillin 500mg', 'Amoxicillin Trihydrate', 'Amoxil', 'Antibiotics', 'Broad-spectrum antibiotic for bacterial infections', '500mg', 'Capsule', true, false, '1234567890123', 'GlaxoSmithKline', (SELECT id FROM suppliers WHERE name = 'Kenya Medical Supplies'), 50, 200, 15.50, 25.00, 16.00, 'active'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Paracetamol 500mg', 'Paracetamol', 'Panadol', 'Analgesics', 'Pain reliever and fever reducer', '500mg', 'Tablet', false, false, '2345678901234', 'GSK', (SELECT id FROM suppliers WHERE name = 'Kenya Medical Supplies'), 100, 500, 2.50, 5.00, 16.00, 'active'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Lisinopril 10mg', 'Lisinopril', 'Zestril', 'Antihypertensives', 'ACE inhibitor for blood pressure', '10mg', 'Tablet', true, false, '3456789012345', 'AstraZeneca', (SELECT id FROM suppliers WHERE name = 'Global Pharma Ltd'), 30, 150, 25.00, 45.00, 16.00, 'active'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Metformin 500mg', 'Metformin Hydrochloride', 'Glucophage', 'Antidiabetics', 'Oral diabetes medication', '500mg', 'Tablet', true, false, '4567890123456', 'Merck', (SELECT id FROM suppliers WHERE name = 'Global Pharma Ltd'), 40, 200, 8.00, 15.00, 16.00, 'active'),
-- Over-the-counter medications
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Vitamin C 1000mg', 'Ascorbic Acid', 'Cebion', 'Vitamins', 'Vitamin C supplement for immune support', '1000mg', 'Tablet', false, false, '5678901234567', 'Bayer', (SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 60, 300, 3.50, 8.00, 16.00, 'active'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Ibuprofen 400mg', 'Ibuprofen', 'Advil', 'Analgesics', 'NSAID for pain and inflammation', '400mg', 'Tablet', false, false, '6789012345678', 'Pfizer', (SELECT id FROM suppliers WHERE name = 'Kenya Medical Supplies'), 80, 400, 4.00, 9.00, 16.00, 'active'),
-- Medical supplies
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Disposable Gloves', 'Nitrile Gloves', 'SafeTouch', 'Medical Supplies', 'Powder-free nitrile examination gloves', NULL, 'Gloves', false, false, '7890123456789', 'Ansell', (SELECT id FROM suppliers WHERE name = 'Global Pharma Ltd'), 20, 100, 150.00, 250.00, 16.00, 'active'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'Face Masks', 'Surgical Mask', 'MedMask', 'Medical Supplies', '3-ply surgical face masks', NULL, 'Mask', false, false, '8901234567890', '3M', (SELECT id FROM suppliers WHERE name = 'Kenya Medical Supplies'), 50, 200, 2.00, 5.00, 16.00, 'active');

-- Insert sample inventory
INSERT INTO inventory (tenant_id, branch_id, product_id, quantity_on_hand, quantity_reserved, batch_number, expiry_date, cost_price, selling_price, location) VALUES
-- Prescription medications
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM products WHERE name = 'Amoxicillin 500mg'), 150, 0, 'AMX2023001', '2025-12-31', 15.50, 25.00, 'A1-B1'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM products WHERE name = 'Paracetamol 500mg'), 300, 0, 'PAR2023001', '2024-12-31', 2.50, 5.00, 'A1-B2'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM products WHERE name = 'Lisinopril 10mg'), 80, 0, 'LIS2023001', '2025-06-30', 25.00, 45.00, 'A1-B3'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM products WHERE name = 'Metformin 500mg'), 120, 0, 'MET2023001', '2025-09-30', 8.00, 15.00, 'A1-B4'),
-- Over-the-counter medications
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM products WHERE name = 'Vitamin C 1000mg'), 200, 0, 'VIT2023001', '2024-08-31', 3.50, 8.00, 'B1-C1'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM products WHERE name = 'Ibuprofen 400mg'), 250, 0, 'IBU2023001', '2024-10-31', 4.00, 9.00, 'B1-C2'),
-- Medical supplies
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM products WHERE name = 'Disposable Gloves'), 50, 0, 'GLV2023001', '2025-03-31', 150.00, 250.00, 'C1-D1'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM products WHERE name = 'Face Masks'), 100, 0, 'MSK2023001', '2024-07-31', 2.00, 5.00, 'C1-D2');

-- Insert sample users
INSERT INTO users (tenant_id, branch_id, email, username, password_hash, first_name, last_name, phone, role, permissions, is_active) VALUES
-- Admin
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), 'admin@umi-pharmacy.com', 'pharmacy_admin', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj6ukx.LFvO.', 'James', 'Karanja', '+254-712-345678', 'admin', '["user_management", "inventory_management", "sales_management", "reports"]', true),
-- Pharmacist
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), 'pharmacist@umi-pharmacy.com', 'pharmacist1', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj6ukx.LFvO.', 'Mary', 'Wanjiku', '+254-723-456789', 'pharmacist', '["prescription_management", "inventory_management", "patient_management"]', true),
-- Cashier
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), 'cashier@umi-pharmacy.com', 'cashier1', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj6ukx.LFvO.', 'Peter', 'Ochieng', '+254-734-567890', 'cashier', '["sales_management", "payment_processing"]', true);

-- Insert sample patients
INSERT INTO patients (tenant_id, patient_number, first_name, last_name, date_of_birth, gender, phone, email, address, emergency_contact_name, emergency_contact_phone, allergies, chronic_conditions, insurance_provider, insurance_number) VALUES
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'PAT-12345678-000001', 'John', 'Mwangi', '1985-03-15', 'Male', '+254-712-123456', 'john.mwangi@email.com', '123 Kileleshwa, Nairobi', 'Grace Mwangi', '+254-712-123457', ARRAY['Penicillin'], ARRAY['Hypertension'], 'NHIF', 'NHIF123456'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'PAT-12345678-000002', 'Sarah', 'Ochieng', '1992-07-22', 'Female', '+254-723-234567', 'sarah.ochieng@email.com', '456 Westlands, Nairobi', 'David Ochieng', '+254-723-234568', ARRAY['Sulfa'], ARRAY['Diabetes Type 2'], 'Jubilee', 'JUB789012'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), 'PAT-12345678-000003', 'Michael', 'Kamau', '1978-11-08', 'Male', '+254-734-345678', 'michael.kamau@email.com', '789 Karen, Nairobi', 'Lucy Kamau', '+254-734-345679', '{}', ARRAY['Asthma'], 'UAP', 'UAP345678');

-- Insert sample prescriptions
INSERT INTO prescriptions (tenant_id, branch_id, patient_id, prescriber_id, prescription_number, diagnosis, notes, status) VALUES
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM patients WHERE first_name = 'John' AND last_name = 'Mwangi'), (SELECT id FROM users WHERE role = 'pharmacist'), 'RX-12345678-000001', 'Upper respiratory tract infection', 'Complete full course of antibiotics', 'pending'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM patients WHERE first_name = 'Sarah' AND last_name = 'Ochieng'), (SELECT id FROM users WHERE role = 'pharmacist'), 'RX-12345678-000002', 'Type 2 Diabetes', 'Monitor blood sugar regularly', 'pending'),
((SELECT id FROM tenants WHERE slug = 'umi-pharmacy'), (SELECT id FROM branches WHERE code = 'MAIN'), (SELECT id FROM patients WHERE first_name = 'Michael' AND last_name = 'Kamau'), (SELECT id FROM users WHERE role = 'pharmacist'), 'RX-12345678-000003', 'Hypertension', 'Continue with regular exercise', 'pending');

-- Insert sample prescription items
INSERT INTO prescription_items (prescription_id, product_id, dosage, frequency, duration, quantity, instructions) VALUES
((SELECT id FROM prescriptions WHERE prescription_number = 'RX-12345678-000001'), (SELECT id FROM products WHERE name = 'Amoxicillin 500mg'), '500mg', '3 times daily', '7 days', 21, 'Take with food'),
((SELECT id FROM prescriptions WHERE prescription_number = 'RX-12345678-000002'), (SELECT id FROM products WHERE name = 'Metformin 500mg'), '500mg', '2 times daily', '30 days', 60, 'Take after meals'),
((SELECT id FROM prescriptions WHERE prescription_number = 'RX-12345678-000003'), (SELECT id FROM products WHERE name = 'Lisinopril 10mg'), '10mg', 'Once daily', '30 days', 30, 'Take in the morning');
