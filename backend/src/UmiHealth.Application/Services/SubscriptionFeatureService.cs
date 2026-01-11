using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Persistence.Data;
using UmiHealth.Application.Models;

namespace UmiHealth.Application.Services
{
    public interface ISubscriptionFeatureService
    {
        Task<bool> IsFeatureAllowedAsync(Guid tenantId, string feature);
        Task<bool> IsWithinUserLimitAsync(Guid tenantId);
        Task<bool> IsWithinBranchLimitAsync(Guid tenantId);
        Task<IActionResult> EnforceFeatureAccessAsync(HttpContext context, string feature);
        Task<IActionResult> EnforceUserLimitAsync(HttpContext context);
        Task<IActionResult> EnforceBranchLimitAsync(HttpContext context);
        List<string> GetAvailableFeatures(string planType);
    }

    public class SubscriptionFeatureService : ISubscriptionFeatureService
    {
        private readonly SharedDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionFeatureService> _logger;

        public SubscriptionFeatureService(
            SharedDbContext context,
            ISubscriptionService subscriptionService,
            ILogger<SubscriptionFeatureService> logger)
        {
            _context = context;
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        public async Task<bool> IsFeatureAllowedAsync(Guid tenantId, string feature)
        {
            try
            {
                var subscriptionStatus = await GetSubscriptionStatus(tenantId);
                if (!subscriptionStatus.HasAccess)
                {
                    return false;
                }

                // Trial users have access to all features
                if (subscriptionStatus.IsTrial)
                {
                    return true;
                }

                var availableFeatures = GetAvailableFeatures(subscriptionStatus.PlanType);
                return availableFeatures.Contains(feature, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking feature access for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<bool> IsWithinUserLimitAsync(Guid tenantId)
        {
            try
            {
                var subscriptionStatus = await GetSubscriptionStatus(tenantId);
                if (!subscriptionStatus.HasAccess || subscriptionStatus.IsTrial)
                {
                    return true; // No limits during trial
                }

                // For now, assume within limits - this can be enhanced with proper user counting later
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user limit for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<bool> IsWithinBranchLimitAsync(Guid tenantId)
        {
            try
            {
                var subscriptionStatus = await GetSubscriptionStatus(tenantId);
                if (!subscriptionStatus.HasAccess || subscriptionStatus.IsTrial)
                {
                    return true; // No limits during trial
                }

                // For now, assume within limits - this can be enhanced with proper branch counting later
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking branch limit for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<IActionResult> EnforceFeatureAccessAsync(HttpContext context, string feature)
        {
            var tenantId = ExtractTenantId(context);
            if (!tenantId.HasValue)
            {
                return new Microsoft.AspNetCore.Mvc.ForbidResult();
            }

            var isAllowed = await IsFeatureAllowedAsync(tenantId.Value, feature);
            if (!isAllowed)
            {
                var subscriptionStatus = await GetSubscriptionStatus(tenantId.Value);
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
                {
                    success = false,
                    message = $"Feature '{feature}' is not available in your subscription plan",
                    currentPlan = subscriptionStatus.PlanType,
                    requiredPlan = GetRequiredPlanForFeature(feature),
                    upgradeUrl = "/api/v1/subscription/plans"
                });
            }

            return null; // Access granted
        }

        public async Task<IActionResult> EnforceUserLimitAsync(HttpContext context)
        {
            var tenantId = ExtractTenantId(context);
            if (!tenantId.HasValue)
            {
                return new Microsoft.AspNetCore.Mvc.ForbidResult();
            }

            var withinLimit = await IsWithinUserLimitAsync(tenantId.Value);
            if (!withinLimit)
            {
                var subscriptionStatus = await GetSubscriptionStatus(tenantId.Value);
                var currentUsers = await GetCurrentUserCountAsync(tenantId.Value);
                
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
                {
                    success = false,
                    message = "User limit reached for your subscription plan",
                    currentPlan = subscriptionStatus.PlanType,
                    maxUsers = subscriptionStatus.MaxUsers,
                    currentUsers = currentUsers,
                    requiresUpgrade = true,
                    upgradeUrl = "/api/v1/subscription/plans"
                });
            }

            return null; // Within limit
        }

        public async Task<IActionResult> EnforceBranchLimitAsync(HttpContext context)
        {
            var tenantId = ExtractTenantId(context);
            if (!tenantId.HasValue)
            {
                return new Microsoft.AspNetCore.Mvc.ForbidResult();
            }

            var withinLimit = await IsWithinBranchLimitAsync(tenantId.Value);
            if (!withinLimit)
            {
                var subscriptionStatus = await GetSubscriptionStatus(tenantId.Value);
                var currentBranches = await GetCurrentBranchCountAsync(tenantId.Value);
                
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
                {
                    success = false,
                    message = "Branch limit reached for your subscription plan",
                    currentPlan = subscriptionStatus.PlanType,
                    maxBranches = subscriptionStatus.MaxBranches,
                    currentBranches = currentBranches,
                    requiresUpgrade = true,
                    upgradeUrl = "/api/v1/subscription/plans"
                });
            }

            return null; // Within limit
        }

        public List<string> GetAvailableFeatures(string planType)
        {
            return planType.ToLower() switch
            {
                "starter" => new List<string>
                {
                    "basic_prescriptions", "inventory_tracking", "basic_reports", "email_support",
                    "user_management", "patient_management", "basic_analytics"
                },
                "professional" => new List<string>
                {
                    "basic_prescriptions", "inventory_tracking", "basic_reports", "email_support",
                    "user_management", "patient_management", "basic_analytics",
                    "advanced_analytics", "multi_branch_support", "priority_support", "api_access"
                },
                "business" => new List<string>
                {
                    "basic_prescriptions", "inventory_tracking", "basic_reports", "email_support",
                    "user_management", "patient_management", "basic_analytics",
                    "advanced_analytics", "multi_branch_support", "priority_support", "api_access",
                    "custom_integrations", "dedicated_support", "advanced_security", "priority_updates"
                },
                "enterprise" => new List<string>
                {
                    "basic_prescriptions", "inventory_tracking", "basic_reports", "email_support",
                    "user_management", "patient_management", "basic_analytics",
                    "advanced_analytics", "multi_branch_support", "priority_support", "api_access",
                    "custom_integrations", "dedicated_support", "advanced_security", "priority_updates",
                    "unlimited_storage", "24_7_support", "custom_development", "white_label_options"
                },
                _ => new List<string>()
            };
        }

        private string GetRequiredPlanForFeature(string feature)
        {
            return feature.ToLower() switch
            {
                "advanced_analytics" or "multi_branch_support" or "priority_support" or "api_access" => "Professional",
                "custom_integrations" or "dedicated_support" or "advanced_security" or "priority_updates" => "Business",
                "unlimited_storage" or "24_7_support" or "custom_development" or "white_label_options" => "Enterprise",
                _ => "Starter"
            };
        }

        private async Task<SubscriptionStatus> GetSubscriptionStatus(Guid tenantId)
        {
            try
            {
                // For now, return a basic status - this can be enhanced later with proper entity integration
                var subscription = await _subscriptionService.GetTenantSubscriptionAsync(tenantId);
                if (subscription != null && subscription.EndDate > DateTime.UtcNow)
                {
                    var planLimits = GetPlanLimits(subscription.PlanType);
                    return new SubscriptionStatus
                    {
                        HasAccess = true,
                        IsTrial = false,
                        PlanType = subscription.PlanType,
                        SubscriptionEndDate = subscription.EndDate,
                        MaxUsers = planLimits.MaxUsers,
                        MaxBranches = planLimits.MaxBranches,
                        Reason = "Active subscription",
                        IsPaidSubscription = true
                    };
                }

                // Default to trial status for now
                return new SubscriptionStatus
                {
                    HasAccess = true,
                    IsTrial = true,
                    PlanType = "Trial",
                    TrialEndDate = DateTime.UtcNow.AddDays(14),
                    MaxUsers = int.MaxValue,
                    MaxBranches = int.MaxValue,
                    Reason = "Trial period active"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription status for tenant {TenantId}", tenantId);
                return new SubscriptionStatus
                {
                    HasAccess = false,
                    Reason = "Error checking subscription status"
                };
            }
        }

        private PlanLimits GetPlanLimits(string planType)
        {
            return planType.ToLower() switch
            {
                "starter" => new PlanLimits { MaxUsers = 3, MaxBranches = 1 },
                "professional" => new PlanLimits { MaxUsers = 10, MaxBranches = 3 },
                "business" => new PlanLimits { MaxUsers = 25, MaxBranches = 5 },
                "enterprise" => new PlanLimits { MaxUsers = 50, MaxBranches = 10 },
                _ => new PlanLimits { MaxUsers = 3, MaxBranches = 1 } // Default to starter limits
            };
        }

        private Guid? ExtractTenantId(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = context.User.FindFirst("TenantId") ?? context.User.FindFirst("tenant_id");
                if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
                {
                    return tenantId;
                }
            }
            return null;
        }

        private async Task<int> GetCurrentUserCountAsync(Guid tenantId)
        {
            try
            {
                return await _context.Users.CountAsync(u => u.TenantId == tenantId);
            }
            catch
            {
                return 0;
            }
        }

        private async Task<int> GetCurrentBranchCountAsync(Guid tenantId)
        {
            try
            {
                return await _context.Branches.CountAsync(b => b.TenantId == tenantId);
            }
            catch
            {
                return 0;
            }
        }
    }

    public class PlanLimits
    {
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }
    }
}
