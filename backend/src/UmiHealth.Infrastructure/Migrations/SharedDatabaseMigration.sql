-- UmiHealth Shared Database Migration
-- This script creates the shared database schema for multi-tenant architecture

-- Create schemas
CREATE SCHEMA IF NOT EXISTS shared;
CREATE SCHEMA IF NOT EXISTS superadmin;

-- Create shared schema tables
-- Tenants table
CREATE TABLE IF NOT EXISTS shared.tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    subdomain VARCHAR(100) NOT NULL UNIQUE,
    database_name VARCHAR(100) NOT NULL UNIQUE,
    status VARCHAR(50) DEFAULT 'active',
    subscription_plan VARCHAR(50) DEFAULT 'basic',
    max_branches INTEGER DEFAULT 1,
    max_users INTEGER DEFAULT 5,
    settings JSONB DEFAULT '{}',
    billing_info JSONB DEFAULT '{}',
    compliance_settings JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL
);

-- Branches table
CREATE TABLE IF NOT EXISTS shared.branches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES shared.tenants(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    code VARCHAR(50) NOT NULL,
    address TEXT,
    phone VARCHAR(50),
    email VARCHAR(100),
    license_number VARCHAR(100),
    operating_hours JSONB DEFAULT '{}',
    settings JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL,
    UNIQUE(tenant_id, code)
);

-- Users table
CREATE TABLE IF NOT EXISTS shared.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES shared.tenants(id) ON DELETE CASCADE,
    branch_id UUID REFERENCES shared.branches(id) ON DELETE SET NULL,
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    username VARCHAR(255),
    phone VARCHAR(50),
    role VARCHAR(50) NOT NULL,
    branch_access UUID[] DEFAULT '{}',
    permissions JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT true,
    email_verified BOOLEAN DEFAULT false,
    phone_verified BOOLEAN DEFAULT false,
    two_factor_enabled BOOLEAN DEFAULT false,
    two_factor_secret VARCHAR(255),
    last_login TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL,
    UNIQUE(tenant_id, email)
);

-- Subscriptions table
CREATE TABLE IF NOT EXISTS shared.subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES shared.tenants(id) ON DELETE CASCADE,
    plan_type VARCHAR(50) NOT NULL,
    status VARCHAR(50) DEFAULT 'active',
    billing_cycle VARCHAR(20) DEFAULT 'monthly',
    amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'ZMW',
    features JSONB DEFAULT '{}',
    limits JSONB DEFAULT '{}',
    start_date TIMESTAMP WITH TIME ZONE NOT NULL,
    end_date TIMESTAMP WITH TIME ZONE,
    next_billing TIMESTAMP WITH TIME ZONE,
    auto_renew BOOLEAN DEFAULT true,
    trial_days_used INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL
);

-- Super Admin schema tables
-- Super admin users
CREATE TABLE IF NOT EXISTS superadmin.super_admin_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    role VARCHAR(50) DEFAULT 'superadmin',
    permissions TEXT[] DEFAULT '{}',
    is_active BOOLEAN DEFAULT true,
    two_factor_secret VARCHAR(255),
    backup_codes TEXT[] DEFAULT '{}',
    preferences JSONB DEFAULT '{}',
    last_login TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE NULL
);

-- System settings
CREATE TABLE IF NOT EXISTS superadmin.system_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key VARCHAR(100) NOT NULL UNIQUE,
    value TEXT NOT NULL,
    category VARCHAR(50) NOT NULL,
    data_type VARCHAR(20) DEFAULT 'string',
    description TEXT,
    validation_rules JSONB DEFAULT '{}',
    is_public BOOLEAN DEFAULT false,
    updated_by VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- System analytics
CREATE TABLE IF NOT EXISTS superadmin.system_analytics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    date DATE NOT NULL UNIQUE,
    tenant_stats JSONB DEFAULT '{}',
    user_role_stats JSONB DEFAULT '{}',
    api_usage_stats JSONB DEFAULT '{}',
    performance_metrics JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Security events
CREATE TABLE IF NOT EXISTS superadmin.security_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type VARCHAR(50) NOT NULL,
    severity VARCHAR(20) NOT NULL,
    user_id VARCHAR(255),
    tenant_id VARCHAR(255),
    ip_address INET,
    user_agent TEXT,
    resource VARCHAR(255),
    action VARCHAR(100),
    failure_reason VARCHAR(500),
    details JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Super admin logs
CREATE TABLE IF NOT EXISTS superadmin.super_admin_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    log_level VARCHAR(20) NOT NULL,
    category VARCHAR(50) NOT NULL,
    message TEXT NOT NULL,
    user_id VARCHAR(255),
    tenant_id VARCHAR(255),
    ip_address INET,
    user_agent TEXT,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- System notifications
CREATE TABLE IF NOT EXISTS superadmin.system_notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    type VARCHAR(20) NOT NULL,
    target_audience VARCHAR(50),
    target_tenants TEXT[] DEFAULT '{}',
    target_users TEXT[] DEFAULT '{}',
    is_active BOOLEAN DEFAULT true,
    scheduled_for TIMESTAMP WITH TIME ZONE,
    created_by VARCHAR(255),
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Backup records
CREATE TABLE IF NOT EXISTS superadmin.backup_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL,
    status VARCHAR(20) DEFAULT 'pending',
    tenant_id VARCHAR(255),
    file_path VARCHAR(500),
    file_size BIGINT,
    checksum VARCHAR(255),
    compression_type VARCHAR(20) DEFAULT 'gzip',
    encryption_enabled BOOLEAN DEFAULT false,
    created_by VARCHAR(255),
    error_message TEXT,
    configuration JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP WITH TIME ZONE
);

