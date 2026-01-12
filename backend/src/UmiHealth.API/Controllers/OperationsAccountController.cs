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
    /// Operations controller for account management and monitoring
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "admin,operations")]
    public class OperationsAccountController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<OperationsAccountController> _logger;

        public OperationsAccountController(
            SharedDbContext context,
            ILogger<OperationsAccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get tenant account details (for operations users)
        /// </summary>
        [HttpGet("tenant-details")]
        public async Task<IActionResult> GetTenantDetails()
        {
            try
            {
                var tenantId = GetTenantIdFromClaims();
                if (tenantId == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "Tenant ID not found" });
                }

                var tenant = await _context.Tenants
                    .Include(t => t.Users)
                    .Include(t => t.Subscriptions)
                    .Include(t => t.Branches)
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenant == null)
                {
                    return NotFound(new { success = false, message = "Tenant not found" });
                }

                var tenantDetails = new
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
                    UserStatistics = GetUserStatistics(tenant.Users),
                    SubscriptionStatus = GetSubscriptionStatus(tenant)
                };

                return Ok(new { success = true, data = tenantDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant details");
                return StatusCode(500, new { success = false, message = "Error retrieving tenant details" });
            }
        }

        /// <summary>
        /// Get user management data
        /// </summary>
        [HttpGet("user-management")]
        public async Task<IActionResult> GetUserManagement()
        {
            try
            {
                var tenantId = GetTenantIdFromClaims();
                if (tenantId == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "Tenant ID not found" });
                }

                var users = await _context.Users
                    .Include(u => u.Branch)
                    .Where(u => u.TenantId == tenantId)
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new
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
                        u.LastLogin,
                        Branch = u.Branch != null ? new
                        {
                            u.Branch.Id,
                            u.Branch.Name,
                            u.Branch.Code
                        } : null,
                        Status = GetUserStatus(u),
                        Permissions = GetUserPermissions(u.Role)
                    })
                    .ToListAsync();

                var userManagement = new
                {
                    Users = users,
                    Statistics = new
                    {
                        TotalUsers = users.Count,
                        ActiveUsers = users.Count(u => u.IsActive),
                        InactiveUsers = users.Count(u => !u.IsActive),
                        AdminUsers = users.Count(u => u.Role == "admin"),
                        CashierUsers = users.Count(u => u.Role == "cashier"),
                        PharmacistUsers = users.Count(u => u.Role == "pharmacist"),
                        OperationsUsers = users.Count(u => u.Role == "operations"),
                        UsersWithRecentLogin = users.Count(u => u.LastLogin.HasValue && u.LastLogin >= DateTime.UtcNow.AddDays(-7)),
                        UnverifiedUsers = users.Count(u => !u.IsEmailVerified)
                    },
                    RoleDistribution = users
                        .GroupBy(u => u.Role)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    BranchDistribution = users
                        .Where(u => u.Branch != null)
                        .GroupBy(u => u.Branch.Name)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(new { success = true, data = userManagement });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user management data");
                return StatusCode(500, new { success = false, message = "Error retrieving user management data" });
            }
        }

        /// <summary>
        /// Get subscription and billing information
        /// </summary>
        [HttpGet("subscription-management")]
        public async Task<IActionResult> GetSubscriptionManagement()
        {
            try
            {
                var tenantId = GetTenantIdFromClaims();
                if (tenantId == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "Tenant ID not found" });
                }

                var tenant = await _context.Tenants
                    .Include(t => t.Subscriptions)
                    .Include(t => t.SubscriptionTransactions)
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenant == null)
                {
                    return NotFound(new { success = false, message = "Tenant not found" });
                }

                var currentSubscription = tenant.Subscriptions
                    .Where(s => s.Status == "active")
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefault();

                var subscriptionManagement = new
                {
                    CurrentSubscription = currentSubscription != null ? new
                    {
                        currentSubscription.Id,
                        currentSubscription.PlanType,
                        currentSubscription.Status,
                        currentSubscription.BillingCycle,
                        currentSubscription.Currency,
                        currentSubscription.StartDate,
                        currentSubscription.EndDate,
                        DaysRemaining = currentSubscription.EndDate.HasValue 
                            ? Math.Max(0, (currentSubscription.EndDate.Value - DateTime.UtcNow).Days)
                            : 0,
                        Features = currentSubscription.Features ?? new Dictionary<string, object>(),
                        Limits = currentSubscription.Limits ?? new Dictionary<string, object>()
                    } : null,
                    SubscriptionHistory = tenant.Subscriptions
                        .OrderByDescending(s => s.CreatedAt)
                        .Select(s => new
                        {
                            s.Id,
                            s.PlanType,
                            s.Status,
                            s.BillingCycle,
                            s.StartDate,
                            s.EndDate,
                            s.CreatedAt
                        }),
                    Transactions = tenant.SubscriptionTransactions
                        .OrderByDescending(t => t.CreatedAt)
                        .Select(t => new
                        {
                            t.Id,
                            t.TransactionId,
                            t.Type,
                            t.Amount,
                            t.Currency,
                            t.Status,
                            t.PlanFrom,
                            t.PlanTo,
                            t.CreatedAt,
                            t.ApprovedAt
                        }),
                    TrialStatus = new
                    {
                        IsTrial = tenant.SubscriptionPlan == "trial",
                        TrialStartDate = tenant.CreatedAt,
                        TrialEndDate = tenant.CreatedAt.AddDays(14),
                        TrialDaysRemaining = Math.Max(0, (tenant.CreatedAt.AddDays(14) - DateTime.UtcNow).Days),
                        TrialExpired = tenant.CreatedAt.AddDays(14) < DateTime.UtcNow
                    },
                    AvailablePlans = await GetAvailableSubscriptionPlans()
                };

                return Ok(new { success = true, data = subscriptionManagement });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription management data");
                return StatusCode(500, new { success = false, message = "Error retrieving subscription management data" });
            }
        }

        /// <summary>
        /// Get branch management data
        /// </summary>
        [HttpGet("branch-management")]
        public async Task<IActionResult> GetBranchManagement()
        {
            try
            {
                var tenantId = GetTenantIdFromClaims();
                if (tenantId == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "Tenant ID not found" });
                }

                var branches = await _context.Branches
                    .Include(b => b.Users)
                    .Where(b => b.TenantId == tenantId)
                    .OrderBy(b => b.Name)
                    .Select(b => new
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
                        b.OperatingHours,
                        b.Settings,
                        b.CreatedAt,
                        UserCount = b.Users.Count,
                        ActiveUserCount = b.Users.Count(u => u.IsActive),
                        Status = GetBranchStatus(b)
                    })
                    .ToListAsync();

                var branchManagement = new
                {
                    Branches = branches,
                    Statistics = new
                    {
                        TotalBranches = branches.Count,
                        ActiveBranches = branches.Count(b => b.IsActive),
                        InactiveBranches = branches.Count(b => !b.IsActive),
                        MainBranches = branches.Count(b => b.IsMainBranch),
                        TotalUsers = branches.Sum(b => b.UserCount),
                        TotalActiveUsers = branches.Sum(b => b.ActiveUserCount)
                    }
                };

                return Ok(new { success = true, data = branchManagement });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch management data");
                return StatusCode(500, new { success = false, message = "Error retrieving branch management data" });
            }
        }

        /// <summary>
        /// Get account activity and audit logs
        /// </summary>
        [HttpGet("account-activity")]
        public async Task<IActionResult> GetAccountActivity([FromQuery] int days = 30)
        {
            try
            {
                var tenantId = GetTenantIdFromClaims();
                if (tenantId == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "Tenant ID not found" });
                }

                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                
                var activities = new List<object>();

                // Get user activities
                var userActivities = await _context.Users
                    .Where(u => u.TenantId == tenantId && u.CreatedAt >= cutoffDate)
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new
                    {
                        Type = "User Activity",
                        Timestamp = u.CreatedAt,
                        Description = $"User {u.FirstName} {u.LastName} ({u.Email}) registered as {u.Role}",
                        Details = new { u.Id, u.FirstName, u.LastName, u.Email, u.Role },
                        Category = "User Management"
                    })
                    .ToListAsync();

                activities.AddRange(userActivities);

                // Get security events
                var securityEvents = await _context.SecurityEvents
                    .Where(e => e.TenantId == tenantId && e.CreatedAt >= cutoffDate)
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => new
                    {
                        Type = "Security Event",
                        Timestamp = e.CreatedAt,
                        Description = $"{e.EventType}: {e.Description}",
                        Details = new { e.EventType, e.Description, e.IpAddress, e.RiskLevel },
                        Category = "Security"
                    })
                    .ToListAsync();

                activities.AddRange(securityEvents);

                // Sort by timestamp
                var sortedActivities = activities
                    .OrderByDescending(a => ((dynamic)a).Timestamp)
                    .ToList();

                var accountActivity = new
                {
                    Activities = sortedActivities,
                    Summary = new
                    {
                        TotalActivities = sortedActivities.Count,
                        UserActivities = userActivities.Count,
                        SecurityEvents = securityEvents.Count,
                        HighRiskEvents = securityEvents.Count(e => ((dynamic)e).Details.RiskLevel >= 3),
                        DateRange = new
                        {
                            Start = cutoffDate,
                            End = DateTime.UtcNow
                        }
                    }
                };

                return Ok(new { success = true, data = accountActivity });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account activity");
                return StatusCode(500, new { success = false, message = "Error retrieving account activity" });
            }
        }

        private Guid GetTenantIdFromClaims()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : Guid.Empty;
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

        private object GetUserStatistics(IEnumerable<User> users)
        {
            var userList = users.ToList();
            return new
            {
                Total = userList.Count,
                Active = userList.Count(u => u.IsActive),
                Inactive = userList.Count(u => !u.IsActive),
                Admin = userList.Count(u => u.Role == "admin"),
                Cashier = userList.Count(u => u.Role == "cashier"),
                Pharmacist = userList.Count(u => u.Role == "pharmacist"),
                Operations = userList.Count(u => u.Role == "operations"),
                EmailVerified = userList.Count(u => u.IsEmailVerified),
                RecentLogins = userList.Count(u => u.LastLogin.HasValue && u.LastLogin >= DateTime.UtcNow.AddDays(-7))
            };
        }

        private object GetSubscriptionStatus(Tenant tenant)
        {
            var now = DateTime.UtcNow;
            var isTrialExpired = tenant.SubscriptionPlan == "trial" && tenant.CreatedAt.AddDays(14) < now;
            
            return new
            {
                CurrentPlan = tenant.SubscriptionPlan,
                Status = tenant.Status,
                IsTrial = tenant.SubscriptionPlan == "trial",
                TrialEndDate = tenant.CreatedAt.AddDays(14),
                TrialDaysRemaining = Math.Max(0, (tenant.CreatedAt.AddDays(14) - now).Days),
                IsTrialExpired = isTrialExpired,
                RequiresAttention = isTrialExpired || tenant.Status == "suspended"
            };
        }

        private string GetUserStatus(User user)
        {
            if (!user.IsActive)
                return "Inactive";
            
            if (!user.IsEmailVerified)
                return "Pending Verification";
            
            if (user.LastLogin.HasValue && user.LastLogin >= DateTime.UtcNow.AddDays(-7))
                return "Active";
            
            return "Inactive";
        }

        private List<string> GetUserPermissions(string role)
        {
            return role.ToLower() switch
            {
                "admin" => new List<string> { "user_management", "branch_management", "subscription_management", "reports", "settings" },
                "cashier" => new List<string> { "sales", "payments", "customer_management" },
                "pharmacist" => new List<string> { "prescriptions", "inventory", "patient_management" },
                "operations" => new List<string> { "user_management", "branch_management", "reports", "audit_logs" },
                _ => new List<string>()
            };
        }

        private string GetBranchStatus(Branch branch)
        {
            if (!branch.IsActive)
                return "Inactive";
            
            if (branch.IsMainBranch)
                return "Main Branch";
            
            return "Active";
        }

        private async Task<List<object>> GetAvailableSubscriptionPlans()
        {
            // This would typically come from a database table
            // For now, return hardcoded plans
            return new List<object>
            {
                new
                {
                    Name = "Basic",
                    Price = 299,
                    Currency = "ZMW",
                    Features = new { Users = 5, Branches = 1, Storage = "1GB", Support = "Email" }
                },
                new
                {
                    Name = "Professional",
                    Price = 599,
                    Currency = "ZMW",
                    Features = new { Users = 20, Branches = 3, Storage = "5GB", Support = "Priority" }
                },
                new
                {
                    Name = "Enterprise",
                    Price = 1299,
                    Currency = "ZMW",
                    Features = new { Users = "Unlimited", Branches = "Unlimited", Storage = "50GB", Support = "24/7" }
                }
            };
        }
    }
}
