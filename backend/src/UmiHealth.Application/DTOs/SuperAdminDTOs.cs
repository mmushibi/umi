using System;
using System.Collections.Generic;

namespace UmiHealth.Application.DTOs
{
    // Analytics DTOs
    public class AnalyticsDto
    {
        public DateTime Date { get; set; }
        public int ActiveTenants { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public Dictionary<string, int> TenantStats { get; set; } = new();
        public Dictionary<string, int> UserRoleStats { get; set; } = new();
        public Dictionary<string, int> ApiUsageStats { get; set; } = new();
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    }

    public class AnalyticsFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Period { get; set; } = "day"; // day, week, month, year
        public List<string>? Metrics { get; set; }
    }

    // Logs DTOs
    public class SuperAdminLogDto
    {
        public Guid Id { get; set; }
        public string LogLevel { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class LogFilterDto
    {
        public string? LogLevel { get; set; }
        public string? Category { get; set; }
        public string? Search { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // Reports DTOs
    public class SuperAdminReportDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Results { get; set; } = new();
        public string? FilePath { get; set; }
        public string? GeneratedBy { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class CreateReportDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
    }

    // Security DTOs
    public class SecurityEventDto
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Resource { get; set; }
        public string? Action { get; set; }
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class SecurityFilterDto
    {
        public string? EventType { get; set; }
        public string? Severity { get; set; }
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
        public bool? Success { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // System Settings DTOs
    public class SystemSettingDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public bool IsEditable { get; set; }
        public Dictionary<string, object> ValidationRules { get; set; } = new();
        public string? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateSystemSettingDto
    {
        public string Value { get; set; } = string.Empty;
    }

    // Super Admin Users DTOs
    public class SuperAdminUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public Dictionary<string, object> Preferences { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateSuperAdminUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "superadmin";
        public List<string> Permissions { get; set; } = new();
        public Dictionary<string, object> Preferences { get; set; } = new();
    }

    public class UpdateSuperAdminUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Role { get; set; }
        public List<string>? Permissions { get; set; }
        public bool? IsActive { get; set; }
        public Dictionary<string, object>? Preferences { get; set; }
    }

    // Notifications DTOs
    public class SystemNotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? TargetAudience { get; set; }
        public List<string> TargetTenants { get; set; } = new();
        public List<string> TargetUsers { get; set; } = new();
        public bool IsGlobal { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateSystemNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? TargetAudience { get; set; }
        public List<string> TargetTenants { get; set; } = new();
        public List<string> TargetUsers { get; set; } = new();
        public bool IsGlobal { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Backup DTOs
    public class BackupRecordDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public string? FilePath { get; set; }
        public long FileSize { get; set; }
        public string? Checksum { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = "backup.zip";
    }

    public class CreateBackupDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
    }

    // API Keys DTOs
    public class ApiKeyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Prefix { get; set; }
        public List<string> Permissions { get; set; } = new();
        public List<string> AllowedEndpoints { get; set; } = new();
        public List<string> AllowedIpAddresses { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime? LastUsed { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int UsageCount { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateApiKeyDto
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
        public List<string> AllowedEndpoints { get; set; } = new();
        public List<string> AllowedIpAddresses { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // Dashboard Summary DTOs
    public class DashboardSummaryDto
    {
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public int CriticalSecurityEvents { get; set; }
        public int PendingReports { get; set; }
        public int FailedBackups { get; set; }
        public List<AnalyticsDto> RecentAnalytics { get; set; } = new();
        public List<SecurityEventDto> RecentSecurityEvents { get; set; } = new();
        public List<SuperAdminLogDto> RecentLogs { get; set; } = new();
    }

    // Pagination Response DTO
    public class PaginatedResponseDto<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
