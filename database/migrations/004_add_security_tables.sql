-- Security Audit Tables Migration
-- Add persistent storage for security events and IP blocking

-- Security Events Table
CREATE TABLE security_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type VARCHAR(50) NOT NULL,
    description TEXT NOT NULL,
    ip_address INET,
    user_id UUID REFERENCES users(id),
    user_agent TEXT,
    request_path VARCHAR(500),
    risk_level INTEGER NOT NULL CHECK (risk_level BETWEEN 1 AND 4),
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    metadata JSONB DEFAULT '{}',
    tenant_id UUID REFERENCES tenants(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Blocked IP Addresses Table
CREATE TABLE blocked_ip_addresses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ip_address INET NOT NULL UNIQUE,
    reason TEXT NOT NULL,
    blocked_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    unblock_at TIMESTAMP WITH TIME ZONE NOT NULL,
    blocked_by UUID REFERENCES users(id),
    is_permanent BOOLEAN DEFAULT FALSE,
    tenant_id UUID REFERENCES tenants(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Security Incidents Table (for high-priority events)
CREATE TABLE security_incidents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    incident_type VARCHAR(50) NOT NULL,
    severity VARCHAR(20) NOT NULL CHECK (severity IN ('Low', 'Medium', 'High', 'Critical')),
    title VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    status VARCHAR(20) DEFAULT 'Open' CHECK (status IN ('Open', 'Investigating', 'Resolved', 'Closed')),
    ip_addresses INET[],
    affected_users UUID[] REFERENCES users(id),
    metadata JSONB DEFAULT '{}',
    tenant_id UUID REFERENCES tenants(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    resolved_at TIMESTAMP WITH TIME ZONE
);

-- Security Metrics Table (for aggregated data)
CREATE TABLE security_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    metric_date DATE NOT NULL,
    tenant_id UUID REFERENCES tenants(id),
    total_events INTEGER DEFAULT 0,
    failed_logins INTEGER DEFAULT 0,
    successful_logins INTEGER DEFAULT 0,
    blocked_ips INTEGER DEFAULT 0,
    high_risk_events INTEGER DEFAULT 0,
    suspicious_activities INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(tenant_id, metric_date)
);

-- Indexes for performance
CREATE INDEX idx_security_events_timestamp ON security_events(timestamp DESC);
CREATE INDEX idx_security_events_ip_address ON security_events(ip_address);
CREATE INDEX idx_security_events_user_id ON security_events(user_id);
CREATE INDEX idx_security_events_tenant_id ON security_events(tenant_id);
CREATE INDEX idx_security_events_risk_level ON security_events(risk_level);
CREATE INDEX idx_security_events_event_type ON security_events(event_type);

CREATE INDEX idx_blocked_ips_ip_address ON blocked_ip_addresses(ip_address);
CREATE INDEX idx_blocked_ips_unblock_at ON blocked_ip_addresses(unblock_at);
CREATE INDEX idx_blocked_ips_tenant_id ON blocked_ip_addresses(tenant_id);

CREATE INDEX idx_security_incidents_created_at ON security_incidents(created_at DESC);
CREATE INDEX idx_security_incidents_tenant_id ON security_incidents(tenant_id);
CREATE INDEX idx_security_incidents_status ON security_incidents(status);

CREATE INDEX idx_security_metrics_date ON security_metrics(metric_date DESC);
CREATE INDEX idx_security_metrics_tenant_id ON security_metrics(tenant_id);

-- Row Level Security for multi-tenant isolation
ALTER TABLE security_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE blocked_ip_addresses ENABLE ROW LEVEL SECURITY;
ALTER TABLE security_incidents ENABLE ROW LEVEL SECURITY;
ALTER TABLE security_metrics ENABLE ROW LEVEL SECURITY;

-- RLS Policies
CREATE POLICY security_events_tenant_policy ON security_events
    FOR ALL TO authenticated_users
    USING (tenant_id IS NULL OR tenant_id = current_setting('app.current_tenant_id')::UUID);

CREATE POLICY blocked_ips_tenant_policy ON blocked_ip_addresses
    FOR ALL TO authenticated_users
    USING (tenant_id IS NULL OR tenant_id = current_setting('app.current_tenant_id')::UUID);

CREATE POLICY security_incidents_tenant_policy ON security_incidents
    FOR ALL TO authenticated_users
    USING (tenant_id IS NULL OR tenant_id = current_setting('app.current_tenant_id')::UUID);

CREATE POLICY security_metrics_tenant_policy ON security_metrics
    FOR ALL TO authenticated_users
    USING (tenant_id IS NULL OR tenant_id = current_setting('app.current_tenant_id')::UUID);

-- Function to automatically clean old security events
CREATE OR REPLACE FUNCTION cleanup_old_security_events()
RETURNS void AS $$
BEGIN
    DELETE FROM security_events 
    WHERE timestamp < NOW() - INTERVAL '90 days';
    
    DELETE FROM blocked_ip_addresses 
    WHERE unblock_at < NOW() AND is_permanent = FALSE;
    
    DELETE FROM security_metrics 
    WHERE metric_date < NOW() - INTERVAL '1 year';
END;
$$ LANGUAGE plpgsql;

-- Create a trigger to automatically update security metrics
CREATE OR REPLACE FUNCTION update_security_metrics()
RETURNS trigger AS $$
BEGIN
    INSERT INTO security_metrics (
        metric_date,
        tenant_id,
        total_events,
        failed_logins,
        successful_logins,
        blocked_ips,
        high_risk_events,
        suspicious_activities
    ) VALUES (
        CURRENT_DATE,
        NEW.tenant_id,
        1,
        CASE WHEN NEW.event_type = 'LoginFailure' THEN 1 ELSE 0 END,
        CASE WHEN NEW.event_type = 'LoginSuccess' THEN 1 ELSE 0 END,
        CASE WHEN NEW.event_type = 'UnauthorizedAccess' THEN 1 ELSE 0 END,
        CASE WHEN NEW.risk_level >= 3 THEN 1 ELSE 0 END,
        CASE WHEN NEW.event_type = 'SuspiciousActivity' THEN 1 ELSE 0 END
    )
    ON CONFLICT (tenant_id, metric_date) DO UPDATE SET
        total_events = security_metrics.total_events + 1,
        failed_logins = security_metrics.failed_logins + 
            CASE WHEN NEW.event_type = 'LoginFailure' THEN 1 ELSE 0 END,
        successful_logins = security_metrics.successful_logins + 
            CASE WHEN NEW.event_type = 'LoginSuccess' THEN 1 ELSE 0 END,
        blocked_ips = security_metrics.blocked_ips + 
            CASE WHEN NEW.event_type = 'UnauthorizedAccess' THEN 1 ELSE 0 END,
        high_risk_events = security_metrics.high_risk_events + 
            CASE WHEN NEW.risk_level >= 3 THEN 1 ELSE 0 END,
        suspicious_activities = security_metrics.suspicious_activities + 
            CASE WHEN NEW.event_type = 'SuspiciousActivity' THEN 1 ELSE 0 END;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for automatic metrics updates
CREATE TRIGGER trigger_update_security_metrics
    AFTER INSERT ON security_events
    FOR EACH ROW
    EXECUTE FUNCTION update_security_metrics();

-- Grant permissions
GRANT SELECT, INSERT ON security_events TO authenticated_users;
GRANT SELECT, INSERT, UPDATE, DELETE ON blocked_ip_addresses TO authenticated_users;
GRANT SELECT, INSERT, UPDATE, DELETE ON security_incidents TO authenticated_users;
GRANT SELECT, INSERT, UPDATE ON security_metrics TO authenticated_users;

-- Grant usage on sequences
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO authenticated_users;

-- Create view for security dashboard
CREATE VIEW security_dashboard AS
SELECT 
    tenant_id,
    COUNT(*) as total_events,
    COUNT(*) FILTER (WHERE timestamp >= NOW() - INTERVAL '24 hours') as events_last_24h,
    COUNT(*) FILTER (WHERE risk_level >= 3) as high_risk_events,
    COUNT(*) FILTER (WHERE event_type = 'LoginFailure') as failed_logins,
    COUNT(*) FILTER (WHERE event_type = 'LoginSuccess') as successful_logins,
    COUNT(DISTINCT ip_address) as unique_ips,
    MAX(timestamp) as last_event
FROM security_events
GROUP BY tenant_id;

GRANT SELECT ON security_dashboard TO authenticated_users;
