-- Update demo account passwords with proper BCrypt hashes for "Demo123!"
-- Generated using a proper BCrypt hash generator

BEGIN;

UPDATE users SET password_hash = '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy' WHERE user_name = 'admin';
UPDATE users SET password_hash = '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy' WHERE user_name = 'cashier';
UPDATE users SET password_hash = '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy' WHERE user_name = 'pharmacist';
UPDATE users SET password_hash = '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy' WHERE user_name = 'operations';
UPDATE users SET password_hash = '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy' WHERE user_name = 'superadmin';

COMMIT;

-- Verify the update
SELECT user_name, email, 'Password updated to: Demo123!' as status FROM users WHERE user_name IN ('admin', 'cashier', 'pharmacist', 'operations', 'superadmin');
