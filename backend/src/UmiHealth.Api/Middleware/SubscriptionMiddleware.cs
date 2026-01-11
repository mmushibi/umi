using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Persistence.Data;

namespace UmiHealth.API.Middleware
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SubscriptionMiddleware> _logger;
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionMiddleware(
            RequestDelegate next, 
            ILogger<SubscriptionMiddleware> logger,
            ISubscriptionService subscriptionService)
        {
            _next = next;
            _logger = logger;
            _subscriptionService = subscriptionService;
        }

        public async Task InvokeAsync(HttpContext context, SharedDbContext dbContext)
        {
            // Skip subscription check for auth endpoints and health checks
            if (ShouldSkipSubscriptionCheck(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Extract tenant information
            var tenantId = ExtractTenantId(context);
            if (!tenantId.HasValue)
            {
                await _next(context);
                return;
            }

            try
            {
                // Check if tenant has active subscription or is in trial period
                var subscriptionStatus = await GetSubscriptionStatus(tenantId.Value, dbContext);
                
                if (!subscriptionStatus.HasAccess)
                {
                    _logger.LogWarning("Access denied for tenant {TenantId}: {Reason}", 
                        tenantId.Value, subscriptionStatus.Reason);
                    
                    // Handle suspended tenants differently
                    if (subscriptionStatus.TenantSuspended)
                    {
                        // For API calls, return 403 Forbidden
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsJsonAsync(new { 
                                success = false, 
                                message = subscriptionStatus.Reason,
                                tenantSuspended = true,
                                requiresSupportContact = true
                            });
                        }
                        // For web page requests, redirect to suspended page
                        else
                        {
                            context.Response.Redirect("/public/account-suspended.html");
                        }
                        return;
                    }
                    
                    // Handle subscription expired
                    // For API calls, return 402 Payment Required
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                        await context.Response.WriteAsJsonAsync(new { 
                            success = false, 
                            message = subscriptionStatus.Reason,
                            requiresSubscription = true,
                            trialExpired = subscriptionStatus.TrialExpired,
                            subscriptionRequiredUrl = "/portals/admin/subscription.html"
                        });
                    }
                    // For web page requests, redirect to subscription page
                    else
                    {
                        context.Response.Redirect("/portals/admin/subscription.html");
                    }
                    return;
                }

                // Add subscription info to context for use in controllers
                context.Items["SubscriptionStatus"] = subscriptionStatus;
                context.Items["SubscriptionPlan"] = subscriptionStatus.PlanType;
                context.Items["IsTrial"] = subscriptionStatus.IsTrial;
                context.Items["MaxUsers"] = subscriptionStatus.MaxUsers;
                context.Items["MaxBranches"] = subscriptionStatus.MaxBranches;

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscription for tenant {TenantId}", tenantId.Value);
                // Allow access on subscription system errors to prevent breaking the app
                await _next(context);
            }
        }

        private bool ShouldSkipSubscriptionCheck(PathString path)
        {
            var skipPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/refresh-token",
                "/api/subscription/plans",
                "/api/health",
                "/test",
                "/public/signin.html",
                "/public/admin-signin.html",
                "/public/onboarding.html",
                "/public/accept-invitation.html",
                "/public/contact.html",
                "/public/account-suspended.html"
            };

            return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
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

        private async Task<SubscriptionStatus> GetSubscriptionStatus(Guid tenantId, SharedDbContext dbContext)
        {
            try
            {
                // Get tenant creation date for trial calculation
                var tenant = await dbContext.Tenants.FindAsync(tenantId);
                if (tenant == null)
                {
                    return new SubscriptionStatus 
                    { 
                        HasAccess = false, 
                        Reason = "Tenant not found" 
                    };
                }

                // Check if tenant is suspended
                if (tenant.IsSuspended)
                {
                    return new SubscriptionStatus
                    {
                        HasAccess = false,
                        IsTrial = false,
                        PlanType = "Suspended",
                        TrialExpired = false,
                        Reason = "Tenant account is suspended. Please contact support.",
                        TenantSuspended = true
                    };
                }

                // Check if tenant has active paid subscription
                var subscription = await _subscriptionService.GetTenantSubscriptionAsync(tenantId);
                if (subscription != null && subscription.EndDate > DateTime.UtcNow)
                {
                    // Get subscription plan details
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

                // Check if tenant is in 14-day trial period
                var trialEndDate = tenant.CreatedAt.AddDays(14);
                var isInTrial = DateTime.UtcNow <= trialEndDate;

                if (isInTrial)
                {
                    return new SubscriptionStatus
                    {
                        HasAccess = true,
                        IsTrial = true,
                        PlanType = "Trial",
                        TrialEndDate = trialEndDate,
                        MaxUsers = int.MaxValue, // Unlimited during trial
                        MaxBranches = int.MaxValue,
                        Reason = "Trial period active"
                    };
                }

                // No active subscription and trial expired
                return new SubscriptionStatus
                {
                    HasAccess = false,
                    IsTrial = false,
                    TrialExpired = true,
                    Reason = "Trial period expired and no active subscription"
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
    }

    public class SubscriptionStatus
    {
        public bool HasAccess { get; set; }
        public bool IsTrial { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public DateTime? TrialEndDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool TrialExpired { get; set; }
        public bool TenantSuspended { get; set; }
        public bool IsPaidSubscription { get; set; }
    }

    public class PlanLimits
    {
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }
    }
}
