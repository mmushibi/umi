-- Test DataSyncService functionality
-- 1. Test branch-level payment sync

-- Insert test data with branch_id and deleted_at
INSERT INTO public.payments (id, tenant_id, sale_id, payment_method, amount, reference_number, status, created_at, updated_at, branch_id, deleted_at)
VALUES 
    (gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), 'cash', 100.00, 'TEST001', 'pending', NOW(), NOW(), gen_random_uuid(), NULL),
    (gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), 'card', 200.00, 'TEST002', 'approved', NOW(), NOW(), gen_random_uuid(), NULL),
    (gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), 'mobile', 150.00, 'TEST003', 'pending', NOW(), NOW(), gen_random_uuid(), NOW());

-- 2. Test branch-level filtering
SELECT 
    id,
    tenant_id,
    branch_id,
    amount,
    status,
    deleted_at,
    updated_at
FROM public.payments 
WHERE branch_id IS NOT NULL 
  AND deleted_at IS NULL
ORDER BY updated_at DESC;

-- 3. Test soft delete filtering
SELECT 
    COUNT(*) as total_payments,
    COUNT(*) FILTER (WHERE deleted_at IS NULL) as active_payments,
    COUNT(*) FILTER (WHERE deleted_at IS NOT NULL) as deleted_payments
FROM public.payments;

-- 4. Test UpdatedAt ordering
SELECT 
    id,
    amount,
    updated_at
FROM public.payments 
WHERE deleted_at IS NULL
ORDER BY updated_at DESC
LIMIT 5;
