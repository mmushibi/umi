-- SQLite script to create demo data for Umi Health
-- This script creates demo users for Admin, Cashier, Pharmacist, and Operations portals

-- Clear existing data
DELETE FROM user_roles;
DELETE FROM users;
DELETE FROM tenants;

-- Create demo tenant
INSERT INTO tenants (Id, Name, Email, Status, SubscriptionPlan, CreatedAt) VALUES 
('demo-tenant-001', 'Umi Health Demo Pharmacy', 'demo@umihealth.com', 'active', 'Enterprise', datetime('now'));

-- Create demo users
INSERT INTO users (Id, Username, Email, Password, FirstName, LastName, Role, Status, TenantId, CreatedAt) VALUES
-- Admin User
('user-admin-001', 'admin', 'admin@demo.umihealth.com', 'Demo123!', 'John', 'Administrator', 'Admin', 'active', 'demo-tenant-001', datetime('now')),

-- Cashier User
('user-cashier-001', 'cashier', 'cashier@demo.umihealth.com', 'Demo123!', 'Sarah', 'Cashier', 'Cashier', 'active', 'demo-tenant-001', datetime('now')),

-- Pharmacist User
('user-pharmacist-001', 'pharmacist', 'pharmacist@demo.umihealth.com', 'Demo123!', 'Dr. Michael', 'Pharmacist', 'Pharmacist', 'active', 'demo-tenant-001', datetime('now')),

-- Operations User
('user-operations-001', 'operations', 'operations@demo.umihealth.com', 'Demo123!', 'Lisa', 'Operations', 'Operations', 'active', 'demo-tenant-001', datetime('now')),

-- SuperAdmin User
('user-superadmin-001', 'superadmin', 'superadmin@umihealth.com', 'Demo123!', 'Super', 'Administrator', 'SuperAdmin', 'active', 'demo-tenant-001', datetime('now'));

-- Display created demo accounts
SELECT 'Demo Accounts Created:' as info;
SELECT 'Username | Password | Role | Portal' as details;
SELECT '--- | --- | --- | ---';
SELECT 'admin | Demo123! | Admin | Admin Portal';
SELECT 'cashier | Demo123! | Cashier | Cashier Portal';
SELECT 'pharmacist | Demo123! | Pharmacist | Pharmacist Portal';
SELECT 'operations | Demo123! | Operations | Operations Portal';
SELECT 'superadmin | Demo123! | SuperAdmin | Super Admin Portal';

-- Show tenant info
SELECT 'Tenant Information:' as info;
SELECT Name || ' (' || Email || ')' as details FROM tenants WHERE Id = 'demo-tenant-001';
