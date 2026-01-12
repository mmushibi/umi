using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;

namespace UmiHealth.API.Controllers
{
    /// <summary>
    /// Super Admin controller for account management and monitoring
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class SuperAdminAccountController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<SuperAdminAccountController> _logger;

        public SuperAdminAccountController(
            SharedDbContext context,
            ILogger<SuperAdminAccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all recently registered accounts
        /// </summary>
        [HttpGet("recent-registrations")]
        public async Task<IActionResult> GetRecentRegistrations([FromQuery] int days = 7)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                
                var registrations = await _context.Tenants
                    .Include(t => t.Users)
                    .Include(t => t.Subscriptions)
                    .Include(t => t.Branches)
                    .Where(t => t.CreatedAt >= cutoffDate)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.Subdomain,
                        t.Status,
                        t.SubscriptionPlan,
                        CreatedAt = t.CreatedAt,
                        AdminUser = t.Users.FirstOrDefault(u => u.Role == "admin"),
                        Subscription = t.Subscriptions.FirstOrDefault(),
                        BranchCount = t.Branches.Count,
                        UserCount = t.Users.Count,
                        OnboardingStatus = GetOnboardingStatus(t)
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = registrations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent registrations");
                return StatusCode(500, new { success = false, message = "Error retrieving recent registrations" });
            }
        }

        /// <summary>
        /// Get account details with full information
        /// </summary>
        [HttpGet("account-details/{tenantId}")]
        public async Task<IActionResult> GetAccountDetails(Guid tenantId)
        {
            try
            {
                var tenant = await _context.Tenants
                    .Include(t => t.Users)
                    .Include(t => t.Subscriptions)
                    .Include(t => t.Branches)
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenant == null)
                {
                    return NotFound(new { success = false, message = "Account not found" });
                }

                var accountDetails = new
                {
                    tenant.Id,
                    tenant.Name,
                    tenant.Subdomain,
                    tenant.Status,
                    tenant.SubscriptionPlan,
                    tenant.Settings,
                    tenant.CreatedAt,
                    tenant.UpdatedAt,
                    Users = tenant.Users.Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.PhoneNumber,
                        u.Role,
                        u.IsActive,
                        u.IsEmailVerified,
                        u.CreatedAt,
                        u.LastLogin
                    }),
                    Subscriptions = tenant.Subscriptions.Select(s => new
                    {
                        s.Id,
                        s.PlanType,
                        s.Status,
                        s.BillingCycle,
                        s.Currency,
                        s.StartDate,
                        s.EndDate,
                        s.CreatedAt,
                        Features = s.Features ?? new Dictionary<string, object>(),
                        Limits = s.Limits ?? new Dictionary<string, object>()
                    }),
                    Branches = tenant.Branches.Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.Code,
                        b.Phone,
                        b.Email,
                        b.Address,
                        b.IsMainBranch,
                        b.IsActive,
                        b.LicenseNumber,
                        b.CreatedAt
                    }),
                    OnboardingStatus = GetOnboardingStatus(tenant),
                    SecurityMetrics = await GetSecurityMetrics(tenantId),
                    ActivityMetrics = await GetActivityMetrics(tenantId)
                };

                return Ok(new { success = true, data = accountDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account details for tenant {TenantId}", tenantId);
                return StatusCode(500, new { success = false, message = "Error retrieving account details" });
            }
        }

        /// <summary>
        /// Get accounts requiring attention (suspended, expired trials, etc.)
        /// </summary>
        [HttpGet("accounts-requiring-attention")]
        public async Task<IActionResult> GetAccountsRequiringAttention()
        {
            try
            {
                var now = DateTime.UtcNow;
                
                var accountsNeedingAttention = await _context.Tenants
                    .Include(t => t.Users)
                    .Include(t => t.Subscriptions)
                    .Where(t => 
                        t.Status == "suspended" ||
                        (t.SubscriptionPlan == "trial" && t.CreatedAt.AddDays(14) < now) ||
                        (t.Subscriptions.Any(s => s.EndDate < now && s.Status == "active"))
                    )
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.Subdomain,
                        t.Status,
                        t.SubscriptionPlan,
                        t.CreatedAt,
                        Issue = GetAccountIssue(t, now),
                        Urgency = GetUrgencyLevel(t, now),
                        AdminUser = t.Users.FirstOrDefault(u => u.Role == "admin"),
                        Subscription = t.Subscriptions.FirstOrDefault()
                    })
                    .OrderByDescending(t => t.Urgency)
                    .ThenBy(t => t.CreatedAt)
                    .ToListAsync();

                return Ok(new { success = true, data = accountsNeedingAttention });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accounts requiring attention");
                return StatusCode(500, new { success = false, message = "Error retrieving accounts requiring attention" });
            }
        }

        /// <summary>
        /// Get system-wide account statistics
        /// </summary>
        [HttpGet("account-statistics")]
        public async Task<IActionResult> GetAccountStatistics()
        {
            try
            {
                var now = DateTime.UtcNow;
                var last30Days = now.AddDays(-30);
                var last7Days = now.AddDays(-7);

                var tenants = await _context.Tenants
                    .Include(t => t.Users)
                    .Include(t => t.Subscriptions)
                    .ToListAsync();

                var statistics = new
                {
                    TotalAccounts = tenants.Count,
                    ActiveAccounts = tenants.Count(t => t.Status == "active"),
                    SuspendedAccounts = tenants.Count(t => t.Status == "suspended"),
                    TrialAccounts = tenants.Count(t => t.SubscriptionPlan == "trial"),
                    PaidAccounts = tenants.Count(t => t.SubscriptionPlan != "trial"),
                    ExpiredTrials = tenants.Count(t => 
                        t.SubscriptionPlan == "trial" && t.CreatedAt.AddDays(14) < now),
                    NewAccountsThisMonth = tenants.Count(t => t.CreatedAt >= last30Days),
                    NewAccountsThisWeek = tenants.Count(t => t.CreatedAt >= last7Days),
                    TotalUsers = tenants.Sum(t => t.Users.Count),
                    ActiveUsers = tenants.Sum(t => t.Users.Count(u => u.IsActive)),
                    SubscriptionBreakdown = tenants
                        .GroupBy(t => t.SubscriptionPlan)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    OnboardingCompletion = new
                    {
                        Completed = tenants.Count(t => IsOnboardingComplete(t)),
                        Pending = tenants.Count(t => !IsOnboardingComplete(t)),
                        Percentage = tenants.Count > 0 
                            ? Math.Round((double)tenants.Count(t => IsOnboardingComplete(t)) / tenants.Count * 100, 2)
                            : 0
                    }
                };

                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account statistics");
                return StatusCode(500, new { success = false, message = "Error retrieving account statistics" });
            }
        }

        /// <summary>
        /// Get account activity timeline
        /// </summary>
        [HttpGet("account-timeline/{tenantId}")]
        public async Task<IActionResult> GetAccountTimeline(Guid tenantId, [FromQuery] int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                
                var timeline = new List<object>();

                // Get user registrations
                var users = await _context.Users
                    .Where(u => u.TenantId == tenantId && u.CreatedAt >= cutoffDate)
                    .OrderBy(u => u.CreatedAt)
                    .Select(u => new
                    {
                        Type = "User Registration",
                        Timestamp = u.CreatedAt,
                        Description = $"User {u.FirstName} {u.LastName} ({u.Email}) registered as {u.Role}",
                        Details = new { u.Id, u.FirstName, u.LastName, u.Email, u.Role }
                    })
                    .ToListAsync();

                timeline.AddRange(users);

                // Get subscription changes
                var subscriptions = await _context.Subscriptions
                    .Where(s => s.TenantId == tenantId && s.CreatedAt >= cutoffDate)
                    .OrderBy(s => s.CreatedAt)
                    .Select(s => new
                    {
                        Type = "Subscription Change",
                        Timestamp = s.CreatedAt,
                        Description = $"Subscription {s.PlanType} {s.Status}",
                        Details = new { s.Id, s.PlanType, s.Status, s.StartDate, s.EndDate }
                    })
                    .ToListAsync();

                timeline.AddRange(subscriptions);

                // Get security events
                var securityEvents = await _context.SecurityEvents
                    .Where(e => e.TenantId == tenantId && e.CreatedAt >= cutoffDate)
                    .OrderBy(e => e.CreatedAt)
                    .Select(e => new
                    {
                        Type = "Security Event",
                        Timestamp = e.CreatedAt,
                        Description = $"{e.EventType}: {e.Description}",
                        Details = new { e.EventType, e.Description, e.IpAddress, e.RiskLevel }
                    })
                    .ToListAsync();

                timeline.AddRange(securityEvents);

                // Sort by timestamp
                var sortedTimeline = timeline
                    .OrderByDescending(t => ((dynamic)t).Timestamp)
                    .ToList();

                return Ok(new { success = true, data = sortedTimeline });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account timeline for tenant {TenantId}", tenantId);
                return StatusCode(500, new { success = false, message = "Error retrieving account timeline" });
            }
        }

        private object GetOnboardingStatus(Tenant tenant)
        {
            var settings = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(tenant.Settings))
            {
                try
                {
                    settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tenant.Settings);
                }
                catch
                {
                    // Use empty settings if deserialization fails
                }
            }

            return new
            {
                IsCompleted = settings.ContainsKey("onboardingCompleted") && 
                              bool.TryParse(settings["onboardingCompleted"].ToString(), out var completed) && completed,
                CompletedAt = settings.ContainsKey("onboardingCompletedAt") 
                    ? settings["onboardingCompletedAt"] 
                    : null,
                PharmacyType = settings.ContainsKey("pharmacyType") ? settings["pharmacyType"] : null,
                HasLicense = settings.ContainsKey("zamraNumber") && !string.IsNullOrEmpty(settings["zamraNumber"].ToString()),
                HasAddress = settings.ContainsKey("physicalAddress") && !string.IsNullOrEmpty(settings["physicalAddress"].ToString()),
                HasServices = settings.ContainsKey("services") && settings["services"] != null,
                CompletionPercentage = CalculateOnboardingPercentage(settings)
            };
        }

        private bool IsOnboardingComplete(Tenant tenant)
        {
            var status = GetOnboardingStatus(tenant);
            return ((dynamic)status).IsCompleted;
        }

        private double CalculateOnboardingPercentage(Dictionary<string, object> settings)
        {
            var requiredFields = new[]
            {
                "pharmacyType", "zamraNumber", "physicalAddress", 
                "city", "province", "services"
            };

            var completedFields = requiredFields.Count(field => 
                settings.ContainsKey(field) && 
                !string.IsNullOrEmpty(settings[field]?.ToString())
            );

            return Math.Round((double)completedFields / requiredFields.Length * 100, 2);
        }

        private string GetAccountIssue(Tenant tenant, DateTime now)
        {
            if (tenant.Status == "suspended")
                return "Account Suspended";
            
            if (tenant.SubscriptionPlan == "trial" && tenant.CreatedAt.AddDays(14) < now)
                return "Trial Expired";
            
            var activeSubscription = tenant.Subscriptions?.FirstOrDefault(s => s.Status == "active");
            if (activeSubscription != null && activeSubscription.EndDate < now)
                return "Subscription Expired";
            
            return "Attention Required";
        }

        private int GetUrgencyLevel(Tenant tenant, DateTime now)
        {
            if (tenant.Status == "suspended")
                return 4; // Critical
            
            if (tenant.SubscriptionPlan == "trial" && tenant.CreatedAt.AddDays(14) < now)
                return 3; // High
            
            var activeSubscription = tenant.Subscriptions?.FirstOrDefault(s => s.Status == "active");
            if (activeSubscription != null && activeSubscription.EndDate < now)
                return 3; // High
            
            return 1; // Low
        }

        private async Task<object> GetSecurityMetrics(Guid tenantId)
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            
            var securityEvents = await _context.SecurityEvents
                .Where(e => e.TenantId == tenantId && e.CreatedAt >= last24Hours)
                .ToListAsync();

            return new
            {
                EventsLast24Hours = securityEvents.Count,
                FailedLogins = securityEvents.Count(e => e.EventType == "LoginFailure"),
                SuccessfulLogins = securityEvents.Count(e => e.EventType == "LoginSuccess"),
                HighRiskEvents = securityEvents.Count(e => e.RiskLevel >= 3),
                UniqueIpAddresses = securityEvents.Select(e => e.IpAddress).Distinct().Count()
            };
        }

        private async Task<object> GetActivityMetrics(Guid tenantId)
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);
            
            var users = await _context.Users
                .Where(u => u.TenantId == tenantId)
                .ToListAsync();

            return new
            {
                TotalUsers = users.Count,
                ActiveUsers = users.Count(u => u.IsActive),
                UsersWithRecentLogin = users.Count(u => u.LastLogin.HasValue && u.LastLogin >= last30Days),
                AdminUsers = users.Count(u => u.Role == "admin"),
                CashierUsers = users.Count(u => u.Role == "cashier"),
                PharmacistUsers = users.Count(u => u.Role == "pharmacist"),
                OperationsUsers = users.Count(u => u.Role == "operations")
            };
        }
    }
}
