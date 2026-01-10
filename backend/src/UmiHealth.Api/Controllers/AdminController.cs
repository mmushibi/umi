using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UmiHealth.Application.DTOs;
using UmiHealth.Application.Services;
using UmiHealth.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IReportsService _reportsService;
        private readonly ITenantService _tenantService;

        public AdminController(
            ILogger<AdminController> logger,
            IReportsService reportsService,
            ITenantService tenantService)
        {
            _logger = logger;
            _reportsService = reportsService;
            _tenantService = tenantService;
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

                // Mock data for now
                var activity = new[]
                {
                    new { id = 1, action = "New patient registered", user = "Bwalya Mwansa", time = "2 mins ago", type = "info" },
                    new { id = 2, action = "Prescription approved", user = "Dr. Mutale Chanda", time = "5 mins ago", type = "success" },
                    new { id = 3, action = "Low stock alert", user = "System", time = "10 mins ago", type = "warning" }
                };

                return Ok(activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent activity for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch recent activity" });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] Guid tenantId, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // Mock data for now - replace with real user service when available
                var users = new[]
                {
                    new UmiHealth.Application.DTOs.UserDto { Id = Guid.NewGuid(), Name = "Admin User", Email = "admin@example.com", Role = "Admin", Status = "active" },
                    new UmiHealth.Application.DTOs.UserDto { Id = Guid.NewGuid(), Name = "Cashier User", Email = "cashier@example.com", Role = "Cashier", Status = "active" }
                };

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch users" });
            }
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromQuery] Guid tenantId, [FromBody] UpdateUserRequest userDto)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var user = new UmiHealth.Application.DTOs.UserDto 
                { 
                    Id = Guid.NewGuid(), 
                    Name = userDto.Name, 
                    Email = userDto.Email, 
                    Role = userDto.Role, 
                    Status = userDto.Status 
                };

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to create user" });
            }
        }

        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser([FromQuery] Guid tenantId, Guid userId, [FromBody] UpdateUserRequest userDto)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var user = new UmiHealth.Application.DTOs.UserDto 
                { 
                    Id = userId, 
                    Name = userDto.Name, 
                    Email = userDto.Email, 
                    Role = userDto.Role, 
                    Status = userDto.Status 
                };

                return Ok(user);
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

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId} for tenant {TenantId}", userId, tenantId);
                return StatusCode(500, new { error = "Failed to delete user" });
            }
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

                // Mock data for now
                var inventory = new[]
                {
                    new { id = "1", name = "Paracetamol 500mg", sku = "PAR001", stock = 150, lowStock = false, price = 5.99 },
                    new { id = "2", name = "Ibuprofen 400mg", sku = "IBU002", stock = 12, lowStock = true, price = 7.99 }
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

                var sales = new[]
                {
                    new { id = "1", total = 45.99, items = 3, customer = "John Doe", date = "2024-01-15", status = "completed" },
                    new { id = "2", total = 23.50, items = 2, customer = "Jane Smith", date = "2024-01-15", status = "completed" }
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
