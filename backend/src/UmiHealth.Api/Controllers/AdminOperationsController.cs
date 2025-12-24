using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "admin,superadmin")]
    public class AdminOperationsController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ICrossPortalSyncService _crossPortalSync;
        private readonly ILogger<AdminOperationsController> _logger;

        public AdminOperationsController(
            SharedDbContext context,
            ICrossPortalSyncService crossPortalSync,
            ILogger<AdminOperationsController> logger)
        {
            _context = context;
            _crossPortalSync = crossPortalSync;
            _logger = logger;
        }

        [HttpGet("tenants")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllTenantsForAdmin(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.Tenants.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(t => 
                        t.Name.Contains(search) || 
                        t.Subdomain.Contains(search) ||
                        (t.ContactEmail != null && t.ContactEmail.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(t => t.Status == status);
                }

                var tenants = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.Subdomain,
                        t.ContactEmail,
                        t.ContactPhone,
                        t.Status,
                        t.SubscriptionPlan,
                        t.MaxBranches,
                        t.MaxUsers,
                        t.CreatedAt,
                        t.UpdatedAt,
                        ActiveUsers = _context.Users.Count(u => u.TenantId == t.Id && u.IsActive),
                        TotalTransactions = _context.Sales.Count(s => s.BranchId == t.Id && s.DeletedAt == null),
                        LastActivity = _context.Sales
                            .Where(s => s.BranchId == t.Id && s.DeletedAt == null)
                            .OrderByDescending(s => s.SaleDate)
                            .Select(s => (DateTime?)s.SaleDate)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var totalCount = await query.CountAsync();

                return Ok(new
                {
                    tenants,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenants for admin");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllTransactionsForAdmin(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? tenantId = null)
        {
            try
            {
                var query = _context.Sales
                    .Include(s => s.Items)
                    .Include(s => s.Payments)
                    .Where(s => s.DeletedAt == null);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => 
                        s.SaleNumber.Contains(search) ||
                        (s.PatientId.HasValue && s.PatientId.ToString().Contains(search)));
                }

                if (startDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(tenantId))
                {
                    var tenantGuid = Guid.Parse(tenantId);
                    query = query.Where(s => s.BranchId == tenantGuid);
                }

                var transactions = await query
                    .OrderByDescending(s => s.SaleDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        s.Id,
                        s.SaleNumber,
                        s.BranchId,
                        TenantName = _context.Tenants.FirstOrDefault(t => t.Id == s.BranchId)?.Name ?? "Unknown",
                        s.PatientId,
                        s.CashierId,
                        s.SaleDate,
                        s.Subtotal,
                        s.TaxAmount,
                        s.DiscountAmount,
                        s.TotalAmount,
                        s.PaymentMethod,
                        s.Status,
                        s.PaymentStatus,
                        Items = s.Items.Select(si => new
                        {
                            si.Id,
                            si.ProductId,
                            ProductName = si.Product != null ? si.Product.Name : "Unknown Product",
                            si.Quantity,
                            si.UnitPrice,
                            si.DiscountAmount,
                            si.TotalPrice
                        }).ToList(),
                        Payments = s.Payments.Select(p => new
                        {
                            p.Id,
                            p.PaymentNumber,
                            p.Amount,
                            p.PaymentMethod,
                            p.PaymentDate,
                            p.Status,
                            p.ReferenceNumber
                        }).ToList()
                    })
                    .ToListAsync();

                var totalCount = await query.CountAsync();

                return Ok(new
                {
                    transactions,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions for admin");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllUsersForAdmin(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] string? status = null,
            [FromQuery] string? tenantId = null)
        {
            try
            {
                // Mock users data for admin view - in real implementation this would query a Users table
                var users = new List<object>();
                
                for (int i = 1; i <= 100; i++)
                {
                    users.Add(new
                    {
                        Id = Guid.NewGuid(),
                        Username = $"user{i}",
                        Email = $"user{i}@example.com",
                        FirstName = $"User{i}",
                        LastName = $"Test",
                        Role = i % 3 == 0 ? "Admin" : i % 2 == 0 ? "Cashier" : "Operations",
                        Status = i % 4 == 0 ? "inactive" : "active",
                        TenantId = string.IsNullOrEmpty(tenantId) ? Guid.NewGuid() : Guid.Parse(tenantId),
                        BranchId = Guid.NewGuid(),
                        LastLogin = DateTime.UtcNow.AddDays(-i),
                        CreatedAt = DateTime.UtcNow.AddDays(-i * 10),
                        Permissions = new List<string> { "read", "write" }
                    });
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    users = users.Where(u => 
                        u.Email.ToString().Contains(searchLower) ||
                        u.FirstName.ToString().Contains(searchLower) ||
                        u.LastName.ToString().Contains(searchLower) ||
                        u.Username.ToString().Contains(searchLower)).ToList();
                }

                if (!string.IsNullOrEmpty(role))
                {
                    users = users.Where(u => u.Role.ToString().Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(status))
                {
                    users = users.Where(u => u.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var totalCount = users.Count;
                var pagedUsers = users
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    users = pagedUsers,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for admin");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("subscriptions")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllSubscriptionsForAdmin(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? planType = null)
        {
            try
            {
                // Mock subscription data for admin view
                var subscriptions = new List<object>();
                
                for (int i = 1; i <= 50; i++)
                {
                    subscriptions.Add(new
                    {
                        Id = Guid.NewGuid(),
                        TenantId = Guid.NewGuid(),
                        TenantName = $"Tenant {i}",
                        TenantDomain = $"tenant{i}.umihealth.com",
                        PlanType = i % 3 == 0 ? "basic" : i % 2 == 0 ? "professional" : "enterprise",
                        Status = i % 4 == 0 ? "expired" : i % 3 == 0 ? "cancelled" : "active",
                        BillingCycle = i % 2 == 0 ? "monthly" : "yearly",
                        Amount = i % 3 == 0 ? 50 : i % 2 == 0 ? 150 : 500,
                        Currency = "ZMW",
                        StartDate = DateTime.UtcNow.AddDays(-i * 30),
                        EndDate = DateTime.UtcNow.AddDays(i * 30),
                        AutoRenew = i % 3 != 0,
                        Features = i % 3 == 0 ? 
                            new List<string> { "Basic POS", "Inventory Management" } :
                            i % 2 == 0 ?
                            new List<string> { "Advanced POS", "Inventory Management", "Reports", "Multi-branch" } :
                            new List<string> { "Full POS Suite", "Advanced Analytics", "API Access", "Priority Support", "Unlimited Branches" },
                        Limits = i % 3 == 0 ?
                            new Dictionary<string, object> { ["users"] = 5, ["branches"] = 1, ["transactions"] = 1000 } :
                            i % 2 == 0 ?
                            new Dictionary<string, object> { ["users"] = 25, ["branches"] = 5, ["transactions"] = 10000 } :
                            new Dictionary<string, object> { ["users"] = 100, ["branches"] = 50, ["transactions"] = "unlimited" },
                        CreatedAt = DateTime.UtcNow.AddDays(-i * 30),
                        UpdatedAt = DateTime.UtcNow.AddDays(-i * 10)
                    });
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    subscriptions = subscriptions.Where(s => 
                        s.Id.ToString().Contains(searchLower) ||
                        s.TenantName.ToString().Contains(searchLower) ||
                        s.PlanType.ToString().Contains(searchLower) ||
                        s.Status.ToString().Contains(searchLower)).ToList();
                }

                if (!string.IsNullOrEmpty(status))
                {
                    subscriptions = subscriptions.Where(s => s.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(planType))
                {
                    subscriptions = subscriptions.Where(s => s.PlanType.ToString().Equals(planType, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var totalCount = subscriptions.Count;
                var pagedSubscriptions = subscriptions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    subscriptions = pagedSubscriptions,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscriptions for admin");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashboardStats>> GetDashboardStats()
        {
            try
            {
                var totalTenants = await _context.Tenants.CountAsync();
                var activeTenants = await _context.Tenants.CountAsync(t => t.Status == "active");
                var totalTransactions = await _context.Sales.CountAsync(s => s.DeletedAt == null);
                var totalRevenue = await _context.Sales.Where(s => s.DeletedAt == null).SumAsync(s => s.TotalAmount);
                
                var todayTransactions = await _context.Sales
                    .Where(s => s.DeletedAt == null && s.SaleDate.Date == DateTime.UtcNow.Date)
                    .CountAsync();
                
                var todayRevenue = await _context.Sales
                    .Where(s => s.DeletedAt == null && s.SaleDate.Date == DateTime.UtcNow.Date)
                    .SumAsync(s => s.TotalAmount);

                var subscriptionStats = await _context.Tenants
                    .GroupBy(t => t.SubscriptionPlan)
                    .Select(g => new { Plan = g.Key, Count = g.Count() })
                    .ToListAsync();

                var monthlyStats = await _context.Sales
                    .Where(s => s.DeletedAt == null && s.SaleDate >= DateTime.UtcNow.AddDays(-30))
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count(), Revenue = g.Sum(s => s.TotalAmount) })
                    .ToListAsync();

                return Ok(new AdminDashboardStats
                {
                    TotalTenants = totalTenants,
                    ActiveTenants = activeTenants,
                    TotalTransactions = totalTransactions,
                    TotalRevenue = totalRevenue,
                    TodayTransactions = todayTransactions,
                    TodayRevenue = todayRevenue,
                    SubscriptionStats = subscriptionStats,
                    MonthlyStats = monthlyStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard stats for admin");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("broadcast/{entityType}")]
        public async Task<IActionResult> BroadcastToOperations(string entityType, [FromBody] BroadcastToOperationsRequest request)
        {
            try
            {
                // Broadcast data change to operations portal
                await _crossPortalSync.BroadcastDataChangeAsync(
                    entityType, 
                    request.Data, 
                    "admin", 
                    request.TenantId);

                return Ok(new { message = "Data broadcasted to operations portal successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting to operations portal");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class BroadcastToOperationsRequest
    {
        public object Data { get; set; } = new();
        public Guid? TenantId { get; set; }
        public string? TargetPortal { get; set; }
    }

    public class AdminDashboardStats
    {
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TodayTransactions { get; set; }
        public decimal TodayRevenue { get; set; }
        public List<object> SubscriptionStats { get; set; } = new();
        public List<object> MonthlyStats { get; set; } = new();
    }
}
