using System;
using UmiHealth.Core.Entities;

namespace UmiHealth.Domain.Entities
{
    /// <summary>
    /// Audit log entity for tracking all system changes
    /// </summary>
    public class AuditLog : TenantEntity
    {
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // create, update, delete, login, logout
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? ChangedFields { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public string? Module { get; set; }
        public string? ActionDescription { get; set; }
        public DateTime ActionTime { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? RequestId { get; set; }
        public string? CorrelationId { get; set; }
        public string? SessionId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Metadata { get; set; }
    }
}
