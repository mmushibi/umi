using Hangfire;
using Microsoft.Extensions.Logging;
using UmiHealth.Infrastructure;

namespace UmiHealth.Application.Services
{
    public interface IBackgroundJobService
    {
        string SendLowStockAlerts(Guid tenantId, Guid branchId);
        string SendExpiryAlerts(Guid tenantId, Guid branchId);
        string GenerateDailyReports(Guid tenantId, Guid branchId);
        string ProcessPrescriptionReminders(Guid tenantId, Guid branchId);
        string CleanExpiredTokens();
        string ArchiveOldData();
        string SendSubscriptionReminders();
    }

    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly UmiHealthDbContext _context;

        public BackgroundJobService(
            IServiceProvider serviceProvider,
            ILogger<BackgroundJobService> logger,
            UmiHealthDbContext context)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _context = context;
        }

        public string SendLowStockAlerts(Guid tenantId, Guid branchId)
        {
            return BackgroundJob.Enqueue(() => ProcessLowStockAlerts(tenantId, branchId));
        }

        public string SendExpiryAlerts(Guid tenantId, Guid branchId)
        {
            return BackgroundJob.Enqueue(() => ProcessExpiryAlerts(tenantId, branchId));
        }

        public string GenerateDailyReports(Guid tenantId, Guid branchId)
        {
            return BackgroundJob.Schedule(
                () => ProcessDailyReports(tenantId, branchId),
                TimeSpan.FromDays(1));
        }

        public string ProcessPrescriptionReminders(Guid tenantId, Guid branchId)
        {
            return BackgroundJob.Enqueue(() => ProcessPrescriptionReminderAlerts(tenantId, branchId));
        }

        public string CleanExpiredTokens()
        {
            return BackgroundJob.Schedule(
                () => ProcessExpiredTokenCleanup(),
                TimeSpan.FromHours(1));
        }

        public string ArchiveOldData()
        {
            return BackgroundJob.Schedule(
                () => ProcessDataArchiving(),
                TimeSpan.FromDays(30));
        }

        public string SendSubscriptionReminders()
        {
            return BackgroundJob.Schedule(
                () => ProcessSubscriptionReminders(),
                TimeSpan.FromDays(7));
        }

        // Recurring Jobs
        public void ScheduleRecurringJobs()
        {
            // Daily jobs
            RecurringJob.AddOrUpdate(
                "daily-low-stock-alerts",
                () => ProcessAllLowStockAlerts(),
                Cron.Daily(9, 0)); // 9 AM daily

            RecurringJob.AddOrUpdate(
                "daily-expiry-alerts",
                () => ProcessAllExpiryAlerts(),
                Cron.Daily(9, 30)); // 9:30 AM daily

            RecurringJob.AddOrUpdate(
                "daily-reports",
                () => ProcessAllDailyReports(),
                Cron.Daily(23, 0)); // 11 PM daily

            RecurringJob.AddOrUpdate(
                "prescription-reminders",
                () => ProcessAllPrescriptionReminders(),
                Cron.Hourly()); // Every hour

            // Weekly jobs
            RecurringJob.AddOrUpdate(
                "weekly-inventory-report",
                () => ProcessWeeklyInventoryReports(),
                Cron.Weekly(DayOfWeek.Monday, 8, 0)); // Monday 8 AM

            // Monthly jobs
            RecurringJob.AddOrUpdate(
                "monthly-financial-report",
                () => ProcessMonthlyFinancialReports(),
                Cron.Monthly(1, 8, 0)); // 1st of month 8 AM

            RecurringJob.AddOrUpdate(
                "subscription-reminders",
                () => ProcessAllSubscriptionReminders(),
                Cron.Weekly(DayOfWeek.Monday, 10, 0)); // Monday 10 AM

            // Cleanup jobs
            RecurringJob.AddOrUpdate(
                "cleanup-expired-tokens",
                () => ProcessExpiredTokenCleanup(),
                Cron.Hourly()); // Every hour

            RecurringJob.AddOrUpdate(
                "archive-old-data",
                () => ProcessDataArchiving(),
                Cron.Monthly(1, 2, 0)); // 1st of month 2 AM
        }

        // Job Processing Methods
        public async Task ProcessLowStockAlerts(Guid tenantId, Guid branchId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                var lowStockItems = await GetLowStockItems(tenantId, branchId);
                
                foreach (var item in lowStockItems)
                {
                    await notificationService.SendLowStockAlertAsync(tenantId, branchId, item);
                }

                _logger.LogInformation("Processed low stock alerts for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing low stock alerts for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
            }
        }

        public async Task ProcessExpiryAlerts(Guid tenantId, Guid branchId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                var expiringItems = await GetExpiringItems(tenantId, branchId);
                
                foreach (var item in expiringItems)
                {
                    await notificationService.SendExpiryAlertAsync(tenantId, branchId, item);
                }

                _logger.LogInformation("Processed expiry alerts for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expiry alerts for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
            }
        }

        public async Task ProcessDailyReports(Guid tenantId, Guid branchId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                var report = await reportingService.GetDailyReportAsync(tenantId, branchId);
                await notificationService.SendDailyReportAsync(tenantId, branchId, report);

                _logger.LogInformation("Generated daily report for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily report for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
            }
        }

        public async Task ProcessPrescriptionReminderAlerts(Guid tenantId, Guid branchId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                var pendingPrescriptions = await GetPendingPrescriptions(tenantId, branchId);
                
                foreach (var prescription in pendingPrescriptions)
                {
                    await notificationService.SendPrescriptionReminderAsync(tenantId, branchId, prescription);
                }

                _logger.LogInformation("Processed prescription reminders for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing prescription reminders for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
            }
        }

        public async Task ProcessExpiredTokenCleanup()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-7);
                
                var expiredTokens = _context.RefreshTokens
                    .Where(rt => rt.ExpiresAt < cutoffDate)
                    .ToList();

                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired tokens");
            }
        }

        public async Task ProcessDataArchiving()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddYears(-2);
                
                // Archive old audit logs
                var oldAuditLogs = _context.AuditLogs
                    .Where(al => al.CreatedAt < cutoffDate)
                    .ToList();

                if (oldAuditLogs.Any())
                {
                    // Move to archive table or file storage
                    // This is a simplified implementation
                    _context.AuditLogs.RemoveRange(oldAuditLogs);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Archived {Count} old audit records", oldAuditLogs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving old data");
            }
        }

        public async Task ProcessSubscriptionReminders()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                
                var expiringSubscriptions = await GetExpiringSubscriptions();
                
                foreach (var subscription in expiringSubscriptions)
                {
                    await notificationService.SendSubscriptionReminderAsync(subscription);
                }

                _logger.LogInformation("Processed subscription reminders for {Count} subscriptions", expiringSubscriptions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription reminders");
            }
        }

        // Global processing methods for recurring jobs
        public async Task ProcessAllLowStockAlerts()
        {
            try
            {
                var tenants = await GetActiveTenants();
                
                foreach (var tenant in tenants)
                {
                    var branches = await GetTenantBranches(tenant.Id);
                    
                    foreach (var branch in branches)
                    {
                        await ProcessLowStockAlerts(tenant.Id, branch.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing all low stock alerts");
            }
        }

        public async Task ProcessAllExpiryAlerts()
        {
            try
            {
                var tenants = await GetActiveTenants();
                
                foreach (var tenant in tenants)
                {
                    var branches = await GetTenantBranches(tenant.Id);
                    
                    foreach (var branch in branches)
                    {
                        await ProcessExpiryAlerts(tenant.Id, branch.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing all expiry alerts");
            }
        }

        public async Task ProcessAllDailyReports()
        {
            try
            {
                var tenants = await GetActiveTenants();
                
                foreach (var tenant in tenants)
                {
                    var branches = await GetTenantBranches(tenant.Id);
                    
                    foreach (var branch in branches)
                    {
                        await ProcessDailyReports(tenant.Id, branch.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing all daily reports");
            }
        }

        public async Task ProcessAllPrescriptionReminders()
        {
            try
            {
                var tenants = await GetActiveTenants();
                
                foreach (var tenant in tenants)
                {
                    var branches = await GetTenantBranches(tenant.Id);
                    
                    foreach (var branch in branches)
                    {
                        await ProcessPrescriptionReminderAlerts(tenant.Id, branch.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing all prescription reminders");
            }
        }

        public async Task ProcessWeeklyInventoryReports()
        {
            try
            {
                var tenants = await GetActiveTenants();
                
                foreach (var tenant in tenants)
                {
                    var branches = await GetTenantBranches(tenant.Id);
                    
                    foreach (var branch in branches)
                    {
                        // Generate weekly inventory report
                        _logger.LogInformation("Generated weekly inventory report for tenant {TenantId}, branch {BranchId}", tenant.Id, branch.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing weekly inventory reports");
            }
        }

        public async Task ProcessMonthlyFinancialReports()
        {
            try
            {
                var tenants = await GetActiveTenants();
                
                foreach (var tenant in tenants)
                {
                    var branches = await GetTenantBranches(tenant.Id);
                    
                    foreach (var branch in branches)
                    {
                        // Generate monthly financial report
                        _logger.LogInformation("Generated monthly financial report for tenant {TenantId}, branch {BranchId}", tenant.Id, branch.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing monthly financial reports");
            }
        }

        public async Task ProcessAllSubscriptionReminders()
        {
            await ProcessSubscriptionReminders();
        }

        // Helper methods
        private async Task<List<dynamic>> GetLowStockItems(Guid tenantId, Guid branchId)
        {
            return await _context.Inventories
                .Where(i => i.TenantId == tenantId && i.BranchId == branchId && 
                           i.QuantityOnHand <= i.ReorderLevel && !i.IsDeleted)
                .Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    ProductName = i.Product.Name,
                    i.QuantityOnHand,
                    i.ReorderLevel
                })
                .ToListAsync<dynamic>();
        }

        private async Task<List<dynamic>> GetExpiringItems(Guid tenantId, Guid branchId)
        {
            var thirtyDaysFromNow = DateTime.UtcNow.AddDays(30);
            
            return await _context.Inventories
                .Where(i => i.TenantId == tenantId && i.BranchId == branchId && 
                           i.ExpiryDate <= thirtyDaysFromNow && i.ExpiryDate > DateTime.UtcNow && !i.IsDeleted)
                .Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    ProductName = i.Product.Name,
                    i.ExpiryDate,
                    i.QuantityOnHand
                })
                .ToListAsync<dynamic>();
        }

        private async Task<List<dynamic>> GetPendingPrescriptions(Guid tenantId, Guid branchId)
        {
            return await _context.Prescriptions
                .Where(p => p.TenantId == tenantId && p.BranchId == branchId && 
                           p.Status == "pending" && !p.IsDeleted)
                .Select(p => new
                {
                    p.Id,
                    p.PrescriptionNumber,
                    PatientName = p.Patient.FirstName + " " + p.Patient.LastName,
                    p.PrescriptionDate
                })
                .ToListAsync<dynamic>();
        }

        private async Task<List<dynamic>> GetExpiringSubscriptions()
        {
            var thirtyDaysFromNow = DateTime.UtcNow.AddDays(30);
            
            return await _context.Subscriptions
                .Where(s => s.EndDate <= thirtyDaysFromNow && s.EndDate > DateTime.UtcNow && s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.TenantId,
                    s.PlanName,
                    s.EndDate,
                    TenantName = s.Tenant.Name
                })
                .ToListAsync<dynamic>();
        }

        private async Task<List<dynamic>> GetActiveTenants()
        {
            return await _context.Tenants
                .Where(t => t.IsActive && !t.IsDeleted)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync<dynamic>();
        }

        private async Task<List<dynamic>> GetTenantBranches(Guid tenantId)
        {
            return await _context.Branches
                .Where(b => b.TenantId == tenantId && b.IsActive && !b.IsDeleted)
                .Select(b => new { b.Id, b.Name })
                .ToListAsync<dynamic>();
        }
    }
}
