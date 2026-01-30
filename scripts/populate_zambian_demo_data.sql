-- Zambian Demo Data Population Script
-- This script populates the demo accounts with realistic Zambian pharmacy data

-- Clear existing demo data (keeping users)
DELETE FROM prescriptions;
DELETE FROM sales;
DELETE FROM inventory;
DELETE FROM customers;
DELETE FROM suppliers;
DELETE FROM categories;

-- Insert Zambian Categories
INSERT INTO categories (Id, Name, Description, TenantId, CreatedAt) VALUES
('cat-001', 'Antibiotics', 'Antibiotic medications for treating infections', 'demo-tenant-001', datetime('now')),
('cat-002', 'Pain Relief', 'Pain relief and anti-inflammatory medications', 'demo-tenant-001', datetime('now')),
('cat-003', 'Antimalarials', 'Medications for malaria prevention and treatment', 'demo-tenant-001', datetime('now')),
('cat-004', 'HIV/AIDS', 'Antiretroviral and HIV-related medications', 'demo-tenant-001', datetime('now')),
('cat-005', 'Diabetes', 'Diabetes management medications and supplies', 'demo-tenant-001', datetime('now')),
('cat-006', 'Cardiovascular', 'Heart and blood pressure medications', 'demo-tenant-001', datetime('now')),
('cat-007', 'Vitamins & Supplements', 'Nutritional supplements and vitamins', 'demo-tenant-001', datetime('now')),
('cat-008', 'First Aid', 'First aid supplies and wound care', 'demo-tenant-001', datetime('now'));

-- Insert Zambian Suppliers
INSERT INTO suppliers (Id, Name, ContactPerson, Phone, Email, Address, City, Country, TenantId, CreatedAt) VALUES
('sup-001', 'PharmaMed Zambia Ltd', 'Mr. James Banda', '+260 211 234567', 'sales@pharmamed.co.zm', 'Plot 1234, Cairo Road', 'Lusaka', 'Zambia', 'demo-tenant-001', datetime('now')),
('sup-002', 'ZamPharm Distributors', 'Ms. Grace Mulenga', '+260 215 876543', 'orders@zampharm.zm', 'Building 45, Independence Avenue', 'Kitwe', 'Zambia', 'demo-tenant-001', datetime('now')),
('sup-003', 'Medical Supplies Ltd', 'Dr. Peter Mwila', '+260 213 456789', 'info@medsupplies.co.zm', 'Stand 567, Great East Road', 'Lusaka', 'Zambia', 'demo-tenant-001', datetime('now')),
('sup-004', 'Global Pharma Africa', 'Mrs. Esther Tembo', '+260 212 345678', 'zambia@globalpharma.africa', 'Shop 23, Manda Hill', 'Lusaka', 'Zambia', 'demo-tenant-001', datetime('now'));

-- Insert Zambian Customers
INSERT INTO customers (Id, FirstName, LastName, Phone, Email, Address, City, DateOfBirth, Gender, TenantId, CreatedAt) VALUES
('cust-001', 'John', 'Banda', '+260 976 123456', 'john.banda@email.com', 'House 23, Chilenje', 'Lusaka', '1985-03-15', 'Male', 'demo-tenant-001', datetime('now')),
('cust-002', 'Mary', 'Mulenga', '+260 977 234567', 'mary.mulenga@email.com', 'Flat 12, Rhodes Park', 'Lusaka', '1992-07-22', 'Female', 'demo-tenant-001', datetime('now')),
('cust-003', 'Joseph', 'Mwila', '+260 966 345678', 'jmwila@email.com', 'Plot 45, Kabulonga', 'Lusaka', '1978-11-08', 'Male', 'demo-tenant-001', datetime('now')),
('cust-004', 'Grace', 'Tembo', '+260 955 456789', 'grace.tembo@email.com', 'House 89, Woodlands', 'Lusaka', '1989-05-30', 'Female', 'demo-tenant-001', datetime('now')),
('cust-005', 'Peter', 'Phiri', '+260 974 567890', 'peter.phiri@email.com', 'Stand 23, Kalingalinga', 'Lusaka', '1995-09-12', 'Male', 'demo-tenant-001', datetime('now')),
('cust-006', 'Esther', 'Chanda', '+260 965 678901', 'e.chanda@email.com', 'Flat 5, Northmead', 'Lusaka', '1982-12-25', 'Female', 'demo-tenant-001', datetime('now')),
('cust-007', 'Michael', 'Kabwe', '+260 975 789012', 'mkabwe@email.com', 'House 156, Roma', 'Lusaka', '1990-02-18', 'Male', 'demo-tenant-001', datetime('now')),
('cust-008', 'Agnes', 'Sichone', '+260 956 890123', 'agnes.s@email.com', 'Plot 789, Ibex Hill', 'Lusaka', '1987-08-14', 'Female', 'demo-tenant-001', datetime('now'));

