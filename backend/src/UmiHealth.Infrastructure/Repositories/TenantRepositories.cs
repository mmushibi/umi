using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Infrastructure.Repositories
{
    public interface ITenantRepository : ITenantRepository<Tenant>
    {
        Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
        Task<Tenant?> GetByDatabaseNameAsync(string databaseName, CancellationToken cancellationToken = default);
        Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);
        Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTenantId = null, CancellationToken cancellationToken = default);
    }

    public class TenantRepository : Repository<Tenant>, ITenantRepository
    {
        public TenantRepository(SharedDbContext context) : base(context)
        {
        }

        public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Subdomain.ToLower() == subdomain.ToLower(), cancellationToken);
        }

        public async Task<Tenant?> GetByDatabaseNameAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.DatabaseName == databaseName, cancellationToken);
        }

        public async Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.Status == "active")
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTenantId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(t => t.Subdomain.ToLower() == subdomain.ToLower());
            
            if (excludeTenantId.HasValue)
            {
                query = query.Where(t => t.Id != excludeTenantId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }

        public async Task<IEnumerable<Tenant>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            return await _dbSet
                .Where(t => t.Id == tenantGuid)
                .ToListAsync(cancellationToken);
        }
    }

    public interface IUserRepository : ITenantRepository<User>
    {
        Task<User?> GetByEmailAsync(string tenantId, string email, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameAsync(string tenantId, string username, CancellationToken cancellationToken = default);
        Task<IEnumerable<User>> GetByBranchAsync(string tenantId, string branchId, CancellationToken cancellationToken = default);
        Task<IEnumerable<User>> GetByRoleAsync(string tenantId, string role, CancellationToken cancellationToken = default);
        Task<bool> IsEmailAvailableAsync(string tenantId, string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
    }

    public class UserRepository : TenantRepository<User>
    {
        public UserRepository(SharedDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string tenantId, string email, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            return await _dbSet
                .FirstOrDefaultAsync(u => u.TenantId == tenantGuid && u.Email.ToLower() == email.ToLower(), cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string tenantId, string username, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            return await _dbSet
                .FirstOrDefaultAsync(u => u.TenantId == tenantGuid && u.Username != null && u.Username.ToLower() == username.ToLower(), cancellationToken);
        }

        public async Task<IEnumerable<User>> GetByBranchAsync(string tenantId, string branchId, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            var branchGuid = Guid.Parse(branchId);
            return await _dbSet
                .Where(u => u.TenantId == tenantGuid && u.BranchId == branchGuid)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(string tenantId, string role, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            return await _dbSet
                .Where(u => u.TenantId == tenantGuid && u.Role.ToLower() == role.ToLower())
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsEmailAvailableAsync(string tenantId, string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            var query = _dbSet.Where(u => u.TenantId == tenantGuid && u.Email.ToLower() == email.ToLower());
            
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }

        public override async Task<IEnumerable<User>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            return await _dbSet
                .Where(u => u.TenantId == tenantGuid)
                .ToListAsync(cancellationToken);
        }

        public override async Task<IEnumerable<User>> GetByTenantAndBranchAsync(string tenantId, string branchId, CancellationToken cancellationToken = default)
        {
            return await GetByBranchAsync(tenantId, branchId, cancellationToken);
        }

        public override async Task<User?> GetByTenantAndIdAsync(string tenantId, object id, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            var userGuid = Guid.Parse(id.ToString());
            return await _dbSet
                .FirstOrDefaultAsync(u => u.TenantId == tenantGuid && u.Id == userGuid, cancellationToken);
        }
    }

    public interface IBranchRepository : ITenantRepository<Branch>
    {
        Task<Branch?> GetByCodeAsync(string tenantId, string code, CancellationToken cancellationToken = default);
        Task<bool> IsCodeAvailableAsync(string tenantId, string code, Guid? excludeBranchId = null, CancellationToken cancellationToken = default);
    }

    public class BranchRepository : TenantRepository<Branch>
    {
        public BranchRepository(SharedDbContext context) : base(context)
        {
        }

        public async Task<Branch?> GetByCodeAsync(string tenantId, string code, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            return await _dbSet
                .FirstOrDefaultAsync(b => b.TenantId == tenantGuid && b.Code.ToLower() == code.ToLower(), cancellationToken);
        }

        public async Task<bool> IsCodeAvailableAsync(string tenantId, string code, Guid? excludeBranchId = null, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            var query = _dbSet.Where(b => b.TenantId == tenantGuid && b.Code.ToLower() == code.ToLower());
            
            if (excludeBranchId.HasValue)
            {
                query = query.Where(b => b.Id != excludeBranchId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }

        public override async Task<IEnumerable<Branch>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            return await _dbSet
                .Where(b => b.TenantId == tenantGuid)
                .ToListAsync(cancellationToken);
        }

        public override async Task<IEnumerable<Branch>> GetByTenantAndBranchAsync(string tenantId, string branchId, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            var branchGuid = Guid.Parse(branchId);
            return await _dbSet
                .Where(b => b.TenantId == tenantGuid && b.Id == branchGuid)
                .ToListAsync(cancellationToken);
        }

        public override async Task<Branch?> GetByTenantAndIdAsync(string tenantId, object id, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            var branchGuid = Guid.Parse(id.ToString());
            return await _dbSet
                .FirstOrDefaultAsync(b => b.TenantId == tenantGuid && b.Id == branchGuid, cancellationToken);
        }
    }
}
