using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    /// <summary>
    /// Database-backed security audit service for persistent security event storage
    /// </summary>
    public interface IDatabaseSecurityAuditService
    {
        Task LogSecurityEventAsync(SecurityEvent securityEvent);
        Task<List<SecurityEvent>> GetRecentSecurityEventsAsync(int count = 100, Guid? tenantId = null);
        Task<object> GetSecurityMetricsAsync(Guid? tenantId = null);
        Task<bool> IsIpAddressBlockedAsync(string ipAddress, Guid? tenantId = null);
        Task BlockIpAddressAsync(string ipAddress, string reason, TimeSpan? duration = null, Guid? tenantId = null, Guid? blockedBy = null);
        Task<List<BlockedIpAddress>> GetBlockedIpAddressesAsync(Guid? tenantId = null);
        Task UnblockIpAddressAsync(string ipAddress, Guid? tenantId = null);
        Task<List<SecurityIncident>> GetSecurityIncidentsAsync(Guid? tenantId = null);
        Task CreateSecurityIncidentAsync(SecurityIncident incident);
        Task CleanupOldEventsAsync();
    }

    public class DatabaseSecurityAuditService : IDatabaseSecurityAuditService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<DatabaseSecurityAuditService> _logger;
        private readonly IConfiguration _configuration;

        public DatabaseSecurityAuditService(
            SharedDbContext context,
            ILogger<DatabaseSecurityAuditService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
        {
            try
            {
                // The domain SecurityEvent already maps to DB entity registered in SharedDbContext.
                // Ensure Id and CreatedAt are set, then persist domain entity directly.
                if (securityEvent.Id == Guid.Empty)
                    securityEvent.Id = Guid.NewGuid();

                if (securityEvent.CreatedAt == default)
                    securityEvent.CreatedAt = DateTime.UtcNow;

                _context.SecurityEvents.Add(securityEvent);
                await _context.SaveChangesAsync();

                // Auto-block IP for certain events (use Severity on domain SecurityEvent)
                if (ShouldAutoBlock(securityEvent))
                {
                    Guid? tenantGuid = null;
                    if (!string.IsNullOrEmpty(securityEvent.TenantId) && Guid.TryParse(securityEvent.TenantId, out var parsedTenant))
                        tenantGuid = parsedTenant;

                    await BlockIpAddressAsync(
                        securityEvent.IpAddress ?? string.Empty,
                        $"Auto-block for {securityEvent.EventType}",
                        TimeSpan.FromHours(1),
                        tenantGuid
                    );
                }

                _logger.LogInformation(
                    "Security event logged: {EventType} - {Description} from {IpAddress}",
                    securityEvent.EventType,
                    // Domain SecurityEvent uses 'Details' / 'FailureReason' / 'Description' may not exist consistently;
                    // use FailureReason when provided otherwise log an empty string to avoid null issues.
                    securityEvent.FailureReason ?? string.Empty,
                    securityEvent.IpAddress
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event");
            }
        }

        public async Task<List<SecurityEvent>> GetRecentSecurityEventsAsync(int count = 100, Guid? tenantId = null)
        {
            try
            {
                var query = _context.SecurityEvents.AsQueryable();

                if (tenantId.HasValue)
                {
                    // Domain.SecurityEvent.TenantId is a string? in this codebase — compare by string
                    var tenantStr = tenantId.Value.ToString();
                    query = query.Where(e => e.TenantId == tenantStr);
                }

                var events = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security events");
                return new List<SecurityEvent>();
            }
        }

        public async Task<object> GetSecurityMetricsAsync(Guid? tenantId = null)
        {
            try
            {
                var now = DateTime.UtcNow;
                var last24Hours = now.AddHours(-24);

                var query = _context.SecurityEvents.AsQueryable();
                if (tenantId.HasValue)
                {
                    var tenantStr = tenantId.Value.ToString();
                    query = query.Where(e => e.TenantId == tenantStr);
                }

                var totalEvents = await query.CountAsync();
                var eventsLast24Hours = await query.CountAsync(e => e.CreatedAt >= last24Hours);

                // Domain.SecurityEvent uses RiskLevel enum (Low, Medium, High, Critical)
                var highRiskEvents = await query.CountAsync(e => e.RiskLevel >= SecurityRiskLevel.High);
                var mediumRiskEvents = await query.CountAsync(e => e.RiskLevel == SecurityRiskLevel.Medium);
                var lowRiskEvents = await query.CountAsync(e => e.RiskLevel == SecurityRiskLevel.Low);

                var blockedIpsQuery = _context.BlockedIpAddresses.AsQueryable();
                if (tenantId.HasValue)
                {
                    blockedIpsQuery = blockedIpsQuery.Where(b => b.TenantId == tenantId || b.TenantId == null);
                }
                var blockedIpAddresses = await blockedIpsQuery.CountAsync();

                // Event types breakdown (string -> count)
                var eventTypes = await query
                    .GroupBy(e => e.EventType)
                    .Select(g => new { EventType = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.EventType, g => g.Count);

                // Return a plain object (method signature returns Task<object>)
                return new
                {
                    TotalEvents = totalEvents,
                    EventsLast24Hours = eventsLast24Hours,
                    BlockedIpAddresses = blockedIpAddresses,
                    HighRiskEvents = highRiskEvents,
                    MediumRiskEvents = mediumRiskEvents,
                    LowRiskEvents = lowRiskEvents,
                    EventTypes = eventTypes,
                    LastUpdated = now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security metrics");
                return new { LastUpdated = DateTime.UtcNow };
            }
        }

        public async Task<bool> IsIpAddressBlockedAsync(string ipAddress, Guid? tenantId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(ipAddress))
                    return false;

                var query = _context.BlockedIpAddresses.AsQueryable();
                if (tenantId.HasValue)
                {
                    query = query.Where(b => b.TenantId == tenantId.Value);
                }

                var isBlocked = await query
                    .Where(b => b.IpAddress == ipAddress)
                    .Where(b => b.IsPermanent || b.UnblockAt > DateTime.UtcNow)
                    .AnyAsync();

                return isBlocked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking IP block status");
                return false;
            }
        }

        public async Task BlockIpAddressAsync(
            string ipAddress,
            string reason,
            TimeSpan? duration = null,
            Guid? tenantId = null,
            Guid? blockedBy = null)
        {
            try
            {
                if (string.IsNullOrEmpty(ipAddress))
                    return;

                var blockDuration = duration ?? TimeSpan.FromHours(1);
                var unblockTime = DateTime.UtcNow.Add(blockDuration);

                var existingBlock = await _context.BlockedIpAddresses
                    .Where(b => b.IpAddress == ipAddress)
                    .Where(b => b.TenantId == tenantId || b.TenantId == null)
                    .FirstOrDefaultAsync();

                if (existingBlock != null)
                {
                    // Update existing block
                    existingBlock.UnblockAt = unblockTime;
                    existingBlock.Reason = reason;
                    existingBlock.BlockedBy = blockedBy;
                    existingBlock.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new block
                    var blockedIp = new UmiHealth.Domain.Entities.BlockedIpAddress
                    {
                        Id = Guid.NewGuid(),
                        IpAddress = ipAddress,
                        Reason = reason,
                        BlockedAt = DateTime.UtcNow,
                        UnblockAt = unblockTime,
                        BlockedBy = blockedBy,
                        IsPermanent = false,
                        TenantId = tenantId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.BlockedIpAddresses.Add(blockedIp);
                }

                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "IP Address {IpAddress} blocked until {UnblockTime}. Reason: {Reason}",
                    ipAddress, unblockTime, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking IP address");
            }
        }

        public async Task<List<BlockedIpAddress>> GetBlockedIpAddressesAsync(Guid? tenantId = null)
        {
            try
            {
                var query = _context.BlockedIpAddresses.AsQueryable();
                if (tenantId.HasValue)
                {
                    query = query.Where(b => b.TenantId == tenantId.Value);
                }

                return await query
                    .Where(b => b.IsPermanent || b.UnblockAt > DateTime.UtcNow)
                    .OrderByDescending(b => b.BlockedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blocked IP addresses");
                return new List<BlockedIpAddress>();
            }
        }

        public async Task UnblockIpAddressAsync(string ipAddress, Guid? tenantId = null)
        {
            try
            {
                var blockedIp = await _context.BlockedIpAddresses
                    .Where(b => b.IpAddress == ipAddress)
                    .Where(b => b.TenantId == tenantId || b.TenantId == null)
                    .FirstOrDefaultAsync();

                if (blockedIp != null)
                {
                    _context.BlockedIpAddresses.Remove(blockedIp);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("IP Address {IpAddress} unblocked", ipAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking IP address");
            }
        }

        public async Task<List<SecurityIncident>> GetSecurityIncidentsAsync(Guid? tenantId = null)
        {
            try
            {
                var query = _context.SecurityIncidents.AsQueryable();
                if (tenantId.HasValue)
                {
                    query = query.Where(i => i.TenantId == tenantId.Value);
                }

                return await query
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security incidents");
                return new List<SecurityIncident>();
            }
        }

        public async Task CreateSecurityIncidentAsync(SecurityIncident incident)
        {
            try
            {
                incident.Id = Guid.NewGuid();
                incident.CreatedAt = DateTime.UtcNow;
                incident.UpdatedAt = DateTime.UtcNow;

                _context.SecurityIncidents.Add(incident);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Security incident created: {Title}", incident.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating security incident");
            }
        }

        public async Task CleanupOldEventsAsync()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-90); // Keep 90 days

                // Clean old security events
                var oldEvents = await _context.SecurityEvents
                    .Where(e => e.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (oldEvents.Any())
                {
                    _context.SecurityEvents.RemoveRange(oldEvents);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} old security events", oldEvents.Count);
                }

                // Clean expired IP blocks
                var expiredBlocks = await _context.BlockedIpAddresses
                    .Where(b => b.UnblockAt < DateTime.UtcNow && !b.IsPermanent)
                    .ToListAsync();

                if (expiredBlocks.Any())
                {
                    _context.BlockedIpAddresses.RemoveRange(expiredBlocks);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} expired IP blocks", expiredBlocks.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during security cleanup");
            }
        }

        private bool ShouldAutoBlock(SecurityEvent securityEvent)
        {
            // Domain.SecurityEvent uses RiskLevel enum; treat High/Critical as auto-block
            if (securityEvent.RiskLevel >= SecurityRiskLevel.High)
                return true;

            // Keep default behaviour minimal here (database queries should be async).
            return false;
        }
    }
}