-- Insert Zambian Inventory Items
INSERT INTO inventory (Id, Name, Description, CategoryId, SupplierId, UnitPrice, StockQuantity, ReorderLevel, ExpiryDate, Barcode, TenantId, CreatedAt) VALUES
-- Antibiotics
('inv-001', 'Amoxicillin 500mg', 'Amoxicillin capsules 500mg', 'cat-001', 'sup-001', 45.50, 150, 50, '2025-12-31', '1234567890123', 'demo-tenant-001', datetime('now')),
('inv-002', 'Azithromycin 250mg', 'Azithromycin tablets 250mg', 'cat-001', 'sup-002', 120.00, 80, 30, '2025-08-15', '1234567890124', 'demo-tenant-001', datetime('now')),
('inv-003', 'Ciprofloxacin 500mg', 'Ciprofloxacin tablets 500mg', 'cat-001', 'sup-001', 85.75, 120, 40, '2025-10-20', '1234567890125', 'demo-tenant-001', datetime('now')),

-- Pain Relief
('inv-004', 'Paracetamol 500mg', 'Paracetamol tablets 500mg', 'cat-002', 'sup-003', 15.00, 500, 100, '2026-03-31', '1234567890126', 'demo-tenant-001', datetime('now')),
('inv-005', 'Ibuprofen 400mg', 'Ibuprofen tablets 400mg', 'cat-002', 'sup-003', 25.50, 200, 60, '2025-11-30', '1234567890127', 'demo-tenant-001', datetime('now')),
('inv-006', 'Aspirin 300mg', 'Aspirin tablets 300mg', 'cat-002', 'sup-004', 18.75, 300, 80, '2026-01-15', '1234567890128', 'demo-tenant-001', datetime('now')),

-- Antimalarials
('inv-007', 'Coartem 80/480mg', 'Artemether/Lumefantrine tablets', 'cat-003', 'sup-002', 65.00, 100, 40, '2025-09-30', '1234567890129', 'demo-tenant-001', datetime('now')),
('inv-008', 'Fansidar 500/25mg', 'Sulfadoxine/Pyrimethamine tablets', 'cat-003', 'sup-001', 35.50, 150, 50, '2025-07-15', '1234567890130', 'demo-tenant-001', datetime('now')),
('inv-009', 'Quinine 300mg', 'Quinine sulfate tablets 300mg', 'cat-003', 'sup-003', 42.25, 80, 30, '2025-08-20', '1234567890131', 'demo-tenant-001', datetime('now')),

-- HIV/AIDS
('inv-010', 'TDF/3TC/EFV 300/300/600mg', 'Antiretroviral combination therapy', 'cat-004', 'sup-002', 450.00, 60, 20, '2025-06-30', '1234567890132', 'demo-tenant-001', datetime('now')),
('inv-011', 'TDF/3TC 300/300mg', 'Tenofovir/Lamivudine tablets', 'cat-004', 'sup-002', 380.00, 75, 25, '2025-07-31', '1234567890133', 'demo-tenant-001', datetime('now')),
('inv-012', 'Dolutegravir 50mg', 'Dolutegravir tablets 50mg', 'cat-004', 'sup-001', 520.00, 40, 15, '2025-05-15', '1234567890134', 'demo-tenant-001', datetime('now')),

