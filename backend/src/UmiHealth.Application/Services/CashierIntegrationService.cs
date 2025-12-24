using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UmiHealth.Application.Services
{
    public interface ICashierIntegrationService
    {
        Task RegisterCashierPortalAsync(Guid tenantId, Guid branchId, Guid userId);
        Task UnregisterCashierPortalAsync(Guid tenantId, Guid branchId, Guid userId);
        Task NotifyDataChangeAsync(Guid tenantId, Guid branchId, string entityType, object data);
    }

    public class CashierIntegrationService : BackgroundService, ICashierIntegrationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CashierIntegrationService> _logger;
        private readonly Dictionary<string, DateTime> _activeCashierPortals;

        public CashierIntegrationService(
            IServiceProvider serviceProvider,
            ILogger<CashierIntegrationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _activeCashierPortals = new Dictionary<string, DateTime>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupInactivePortals();
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in cashier integration service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        public async Task RegisterCashierPortalAsync(Guid tenantId, Guid branchId, Guid userId)
        {
            var portalKey = $"{tenantId}_{branchId}_{userId}";
            _activeCashierPortals[portalKey] = DateTime.UtcNow;
            
            _logger.LogInformation("Cashier portal registered: {PortalKey}", portalKey);
            
            // Trigger initial data sync for this portal
            using var scope = _serviceProvider.CreateScope();
            var dataSyncService = scope.ServiceProvider.GetRequiredService<IDataSyncService>();
            await dataSyncService.SyncAllAsync(tenantId, branchId);
        }

        public async Task UnregisterCashierPortalAsync(Guid tenantId, Guid branchId, Guid userId)
        {
            var portalKey = $"{tenantId}_{branchId}_{userId}";
            _activeCashierPortals.Remove(portalKey);
            
            _logger.LogInformation("Cashier portal unregistered: {PortalKey}", portalKey);
        }

        public async Task NotifyDataChangeAsync(Guid tenantId, Guid branchId, string entityType, object data)
        {
            // Check if any active cashier portals for this tenant/branch
            var hasActivePortals = _activeCashierPortals.Keys
                .Any(key => key.StartsWith($"{tenantId}_{branchId}_"));

            if (!hasActivePortals)
            {
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dataSyncService = scope.ServiceProvider.GetRequiredService<IDataSyncService>();
                
                // Invalidate cache for the specific entity type
                await dataSyncService.InvalidateCacheAsync(tenantId, branchId, entityType);
                
                // Trigger sync for the specific entity type
                switch (entityType.ToLower())
                {
                    case "patients":
                        await dataSyncService.SyncPatientsAsync(tenantId, branchId);
                        break;
                    case "sales":
                        await dataSyncService.SyncSalesAsync(tenantId, branchId);
                        break;
                    case "payments":
                        await dataSyncService.SyncPaymentsAsync(tenantId, branchId);
                        break;
                    case "inventory":
                        await dataSyncService.SyncInventoryAsync(tenantId, branchId);
                        break;
                    default:
                        await dataSyncService.SyncAllAsync(tenantId, branchId);
                        break;
                }

                _logger.LogInformation("Data change notification sent for {EntityType} in tenant {TenantId}, branch {BranchId}", 
                    entityType, tenantId, branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending data change notification for {EntityType}", entityType);
            }
        }

        private async Task CleanupInactivePortals()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-30); // Remove portals inactive for 30 minutes
            var inactivePortals = _activeCashierPortals
                .Where(kvp => kvp.Value < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var portalKey in inactivePortals)
            {
                _activeCashierPortals.Remove(portalKey);
                _logger.LogInformation("Cleaned up inactive cashier portal: {PortalKey}", portalKey);
            }
        }
    }
}
