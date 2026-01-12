using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UmiHealth.Application.DTOs;
using UmiHealth.Application.Services;
using UmiHealth.Core.Interfaces;
using UmiHealth.Infrastructure.Data;
using UmiHealth.API.Middleware;
using System;
using System.Threading.Tasks;
using System.Linq;
namespace UmiHealth.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IUserService _userService;
        private readonly IReportsService _reportsService;
        private readonly ITenantService _tenantService;
        private readonly IUserInvitationService _userInvitationService;

        public AdminController(
            ILogger<AdminController> logger,
            IUserService userService,
            IReportsService reportsService,
            ITenantService tenantService,
            IUserInvitationService userInvitationService)
        {
            _logger = logger;
            _userService = userService;
            _reportsService = reportsService;
            _tenantService = tenantService;
            _userInvitationService = userInvitationService;
        }
[HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] Guid tenantId, [FromQuery] Guid? branchId = null)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // Real service call for dashboard analytics
                var dashboardAnalytics = await _reportsService.GetDashboardAnalyticsAsync(tenantId, branchId, 30);
                
                var dashboardStats = new
                {
                    totalPatients = dashboardAnalytics.PatientMetrics?.TotalPatients ?? 0,
                    activePrescriptions = dashboardAnalytics.PrescriptionMetrics?.TotalPrescriptions ?? 0,
                    totalSales = dashboardAnalytics.SalesMetrics?.TotalRevenue ?? 0m,
                    lowStockItems = dashboardAnalytics.InventoryMetrics?.LowStockItems ?? 0,
                    newPatientsToday = dashboardAnalytics.PatientMetrics?.NewPatients ?? 0,
                    pendingApprovals = dashboardAnalytics.PrescriptionMetrics?.PendingPrescriptions ?? 0,
                    monthlyRevenue = dashboardAnalytics.SalesMetrics?.TotalRevenue ?? 0m,
                    totalBranches = 1, // This would need to be calculated from tenant branches
                    activeUsers = 0 // This would need to be calculated from user service
                };

                return Ok(dashboardStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard stats for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch dashboard stats" });
            }
        }

        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity([FromQuery] Guid tenantId, [FromQuery] Guid? branchId = null)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // Get recent activity from audit logs or database
                // TODO: Implement proper activity logging service
                var recentActivity = new[]
                {
                    new { id = 1, action = "System activity logging will be implemented", user = "System", time = "Just now", type = "info" }
                };

                return Ok(recentActivity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent activity for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch recent activity" });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] Guid tenantId, [FromQuery] int page = 1, [FromQuery] int limit = 50, [FromQuery] string? search = null, [FromQuery] string? role = null)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // Check subscription limits
                var subscriptionStatus = HttpContext.Items["SubscriptionStatus"] as SubscriptionStatus;
                if (subscriptionStatus != null && !subscriptionStatus.IsTrial)
                {
                    // Enforce user limit for paid plans
                    var currentUsers = await _userService.GetUsersCountAsync(tenantId);
                    if (currentUsers >= subscriptionStatus.MaxUsers)
                    {
                        return BadRequest(new { 
                            error = "User limit reached for your subscription plan",
                            maxUsers = subscriptionStatus.MaxUsers,
                            currentUsers = currentUsers,
                            requiresUpgrade = true
                        });
                    }
                }

                var users = await _userService.GetUsersAsync(tenantId, page, limit, search, role);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch users" });
            }
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromQuery] Guid tenantId, [FromBody] CreateUserRequest userRequest)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // Check subscription limits
                var subscriptionStatus = HttpContext.Items["SubscriptionStatus"] as SubscriptionStatus;
                if (subscriptionStatus != null && !subscriptionStatus.IsTrial)
                {
                    // Enforce user limit for paid plans
                    var currentUsers = await _userService.GetUsersCountAsync(tenantId);
                    if (currentUsers >= subscriptionStatus.MaxUsers)
                    {
                        return BadRequest(new { 
                            error = "User limit reached for your subscription plan",
                            maxUsers = subscriptionStatus.MaxUsers,
                            currentUsers = currentUsers,
                            requiresUpgrade = true,
                            upgradeUrl = "/api/v1/subscription/plans"
                        });
                    }
                }

                var user = await _userService.CreateUserAsync(tenantId, userRequest);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to create user" });
            }
        }

        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser([FromQuery] Guid tenantId, Guid userId, [FromBody] UpdateUserRequest userRequest)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var user = await _userService.UpdateUserAsync(userId, tenantId, userRequest);
                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId} for tenant {TenantId}", userId, tenantId);
                return StatusCode(500, new { error = "Failed to update user" });
            }
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser([FromQuery] Guid tenantId, Guid userId)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var result = await _userService.DeleteUserAsync(userId, tenantId);
                if (result)
                {
                    return Ok(new { message = "User deleted successfully" });
                }
                else
                {
                    return NotFound(new { error = "User not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId} for tenant {TenantId}", userId, tenantId);
                return StatusCode(500, new { error = "Failed to delete user" });
            }
        }

        // User invitation endpoints
        [HttpPost("invite-user")]
        public async Task<IActionResult> InviteUser([FromQuery] Guid tenantId, [FromBody] CreateUserRequest userRequest)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var currentUserId = GetUserIdFromClaims();
                var result = await _userInvitationService.SendUserInvitationAsync(tenantId, currentUserId, userRequest);
                
                if (result)
                {
                    return Ok(new { message = "Invitation sent successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to send invitation" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending user invitation for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to send invitation" });
            }
        }

        [HttpGet("validate-invitation")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateInvitation([FromQuery] string token)
        {
            try
            {
                var result = await _userInvitationService.ValidateInvitationTokenAsync(token);
                
                return Ok(new { 
                    valid = result,
                    message = result ? "Invitation is valid" : "Invalid or expired invitation" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating invitation token");
                return StatusCode(500, new { error = "Failed to validate invitation" });
            }
        }

        [HttpPost("accept-invitation")]
        [AllowAnonymous]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
        {
            try
            {
                var result = await _userInvitationService.AcceptInvitationAsync(request.Token, request.Password);
                
                if (result.Success)
                {
                    return Ok(new { 
                        success = true,
                        message = result.Message,
                        redirectUrl = result.RedirectUrl,
                        user = result.User
                    });
                }
                else
                {
                    return BadRequest(new { 
                        success = false, 
                        message = result.Message 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting invitation");
                return StatusCode(500, new { error = "Failed to accept invitation" });
            }
        }

        // Helper method
        private Guid GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }

    // Request DTOs
    public class AcceptInvitationRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

        [HttpGet("branches")]
        public async Task<IActionResult> GetBranches([FromQuery] Guid tenantId)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // Real service call for tenant branches
                var branches = await _tenantService.GetTenantBranchesAsync(tenantId);
                var branchDtos = branches.Select(b => new
                {
                    id = b.Id,
                    name = b.Name,
                    address = b.Address,
                    phone = b.Phone,
                    status = b.IsActive ? "active" : "inactive"
                });

                return Ok(branchDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branches for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch branches" });
            }
        }

        [HttpPost("branches")]
        public async Task<IActionResult> CreateBranch([FromQuery] Guid tenantId, [FromBody] CreateBranchRequest branchRequest)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // Check subscription branch limits
                var subscriptionStatus = HttpContext.Items["SubscriptionStatus"] as SubscriptionStatus;
                if (subscriptionStatus != null && !subscriptionStatus.IsTrial)
                {
                    // Enforce branch limit for paid plans
                    var currentBranches = await _tenantService.GetBranchCountAsync(tenantId);
                    if (currentBranches >= subscriptionStatus.MaxBranches)
                    {
                        return BadRequest(new { 
                            error = "Branch limit reached for your subscription plan",
                            maxBranches = subscriptionStatus.MaxBranches,
                            currentBranches = currentBranches,
                            requiresUpgrade = true,
                            upgradeUrl = "/api/v1/subscription/plans"
                        });
                    }
                }

                var branch = await _tenantService.CreateBranchAsync(tenantId, branchRequest);
                var branchDto = new
                {
                    id = branch.Id,
                    name = branch.Name,
                    address = branch.Address,
                    phone = branch.Phone,
                    status = branch.IsActive ? "active" : "inactive"
                };

                return Ok(branchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to create branch" });
            }
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory([FromQuery] Guid tenantId, [FromQuery] Guid? branchId = null, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // TODO: Implement proper inventory service
                var inventory = new[]
                {
                    new { id = "1", name = "Inventory service will be implemented", sku = "TODO001", stock = 0, lowStock = false, price = 0.00 }
                };

                return Ok(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch inventory" });
            }
        }

        [HttpGet("sales")]
        public async Task<IActionResult> GetSales([FromQuery] Guid tenantId, [FromQuery] Guid? branchId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // TODO: Implement proper sales reporting service
                var sales = new[]
                {
                    new { id = "1", total = 0.00, items = 0, customer = "Sales service will be implemented", date = DateTime.Now.ToString("yyyy-MM-dd"), status = "pending" }
                };

                return Ok(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch sales" });
            }
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports([FromQuery] Guid tenantId, [FromQuery] string reportType = "summary", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                switch (reportType.ToLower())
                {
                    case "sales":
                        var salesReport = await _reportsService.GetSalesReportAsync(tenantId, start, end, null, "day");
                        return Ok(new { summary = salesReport });
                    
                    case "inventory":
                        var inventoryReport = await _reportsService.GetInventoryReportAsync(tenantId, null, null, null, null);
                        return Ok(new { summary = inventoryReport });
                    
                    case "patients":
                        var patientsReport = await _reportsService.GetPatientsReportAsync(tenantId, start, end, "month");
                        return Ok(new { summary = patientsReport });
                    
                    case "financial":
                        var financialReport = await _reportsService.GetFinancialReportAsync(tenantId, start, end, null, "summary");
                        return Ok(new { summary = financialReport });
                    
                    default:
                        var dashboardAnalytics = await _reportsService.GetDashboardAnalyticsAsync(tenantId, null, 30);
                        return Ok(new { summary = dashboardAnalytics });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reports for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch reports" });
            }
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings([FromQuery] Guid tenantId)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var settings = new { 
                    systemName = "Umi Health", 
                    timezone = "Africa/Lusaka", 
                    currency = "ZMW",
                    notificationsEnabled = true 
                };

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching settings for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch settings" });
            }
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromQuery] Guid tenantId, [FromBody] object settingsDto)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                return Ok(new { message = "Settings updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to update settings" });
            }
        }
    }
}