-- Diabetes
('inv-013', 'Metformin 500mg', 'Metformin tablets 500mg', 'cat-005', 'sup-003', 28.50, 200, 60, '2026-02-28', '1234567890135', 'demo-tenant-001', datetime('now')),
('inv-014', 'Glibenclamide 5mg', 'Glibenclamide tablets 5mg', 'cat-005', 'sup-004', 35.75, 120, 40, '2025-11-15', '1234567890136', 'demo-tenant-001', datetime('now')),
('inv-015', 'Insulin Glargine', 'Long-acting insulin 100U/ml', 'cat-005', 'sup-001', 280.00, 30, 10, '2025-04-30', '1234567890137', 'demo-tenant-001', datetime('now')),

-- Cardiovascular
('inv-016', 'Amlodipine 10mg', 'Amlodipine tablets 10mg', 'cat-006', 'sup-003', 45.00, 150, 50, '2025-10-15', '1234567890138', 'demo-tenant-001', datetime('now')),
('inv-017', 'Lisinopril 10mg', 'Lisinopril tablets 10mg', 'cat-006', 'sup-004', 38.50, 180, 60, '2025-12-31', '1234567890139', 'demo-tenant-001', datetime('now')),
('inv-018', 'Atorvastatin 20mg', 'Atorvastatin tablets 20mg', 'cat-006', 'sup-001', 125.00, 90, 30, '2025-09-20', '1234567890140', 'demo-tenant-001', datetime('now')),

-- Vitamins & Supplements
('inv-019', 'Vitamin C 500mg', 'Ascorbic acid tablets 500mg', 'cat-007', 'sup-004', 12.50, 400, 100, '2026-04-30', '1234567890141', 'demo-tenant-001', datetime('now')),
('inv-020', 'Multivitamin Adults', 'Complete multivitamin supplement', 'cat-007', 'sup-003', 85.00, 100, 30, '2025-08-31', '1234567890142', 'demo-tenant-001', datetime('now')),
('inv-021', 'Folic Acid 5mg', 'Folic acid tablets 5mg', 'cat-007', 'sup-002', 22.75, 200, 60, '2026-01-31', '1234567890143', 'demo-tenant-001', datetime('now')),

-- First Aid
('inv-022', 'Band-Aid Assorted', 'Adhesive bandages assorted sizes', 'cat-008', 'sup-004', 35.00, 50, 20, '2026-06-30', '1234567890144', 'demo-tenant-001', datetime('now')),
('inv-023', 'Gauze Roll 10cm', 'Sterile gauze roll 10cm x 5m', 'cat-008', 'sup-003', 45.50, 30, 10, '2025-12-15', '1234567890145', 'demo-tenant-001', datetime('now')),
('inv-024', 'Antiseptic Solution', 'Povidone-iodine solution 10%', 'cat-008', 'sup-001', 65.00, 40, 15, '2025-07-20', '1234567890146', 'demo-tenant-001', datetime('now'));

-- Insert Sample Sales Data
INSERT INTO sales (Id, CustomerId, TotalAmount, DiscountAmount, FinalAmount, PaymentMethod, Status, TenantId, CreatedAt) VALUES
('sale-001', 'cust-001', 245.50, 12.25, 233.25, 'Cash', 'Completed', 'demo-tenant-001', datetime('now', '-7 days')),
('sale-002', 'cust-002', 680.00, 34.00, 646.00, 'Mobile Money', 'Completed', 'demo-tenant-001', datetime('now', '-6 days')),
('sale-003', 'cust-003', 125.75, 0.00, 125.75, 'Cash', 'Completed', 'demo-tenant-001', datetime('now', '-5 days')),
('sale-004', 'cust-004', 890.25, 44.50, 845.75, 'Insurance', 'Completed', 'demo-tenant-001', datetime('now', '-4 days')),
('sale-005', 'cust-005', 156.00, 7.80, 148.20, 'Cash', 'Completed', 'demo-tenant-001', datetime('now', '-3 days')),
('sale-006', 'cust-006', 450.00, 22.50, 427.50, 'Mobile Money', 'Completed', 'demo-tenant-001', datetime('now', '-2 days')),
('sale-007', 'cust-007', 78.50, 0.00, 78.50, 'Cash', 'Completed', 'demo-tenant-001', datetime('now', '-1 days')),
('sale-008', 'cust-008', 1120.00, 56.00, 1064.00, 'Insurance', 'Completed', 'demo-tenant-001', datetime('now'));

