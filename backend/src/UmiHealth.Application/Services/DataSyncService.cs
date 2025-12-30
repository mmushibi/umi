using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public interface IDataSyncService
    {
        Task SyncPatientsAsync(Guid tenantId, Guid branchId);
        Task SyncSalesAsync(Guid tenantId, Guid branchId);
        Task SyncPaymentsAsync(Guid tenantId, Guid branchId);
        Task SyncInventoryAsync(Guid tenantId, Guid branchId);
        Task SyncAllAsync(Guid tenantId, Guid branchId);
        Task<SyncStatus> GetSyncStatusAsync(Guid tenantId, Guid branchId);
        Task<SyncStatus> GetSyncStatusAsync();
        Task TriggerSyncAsync(string entityType);
        Task InvalidateCacheAsync(Guid tenantId, Guid branchId, string entityType);
    }

    public class DataSyncService : BackgroundService, IDataSyncService
    {
        private readonly SharedDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DataSyncService> _logger;
        private readonly Dictionary<(Guid, Guid), SyncStatus> _syncStatuses;

        public DataSyncService(
            SharedDbContext context,
            IMemoryCache cache,
            ILogger<DataSyncService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _syncStatuses = new Dictionary<(Guid, Guid), SyncStatus>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformPeriodicSync(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in periodic data sync");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task PerformPeriodicSync(CancellationToken cancellationToken)
        {
            var activeTenants = await GetActiveTenantsAsync();
            
            foreach (var tenant in activeTenants)
            {
                try
                {
                    var branches = await GetTenantBranchesAsync(tenant.Id);
                    
                    foreach (var branch in branches)
                    {
                        await SyncAllAsync(tenant.Id, branch.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing tenant {TenantId}", tenant.Id);
                }
            }
        }

        public async Task SyncPatientsAsync(Guid tenantId, Guid branchId)
        {
            var key = (tenantId, branchId);
            var status = GetOrCreateSyncStatus(key);
            
            try
            {
                status.IsSyncing = true;
                status.LastSyncAttempt = DateTime.UtcNow;
                status.CurrentOperation = "Syncing Patients";

                var patients = await _context.Patients
                    .Where(p => p.BranchId == branchId && p.DeletedAt == null)
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToListAsync();

                var cacheKey = $"patients_{tenantId}_{branchId}";
                _cache.Set(cacheKey, patients, TimeSpan.FromMinutes(10));

                status.LastSyncSuccess = DateTime.UtcNow;
                status.SyncErrors.Clear();
                _logger.LogInformation("Successfully synced {Count} patients for tenant {TenantId}, branch {BranchId}", 
                    patients.Count, tenantId, branchId);
            }
            catch (Exception ex)
            {
                status.LastSyncFailure = DateTime.UtcNow;
                status.SyncErrors.Add($"Patients sync failed: {ex.Message}");
                _logger.LogError(ex, "Error syncing patients for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
            finally
            {
                status.IsSyncing = false;
                status.CurrentOperation = null;
            }
        }

        public async Task SyncSalesAsync(Guid tenantId, Guid branchId)
        {
            var key = (tenantId, branchId);
            var status = GetOrCreateSyncStatus(key);
            
            try
            {
                status.IsSyncing = true;
                status.LastSyncAttempt = DateTime.UtcNow;
                status.CurrentOperation = "Syncing Sales";

                var sales = await _context.Sales
                    .Include(s => s.Items)
                    .Where(s => s.BranchId == branchId && s.DeletedAt == null)
                    .OrderByDescending(s => s.UpdatedAt)
                    .ToListAsync();

                var cacheKey = $"sales_{tenantId}_{branchId}";
                _cache.Set(cacheKey, sales, TimeSpan.FromMinutes(5));

                status.LastSyncSuccess = DateTime.UtcNow;
                status.SyncErrors.Clear();
                _logger.LogInformation("Successfully synced {Count} sales for tenant {TenantId}, branch {BranchId}", 
                    sales.Count, tenantId, branchId);
            }
            catch (Exception ex)
            {
                status.LastSyncFailure = DateTime.UtcNow;
                status.SyncErrors.Add($"Sales sync failed: {ex.Message}");
                _logger.LogError(ex, "Error syncing sales for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
            finally
            {
                status.IsSyncing = false;
                status.CurrentOperation = null;
            }
        }

        public async Task SyncPaymentsAsync(Guid tenantId, Guid branchId)
        {
            var key = (tenantId, branchId);
            var status = GetOrCreateSyncStatus(key);
            
            try
            {
                status.IsSyncing = true;
                status.LastSyncAttempt = DateTime.UtcNow;
                status.CurrentOperation = "Syncing Payments";

                var payments = await _context.Payments
                    .Where(p => p.BranchId == branchId && p.DeletedAt == null)
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToListAsync();

                var cacheKey = $"payments_{tenantId}_{branchId}";
                _cache.Set(cacheKey, payments, TimeSpan.FromMinutes(5));

                status.LastSyncSuccess = DateTime.UtcNow;
                status.SyncErrors.Clear();
                _logger.LogInformation("Successfully synced {Count} payments for tenant {TenantId}, branch {BranchId}", 
                    payments.Count, tenantId, branchId);
            }
            catch (Exception ex)
            {
                status.LastSyncFailure = DateTime.UtcNow;
                status.SyncErrors.Add($"Payments sync failed: {ex.Message}");
                _logger.LogError(ex, "Error syncing payments for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
            finally
            {
                status.IsSyncing = false;
                status.CurrentOperation = null;
            }
        }

        public async Task SyncInventoryAsync(Guid tenantId, Guid branchId)
        {
            var key = (tenantId, branchId);
            var status = GetOrCreateSyncStatus(key);
            
            try
            {
                status.IsSyncing = true;
                status.LastSyncAttempt = DateTime.UtcNow;
                status.CurrentOperation = "Syncing Inventory";

                var inventory = await _context.Inventories
                    .Include(i => i.Product)
                    .Where(i => i.BranchId == branchId && i.DeletedAt == null)
                    .OrderBy(i => i.Product.Name)
                    .ToListAsync();

                var cacheKey = $"inventory_{tenantId}_{branchId}";
                _cache.Set(cacheKey, inventory, TimeSpan.FromMinutes(3));

                status.LastSyncSuccess = DateTime.UtcNow;
                status.SyncErrors.Clear();
                _logger.LogInformation("Successfully synced {Count} inventory items for tenant {TenantId}, branch {BranchId}", 
                    inventory.Count, tenantId, branchId);
            }
            catch (Exception ex)
            {
                status.LastSyncFailure = DateTime.UtcNow;
                status.SyncErrors.Add($"Inventory sync failed: {ex.Message}");
                _logger.LogError(ex, "Error syncing inventory for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
            finally
            {
                status.IsSyncing = false;
                status.CurrentOperation = null;
            }
        }

        public async Task SyncAllAsync(Guid tenantId, Guid branchId)
        {
            _logger.LogInformation("Starting full sync for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
            
            var tasks = new[]
            {
                SyncPatientsAsync(tenantId, branchId),
                SyncSalesAsync(tenantId, branchId),
                SyncPaymentsAsync(tenantId, branchId),
                SyncInventoryAsync(tenantId, branchId)
            };

            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Completed full sync for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
        }

        public async Task<SyncStatus> GetSyncStatusAsync(Guid tenantId, Guid branchId)
        {
            var key = (tenantId, branchId);
            return GetOrCreateSyncStatus(key);
        }

        public async Task InvalidateCacheAsync(Guid tenantId, Guid branchId, string entityType)
        {
            var cacheKeys = new List<string>();
            
            switch (entityType.ToLower())
            {
                case "patients":
                    cacheKeys.Add($"patients_{tenantId}_{branchId}");
                    break;
                case "sales":
                    cacheKeys.Add($"sales_{tenantId}_{branchId}");
                    break;
                case "payments":
                    cacheKeys.Add($"payments_{tenantId}_{branchId}");
                    break;
                case "inventory":
                    cacheKeys.Add($"inventory_{tenantId}_{branchId}");
                    break;
                case "all":
                    cacheKeys.AddRange(new[]
                    {
                        $"patients_{tenantId}_{branchId}",
                        $"sales_{tenantId}_{branchId}",
                        $"payments_{tenantId}_{branchId}",
                        $"inventory_{tenantId}_{branchId}"
                    });
                    break;
            }

            foreach (var key in cacheKeys)
            {
                _cache.Remove(key);
            }

            _logger.LogInformation("Invalidated cache for {EntityType} in tenant {TenantId}, branch {BranchId}", 
                entityType, tenantId, branchId);
        }

        private SyncStatus GetOrCreateSyncStatus((Guid, Guid) key)
        {
            if (!_syncStatuses.ContainsKey(key))
            {
                _syncStatuses[key] = new SyncStatus();
            }
            return _syncStatuses[key];
        }

        private async Task<List<Tenant>> GetActiveTenantsAsync()
        {
            return await _context.Tenants
                .Where(t => t.Status == "active")
                .ToListAsync();
        }

        private async Task<List<Branch>> GetTenantBranchesAsync(Guid tenantId)
        {
            // This would typically come from a branches table
            // For now, return a default branch
            return new List<Branch>
            {
                new Branch { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Main Branch" }
            };
        }

        public async Task<SyncStatus> GetSyncStatusAsync()
        {
            // Return overall sync status
            return new SyncStatus
            {
                IsSyncing = false,
                CurrentOperation = null,
                LastSyncAttempt = DateTime.UtcNow,
                LastSyncSuccess = DateTime.UtcNow,
                SyncErrors = new List<string>()
            };
        }

        public async Task TriggerSyncAsync(string entityType)
        {
            _logger.LogInformation("Triggering sync for entity type: {EntityType}", entityType);
            
            // This would trigger sync for specific entity type
            // Implementation depends on specific requirements
            await Task.CompletedTask;
        }
    }

    public class SyncStatus
    {
        public bool IsSyncing { get; set; }
        public string? CurrentOperation { get; set; }
        public DateTime? LastSyncAttempt { get; set; }
        public DateTime? LastSyncSuccess { get; set; }
        public DateTime? LastSyncFailure { get; set; }
        public List<string> SyncErrors { get; set; } = new();
        public double SyncProgress => 0; // Could be implemented for progress tracking
    }

    // Temporary classes for the sync service
    public class Branch
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
