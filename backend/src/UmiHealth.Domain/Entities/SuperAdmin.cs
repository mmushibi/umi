using System;
using System.Collections.Generic;

namespace UmiHealth.Domain.Entities
{
    public enum SecurityRiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public class SuperAdminLog
    {
        public Guid Id { get; set; }
        public string LogLevel { get; set; } = string.Empty; // info, warning, error, critical
        public string Category { get; set; } = string.Empty; // auth, system, api, database
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SuperAdminReport
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // tenant, user, transaction, system
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "generating"; // generating, completed, failed
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Results { get; set; } = new();
        public string? FilePath { get; set; }
        public string? GeneratedBy { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class SystemAnalytics
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public int ActiveTenants { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public Dictionary<string, int> TenantStats { get; set; } = new(); // by plan type
        public Dictionary<string, int> UserRoleStats { get; set; } = new();
        public Dictionary<string, int> ApiUsageStats { get; set; } = new();
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SecurityEvent
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty; // login, logout, failed_login, permission_denied, data_access
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Resource { get; set; }
        public string? Action { get; set; }
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public SecurityRiskLevel RiskLevel { get; set; } = SecurityRiskLevel.Low;
        
        // For compatibility with Application.Models.SecurityEvent
        public DateTime Timestamp => CreatedAt;
        public Dictionary<string, object> Metadata => Details;
    }

    public class SystemSetting
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // general, security, notification, backup
        public string DataType { get; set; } = "string"; // string, number, boolean, json
        public string Description { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = false;
        public bool IsEditable { get; set; } = true;
        public Dictionary<string, object> ValidationRules { get; set; } = new();
        public string? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SuperAdminUser
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "superadmin"; // superadmin, admin
        public List<string> Permissions { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public bool TwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecret { get; set; }
        public List<string> BackupCodes { get; set; } = new();
        public Dictionary<string, object> Preferences { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
    }

    public class SystemNotification
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // info, warning, error, success
        public string? TargetAudience { get; set; } // all, tenants, admins, superadmins
        public List<string> TargetTenants { get; set; } = new();
        public List<string> TargetUsers { get; set; } = new();
        public bool IsGlobal { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class BackupRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // full, incremental, tenant
        public string Status { get; set; } = "pending"; // pending, running, completed, failed
        public string? TenantId { get; set; }
        public string? FilePath { get; set; }
        public long FileSize { get; set; }
        public string? Checksum { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class ApiKey
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string KeyHash { get; set; } = string.Empty;
        public string? Prefix { get; set; }
        public List<string> Permissions { get; set; } = new();
        public List<string> AllowedEndpoints { get; set; } = new();
        public List<string> AllowedIpAddresses { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime? LastUsed { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int UsageCount { get; set; } = 0;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