-- Insert Sample Prescription Data
INSERT INTO prescriptions (Id, CustomerId, DoctorName, DoctorRegistration, Diagnosis, Notes, Status, TenantId, CreatedAt) VALUES
('pres-001', 'cust-001', 'Dr. Robert Mwansa', 'MED/2023/0456', 'Upper respiratory tract infection', 'Complete full course of antibiotics', 'Dispensed', 'demo-tenant-001', datetime('now', '-7 days')),
('pres-002', 'cust-002', 'Dr. Susan Banda', 'MED/2023/0789', 'Hypertension', 'Continue regular monitoring', 'Dispensed', 'demo-tenant-001', datetime('now', '-6 days')),
('pres-003', 'cust-003', 'Dr. James Phiri', 'MED/2023/0234', 'Type 2 Diabetes', 'Diet and exercise recommended', 'Dispensed', 'demo-tenant-001', datetime('now', '-5 days')),
('pres-004', 'cust-004', 'Dr. Grace Mulenga', 'MED/2023/0567', 'Malaria (uncomplicated)', 'Rest and hydration advised', 'Dispensed', 'demo-tenant-001', datetime('now', '-4 days')),
('pres-005', 'cust-005', 'Dr. Peter Tembo', 'MED/2023/0890', 'HIV - ART initiation', 'Counseling completed', 'Active', 'demo-tenant-001', datetime('now', '-3 days')),
('pres-006', 'cust-006', 'Dr. Esther Chanda', 'MED/2023/0123', 'Dyslipidemia', 'Lifestyle modification required', 'Dispensed', 'demo-tenant-001', datetime('now', '-2 days'));

-- Update tenant info with Zambian details
UPDATE tenants SET 
    Name = 'Umi Health Demo Pharmacy - Lusaka',
    Email = 'demo@umihealth.co.zm',
    Phone = '+260 211 234567',
    Address = 'Plot 1234, Cairo Road, Lusaka, Zambia',
    City = 'Lusaka',
    Country = 'Zambia',
    PostalCode = '10101',
    RegistrationNumber = 'PHM/2023/001234',
    TaxIdentificationNumber = '100123456789'
WHERE Id = 'demo-tenant-001';

-- Update user info with Zambian context
UPDATE users SET 
    TenantId = 'demo-tenant-001'
WHERE Username IN ('admin2', 'cashier', 'pharmacist');

-- Create a summary report
SELECT 'Zambian Demo Data Population Complete' as Status,
       COUNT(DISTINCT c.Id) as Categories,
       COUNT(DISTINCT s.Id) as Suppliers,
       COUNT(DISTINCT cu.Id) as Customers,
       COUNT(DISTINCT i.Id) as InventoryItems,
       COUNT(DISTINCT sa.Id) as Sales,
       COUNT(DISTINCT p.Id) as Prescriptions
FROM categories c, suppliers s, customers cu, inventory i, sales sa, prescriptions p
WHERE c.TenantId = 'demo-tenant-001' 
  AND s.TenantId = 'demo-tenant-001'
  AND cu.TenantId = 'demo-tenant-001'
  AND i.TenantId = 'demo-tenant-001'
  AND sa.TenantId = 'demo-tenant-001'
  AND p.TenantId = 'demo-tenant-001';
