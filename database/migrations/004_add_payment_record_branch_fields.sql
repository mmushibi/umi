-- Add BranchId, UpdatedAt, and DeletedAt to PaymentRecord
-- Migration for Option B: Make PaymentRecord branch-aware and soft-deletable

-- Add BranchId column (nullable to avoid migration conflicts)
ALTER TABLE shared.payment_records 
ADD COLUMN branch_id UUID NULL;

-- Add UpdatedAt column with default value
ALTER TABLE shared.payment_records 
ADD COLUMN updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW();

-- Add DeletedAt column for soft delete support
ALTER TABLE shared.payment_records 
ADD COLUMN deleted_at TIMESTAMP WITH TIME ZONE NULL;

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS ix_payment_records_branch_id 
ON shared.payment_records(branch_id);

CREATE INDEX IF NOT EXISTS ix_payment_records_updated_at 
ON shared.payment_records(updated_at DESC);

CREATE INDEX IF NOT EXISTS ix_payment_records_deleted_at 
ON shared.payment_records(deleted_at);

-- Add comments
COMMENT ON COLUMN shared.payment_records.branch_id IS 'Optional branch association for payment records';
COMMENT ON COLUMN shared.payment_records.updated_at IS 'Last update timestamp for sync ordering';
COMMENT ON COLUMN shared.payment_records.deleted_at IS 'Soft delete timestamp';
