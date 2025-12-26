using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UmiHealth.Api.Services
{
    /// <summary>
    /// Service for security auditing and monitoring
    /// </summary>
    public interface ISecurityAuditService
    {
        Task LogSecurityEventAsync(SecurityEvent securityEvent);
        Task<List<SecurityEvent>> GetRecentSecurityEventsAsync(int count = 100);
        Task<SecurityMetrics> GetSecurityMetricsAsync();
        Task<bool> IsIpAddressBlockedAsync(string ipAddress);
        Task BlockIpAddressAsync(string ipAddress, string reason, TimeSpan? duration = null);
    }

    public class SecurityAuditService : ISecurityAuditService
    {
        private readonly ILogger<SecurityAuditService> _logger;
        private readonly IConfiguration _configuration;
        private readonly List<SecurityEvent> _securityEvents;
        private readonly Dictionary<string, DateTime> _blockedIpAddresses;

        public SecurityAuditService(ILogger<SecurityAuditService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _securityEvents = new List<SecurityEvent>();
            _blockedIpAddresses = new Dictionary<string, DateTime>();
        }

        public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
        {
            try
            {
                securityEvent.Timestamp = DateTime.UtcNow;
                securityEvent.Id = Guid.NewGuid();

                _securityEvents.Add(securityEvent);

                // Keep only last 1000 events in memory
                if (_securityEvents.Count > 1000)
                {
                    _securityEvents.RemoveAt(0);
                }

                // Log to structured logging
                _logger.LogWarning("Security Event: {EventType} - {Description} from {IpAddress} - User: {UserId}",
                    securityEvent.EventType,
                    securityEvent.Description,
                    securityEvent.IpAddress,
                    securityEvent.UserId);

                // Auto-block IP for certain events
                if (ShouldAutoBlock(securityEvent))
                {
                    await BlockIpAddressAsync(securityEvent.IpAddress, $"Auto-block for {securityEvent.EventType}", TimeSpan.FromHours(1));
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event");
            }
        }

        public async Task<List<SecurityEvent>> GetRecentSecurityEventsAsync(int count = 100)
        {
            var recentEvents = _securityEvents
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToList();

            await Task.CompletedTask;
            return recentEvents;
        }

        public async Task<SecurityMetrics> GetSecurityMetricsAsync()
        {
            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);

            var metrics = new SecurityMetrics
            {
                TotalEvents = _securityEvents.Count,
                EventsLast24Hours = _securityEvents.Count(e => e.Timestamp >= last24Hours),
                BlockedIpAddresses = _blockedIpAddresses.Count,
                HighRiskEvents = _securityEvents.Count(e => e.RiskLevel >= SecurityRiskLevel.High),
                MediumRiskEvents = _securityEvents.Count(e => e.RiskLevel == SecurityRiskLevel.Medium),
                LowRiskEvents = _securityEvents.Count(e => e.RiskLevel == SecurityRiskLevel.Low),
                LastUpdated = now
            };

            // Calculate event types breakdown
            metrics.EventTypes = _securityEvents
                .GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key, g => g.Count());

            await Task.CompletedTask;
            return metrics;
        }

        public async Task<bool> IsIpAddressBlockedAsync(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return false;

            var isBlocked = _blockedIpAddresses.ContainsKey(ipAddress) && 
                          _blockedIpAddresses[ipAddress] > DateTime.UtcNow;

            // Clean up expired blocks
            var expiredBlocks = _blockedIpAddresses
                .Where(kvp => kvp.Value <= DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var expiredBlock in expiredBlocks)
            {
                _blockedIpAddresses.Remove(expiredBlock);
            }

            await Task.CompletedTask;
            return isBlocked;
        }

        public async Task BlockIpAddressAsync(string ipAddress, string reason, TimeSpan? duration = null)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return;

            var blockDuration = duration ?? TimeSpan.FromHours(1);
            var unblockTime = DateTime.UtcNow.Add(blockDuration);

            _blockedIpAddresses[ipAddress] = unblockTime;

            _logger.LogWarning("IP Address {IpAddress} blocked until {UnblockTime}. Reason: {Reason}",
                ipAddress, unblockTime, reason);

            await Task.CompletedTask;
        }

        private bool ShouldAutoBlock(SecurityEvent securityEvent)
        {
            // Auto-block for high-risk events
            if (securityEvent.RiskLevel >= SecurityRiskLevel.High)
                return true;

            // Auto-block for multiple failed attempts from same IP
            var recentEvents = _securityEvents
                .Where(e => e.IpAddress == securityEvent.IpAddress && 
                           e.Timestamp >= DateTime.UtcNow.AddMinutes(-15))
                .ToList();

            return recentEvents.Count >= 5; // 5 events in 15 minutes
        }
    }

    /// <summary>
    /// Security event model
    /// </summary>
    public class SecurityEvent
    {
        public Guid Id { get; set; }
        public SecurityEventType EventType { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string UserId { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }
        public SecurityRiskLevel RiskLevel { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Security metrics model
    /// </summary>
    public class SecurityMetrics
    {
        public int TotalEvents { get; set; }
        public int EventsLast24Hours { get; set; }
        public int BlockedIpAddresses { get; set; }
        public int HighRiskEvents { get; set; }
        public int MediumRiskEvents { get; set; }
        public int LowRiskEvents { get; set; }
        public Dictionary<SecurityEventType, int> EventTypes { get; set; } = new Dictionary<SecurityEventType, int>();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Security event types
    /// </summary>
    public enum SecurityEventType
    {
        LoginAttempt,
        LoginSuccess,
        LoginFailure,
        PasswordReset,
        AccountLocked,
        SuspiciousActivity,
        XssAttempt,
        SqlInjectionAttempt,
        CsrfAttempt,
        RateLimitExceeded,
        UnauthorizedAccess,
        DataAccess,
        DataModification,
        ConfigurationChange,
        SecurityViolation
    }

    /// <summary>
    /// Security risk levels
    /// </summary>
    public enum SecurityRiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Extension methods for security audit service
    /// </summary>
    public static class SecurityAuditExtensions
    {
        public static IServiceCollection AddSecurityAudit(this IServiceCollection services)
        {
            services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
            return services;
        }
    }
}
