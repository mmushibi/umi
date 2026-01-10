using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface ISubscriptionService
    {
        Task<bool> IsSubscriptionActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Subscription?> GetTenantSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Subscription> CreateSubscriptionAsync(Guid tenantId, string plan, CancellationToken cancellationToken = default);
        Task<bool> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Subscription>> GetSubscriptionsExpiringSoonAsync(int daysAhead = 30, CancellationToken cancellationToken = default);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly UmiHealthDbContext _context;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(UmiHealthDbContext context, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsSubscriptionActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .Where(s => s.TenantId == tenantId && s.IsActive)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync(cancellationToken);

                return subscription != null && subscription.EndDate > DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscription status for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<Subscription?> GetTenantSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Subscriptions
                    .Where(s => s.TenantId == tenantId && s.IsActive)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task<Subscription> CreateSubscriptionAsync(Guid tenantId, string plan, CancellationToken cancellationToken = default)
        {
            try
            {
                var subscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Plan = plan,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(30), // Default 30 days
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created subscription {SubscriptionId} for tenant {TenantId} with plan {Plan}", 
                    subscription.Id, tenantId, plan);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken);

                if (subscription == null)
                    return false;

                subscription.IsActive = false;
                subscription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cancelled subscription {SubscriptionId}", subscriptionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
                return false;
            }
        }

        public async Task<IEnumerable<Subscription>> GetSubscriptionsExpiringSoonAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
                
                return await _context.Subscriptions
                    .Where(s => s.IsActive && 
                               s.EndDate <= cutoffDate && 
                               s.EndDate > DateTime.UtcNow)
                    .Include(s => s.Tenant)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions expiring soon");
                return Enumerable.Empty<Subscription>();
            }
        }
    }
}
