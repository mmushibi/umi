using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public class TenantService : ITenantService
    {
        private readonly IRepository<UmiHealth.Core.Entities.Tenant> _tenantRepository;
        private readonly ITenantRepository<UmiHealth.Core.Entities.Branch> _branchRepository;
        private readonly ILogger<TenantService> _logger;

        public TenantService(
            IRepository<UmiHealth.Core.Entities.Tenant> tenantRepository,
            ITenantRepository<UmiHealth.Core.Entities.Branch> branchRepository,
            ILogger<TenantService> logger)
        {
            _tenantRepository = tenantRepository;
            _branchRepository = branchRepository;
            _logger = logger;
        }

        public async Task<UmiHealth.Core.Entities.Tenant?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task<UmiHealth.Core.Entities.Tenant?> GetTenantBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            try
            {
                var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
                return tenants.FirstOrDefault(t => t.Subdomain?.Equals(subdomain, StringComparison.OrdinalIgnoreCase) == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant by subdomain {Subdomain}", subdomain);
                return null;
            }
        }

        public async Task<IReadOnlyList<UmiHealth.Core.Entities.Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
                return tenants.Where(t => t.IsActive).ToList().AsReadOnly();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tenants");
                return new List<Tenant>().AsReadOnly();
            }
        }

        public async Task<IEnumerable<UmiHealth.Core.Entities.Tenant>> GetAllAsync()
        {
            return await GetAllTenantsAsync();
        }

        public async Task<UmiHealth.Core.Entities.Tenant> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var tenant = new UmiHealth.Core.Entities.Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    Subdomain = request.Subdomain.ToLowerInvariant(),
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Address = request.Address,
                    City = request.City,
                    Country = request.Country,
                    PostalCode = request.PostalCode,
                    SubscriptionPlan = request.SubscriptionPlan,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdTenant = await _tenantRepository.AddAsync(tenant, cancellationToken);

                _logger.LogInformation("Created tenant {TenantName} with ID {TenantId}", tenant.Name, tenant.Id);
                return createdTenant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tenant {TenantName}", request.Name);
                throw;
            }
        }

        public async Task<UmiHealth.Core.Entities.Tenant> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
                if (tenant == null)
                    throw new KeyNotFoundException($"Tenant {tenantId} not found");

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

                _logger.LogInformation("Updated tenant {TenantId}", tenantId);
                return tenant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
                if (tenant == null)
                    return false;

                tenant.IsActive = false;
                tenant.UpdatedAt = DateTime.UtcNow;

                await _tenantRepository.UpdateAsync(tenant, cancellationToken);

                _logger.LogInformation("Deleted tenant {TenantId}", tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<UmiHealth.Core.Entities.Branch> CreateBranchAsync(Guid tenantId, CreateBranchRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
                if (tenant == null)
                    throw new KeyNotFoundException($"Tenant {tenantId} not found");

                var branch = new UmiHealth.Core.Entities.Branch
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = request.Name,
                    Code = request.Code,
                    Address = request.Address,
                    City = request.City,
                    Country = request.Country,
                    Phone = request.Phone,
                    Email = request.Email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdBranch = await _branchRepository.AddAsync(branch, cancellationToken);

                _logger.LogInformation("Created branch {BranchName} for tenant {TenantId}", branch.Name, tenantId);
                return createdBranch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<UmiHealth.Core.Entities.Branch> UpdateBranchAsync(Guid branchId, UpdateBranchRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
                if (branch == null)
                    throw new KeyNotFoundException($"Branch {branchId} not found");

                branch.Name = request.Name;
                branch.Code = request.Code;
                branch.Address = request.Address;
                branch.City = request.City;
                branch.Country = request.Country;
                branch.Phone = request.Phone;
                branch.Email = request.Email;
                branch.UpdatedAt = DateTime.UtcNow;

                await _branchRepository.UpdateAsync(branch, cancellationToken);

                _logger.LogInformation("Updated branch {BranchId}", branchId);
                return branch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch {BranchId}", branchId);
                throw;
            }
        }

        public async Task<bool> DeleteBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
        {
            try
            {
                var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
                if (branch == null)
                    return false;

                branch.IsActive = false;
                branch.UpdatedAt = DateTime.UtcNow;

                await _branchRepository.UpdateAsync(branch, cancellationToken);

                _logger.LogInformation("Deleted branch {BranchId}", branchId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch {BranchId}", branchId);
                throw;
            }
        }

        public async Task<IReadOnlyList<UmiHealth.Core.Entities.Branch>> GetTenantBranchesAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var branches = await _branchRepository.GetByTenantAsync(tenantId, cancellationToken);
                return branches.Where(b => b.IsActive).ToList().AsReadOnly();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branches for tenant {TenantId}", tenantId);
                return new List<UmiHealth.Core.Entities.Branch>().AsReadOnly();
            }
        }

        public async Task<bool> IsSubscriptionActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
                return tenant?.IsActive == true && tenant.SubscriptionPlan != "expired";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscription status for tenant {TenantId}", tenantId);
                return false;
            }
        }
    }
}
