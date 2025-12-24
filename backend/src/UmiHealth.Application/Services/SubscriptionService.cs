using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface ISubscriptionService
    {
        Task<Subscription> CreateTrialSubscriptionAsync(Guid tenantId);
        Task<Subscription> GetActiveSubscriptionAsync(Guid tenantId);
        Task<Subscription> CreateSubscriptionAsync(Guid tenantId, CreateSubscriptionRequest request);
        Task<Subscription> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionRequest request);
        Task<bool> CancelSubscriptionAsync(Guid subscriptionId);
        Task<IEnumerable<Subscription>> GetSubscriptionHistoryAsync(Guid tenantId);
        Task<SubscriptionReminder> GetSubscriptionReminderAsync(Guid tenantId);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly SharedDbContext _context;

        public SubscriptionService(SharedDbContext context)
        {
            _context = context;
        }

        public async Task<Subscription> CreateTrialSubscriptionAsync(Guid tenantId)
        {
            var trialSubscription = new Subscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PlanType = "trial",
                Status = "active",
                BillingCycle = "monthly",
                Amount = 0,
                Currency = "ZMW",
                Features = new Dictionary<string, object>
                {
                    { "max_branches", 1 },
                    { "max_users", 10 },
                    { "inventory_management", true },
                    { "prescription_management", true },
                    { "pos_functionality", true },
                    { "basic_reports", true },
                    { "api_access", true },
                    { "email_support", true }
                },
                Limits = new Dictionary<string, object>
                {
                    { "max_branches", 1 },
                    { "max_users", 10 },
                    { "max_transactions_per_month", 500 },
                    { "max_storage_gb", 5 }
                },
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(14), // 14-day trial
                AutoRenew = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(trialSubscription);
            await _context.SaveChangesAsync();

            return trialSubscription;
        }

        public async Task<Subscription> GetActiveSubscriptionAsync(Guid tenantId)
        {
            return await _context.Subscriptions
                .Where(s => s.TenantId == tenantId && 
                           s.Status == "active" && 
                           (s.EndDate == null || s.EndDate > DateTime.UtcNow))
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Subscription> CreateSubscriptionAsync(Guid tenantId, CreateSubscriptionRequest request)
        {
            // Deactivate existing trial subscription if exists
            var existingTrial = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && 
                                       s.PlanType == "trial" && 
                                       s.Status == "active");

            if (existingTrial != null)
            {
                existingTrial.Status = "expired";
                existingTrial.UpdatedAt = DateTime.UtcNow;
            }

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PlanType = request.PlanType,
                Status = "active",
                BillingCycle = request.BillingCycle,
                Amount = request.Amount,
                Currency = request.Currency,
                Features = GetPlanFeatures(request.PlanType),
                Limits = GetPlanLimits(request.PlanType),
                StartDate = DateTime.UtcNow,
                EndDate = CalculateEndDate(DateTime.UtcNow, request.BillingCycle),
                AutoRenew = request.AutoRenew,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return subscription;
        }

        public async Task<Subscription> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionRequest request)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null)
                return null;

            subscription.PlanType = request.PlanType;
            subscription.Amount = request.Amount;
            subscription.BillingCycle = request.BillingCycle;
            subscription.AutoRenew = request.AutoRenew;
            subscription.Features = GetPlanFeatures(request.PlanType);
            subscription.Limits = GetPlanLimits(request.PlanType);
            subscription.EndDate = CalculateEndDate(subscription.EndDate ?? DateTime.UtcNow, request.BillingCycle);
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
            if (subscription == null)
                return false;

            subscription.Status = "cancelled";
            subscription.AutoRenew = false;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Subscription>> GetSubscriptionHistoryAsync(Guid tenantId)
        {
            return await _context.Subscriptions
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<SubscriptionReminder> GetSubscriptionReminderAsync(Guid tenantId)
        {
            var activeSubscription = await GetActiveSubscriptionAsync(tenantId);
            if (activeSubscription == null || activeSubscription.EndDate == null)
                return null;

            var daysRemaining = (int)(activeSubscription.EndDate.Value - DateTime.UtcNow).TotalDays;

            var reminder = new SubscriptionReminder
            {
                SubscriptionId = activeSubscription.Id,
                DaysRemaining = daysRemaining,
                IsTrial = activeSubscription.PlanType == "trial",
                ShowUpgradeBanner = daysRemaining <= 7 && activeSubscription.PlanType == "trial",
                ReminderMessage = GetReminderMessage(daysRemaining, activeSubscription.PlanType),
                UrgencyLevel = GetUrgencyLevel(daysRemaining),
                RequiresImmediateAction = daysRemaining <= 3
            };

            return reminder;
        }

        private Dictionary<string, object> GetPlanFeatures(string planType)
        {
            return planType.ToLower() switch
            {
                "go" => new Dictionary<string, object>
                {
                    { "max_branches", 1 },
                    { "max_users", 5 },
                    { "inventory_management", true },
                    { "prescription_management", true },
                    { "pos_functionality", true },
                    { "basic_reports", true },
                    { "email_support", true }
                },
                "grow" => new Dictionary<string, object>
                {
                    { "max_branches", 3 },
                    { "max_users", 15 },
                    { "inventory_management", true },
                    { "prescription_management", true },
                    { "pos_functionality", true },
                    { "advanced_reports", true },
                    { "api_access", true },
                    { "email_support", true },
                    { "phone_support", true }
                },
                "pro" => new Dictionary<string, object>
                {
                    { "max_branches", -1 }, // Unlimited
                    { "max_users", -1 }, // Unlimited
                    { "inventory_management", true },
                    { "prescription_management", true },
                    { "pos_functionality", true },
                    { "advanced_reports", true },
                    { "custom_reports", true },
                    { "api_access", true },
                    { "webhooks", true },
                    { "priority_support", true },
                    { "dedicated_account_manager", true }
                },
                _ => new Dictionary<string, object>()
            };
        }

        private Dictionary<string, object> GetPlanLimits(string planType)
        {
            return planType.ToLower() switch
            {
                "go" => new Dictionary<string, object>
                {
                    { "max_branches", 1 },
                    { "max_users", 5 },
                    { "max_transactions_per_month", 1000 },
                    { "max_storage_gb", 10 }
                },
                "grow" => new Dictionary<string, object>
                {
                    { "max_branches", 3 },
                    { "max_users", 15 },
                    { "max_transactions_per_month", 5000 },
                    { "max_storage_gb", 50 }
                },
                "pro" => new Dictionary<string, object>
                {
                    { "max_branches", -1 }, // Unlimited
                    { "max_users", -1 }, // Unlimited
                    { "max_transactions_per_month", -1 }, // Unlimited
                    { "max_storage_gb", -1 } // Unlimited
                },
                _ => new Dictionary<string, object>()
            };
        }

        private DateTime CalculateEndDate(DateTime startDate, string billingCycle)
        {
            return billingCycle.ToLower() switch
            {
                "monthly" => startDate.AddMonths(1),
                "quarterly" => startDate.AddMonths(3),
                "annually" => startDate.AddYears(1),
                _ => startDate.AddMonths(1)
            };
        }

        private string GetReminderMessage(int daysRemaining, string planType)
        {
            if (planType != "trial")
                return $"Your subscription renews in {daysRemaining} days.";

            return daysRemaining switch
            {
                <= 0 => "Your trial has expired. Upgrade now to continue using Umi Health.",
                <= 3 => $"Your trial expires in {daysRemaining} days. Upgrade now to avoid interruption.",
                <= 7 => $"Your trial expires in {daysRemaining} days. Choose a plan to continue.",
                _ => $"Your trial expires in {daysRemaining} days."
            };
        }

        private string GetUrgencyLevel(int daysRemaining)
        {
            return daysRemaining switch
            {
                <= 0 => "critical",
                <= 3 => "high",
                <= 7 => "medium",
                _ => "low"
            };
        }
    }

    // DTOs
    public class CreateSubscriptionRequest
    {
        public string PlanType { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = "monthly";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public bool AutoRenew { get; set; } = true;
    }

    public class UpdateSubscriptionRequest
    {
        public string PlanType { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = "monthly";
        public decimal Amount { get; set; }
        public bool AutoRenew { get; set; } = true;
    }

    public class SubscriptionReminder
    {
        public Guid SubscriptionId { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsTrial { get; set; }
        public bool ShowUpgradeBanner { get; set; }
        public string ReminderMessage { get; set; } = string.Empty;
        public string UrgencyLevel { get; set; } = string.Empty;
        public bool RequiresImmediateAction { get; set; }
    }
}
