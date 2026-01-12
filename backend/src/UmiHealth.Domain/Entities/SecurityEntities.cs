using System;
using System.Collections.Generic;

namespace UmiHealth.Domain.Entities
{
    /// <summary>
    /// Security event entity for audit logging (extended version)
    /// </summary>
    public class SecurityAuditEvent
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public Guid? UserId { get; set; }
        public string? UserAgent { get; set; }
        public string? RequestPath { get; set; }
        public int RiskLevel { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public Guid? TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Blocked IP address entity
    /// </summary>
    public class BlockedIpAddress
    {
        public Guid Id { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime BlockedAt { get; set; }
        public DateTime UnblockAt { get; set; }
        public Guid? BlockedBy { get; set; }
        public bool IsPermanent { get; set; }
        public Guid? TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Security incident entity for high-priority events
    /// </summary>
    public class SecurityIncident
    {
        public Guid Id { get; set; }
        public string IncidentType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public string[]? IpAddresses { get; set; }
        public Guid[]? AffectedUsers { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public Guid? TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    /// <summary>
    /// Security metrics entity for aggregated data
    /// </summary>
    public class SecurityMetric
    {
        public Guid Id { get; set; }
        public DateTime MetricDate { get; set; }
        public Guid? TenantId { get; set; }
        public int TotalEvents { get; set; }
        public int FailedLogins { get; set; }
        public int SuccessfulLogins { get; set; }
        public int BlockedIps { get; set; }
        public int HighRiskEvents { get; set; }
        public int SuspiciousActivities { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
