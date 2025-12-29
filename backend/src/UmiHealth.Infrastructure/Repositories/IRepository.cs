using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;

namespace UmiHealth.Infrastructure.Repositories
{
    public interface IRepository<T> : UmiHealth.Core.Interfaces.IRepository<T> where T : BaseEntity
    {
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    }

    public interface ITenantRepository<T> : UmiHealth.Core.Interfaces.ITenantRepository<T> where T : TenantEntity
    {
        Task<IReadOnlyList<T>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);
    }
}
