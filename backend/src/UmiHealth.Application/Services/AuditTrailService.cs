using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface IAuditTrailService
    {
        Task LogActivityAsync(AuditLogEntry entry);
        Task<List<AuditLogEntry>> GetAuditLogsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null, string? entityType = null, string? action = null, Guid? userId = null);
        Task<List<AuditLogEntry>> GetEntityHistoryAsync(Guid tenantId, string entityType, Guid entityId);
        Task<List<UserActivityReport>> GetUserActivityReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<List<SecurityEvent>> GetSecurityEventsAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<byte[]> ExportAuditLogsAsync(Guid tenantId, DateTime startDate, DateTime endDate, string format = "csv");
        Task<bool> VerifyDataIntegrityAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    }

    public class AuditTrailService : IAuditTrailService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<AuditTrailService> _logger;

        public AuditTrailService(
            SharedDbContext context,
            ILogger<AuditTrailService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActivityAsync(AuditLogEntry entry)
        {
            try
            {
                entry.Timestamp = DateTime.UtcNow;
                entry.Id = Guid.NewGuid();
                
                _context.AuditLogs.Add(entry);
                await _context.SaveChangesAsync();

                // Log critical security events separately
                if (entry.IsSecurityEvent)
                {
                    await LogSecurityEventAsync(entry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit activity: {Activity}", entry.Action);
                // Don't throw - audit logging failure shouldn't break the main flow
            }
        }

        public async Task<List<AuditLogEntry>> GetAuditLogsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null, string? entityType = null, string? action = null, Guid? userId = null)
        {
            var query = _context.AuditLogs.Where(al => al.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(al => al.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(al => al.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(al => al.EntityType == entityType);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(al => al.Action == action);

            if (userId.HasValue)
                query = query.Where(al => al.UserId == userId.Value);

            return await query
                .OrderByDescending(al => al.Timestamp)
                .Take(1000) // Limit to prevent performance issues
                .ToListAsync();
        }

        public async Task<List<AuditLogEntry>> GetEntityHistoryAsync(Guid tenantId, string entityType, Guid entityId)
        {
            return await _context.AuditLogs
                .Where(al => al.TenantId == tenantId && 
                           al.EntityType == entityType && 
                           al.EntityId == entityId.ToString())
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        public async Task<List<UserActivityReport>> GetUserActivityReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var logs = await _context.AuditLogs
                .Where(al => al.TenantId == tenantId && 
                           al.Timestamp >= startDate && 
                           al.Timestamp <= endDate)
                .Include(al => al.User)
                .ToListAsync();

            return logs
                .GroupBy(al => al.UserId)
                .Select(g => new UserActivityReport
                {
                    UserId = g.Key,
                    UserName = g.FirstOrDefault()?.User?.FullName ?? "Unknown",
                    Email = g.FirstOrDefault()?.User?.Email ?? "",
                    TotalActions = g.Count(),
                    UniqueEntities = g.Select(al => al.EntityType).Distinct().Count(),
                    ActionsByType = g.GroupBy(al => al.Action)
                        .ToDictionary(x => x.Key, x => x.Count()),
                    LastActivity = g.Max(al => al.Timestamp),
                    SecurityEvents = g.Count(al => al.IsSecurityEvent),
                    FailedActions = g.Count(al => al.Action.Contains("Failed") || al.Action.Contains("Error"))
                })
                .OrderByDescending(uar => uar.LastActivity)
                .ToList();
        }

        public async Task<List<SecurityEvent>> GetSecurityEventsAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var logs = await _context.AuditLogs
                .Where(al => al.TenantId == tenantId && 
                           al.Timestamp >= startDate && 
                           al.Timestamp <= endDate &&
                           al.IsSecurityEvent)
                .Include(al => al.User)
                .ToListAsync();

            return logs.Select(log => new SecurityEvent
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                EventType = log.Action,
                Description = log.Description,
                UserId = log.UserId,
                UserName = log.User?.FullName ?? "Unknown",
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                Severity = DetermineSeverity(log.Action),
                EntityId = log.EntityId,
                EntityType = log.EntityType,
                AdditionalData = log.AdditionalData
            }).OrderByDescending(se => se.Timestamp).ToList();
        }

        public async Task<byte[]> ExportAuditLogsAsync(Guid tenantId, DateTime startDate, DateTime endDate, string format = "csv")
        {
            var logs = await GetAuditLogsAsync(tenantId, startDate, endDate);

            return format.ToLower() switch
            {
                "csv" => ExportToCsv(logs),
                "json" => ExportToJson(logs),
                _ => ExportToCsv(logs)
            };
        }

        public async Task<bool> VerifyDataIntegrityAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get all logs for the period
                var logs = await GetAuditLogsAsync(tenantId, startDate, endDate);
                
                // Check for gaps in sequence (if using sequence numbers)
                var sequenceGaps = DetectSequenceGaps(logs);
                
                // Verify checksums for critical operations
                var checksumVerification = await VerifyChecksumsAsync(tenantId, logs);
                
                // Check for tampering indicators
                var tamperingIndicators = DetectTampering(logs);

                return !sequenceGaps && checksumVerification && !tamperingIndicators;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify data integrity");
                return false;
            }
        }

        private async Task LogSecurityEventAsync(AuditLogEntry entry)
        {
            var securityEvent = new SecurityEvent
            {
                Id = entry.Id,
                Timestamp = entry.Timestamp,
                EventType = entry.Action,
                Description = entry.Description,
                UserId = entry.UserId,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent,
                Severity = DetermineSeverity(entry.Action),
                EntityId = entry.EntityId,
                EntityType = entry.EntityType,
                AdditionalData = entry.AdditionalData
            };

            _context.SecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();
        }

        private SecuritySeverity DetermineSeverity(string action)
        {
            return action.ToLower() switch
            {
                var a when a.Contains("failed") || a.Contains("denied") => SecuritySeverity.High,
                var a when a.Contains("delete") || a.Contains("remove") => SecuritySeverity.Medium,
                var a when a.Contains("login") || a.Contains("logout") => SecuritySeverity.Low,
                _ => SecuritySeverity.Info
            };
        }

        private bool DetectSequenceGaps(List<AuditLogEntry> logs)
        {
            // Simple implementation - in production, you'd use proper sequence numbers
            var sortedLogs = logs.OrderBy(l => l.Timestamp).ToList();
            for (int i = 1; i < sortedLogs.Count; i++)
            {
                var timeDiff = sortedLogs[i].Timestamp - sortedLogs[i - 1].Timestamp;
                if (timeDiff.TotalMinutes > 60) // Gap of more than 1 hour
                {
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> VerifyChecksumsAsync(Guid tenantId, List<AuditLogEntry> logs)
        {
            // In a real implementation, you'd verify cryptographic checksums
            // For now, just ensure no logs have been modified
            foreach (var log in logs)
            {
                if (log.ModifiedAt.HasValue && log.ModifiedAt.Value > log.Timestamp)
                {
                    return false;
                }
            }
            return true;
        }

        private bool DetectTampering(List<AuditLogEntry> logs)
        {
            // Check for suspicious patterns
            return logs.Any(log => 
                string.IsNullOrEmpty(log.UserId) && !log.Action.Contains("System") ||
                log.Timestamp > DateTime.UtcNow.AddMinutes(5) || // Future timestamps
                log.AdditionalData?.Contains("tampered") == true
            );
        }

        private byte[] ExportToCsv(List<AuditLogEntry> logs)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Timestamp,UserId,UserName,Action,EntityType,EntityId,Description,IpAddress,UserAgent,IsSecurityEvent");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.UserId},{log.User?.FullName ?? ""}," +
                    $"\"{log.Action}\",{log.EntityType},{log.EntityId},\"{log.Description}\",{log.IpAddress}," +
                    $"\"{log.UserAgent}\",{log.IsSecurityEvent}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] ExportToJson(List<AuditLogEntry> logs)
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }

    // Supporting DTOs and Entities
    public class AuditLogEntry : TenantEntity
    {
        public Guid UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public bool IsSecurityEvent { get; set; }
        public string? AdditionalData { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime? ModifiedAt { get; set; }
        
        public virtual User? User { get; set; }
    }

    public class UserActivityReport
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalActions { get; set; }
        public int UniqueEntities { get; set; }
        public Dictionary<string, int> ActionsByType { get; set; } = new();
        public DateTime LastActivity { get; set; }
        public int SecurityEvents { get; set; }
        public int FailedActions { get; set; }
    }

    public class SecurityEvent
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public SecuritySeverity Severity { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? AdditionalData { get; set; }
    }

    public enum SecuritySeverity
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }
}
