using Microsoft.Extensions.Logging;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;
using UmiHealth.Infrastructure.Repositories;

namespace UmiHealth.Application.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository<Tenant> _tenantRepository;
    private readonly ITenantRepository<Branch> _branchRepository;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        ITenantRepository<Tenant> tenantRepository,
        ITenantRepository<Branch> branchRepository,
        ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository;
        _branchRepository = branchRepository;
        _logger = logger;
    }

    public async Task<Tenant?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
    }

    public async Task<Tenant?> GetTenantBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantRepository.FindAsync(
            t => t.Subdomain == subdomain && t.IsActive, 
            cancellationToken);
        return tenants.FirstOrDefault();
    }

    public async Task<IReadOnlyList<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await _tenantRepository.GetAllAsync(cancellationToken);
    }

    public async Task<Tenant> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var existingTenant = await GetTenantBySubdomainAsync(request.Subdomain, cancellationToken);
        if (existingTenant != null)
        {
            throw new InvalidOperationException($"Tenant with subdomain '{request.Subdomain}' already exists.");
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Subdomain = request.Subdomain,
            DatabaseName = $"umihealth_{request.Subdomain}",
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            PostalCode = request.PostalCode,
            SubscriptionPlan = request.SubscriptionPlan,
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30), // Default 30 days
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _tenantRepository.AddAsync(tenant, cancellationToken);
    }

    public async Task<Tenant> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with ID '{tenantId}' not found.");
        }

        tenant.Name = request.Name;
        tenant.Description = request.Description;
        tenant.ContactEmail = request.ContactEmail;
        tenant.ContactPhone = request.ContactPhone;
        tenant.Address = request.Address;
        tenant.City = request.City;
        tenant.Country = request.Country;
        tenant.PostalCode = request.PostalCode;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        return tenant;
    }

    public async Task<bool> DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return false;
        }

        await _tenantRepository.DeleteAsync(tenant, cancellationToken);
        return true;
    }

    public async Task<Branch> CreateBranchAsync(Guid tenantId, CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with ID '{tenantId}' not found.");
        }

        var existingBranch = (await _branchRepository.FindAsync(
            b => b.TenantId == tenantId && b.Code == request.Code, 
            cancellationToken)).FirstOrDefault();

        if (existingBranch != null)
        {
            throw new InvalidOperationException($"Branch with code '{request.Code}' already exists for this tenant.");
        }

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Code = request.Code,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            PostalCode = request.PostalCode,
            Phone = request.Phone,
            Email = request.Email,
            IsMainBranch = request.IsMainBranch,
            ManagerName = request.ManagerName,
            ManagerPhone = request.ManagerPhone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _branchRepository.AddAsync(branch, cancellationToken);
    }

    public async Task<Branch> UpdateBranchAsync(Guid branchId, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch == null)
        {
            throw new KeyNotFoundException($"Branch with ID '{branchId}' not found.");
        }

        branch.Name = request.Name;
        branch.Code = request.Code;
        branch.Address = request.Address;
        branch.City = request.City;
        branch.Country = request.Country;
        branch.PostalCode = request.PostalCode;
        branch.Phone = request.Phone;
        branch.Email = request.Email;
        branch.ManagerName = request.ManagerName;
        branch.ManagerPhone = request.ManagerPhone;
        branch.UpdatedAt = DateTime.UtcNow;

        await _branchRepository.UpdateAsync(branch, cancellationToken);
        return branch;
    }

    public async Task<bool> DeleteBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch == null)
        {
            return false;
        }

        await _branchRepository.DeleteAsync(branch, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Branch>> GetTenantBranchesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _branchRepository.GetByTenantAsync(tenantId, cancellationToken);
    }

    public async Task<bool> IsSubscriptionActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return false;
        }

        return tenant.IsActive && 
               (!tenant.SubscriptionExpiresAt.HasValue || 
                tenant.SubscriptionExpiresAt.Value > DateTime.UtcNow);
    }
}
