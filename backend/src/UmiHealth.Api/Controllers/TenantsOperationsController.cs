using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TenantsOperationsController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<TenantsOperationsController> _logger;

        public TenantsOperationsController(SharedDbContext context, ILogger<TenantsOperationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetTenants(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? subscriptionPlan = null)
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

                if (!string.IsNullOrEmpty(subscriptionPlan))
                {
                    query = query.Where(t => t.SubscriptionPlan == subscriptionPlan);
                }

                var tenants = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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
                _logger.LogError(ex, "Error retrieving tenants");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tenant>> GetTenant(Guid id)
        {
            try
            {
                var tenant = await _context.Tenants.FindAsync(id);
                if (tenant == null)
                {
                    return NotFound();
                }

                return Ok(tenant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant {TenantId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Tenant>> CreateTenant(CreateTenantRequest request)
        {
            try
            {
                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Subdomain = request.Subdomain,
                    DatabaseName = $"umi_tenant_{Guid.NewGuid():N}",
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Address = request.Address,
                    Status = "active",
                    SubscriptionPlan = request.SubscriptionPlan ?? "basic",
                    MaxBranches = request.MaxBranches ?? 1,
                    MaxUsers = request.MaxUsers ?? 5,
                    Settings = request.Settings ?? new Dictionary<string, object>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tenant");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTenant(Guid id, UpdateTenantRequest request)
        {
            try
            {
                var tenant = await _context.Tenants.FindAsync(id);
                if (tenant == null)
                {
                    return NotFound();
                }

                tenant.Name = request.Name ?? tenant.Name;
                tenant.Subdomain = request.Subdomain ?? tenant.Subdomain;
                tenant.ContactEmail = request.ContactEmail ?? tenant.ContactEmail;
                tenant.ContactPhone = request.ContactPhone ?? tenant.ContactPhone;
                tenant.Address = request.Address ?? tenant.Address;
                tenant.Status = request.Status ?? tenant.Status;
                tenant.SubscriptionPlan = request.SubscriptionPlan ?? tenant.SubscriptionPlan;
                tenant.MaxBranches = request.MaxBranches ?? tenant.MaxBranches;
                tenant.MaxUsers = request.MaxUsers ?? tenant.MaxUsers;
                tenant.Settings = request.Settings ?? tenant.Settings;
                tenant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant {TenantId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTenant(Guid id)
        {
            try
            {
                var tenant = await _context.Tenants.FindAsync(id);
                if (tenant == null)
                {
                    return NotFound();
                }

                tenant.Status = "deleted";
                tenant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<TenantStats>> GetTenantStats()
        {
            try
            {
                var totalTenants = await _context.Tenants.CountAsync();
                var activeTenants = await _context.Tenants.CountAsync(t => t.Status == "active");
                var inactiveTenants = await _context.Tenants.CountAsync(t => t.Status == "inactive");
                var suspendedTenants = await _context.Tenants.CountAsync(t => t.Status == "suspended");
                
                var recentTenants = await _context.Tenants
                    .Where(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    .CountAsync();

                var subscriptionStats = await _context.Tenants
                    .GroupBy(t => t.SubscriptionPlan)
                    .Select(g => new { Plan = g.Key, Count = g.Count() })
                    .ToListAsync();

                return Ok(new TenantStats
                {
                    TotalTenants = totalTenants,
                    ActiveTenants = activeTenants,
                    InactiveTenants = inactiveTenants,
                    SuspendedTenants = suspendedTenants,
                    RecentTenants = recentTenants,
                    SubscriptionStats = subscriptionStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant stats");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public Dictionary<string, object>? Address { get; set; }
        public string? SubscriptionPlan { get; set; }
        public int? MaxBranches { get; set; }
        public int? MaxUsers { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }

    public class UpdateTenantRequest
    {
        public string? Name { get; set; }
        public string? Subdomain { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public Dictionary<string, object>? Address { get; set; }
        public string? Status { get; set; }
        public string? SubscriptionPlan { get; set; }
        public int? MaxBranches { get; set; }
        public int? MaxUsers { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }

    public class TenantStats
    {
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int InactiveTenants { get; set; }
        public int SuspendedTenants { get; set; }
        public int RecentTenants { get; set; }
        public List<object> SubscriptionStats { get; set; } = new();
    }
}
