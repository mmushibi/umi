using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UmiHealth.Api.Controllers;

namespace UmiHealth.Application.Services
{
    public class OperationsDataSyncService : IDataSyncService
    {
        private readonly ILogger<OperationsDataSyncService> _logger;
        private static DateTime _lastSyncTime = DateTime.UtcNow;
        private static string _syncStatus = "completed";
        private static int _pendingRecords = 0;
        private static int _failedRecords = 0;

        public OperationsDataSyncService(ILogger<OperationsDataSyncService> logger)
        {
            _logger = logger;
        }

        public async Task<SyncStatusDto> GetSyncStatusAsync()
        {
            try
            {
                return new SyncStatusDto
                {
                    LastSyncTime = _lastSyncTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = _syncStatus,
                    PendingRecords = _pendingRecords,
                    FailedRecords = _failedRecords,
                    Details = new Dictionary<string, object>
                    {
                        ["lastFullSync"] = _lastSyncTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        ["lastIncrementalSync"] = DateTime.UtcNow.AddMinutes(-30).ToString("yyyy-MM-dd HH:mm:ss"),
                        ["syncInterval"] = "30 minutes",
                        ["autoSyncEnabled"] = true,
                        ["nextScheduledSync"] = DateTime.UtcNow.AddMinutes(30).ToString("yyyy-MM-dd HH:mm:ss")
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync status");
                throw;
            }
        }

        public async Task TriggerSyncAsync(string syncType)
        {
            try
            {
                _syncStatus = "running";
                _logger.LogInformation($"Starting {syncType} sync at {DateTime.UtcNow}");

                // Simulate sync process
                await Task.Delay(2000);

                switch (syncType.ToLower())
                {
                    case "full":
                        await PerformFullSync();
                        break;
                    case "incremental":
                        await PerformIncrementalSync();
                        break;
                    case "tenants":
                        await SyncTenants();
                        break;
                    case "users":
                        await SyncUsers();
                        break;
                    case "subscriptions":
                        await SyncSubscriptions();
                        break;
                    default:
                        await PerformFullSync();
                        break;
                }

                _lastSyncTime = DateTime.UtcNow;
                _syncStatus = "completed";
                _failedRecords = 0;

                _logger.LogInformation($"Completed {syncType} sync at {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                _syncStatus = "failed";
                _failedRecords++;
                _logger.LogError(ex, $"Error during {syncType} sync");
                throw;
            }
        }

        private async Task PerformFullSync()
        {
            _logger.LogInformation("Performing full sync...");
            _pendingRecords = 150; // Simulate pending records

            await SyncTenants();
            await SyncUsers();
            await SyncSubscriptions();

            _pendingRecords = 0;
        }

        private async Task PerformIncrementalSync()
        {
            _logger.LogInformation("Performing incremental sync...");
            _pendingRecords = 25; // Simulate fewer pending records

            // Sync only recent changes
            await Task.Delay(500);
            _pendingRecords = 0;
        }

        private async Task SyncTenants()
        {
            _logger.LogInformation("Syncing tenants...");
            await Task.Delay(300);
        }

        private async Task SyncUsers()
        {
            _logger.LogInformation("Syncing users...");
            await Task.Delay(400);
        }

        private async Task SyncSubscriptions()
        {
            _logger.LogInformation("Syncing subscriptions...");
            await Task.Delay(300);
        }
    }
}
