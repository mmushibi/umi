namespace UmiHealth.MinimalApi.Services;

/// <summary>
/// Scaffolding for audit logging of sensitive operations (super-admin, cross-tenant, etc).
/// In production, integrate with audit_logs table in database.
/// </summary>
public interface IAuditService
{
    void LogSuperAdminAction(string? userId, string? targetTenantId, string action, string resource, Dictionary<string, object>? details = null);
    void LogRoleEnforcement(string? userId, string role, string resource, bool allowed, string? reason = null);
    void LogCrossTenantAccess(string? userId, string sourceTenant, string targetTenant, string operation);
    void LogAuthenticationEvent(string? userId, string? tenantId, string eventType, string? ipAddress = null, bool success = true, string? details = null);
}

public class AuditService : IAuditService
{
    private readonly List<object> _auditLog = new();

    public void LogSuperAdminAction(string? userId, string? targetTenantId, string action, string resource, Dictionary<string, object>? details = null)
    {
        var entry = new
        {
            Timestamp = DateTime.UtcNow,
            Type = "SUPER_ADMIN_ACTION",
            UserId = userId,
            TargetTenantId = targetTenantId,
            Action = action,
            Resource = resource,
            Details = details ?? new Dictionary<string, object>()
        };
        _auditLog.Add(entry);
        System.Diagnostics.Debug.WriteLine($"[AUDIT] {action} on {resource} by {userId}");
    }

    public void LogRoleEnforcement(string? userId, string role, string resource, bool allowed, string? reason = null)
    {
        var entry = new
        {
            Timestamp = DateTime.UtcNow,
            Type = "ROLE_ENFORCEMENT",
            UserId = userId,
            Role = role,
            Resource = resource,
            Allowed = allowed,
            Reason = reason
        };
        _auditLog.Add(entry);
        System.Diagnostics.Debug.WriteLine($"[AUDIT] Role {role} access to {resource}: {(allowed ? "ALLOWED" : "DENIED")}");
    }

    public void LogCrossTenantAccess(string? userId, string sourceTenant, string targetTenant, string operation)
    {
        var entry = new
        {
            Timestamp = DateTime.UtcNow,
            Type = "CROSS_TENANT_ACCESS",
            UserId = userId,
            SourceTenant = sourceTenant,
            TargetTenant = targetTenant,
            Operation = operation
        };
        _auditLog.Add(entry);
        System.Diagnostics.Debug.WriteLine($"[AUDIT] Cross-tenant operation: {operation} from {sourceTenant} to {targetTenant}");
    }

    public void LogAuthenticationEvent(string? userId, string? tenantId, string eventType, string? ipAddress = null, bool success = true, string? details = null)
    {
        var entry = new
        {
            Timestamp = DateTime.UtcNow,
            Type = "AUTHENTICATION_EVENT",
            UserId = userId,
            TenantId = tenantId,
            EventType = eventType,
            IPAddress = ipAddress,
            Success = success,
            Details = details
        };
        _auditLog.Add(entry);
        System.Diagnostics.Debug.WriteLine($"[AUDIT] Auth event: {eventType} for user {userId} - {(success ? "SUCCESS" : "FAILURE")}");
    }

    public IReadOnlyList<object> GetAuditLog() => _auditLog.AsReadOnly();
}
