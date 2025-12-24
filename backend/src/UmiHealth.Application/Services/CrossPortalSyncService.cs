using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UmiHealth.Application.Services
{
    public interface ICrossPortalSyncService
    {
        Task BroadcastDataChangeAsync(string entityType, object data, string sourcePortal, Guid? tenantId = null);
        Task SubscribeToEntityAsync(string entityType, string portalType, Func<object, Task> callback);
        Task UnsubscribeFromEntityAsync(string entityType, string portalType);
        Task RegisterPortalAsync(string portalType, Guid tenantId, Guid userId);
        Task UnregisterPortalAsync(string portalType, Guid tenantId, Guid userId);
        Task<List<ActivePortal>> GetActivePortalsAsync();
        Task<PortalSyncStats> GetSyncStatsAsync();
    }

    public class CrossPortalSyncService : BackgroundService, ICrossPortalSyncService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CrossPortalSyncService> _logger;
        private readonly ConcurrentDictionary<string, List<Func<object, Task>>> _subscriptions;
        private readonly ConcurrentDictionary<string, ActivePortal> _activePortals;
        private readonly ConcurrentDictionary<string, DateTime> _lastSyncTimes;

        public CrossPortalSyncService(
            IMemoryCache cache,
            ILogger<CrossPortalSyncService> logger)
        {
            _cache = cache;
            _logger = logger;
            _subscriptions = new ConcurrentDictionary<string, List<Func<object, Task>>>();
            _activePortals = new ConcurrentDictionary<string, ActivePortal>();
            _lastSyncTimes = new ConcurrentDictionary<string, DateTime>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupInactivePortals();
                    await ProcessSyncQueue();
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in cross-portal sync service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        public async Task BroadcastDataChangeAsync(string entityType, object data, string sourcePortal, Guid? tenantId = null)
        {
            try
            {
                var subscriptionKey = GetSubscriptionKey(entityType, tenantId);
                
                if (_subscriptions.TryGetValue(subscriptionKey, out var callbacks))
                {
                    var tasks = callbacks.Select(callback => 
                    {
                        try
                        {
                            return callback(data);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in subscription callback for {EntityType}", entityType);
                            return Task.CompletedTask;
                        }
                    });

                    await Task.WhenAll(tasks);
                    
                    _logger.LogInformation("Broadcasted {EntityType} change from {SourcePortal} to {SubscriberCount} subscribers", 
                        entityType, sourcePortal, callbacks.Count);
                }

                // Update last sync time
                var syncKey = GetSyncKey(entityType, sourcePortal, tenantId);
                _lastSyncTimes[syncKey] = DateTime.UtcNow;

                // Cache the data for other portals
                var cacheKey = GetCacheKey(entityType, tenantId);
                _cache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting data change for {EntityType}", entityType);
            }
        }

        public async Task SubscribeToEntityAsync(string entityType, string portalType, Func<object, Task> callback)
        {
            try
            {
                // Extract tenant ID from current context or use null for global
                var tenantId = GetCurrentTenantId();
                var subscriptionKey = GetSubscriptionKey(entityType, tenantId);
                
                _subscriptions.AddOrUpdate(subscriptionKey, 
                    new List<Func<object, Task>> { callback },
                    (key, existing) =>
                    {
                        existing.Add(callback);
                        return existing;
                    });

                _logger.LogInformation("Portal {PortalType} subscribed to {EntityType}", portalType, entityType);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to {EntityType}", entityType);
            }
        }

        public async Task UnsubscribeFromEntityAsync(string entityType, string portalType)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var subscriptionKey = GetSubscriptionKey(entityType, tenantId);
                
                if (_subscriptions.TryRemove(subscriptionKey, out var callbacks))
                {
                    _logger.LogInformation("Portal {PortalType} unsubscribed from {EntityType}", portalType, entityType);
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from {EntityType}", entityType);
            }
        }

        public async Task RegisterPortalAsync(string portalType, Guid tenantId, Guid userId)
        {
            try
            {
                var portalKey = GetPortalKey(portalType, tenantId, userId);
                var portal = new ActivePortal
                {
                    PortalType = portalType,
                    TenantId = tenantId,
                    UserId = userId,
                    RegisteredAt = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    Status = "active"
                };

                _activePortals[portalKey] = portal;
                
                _logger.LogInformation("Portal registered: {PortalType} for tenant {TenantId}", portalType, tenantId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering portal {PortalType}", portalType);
            }
        }

        public async Task UnregisterPortalAsync(string portalType, Guid tenantId, Guid userId)
        {
            try
            {
                var portalKey = GetPortalKey(portalType, tenantId, userId);
                
                if (_activePortals.TryRemove(portalKey, out var portal))
                {
                    _logger.LogInformation("Portal unregistered: {PortalType} for tenant {TenantId}", portalType, tenantId);
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering portal {PortalType}", portalType);
            }
        }

        public async Task<List<ActivePortal>> GetActivePortalsAsync()
        {
            try
            {
                return _activePortals.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active portals");
                return new List<ActivePortal>();
            }
        }

        public async Task<PortalSyncStats> GetSyncStatsAsync()
        {
            try
            {
                var activePortals = _activePortals.Values.ToList();
                var subscriptions = _subscriptions.SelectMany(kvp => kvp.Value).Count();
                var recentSyncs = _lastSyncTimes.Values
                    .Where(time => time >= DateTime.UtcNow.AddMinutes(-5))
                    .Count();

                return new PortalSyncStats
                {
                    ActivePortals = activePortals.Count,
                    ActiveSubscriptions = subscriptions,
                    RecentSyncs = recentSyncs,
                    PortalTypes = activePortals.GroupBy(p => p.PortalType)
                        .Select(g => new { PortalType = g.Key, Count = g.Count() })
                        .ToList(),
                    LastSyncTime = _lastSyncTimes.Values.OrderByDescending(t => t).FirstOrDefault()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync stats");
                return new PortalSyncStats();
            }
        }

        private async Task CleanupInactivePortals()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-30); // Remove portals inactive for 30 minutes
            var inactivePortals = _activePortals
                .Where(kvp => kvp.Value.LastActivity < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var portalKey in inactivePortals)
            {
                if (_activePortals.TryRemove(portalKey, out var portal))
                {
                    _logger.LogInformation("Cleaned up inactive portal: {PortalType}", portal.PortalType);
                }
            }
        }

        private async Task ProcessSyncQueue()
        {
            // This would process any queued sync operations
            // For now, it's a placeholder for future enhancement
            await Task.CompletedTask;
        }

        private string GetSubscriptionKey(string entityType, Guid? tenantId)
        {
            return tenantId.HasValue ? $"{entityType}_{tenantId}" : entityType;
        }

        private string GetCacheKey(string entityType, Guid? tenantId)
        {
            return tenantId.HasValue ? $"cross_portal_{entityType}_{tenantId}" : $"cross_portal_{entityType}";
        }

        private string GetSyncKey(string entityType, string sourcePortal, Guid? tenantId)
        {
            return tenantId.HasValue ? $"{entityType}_{sourcePortal}_{tenantId}" : $"{entityType}_{sourcePortal}";
        }

        private string GetPortalKey(string portalType, Guid tenantId, Guid userId)
        {
            return $"{portalType}_{tenantId}_{userId}";
        }

        private Guid? GetCurrentTenantId()
        {
            // This would typically come from current user context
            // For now, return null to indicate global subscription
            return null;
        }
    }

    public class ActivePortal
    {
        public string PortalType { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime LastActivity { get; set; }
        public string Status { get; set; } = "active";
    }

    public class PortalSyncStats
    {
        public int ActivePortals { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int RecentSyncs { get; set; }
        public List<object> PortalTypes { get; set; } = new();
        public DateTime? LastSyncTime { get; set; }
    }
}
