using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ITenantService _tenantService;
        private readonly ISubscriptionService _subscriptionService;

        public AccountController(
            IAuthenticationService authService,
            ITenantService tenantService,
            ISubscriptionService subscriptionService)
        {
            _authService = authService;
            _tenantService = tenantService;
            _subscriptionService = subscriptionService;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                var tenant = await _tenantService.GetByIdAsync(user.TenantId);
                var subscription = await _subscriptionService.GetActiveSubscriptionAsync(user.TenantId);

                return Ok(new UserProfileDto
                {
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Username = user.Username ?? user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = user.Role,
                        TenantId = user.TenantId,
                        BranchId = user.BranchId,
                        BranchAccess = user.BranchAccess,
                        Permissions = user.Permissions,
                        IsActive = user.IsActive,
                        EmailVerified = user.EmailVerified,
                        PhoneVerified = user.PhoneVerified,
                        TwoFactorEnabled = user.TwoFactorEnabled,
                        LastLogin = user.LastLogin
                    },
                    Tenant = tenant != null ? new TenantDto
                    {
                        Id = tenant.Id,
                        Name = tenant.Name,
                        Subdomain = tenant.Subdomain,
                        DatabaseName = tenant.DatabaseName,
                        Status = tenant.Status,
                        SubscriptionPlan = tenant.SubscriptionPlan,
                        MaxBranches = tenant.MaxBranches,
                        MaxUsers = tenant.MaxUsers,
                        Settings = tenant.Settings,
                        CreatedAt = tenant.CreatedAt
                    } : null,
                    Subscription = subscription != null ? new SubscriptionDto
                    {
                        Id = subscription.Id,
                        PlanType = subscription.PlanType,
                        Status = subscription.Status,
                        BillingCycle = subscription.BillingCycle,
                        Amount = subscription.Amount,
                        Currency = subscription.Currency,
                        Features = subscription.Features,
                        Limits = subscription.Limits,
                        StartDate = subscription.StartDate,
                        EndDate = subscription.EndDate,
                        AutoRenew = subscription.AutoRenew,
                        DaysRemaining = subscription.EndDate.HasValue ? 
                            (int)(subscription.EndDate.Value - DateTime.UtcNow).TotalDays : 0,
                        IsTrial = subscription.PlanType == "trial",
                        RequiresUpgrade = subscription.EndDate.HasValue && 
                                       subscription.EndDate.Value <= DateTime.UtcNow.AddDays(7) &&
                                       subscription.PlanType == "trial"
                    } : null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get user profile." });
            }
        }

        [HttpPut("profile")]
        public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                // Update user fields
                user.FirstName = request.FirstName ?? user.FirstName;
                user.LastName = request.LastName ?? user.LastName;
                user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
                user.UpdatedAt = DateTime.UtcNow;

                // Update password if provided
                if (!string.IsNullOrEmpty(request.CurrentPassword) && !string.IsNullOrEmpty(request.NewPassword))
                {
                    if (BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = "Current password is incorrect" });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Username = user.Username ?? user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    TenantId = user.TenantId,
                    BranchId = user.BranchId,
                    BranchAccess = user.BranchAccess,
                    Permissions = user.Permissions,
                    IsActive = user.IsActive,
                    EmailVerified = user.EmailVerified,
                    PhoneVerified = user.PhoneVerified,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LastLogin = user.LastLogin
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update profile." });
            }
        }

        [HttpGet("tenant-settings")]
        public async Task<ActionResult<TenantSettingsDto>> GetTenantSettings()
        {
            try
            {
                var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantIdClaim))
                {
                    return BadRequest("Tenant information not found");
                }

                var tenantId = Guid.Parse(tenantIdClaim);
                var tenant = await _tenantService.GetByIdAsync(tenantId);
                
                if (tenant == null)
                {
                    return NotFound();
                }

                return Ok(new TenantSettingsDto
                {
                    TenantId = tenant.Id,
                    PharmacyName = tenant.Name,
                    Address = tenant.Settings.GetValueOrDefault("address", "").ToString(),
                    Phone = tenant.Settings.GetValueOrDefault("phone", "").ToString(),
                    Email = tenant.Settings.GetValueOrDefault("email", "").ToString(),
                    LicenseNumber = tenant.Settings.GetValueOrDefault("license_number", "").ToString(),
                    OperatingHours = tenant.Settings.GetValueOrDefault("operating_hours", new { }) as Dictionary<string, object> ?? new(),
                    TaxSettings = tenant.Settings.GetValueOrDefault("tax_settings", new { }) as Dictionary<string, object> ?? new(),
                    ReceiptSettings = tenant.Settings.GetValueOrDefault("receipt_settings", new { }) as Dictionary<string, object> ?? new(),
                    ComplianceSettings = tenant.ComplianceSettings,
                    SetupCompleted = tenant.Settings.ContainsKey("setup_completed")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get tenant settings." });
            }
        }

        [HttpPut("tenant-settings")]
        public async Task<ActionResult> UpdateTenantSettings([FromBody] UpdateTenantSettingsRequest request)
        {
            try
            {
                var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantIdClaim))
                {
                    return BadRequest("Tenant information not found");
                }

                var tenantId = Guid.Parse(tenantIdClaim);
                var tenant = await _tenantService.GetByIdAsync(tenantId);
                
                if (tenant == null)
                {
                    return NotFound();
                }

                // Update tenant settings
                tenant.Settings["address"] = request.Address;
                tenant.Settings["phone"] = request.Phone;
                tenant.Settings["email"] = request.Email;
                tenant.Settings["license_number"] = request.LicenseNumber;
                tenant.Settings["operating_hours"] = request.OperatingHours;
                tenant.Settings["tax_settings"] = request.TaxSettings;
                tenant.Settings["receipt_settings"] = request.ReceiptSettings;
                
                // Mark setup as completed if this is the first setup
                if (!tenant.Settings.ContainsKey("setup_completed"))
                {
                    tenant.Settings["setup_completed"] = true;
                    tenant.Settings["setup_completed_at"] = DateTime.UtcNow.ToString("O");
                }

                tenant.UpdatedAt = DateTime.UtcNow;
                await _tenantService.UpdateAsync(tenantId, tenant);

                return Ok(new { success = true, message = "Tenant settings updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update tenant settings." });
            }
        }

        [HttpGet("subscription-plans")]
        public async Task<ActionResult<IEnumerable<SubscriptionPlanDto>>> GetSubscriptionPlans()
        {
            try
            {
                var plans = new List<SubscriptionPlanDto>
                {
                    new SubscriptionPlanDto
                    {
                        Name = "Basic",
                        Description = "Perfect for small pharmacies",
                        Price = 299,
                        Currency = "ZMW",
                        BillingCycle = "monthly",
                        Features = new Dictionary<string, bool>
                        {
                            { "Inventory Management", true },
                            { "Prescription Management", true },
                            { "POS Functionality", true },
                            { "Basic Reports", true },
                            { "Email Support", true },
                            { "1 Branch", true },
                            { "5 Users", true }
                        },
                        Limits = new Dictionary<string, string>
                        {
                            { "Max Branches", "1" },
                            { "Max Users", "5" },
                            { "Monthly Transactions", "1,000" },
                            { "Storage", "10 GB" }
                        }
                    },
                    new SubscriptionPlanDto
                    {
                        Name = "Professional",
                        Description = "Great for growing pharmacies",
                        Price = 599,
                        Currency = "ZMW",
                        BillingCycle = "monthly",
                        Features = new Dictionary<string, bool>
                        {
                            { "Inventory Management", true },
                            { "Prescription Management", true },
                            { "POS Functionality", true },
                            { "Advanced Reports", true },
                            { "API Access", true },
                            { "Email Support", true },
                            { "Phone Support", true },
                            { "3 Branches", true },
                            { "15 Users", true }
                        },
                        Limits = new Dictionary<string, string>
                        {
                            { "Max Branches", "3" },
                            { "Max Users", "15" },
                            { "Monthly Transactions", "5,000" },
                            { "Storage", "50 GB" }
                        }
                    },
                    new SubscriptionPlanDto
                    {
                        Name = "Enterprise",
                        Description = "For large pharmacy chains",
                        Price = 1299,
                        Currency = "ZMW",
                        BillingCycle = "monthly",
                        Features = new Dictionary<string, bool>
                        {
                            { "Inventory Management", true },
                            { "Prescription Management", true },
                            { "POS Functionality", true },
                            { "Advanced Reports", true },
                            { "Custom Reports", true },
                            { "API Access", true },
                            { "Webhooks", true },
                            { "Priority Support", true },
                            { "Dedicated Account Manager", true },
                            { "Unlimited Branches", true },
                            { "Unlimited Users", true }
                        },
                        Limits = new Dictionary<string, string>
                        {
                            { "Max Branches", "Unlimited" },
                            { "Max Users", "Unlimited" },
                            { "Monthly Transactions", "Unlimited" },
                            { "Storage", "Unlimited" }
                        }
                    }
                };

                return Ok(plans);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get subscription plans." });
            }
        }

        [HttpPost("upgrade-subscription")]
        public async Task<ActionResult<SubscriptionDto>> UpgradeSubscription([FromBody] UpgradeSubscriptionRequest request)
        {
            try
            {
                var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantIdClaim))
                {
                    return BadRequest("Tenant information not found");
                }

                var tenantId = Guid.Parse(tenantIdClaim);
                var createRequest = new CreateSubscriptionRequest
                {
                    PlanType = request.PlanType,
                    BillingCycle = request.BillingCycle,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    AutoRenew = request.AutoRenew
                };

                var subscription = await _subscriptionService.CreateSubscriptionAsync(tenantId, createRequest);

                return Ok(new SubscriptionDto
                {
                    Id = subscription.Id,
                    PlanType = subscription.PlanType,
                    Status = subscription.Status,
                    BillingCycle = subscription.BillingCycle,
                    Amount = subscription.Amount,
                    Currency = subscription.Currency,
                    Features = subscription.Features,
                    Limits = subscription.Limits,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    AutoRenew = subscription.AutoRenew,
                    DaysRemaining = subscription.EndDate.HasValue ? 
                        (int)(subscription.EndDate.Value - DateTime.UtcNow).TotalDays : 0,
                    IsTrial = subscription.PlanType == "trial",
                    RequiresUpgrade = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to upgrade subscription." });
            }
        }
    }

    // DTOs
    public class UserProfileDto
    {
        public UserDto User { get; set; } = null!;
        public TenantDto? Tenant { get; set; }
        public SubscriptionDto? Subscription { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }

    public class TenantSettingsDto
    {
        public Guid TenantId { get; set; }
        public string PharmacyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public Dictionary<string, object> OperatingHours { get; set; } = new();
        public Dictionary<string, object> TaxSettings { get; set; } = new();
        public Dictionary<string, object> ReceiptSettings { get; set; } = new();
        public Dictionary<string, object> ComplianceSettings { get; set; } = new();
        public bool SetupCompleted { get; set; }
    }

    public class UpdateTenantSettingsRequest
    {
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public Dictionary<string, object> OperatingHours { get; set; } = new();
        public Dictionary<string, object> TaxSettings { get; set; } = new();
        public Dictionary<string, object> ReceiptSettings { get; set; } = new();
    }

    public class SubscriptionPlanDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public Dictionary<string, bool> Features { get; set; } = new();
        public Dictionary<string, string> Limits { get; set; } = new();
    }

    public class UpgradeSubscriptionRequest
    {
        public string PlanType { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = "monthly";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public bool AutoRenew { get; set; } = true;
    }
}