-- API keys
CREATE TABLE IF NOT EXISTS superadmin.api_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    key_hash VARCHAR(255) NOT NULL,
    prefix VARCHAR(20),
    permissions TEXT[] DEFAULT '{}',
    allowed_endpoints TEXT[] DEFAULT '{}',
    allowed_ip_addresses INET[] DEFAULT '{}',
    rate_limit_per_hour INTEGER DEFAULT 1000,
    is_active BOOLEAN DEFAULT true,
    expires_at TIMESTAMP WITH TIME ZONE,
    last_used_at TIMESTAMP WITH TIME ZONE,
    created_by VARCHAR(255),
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Super admin reports
CREATE TABLE IF NOT EXISTS superadmin.super_admin_reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL,
    description TEXT,
    status VARCHAR(20) DEFAULT 'generating',
    parameters JSONB DEFAULT '{}',
    results JSONB DEFAULT '{}',
    file_path VARCHAR(500),
    file_size BIGINT,
    generated_by VARCHAR(255),
    generated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better performance
-- Shared schema indexes
CREATE INDEX IF NOT EXISTS idx_tenants_subdomain ON shared.tenants(subdomain);
CREATE INDEX IF NOT EXISTS idx_tenants_status ON shared.tenants(status);
CREATE INDEX IF NOT EXISTS idx_branches_tenant_id ON shared.branches(tenant_id);
CREATE INDEX IF NOT EXISTS idx_branches_code ON shared.branches(code);
CREATE INDEX IF NOT EXISTS idx_users_tenant_id ON shared.users(tenant_id);
CREATE INDEX IF NOT EXISTS idx_users_branch_id ON shared.users(branch_id);
CREATE INDEX IF NOT EXISTS idx_users_email ON shared.users(email);
CREATE INDEX IF NOT EXISTS idx_users_role ON shared.users(role);
CREATE INDEX IF NOT EXISTS idx_subscriptions_tenant_id ON shared.subscriptions(tenant_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_status ON shared.subscriptions(status);

-- Super admin schema indexes
CREATE INDEX IF NOT EXISTS idx_security_events_created_at ON superadmin.security_events(created_at);
CREATE INDEX IF NOT EXISTS idx_security_events_event_type ON superadmin.security_events(event_type);
CREATE INDEX IF NOT EXISTS idx_security_events_severity ON superadmin.security_events(severity);
CREATE INDEX IF NOT EXISTS idx_super_admin_logs_created_at ON superadmin.super_admin_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_super_admin_logs_log_level ON superadmin.super_admin_logs(log_level);
CREATE INDEX IF NOT EXISTS idx_system_analytics_date ON superadmin.system_analytics(date);
CREATE INDEX IF NOT EXISTS idx_system_notifications_created_at ON superadmin.system_notifications(created_at);
CREATE INDEX IF NOT EXISTS idx_system_notifications_is_active ON superadmin.system_notifications(is_active);
CREATE INDEX IF NOT EXISTS idx_backup_records_created_at ON superadmin.backup_records(created_at);
CREATE INDEX IF NOT EXISTS idx_backup_records_status ON superadmin.backup_records(status);
CREATE INDEX IF NOT EXISTS idx_api_keys_created_at ON superadmin.api_keys(created_at);
CREATE INDEX IF NOT EXISTS idx_api_keys_is_active ON superadmin.api_keys(is_active);

-- Create updated_at trigger function
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updated_at columns
CREATE TRIGGER update_tenants_updated_at BEFORE UPDATE ON shared.tenants 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_branches_updated_at BEFORE UPDATE ON shared.branches 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON shared.users 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_subscriptions_updated_at BEFORE UPDATE ON shared.subscriptions 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_super_admin_users_updated_at BEFORE UPDATE ON superadmin.super_admin_users 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_system_settings_updated_at BEFORE UPDATE ON superadmin.system_settings 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_system_notifications_updated_at BEFORE UPDATE ON superadmin.system_notifications 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_api_keys_updated_at BEFORE UPDATE ON superadmin.api_keys 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Insert default system settings
INSERT INTO superadmin.system_settings (key, value, category, description, is_public) VALUES
('system_name', 'UmiHealth', 'general', 'System name displayed in UI', true),
('system_version', '1.0.0', 'general', 'Current system version', true),
('max_file_size_mb', '10', 'storage', 'Maximum file upload size in MB', false),
('allowed_file_extensions', '["jpg","jpeg","png","gif","pdf","doc","docx","txt","csv","xlsx","xls"]', 'storage', 'Allowed file extensions for uploads', false),
('default_subscription_plan', 'Care', 'subscriptions', 'Default subscription plan for new tenants', false),
('trial_period_days', '14', 'subscriptions', 'Number of days for trial period', false),
('password_min_length', '8', 'security', 'Minimum password length', false),
('session_timeout_minutes', '30', 'security', 'Session timeout in minutes', false),
('max_login_attempts', '5', 'security', 'Maximum login attempts before lockout', false),
('lockout_duration_minutes', '15', 'security', 'Account lockout duration in minutes', false),
('backup_retention_days', '30', 'backup', 'Number of days to retain backups', false),
('enable_audit_logging', 'true', 'audit', 'Enable comprehensive audit logging', false)
ON CONFLICT (key) DO NOTHING;

-- Create default super admin user (password: Admin123!)
INSERT INTO superadmin.super_admin_users (email, password_hash, first_name, last_name, role, permissions) VALUES
('admin@umihealth.com', '$2a$10$YourHashedPasswordHere', 'System', 'Administrator', 'superadmin', ARRAY['all'])
ON CONFLICT (email) DO NOTHING;

COMMIT;
