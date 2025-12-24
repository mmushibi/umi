using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface IBranchAccessService
    {
        Task<bool> CanAccessBranchAsync(ClaimsPrincipal user, Guid branchId);
        Task<List<Guid>> GetAccessibleBranchesAsync(ClaimsPrincipal user);
        Task<IQueryable<T>> FilterByBranchAsync<T>(ClaimsPrincipal user, IQueryable<T> query) where T : class, IBranchEntity;
        Task<Guid?> GetCurrentUserBranchAsync(ClaimsPrincipal user);
        Task<bool> AssignUserToBranchAsync(Guid userId, Guid branchId, string role);
        Task<bool> RemoveUserFromBranchAsync(Guid userId, Guid branchId);
        Task<List<User>> GetBranchUsersAsync(Guid branchId);
        Task<bool> UpdateUserBranchAccessAsync(Guid userId, List<Guid> branchIds);
    }

    public class BranchAccessService : IBranchAccessService
    {
        private readonly SharedDbContext _context;
        private readonly IAuthorizationService _authService;

        public BranchAccessService(SharedDbContext context, IAuthorizationService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<bool> CanAccessBranchAsync(ClaimsPrincipal user, Guid branchId)
        {
            // Super Admin can access all branches
            if (await _authService.IsInRoleAsync(user, Roles.SUPER_ADMIN))
                return true;

            // Admin can access all branches within their tenant
            if (await _authService.IsInRoleAsync(user, Roles.ADMIN))
            {
                var userTenantId = user.FindFirst("tenant_id")?.Value;
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (!string.IsNullOrEmpty(userTenantId) && !string.IsNullOrEmpty(userId))
                {
                    var userData = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id.ToString() == userId);
                    
                    if (userData != null && userData.TenantId.ToString() == userTenantId)
                    {
                        // Check if the branch belongs to the same tenant
                        var branch = await _context.Branches
                            .FirstOrDefaultAsync(b => b.Id == branchId && b.TenantId == userData.TenantId);
                        return branch != null;
                    }
                }
            }

            // Check explicit branch access
            var accessibleBranches = await GetAccessibleBranchesAsync(user);
            return accessibleBranches.Contains(branchId);
        }

        public async Task<List<Guid>> GetAccessibleBranchesAsync(ClaimsPrincipal user)
        {
            var accessibleBranches = new List<Guid>();

            // Super Admin - all branches (in practice, would need to implement system-wide branch listing)
            if (await _authService.IsInRoleAsync(user, Roles.SUPER_ADMIN))
            {
                return await Task.FromResult(accessibleBranches);
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return accessibleBranches;

            var userData = await _context.Users
                .Include(u => u.BranchAccessList)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (userData == null)
                return accessibleBranches;

            // Admin - all branches in tenant
            if (await _authService.IsInRoleAsync(user, Roles.ADMIN))
            {
                var tenantBranches = await _context.Branches
                    .Where(b => b.TenantId == userData.TenantId)
                    .Select(b => b.Id)
                    .ToListAsync();
                
                accessibleBranches.AddRange(tenantBranches);
                return await Task.FromResult(accessibleBranches.Distinct().ToList());
            }

            // Other roles - explicit branch access + current branch
            if (userData.BranchId.HasValue)
                accessibleBranches.Add(userData.BranchId.Value);

            if (userData.BranchAccessList != null)
                accessibleBranches.AddRange(userData.BranchAccessList);

            return await Task.FromResult(accessibleBranches.Distinct().ToList());
        }

        public async Task<IQueryable<T>> FilterByBranchAsync<T>(ClaimsPrincipal user, IQueryable<T> query) where T : class, IBranchEntity
        {
            var accessibleBranches = await GetAccessibleBranchesAsync(user);

            // If user can access all branches (Super Admin), return original query
            if (await _authService.IsInRoleAsync(user, Roles.SUPER_ADMIN))
                return query;

            // If no accessible branches, return empty query
            if (!accessibleBranches.Any())
                return query.Take(0);

            // Filter by accessible branches
            return query.Where(e => e.BranchId.HasValue && accessibleBranches.Contains(e.BranchId.Value));
        }

        public async Task<Guid?> GetCurrentUserBranchAsync(ClaimsPrincipal user)
        {
            var branchIdClaim = user.FindFirst("branch_id")?.Value;
            if (string.IsNullOrEmpty(branchIdClaim))
                return null;

            if (Guid.TryParse(branchIdClaim, out var branchId))
                return await Task.FromResult(branchId);

            return null;
        }

        public async Task<bool> AssignUserToBranchAsync(Guid userId, Guid branchId, string role)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.BranchAccessList)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return false;

                var branch = await _context.Branches.FindAsync(branchId);
                if (branch == null)
                    return false;

                // Verify branch belongs to user's tenant
                if (branch.TenantId != user.TenantId)
                    return false;

                // For admin roles, set as primary branch
                if (role == Roles.ADMIN || role == Roles.SUPER_ADMIN)
                {
                    user.BranchId = branchId;
                }
                else
                {
                    // For other roles, add to branch access list
                    if (user.BranchAccessList == null)
                        user.BranchAccessList = new List<Guid>();

                    if (!user.BranchAccessList.Contains(branchId))
                        user.BranchAccessList.Add(branchId);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveUserFromBranchAsync(Guid userId, Guid branchId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.BranchAccessList)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return false;

                // Remove from primary branch if it's the current one
                if (user.BranchId == branchId)
                    user.BranchId = null;

                // Remove from branch access list
                if (user.BranchAccessList != null && user.BranchAccessList.Contains(branchId))
                {
                    user.BranchAccessList.Remove(branchId);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<User>> GetBranchUsersAsync(Guid branchId)
        {
            return await _context.Users
                .Include(u => u.BranchAccessList)
                .Where(u => u.BranchId == branchId || (u.BranchAccessList != null && u.BranchAccessList.Contains(branchId)))
                .ToListAsync();
        }

        public async Task<bool> UpdateUserBranchAccessAsync(Guid userId, List<Guid> branchIds)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.BranchAccessList)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return false;

                // Verify all branches belong to user's tenant
                var branches = await _context.Branches
                    .Where(b => branchIds.Contains(b.Id) && b.TenantId == user.TenantId)
                    .ToListAsync();

                if (branches.Count != branchIds.Count)
                    return false;

                // Update branch access list
                user.BranchAccessList = branchIds;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    // Interface for entities that have branch context
    public interface IBranchEntity
    {
        Guid? BranchId { get; set; }
    }

    // Extension methods for branch filtering
    public static class BranchAccessExtensions
    {
        public static async Task<IQueryable<T>> FilterByCurrentUserBranch<T>(
            this IQueryable<T> query, 
            ClaimsPrincipal user, 
            IBranchAccessService branchAccessService) 
            where T : class, IBranchEntity
        {
            return await branchAccessService.FilterByBranchAsync(user, query);
        }

        public static async Task<bool> EnsureBranchAccessAsync(
            this ClaimsPrincipal user, 
            Guid branchId, 
            IBranchAccessService branchAccessService)
        {
            return await branchAccessService.CanAccessBranchAsync(user, branchId);
        }
    }
}
