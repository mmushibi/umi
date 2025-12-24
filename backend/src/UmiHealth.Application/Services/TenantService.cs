using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface ITenantService
    {
        Task<Tenant?> GetByIdAsync(Guid id);
        Task<Tenant?> GetBySubdomainAsync(string subdomain);
        Task<Tenant> CreateAsync(Tenant tenant);
        Task<Tenant?> UpdateAsync(Guid id, Tenant tenant);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<Branch>> GetTenantBranchesAsync(Guid tenantId);
        Task<Branch> CreateBranchAsync(Guid tenantId, Branch branch);
        Task<bool> IsTenantActiveAsync(Guid tenantId);
    }

    public class TenantService : ITenantService
    {
        private readonly SharedDbContext _context;

        public TenantService(SharedDbContext context)
        {
            _context = context;
        }

        public async Task<Tenant?> GetByIdAsync(Guid id)
        {
            return await _context.Tenants
                .Include(t => t.Branches)
                .Include(t => t.Subscriptions)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Tenant?> GetBySubdomainAsync(string subdomain)
        {
            return await _context.Tenants
                .Include(t => t.Branches)
                .Include(t => t.Subscriptions)
                .FirstOrDefaultAsync(t => t.Subdomain.ToLower() == subdomain.ToLower());
        }

        public async Task<Tenant> CreateAsync(Tenant tenant)
        {
            tenant.Id = Guid.NewGuid();
            tenant.CreatedAt = DateTime.UtcNow;
            tenant.UpdatedAt = DateTime.UtcNow;
            tenant.Status = "active";

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            return tenant;
        }

        public async Task<Tenant?> UpdateAsync(Guid id, Tenant tenant)
        {
            var existingTenant = await _context.Tenants.FindAsync(id);
            if (existingTenant == null)
                return null;

            existingTenant.Name = tenant.Name;
            existingTenant.Subdomain = tenant.Subdomain;
            existingTenant.DatabaseName = tenant.DatabaseName;
            existingTenant.Status = tenant.Status;
            existingTenant.SubscriptionPlan = tenant.SubscriptionPlan;
            existingTenant.MaxBranches = tenant.MaxBranches;
            existingTenant.MaxUsers = tenant.MaxUsers;
            existingTenant.Settings = tenant.Settings;
            existingTenant.BillingInfo = tenant.BillingInfo;
            existingTenant.ComplianceSettings = tenant.ComplianceSettings;
            existingTenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingTenant;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
                return false;

            tenant.DeletedAt = DateTime.UtcNow;
            tenant.Status = "deleted";
            tenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Branch>> GetTenantBranchesAsync(Guid tenantId)
        {
            return await _context.Branches
                .Where(b => b.TenantId == tenantId && b.DeletedAt == null)
                .ToListAsync();
        }

        public async Task<Branch> CreateBranchAsync(Guid tenantId, Branch branch)
        {
            branch.Id = Guid.NewGuid();
            branch.TenantId = tenantId;
            branch.CreatedAt = DateTime.UtcNow;
            branch.UpdatedAt = DateTime.UtcNow;
            branch.IsActive = true;

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            return branch;
        }

        public async Task<bool> IsTenantActiveAsync(Guid tenantId)
        {
            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            return tenant?.Status == "active" && tenant.DeletedAt == null;
        }
    }
}
