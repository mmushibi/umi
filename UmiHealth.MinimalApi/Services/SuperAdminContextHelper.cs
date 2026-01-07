namespace UmiHealth.MinimalApi.Services;

/// <summary>
/// Helper for super-admin/global operations to safely set DB context and track audit.
/// 
/// IMPORTANT: Use only for authenticated super-admin requests after explicit authorization checks.
/// Call AuditLogEntry before/after the operation so audit_logs table records the cross-tenant action.
/// 
/// In production, wrap these calls in transactions and use an elevated DB role.
/// </summary>
public class SuperAdminContextHelper
{
    public class AuditLogEntry
    {
        public string? SuperAdminUserId { get; set; }
        public string? TargetTenantId { get; set; }
        public string? Action { get; set; }
        public string? Resource { get; set; }
        public string? Status { get; set; }
        public Dictionary<string, object>? Details { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Creates an audit entry for a super-admin cross-tenant operation.
    /// In production, insert this into audit_logs table with status='INITIATED'.
    /// </summary>
    public static AuditLogEntry CreateAuditEntry(
        string? superAdminUserId, 
        string? targetTenantId, 
        string action, 
        string resource, 
        Dictionary<string, object>? details = null
    ) => new()
    {
        SuperAdminUserId = superAdminUserId,
        TargetTenantId = targetTenantId,
        Action = action,
        Resource = resource,
        Details = details ?? new(),
        Timestamp = DateTime.UtcNow,
        Status = "INITIATED"
    };

    /// <summary>
    /// Call this to finalize an audit entry (set status to COMPLETED or FAILED).
    /// In production, update the corresponding row in audit_logs.
    /// </summary>
    public static AuditLogEntry CompleteAuditEntry(AuditLogEntry entry, string status = "COMPLETED")
    {
        entry.Status = status;
        return entry;
    }

    /// <summary>
    /// Builds a pseudo-context string for use with database set_config() calls.
    /// In production, call this from a stored procedure that also inserts an audit row atomically.
    /// 
    /// Example:
    ///   var contextSql = SuperAdminContextHelper.BuildDbContextSQL(superAdminId, targetTenantId);
    ///   // Execute contextSql against DB (via `set_config`) along with audit insert in one transaction
    /// </summary>
    public static string BuildDbContextSQL(string superAdminUserId, string targetTenantId)
    {
        // Note: In real implementation, call a stored procedure like:
        //   SELECT set_super_admin_context_with_audit($1::UUID, $2::UUID);
        // which sets config and inserts audit atomically.
        
        return $@"
-- Set tenant context for cross-tenant read/write
SELECT set_config('app.current_tenant_id', '{EscapeSql(targetTenantId)}', true);
SELECT set_config('app.super_admin_override', 'true', true);
SELECT set_config('app.super_admin_user_id', '{EscapeSql(superAdminUserId)}', true);
";
    }

    private static string EscapeSql(string? value) => value?.Replace("'", "''") ?? "";
}
