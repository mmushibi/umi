using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Domain.Entities;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tenant>> GetById(Guid id)
        {
            var tenant = await _tenantService.GetByIdAsync(id);
            if (tenant == null)
                return NotFound();

            return Ok(tenant);
        }

        [HttpGet("by-subdomain/{subdomain}")]
        public async Task<ActionResult<Tenant>> GetBySubdomain(string subdomain)
        {
            var tenant = await _tenantService.GetBySubdomainAsync(subdomain);
            if (tenant == null)
                return NotFound();

            return Ok(tenant);
        }

        [HttpPost]
        [Authorize(Roles = "admin,super_admin")]
        public async Task<ActionResult<Tenant>> Create([FromBody] CreateTenantRequest request)
        {
            var tenant = new Tenant
            {
                Name = request.Name,
                Subdomain = request.Subdomain.ToLower(),
                DatabaseName = $"umi_{request.Subdomain.ToLower()}",
                SubscriptionPlan = request.SubscriptionPlan ?? "basic",
                MaxBranches = request.MaxBranches ?? 1,
                MaxUsers = request.MaxUsers ?? 10,
                Settings = request.Settings ?? new(),
                BillingInfo = request.BillingInfo ?? new(),
                ComplianceSettings = request.ComplianceSettings ?? new()
            };

            var createdTenant = await _tenantService.CreateAsync(tenant);
            return CreatedAtAction(nameof(GetById), new { id = createdTenant.Id }, createdTenant);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin,super_admin")]
        public async Task<ActionResult<Tenant>> Update(Guid id, [FromBody] UpdateTenantRequest request)
        {
            var tenant = new Tenant
            {
                Name = request.Name,
                Subdomain = request.Subdomain.ToLower(),
                DatabaseName = request.DatabaseName,
                Status = request.Status,
                SubscriptionPlan = request.SubscriptionPlan,
                MaxBranches = request.MaxBranches,
                MaxUsers = request.MaxUsers,
                Settings = request.Settings,
                BillingInfo = request.BillingInfo,
                ComplianceSettings = request.ComplianceSettings
            };

            var updatedTenant = await _tenantService.UpdateAsync(id, tenant);
            if (updatedTenant == null)
                return NotFound();

            return Ok(updatedTenant);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "super_admin")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var success = await _tenantService.DeleteAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpGet("{id}/branches")]
        public async Task<ActionResult<IEnumerable<Branch>>> GetBranches(Guid id)
        {
            var branches = await _tenantService.GetTenantBranchesAsync(id);
            return Ok(branches);
        }

        [HttpPost("{id}/branches")]
        [Authorize(Roles = "admin,super_admin")]
        public async Task<ActionResult<Branch>> CreateBranch(Guid id, [FromBody] CreateBranchRequest request)
        {
            var branch = new Branch
            {
                Name = request.Name,
                Code = request.Code,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                LicenseNumber = request.LicenseNumber,
                OperatingHours = request.OperatingHours ?? new(),
                Settings = request.Settings ?? new()
            };

            var createdBranch = await _tenantService.CreateBranchAsync(id, branch);
            return CreatedAtAction(nameof(GetBranches), new { id = id }, createdBranch);
        }

        [HttpGet("{id}/status")]
        public async Task<ActionResult<bool>> GetStatus(Guid id)
        {
            var isActive = await _tenantService.IsTenantActiveAsync(id);
            return Ok(new { isActive });
        }
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string? SubscriptionPlan { get; set; }
        public int? MaxBranches { get; set; }
        public int? MaxUsers { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
        public Dictionary<string, object>? BillingInfo { get; set; }
        public Dictionary<string, object>? ComplianceSettings { get; set; }
    }

    public class UpdateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public int MaxBranches { get; set; }
        public int MaxUsers { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> BillingInfo { get; set; } = new();
        public Dictionary<string, object> ComplianceSettings { get; set; } = new();
    }

    public class CreateBranchRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? LicenseNumber { get; set; }
        public Dictionary<string, object>? OperatingHours { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }
}
