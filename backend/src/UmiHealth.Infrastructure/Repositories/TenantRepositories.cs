using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Core.Entities;
using Tenant = UmiHealth.Domain.Entities.Tenant;
using UmiHealth.Persistence.Data;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Infrastructure.Repositories
{
    public interface ITenantRepository : ITenantRepository<UmiHealth.Domain.Entities.Tenant>
    {
        Task<UmiHealth.Domain.Entities.Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
        Task<UmiHealth.Domain.Entities.Tenant?> GetByDatabaseNameAsync(string databaseName, CancellationToken cancellationToken = default);
        Task<IEnumerable<UmiHealth.Domain.Entities.Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);
        Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTenantId = null, CancellationToken cancellationToken = default);
    }

    public class TenantRepository : AppRepository<UmiHealth.Domain.Entities.Tenant>, ITenantRepository
    {
        public TenantRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UmiHealth.Domain.Entities.Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Subdomain.ToLower() == subdomain.ToLower(), cancellationToken);
        }

        public async Task<UmiHealth.Domain.Entities.Tenant?> GetByDatabaseNameAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.DatabaseName == databaseName, cancellationToken);
        }

        public async Task<IEnumerable<UmiHealth.Domain.Entities.Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
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

        // Implement missing ITenantRepository methods
        public async Task<IReadOnlyList<UmiHealth.Domain.Entities.Tenant>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var result = await _dbSet
                .Where(t => t.Id == tenantId)
                .ToListAsync(cancellationToken);
            return result.AsReadOnly();
        }

        public async Task<UmiHealth.Domain.Entities.Tenant?> GetByIdAndTenantAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, cancellationToken);
        }

        public async Task<IReadOnlyList<UmiHealth.Domain.Entities.Tenant>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
        {
            var result = await _dbSet
                .Where(t => t.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            return result.AsReadOnly();
        }

        public async Task<IEnumerable<UmiHealth.Domain.Entities.Tenant>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            return await _dbSet
                .Where(t => t.Id == tenantGuid)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateRangeAsync(IEnumerable<UmiHealth.Domain.Entities.Tenant> entities, CancellationToken cancellationToken = default)
        {
            _dbSet.UpdateRange(entities);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public interface IUserRepository : ITenantRepository<UmiHealth.Core.Entities.User>
    {
        Task<UmiHealth.Core.Entities.User?> GetByEmailAsync(string tenantId, string email, CancellationToken cancellationToken = default);
        Task<UmiHealth.Core.Entities.User?> GetByUsernameAsync(string tenantId, string username, CancellationToken cancellationToken = default);
        Task<IEnumerable<UmiHealth.Core.Entities.User>> GetByBranchAsync(string tenantId, string branchId, CancellationToken cancellationToken = default);
        Task<IEnumerable<UmiHealth.Core.Entities.User>> GetByRoleAsync(string tenantId, string role, CancellationToken cancellationToken = default);
        Task<bool> IsEmailAvailableAsync(string tenantId, string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
    }

    public class UserRepository : AppRepository<UmiHealth.Core.Entities.User>, ITenantRepository<UmiHealth.Core.Entities.User>
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UmiHealth.Core.Entities.User?> GetByEmailAsync(string tenantId, string email, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            return await _dbSet
                .FirstOrDefaultAsync(u => u.TenantId == tenantGuid && u.Email.ToLower() == email.ToLower(), cancellationToken);
        }

        public async Task<UmiHealth.Core.Entities.User?> GetByUsernameAsync(string tenantId, string username, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            return await _dbSet
                .FirstOrDefaultAsync(u => u.TenantId == tenantGuid && u.UserName != null && u.UserName.ToLower() == username.ToLower(), cancellationToken);
        }

        public async Task<IEnumerable<UmiHealth.Core.Entities.User>> GetByBranchAsync(string tenantId, string branchId, CancellationToken cancellationToken = default)
        {
            var tenantGuid = Guid.Parse(tenantId);
            var branchGuid = Guid.Parse(branchId);
            return await _dbSet
                .Where(u => u.TenantId == tenantGuid && u.BranchId == branchGuid)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<UmiHealth.Core.Entities.User>> GetByRoleAsync(string tenantId, string role, CancellationToken cancellationToken = default)
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

        // Implement missing ITenantRepository methods
        public async Task<IReadOnlyList<UmiHealth.Core.Entities.User>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var result = await _dbSet
                .Where(u => u.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            return result.AsReadOnly();
        }

        public async Task<UmiHealth.Core.Entities.User?> GetByIdAndTenantAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, cancellationToken);
        }

        public async Task<IReadOnlyList<UmiHealth.Core.Entities.User>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
        {
            var result = await _dbSet
                .Where(u => u.TenantId == tenantId && u.BranchId == branchId)
                .ToListAsync(cancellationToken);
            return result.AsReadOnly();
        }

        public async Task UpdateRangeAsync(IEnumerable<UmiHealth.Core.Entities.User> entities, CancellationToken cancellationToken = default)
        {
            _dbSet.UpdateRange(entities);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public interface IBranchRepository : ITenantRepository<UmiHealth.Core.Entities.Branch>
    {
        Task<UmiHealth.Core.Entities.Branch?> GetByCodeAsync(string tenantId, string code, CancellationToken cancellationToken = default);
        Task<bool> IsCodeAvailableAsync(string tenantId, string code, Guid? excludeBranchId = null, CancellationToken cancellationToken = default);
    }

    public class BranchRepository : Repository<UmiHealth.Core.Entities.Branch>, ITenantRepository<UmiHealth.Core.Entities.Branch>
    {
        public BranchRepository(SharedDbContext context) : base(context)
        {
        }

        public async Task<UmiHealth.Core.Entities.Branch?> GetByCodeAsync(string tenantId, string code, CancellationToken cancellationToken = default)
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

        // Implement missing ITenantRepository methods
        public async Task<IReadOnlyList<UmiHealth.Core.Entities.Branch>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var result = await _dbSet
                .Where(b => b.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            return result.AsReadOnly();
        }

        public async Task<UmiHealth.Core.Entities.Branch?> GetByIdAndTenantAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId, cancellationToken);
        }

        public async Task<IReadOnlyList<UmiHealth.Core.Entities.Branch>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
        {
            var result = await _dbSet
                .Where(b => b.TenantId == tenantId && b.Id == branchId)
                .ToListAsync(cancellationToken);
            return result.AsReadOnly();
        }

        public async Task UpdateRangeAsync(IEnumerable<UmiHealth.Core.Entities.Branch> entities, CancellationToken cancellationToken = default)
        {
            _dbSet.UpdateRange(entities);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
