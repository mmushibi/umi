using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealth.Core.Entities;
using UmiHealth.Infrastructure;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TenantController : ControllerBase
    {
        private readonly UmiHealthDbContext _context;
        private readonly ILogger<TenantController> _logger;

        public TenantController(UmiHealthDbContext context, ILogger<TenantController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult<IEnumerable<TenantDto>>> GetTenants()
        {
            try
            {
                var tenants = await _context.Tenants
                    .Where(t => !t.IsDeleted)
                    .Select(t => new TenantDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        Subdomain = t.Subdomain,
                        IsActive = t.IsActive,
                        SubscriptionPlan = t.SubscriptionPlan,
                        SubscriptionExpiresAt = t.SubscriptionExpiresAt,
                        ContactEmail = t.ContactEmail,
                        ContactPhone = t.ContactPhone,
                        Address = t.Address,
                        City = t.City,
                        Country = t.Country,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(tenants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenants");
                return StatusCode(500, new { error = "Failed to retrieve tenants" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TenantDto>> GetTenant(Guid id)
        {
            try
            {
                var tenantId = GetCurrentUserTenantId();
                if (tenantId == null)
                {
                    return Unauthorized();
                }

                // Only allow users to see their own tenant unless admin
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (userRole != "SuperAdmin" && userRole != "Admin" && tenantId != id)
                {
                    return Forbid();
                }

                var tenant = await _context.Tenants
                    .Where(t => t.Id == id && !t.IsDeleted)
                    .Select(t => new TenantDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        Subdomain = t.Subdomain,
                        IsActive = t.IsActive,
                        SubscriptionPlan = t.SubscriptionPlan,
                        SubscriptionExpiresAt = t.SubscriptionExpiresAt,
                        ContactEmail = t.ContactEmail,
                        ContactPhone = t.ContactPhone,
                        Address = t.Address,
                        City = t.City,
                        Country = t.Country,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (tenant == null)
                {
                    return NotFound();
                }

                return Ok(tenant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant {TenantId}", id);
                return StatusCode(500, new { error = "Failed to retrieve tenant" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "RequireOperationsRole")]
        public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantRequest request)
        {
            try
            {
                // Check if subdomain already exists
                var existingTenant = await _context.Tenants
                    .AnyAsync(t => t.Subdomain.ToLower() == request.Subdomain.ToLower() && !t.IsDeleted);

                if (existingTenant)
                {
                    return BadRequest(new { error = "Subdomain already exists" });
                }

                var tenant = new Tenant
                {
                    Name = request.Name,
                    Description = request.Description,
                    Subdomain = request.Subdomain.ToLower(),
                    DatabaseName = $"umihealth_{request.Subdomain.ToLower()}",
                    IsActive = true,
                    SubscriptionPlan = request.SubscriptionPlan ?? "basic",
                    SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30), // 30-day trial
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Address = request.Address,
                    City = request.City,
                    Country = request.Country ?? "Zambia"
                };

                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tenant created: {TenantId} - {TenantName}", tenant.Id, tenant.Name);

                var tenantDto = new TenantDto
                {
                    Id = tenant.Id,
                    Name = tenant.Name,
                    Description = tenant.Description,
                    Subdomain = tenant.Subdomain,
                    IsActive = tenant.IsActive,
                    SubscriptionPlan = tenant.SubscriptionPlan,
                    SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
                    ContactEmail = tenant.ContactEmail,
                    ContactPhone = tenant.ContactPhone,
                    Address = tenant.Address,
                    City = tenant.City,
                    Country = tenant.Country,
                    CreatedAt = tenant.CreatedAt,
                    UpdatedAt = tenant.UpdatedAt
                };

                return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenantDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tenant");
                return StatusCode(500, new { error = "Failed to create tenant" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TenantDto>> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
        {
            try
            {
                var tenantId = GetCurrentUserTenantId();
                if (tenantId == null)
                {
                    return Unauthorized();
                }

                // Only allow users to update their own tenant unless admin
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (userRole != "SuperAdmin" && userRole != "Admin" && tenantId != id)
                {
                    return Forbid();
                }

                var tenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

                if (tenant == null)
                {
                    return NotFound();
                }

                // Update properties
                if (!string.IsNullOrEmpty(request.Name))
                    tenant.Name = request.Name;

                if (!string.IsNullOrEmpty(request.Description))
                    tenant.Description = request.Description;

                if (!string.IsNullOrEmpty(request.ContactEmail))
                    tenant.ContactEmail = request.ContactEmail;

                if (!string.IsNullOrEmpty(request.ContactPhone))
                    tenant.ContactPhone = request.ContactPhone;

                if (!string.IsNullOrEmpty(request.Address))
                    tenant.Address = request.Address;

                if (!string.IsNullOrEmpty(request.City))
                    tenant.City = request.City;

                if (!string.IsNullOrEmpty(request.Country))
                    tenant.Country = request.Country;

                tenant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Tenant updated: {TenantId}", tenant.Id);

                var tenantDto = new TenantDto
                {
                    Id = tenant.Id,
                    Name = tenant.Name,
                    Description = tenant.Description,
                    Subdomain = tenant.Subdomain,
                    IsActive = tenant.IsActive,
                    SubscriptionPlan = tenant.SubscriptionPlan,
                    SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
                    ContactEmail = tenant.ContactEmail,
                    ContactPhone = tenant.ContactPhone,
                    Address = tenant.Address,
                    City = tenant.City,
                    Country = tenant.Country,
                    CreatedAt = tenant.CreatedAt,
                    UpdatedAt = tenant.UpdatedAt
                };

                return Ok(tenantDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant {TenantId}", id);
                return StatusCode(500, new { error = "Failed to update tenant" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireOperationsRole")]
        public async Task<ActionResult> DeleteTenant(Guid id)
        {
            try
            {
                var tenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

                if (tenant == null)
                {
                    return NotFound();
                }

                tenant.IsDeleted = true;
                tenant.DeletedAt = DateTime.UtcNow;
                tenant.DeletedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Tenant deleted: {TenantId}", tenant.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
                return StatusCode(500, new { error = "Failed to delete tenant" });
            }
        }

        [HttpGet("{id}/branches")]
        public async Task<ActionResult<IEnumerable<BranchDto>>> GetTenantBranches(Guid id)
        {
            try
            {
                var tenantId = GetCurrentUserTenantId();
                if (tenantId == null)
                {
                    return Unauthorized();
                }

                // Only allow users to see their own tenant branches unless admin
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (userRole != "SuperAdmin" && userRole != "Admin" && tenantId != id)
                {
                    return Forbid();
                }

                var branches = await _context.Branches
                    .Where(b => b.TenantId == id && !b.IsDeleted)
                    .Select(b => new BranchDto
                    {
                        Id = b.Id,
                        TenantId = b.TenantId,
                        Name = b.Name,
                        Code = b.Code,
                        Address = b.Address,
                        City = b.City,
                        Country = b.Country,
                        PostalCode = b.PostalCode,
                        Phone = b.Phone,
                        Email = b.Email,
                        IsMainBranch = b.IsMainBranch,
                        IsActive = b.IsActive,
                        ManagerName = b.ManagerName,
                        ManagerPhone = b.ManagerPhone,
                        CreatedAt = b.CreatedAt,
                        UpdatedAt = b.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(branches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branches for tenant {TenantId}", id);
                return StatusCode(500, new { error = "Failed to retrieve branches" });
            }
        }

        private Guid? GetCurrentUserTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id");
            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                return tenantId;
            }
            return null;
        }
    }

    // DTOs
    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Subdomain { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
        public DateTime? SubscriptionExpiresAt { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Country { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class BranchDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Country { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsMainBranch { get; set; }
        public bool IsActive { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerPhone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Subdomain { get; set; } = string.Empty;
        public string? SubscriptionPlan { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    public class UpdateTenantRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }
}
