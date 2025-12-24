using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface IBranchPermissionService
    {
        Task<BranchPermission> GrantPermissionAsync(GrantPermissionRequest request);
        Task<bool> RevokePermissionAsync(Guid permissionId);
        Task<IEnumerable<BranchPermission>> GetUserPermissionsAsync(Guid userId);
        Task<IEnumerable<BranchPermission>> GetBranchPermissionsAsync(Guid branchId);
        Task<bool> HasPermissionAsync(Guid userId, Guid branchId, string permission);
        Task<bool> HasAnyPermissionAsync(Guid userId, Guid branchId, List<string> permissions);
        Task<bool> IsBranchManagerAsync(Guid userId, Guid branchId);
        Task<bool> CanTransferStockAsync(Guid userId, Guid branchId);
        Task<bool> CanApproveTransfersAsync(Guid userId, Guid branchId);
        Task<bool> CanViewReportsAsync(Guid userId, Guid branchId);
        Task<bool> CanManageUsersAsync(Guid userId, Guid branchId);
        Task<Dictionary<string, object>> GetUserPermissionSummaryAsync(Guid userId);
        Task<bool> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request);
    }

    public class BranchPermissionService : IBranchPermissionService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<BranchPermissionService> _logger;

        public BranchPermissionService(
            SharedDbContext context,
            ILogger<BranchPermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BranchPermission> GrantPermissionAsync(GrantPermissionRequest request)
        {
            // Check if permission already exists
            var existingPermission = await _context.BranchPermissions
                .FirstOrDefaultAsync(bp => bp.UserId == request.UserId && 
                                         bp.BranchId == request.BranchId);

            if (existingPermission != null)
            {
                // Update existing permission
                existingPermission.Permissions = request.Permissions.Distinct().ToList();
                existingPermission.IsManager = request.IsManager;
                existingPermission.CanTransferStock = request.CanTransferStock;
                existingPermission.CanApproveTransfers = request.CanApproveTransfers;
                existingPermission.CanViewReports = request.CanViewReports;
                existingPermission.CanManageUsers = request.CanManageUsers;
                existingPermission.Restrictions = request.Restrictions ?? new Dictionary<string, object>();
                existingPermission.ExpiresAt = request.ExpiresAt;
                existingPermission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return existingPermission;
            }

            var permission = new BranchPermission
            {
                Id = Guid.NewGuid(),
                TenantId = _context.GetCurrentTenantId(),
                UserId = request.UserId,
                BranchId = request.BranchId,
                Permissions = request.Permissions.Distinct().ToList(),
                IsManager = request.IsManager,
                CanTransferStock = request.CanTransferStock,
                CanApproveTransfers = request.CanApproveTransfers,
                CanViewReports = request.CanViewReports,
                CanManageUsers = request.CanManageUsers,
                Restrictions = request.Restrictions ?? new Dictionary<string, object>(),
                ExpiresAt = request.ExpiresAt,
                GrantedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BranchPermissions.Add(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission granted to user {UserId} for branch {BranchId}", 
                request.UserId, request.BranchId);

            return permission;
        }

        public async Task<bool> RevokePermissionAsync(Guid permissionId)
        {
            var permission = await _context.BranchPermissions.FindAsync(permissionId);
            if (permission == null)
                return false;

            _context.BranchPermissions.Remove(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission {PermissionId} revoked", permissionId);
            return true;
        }

        public async Task<IEnumerable<BranchPermission>> GetUserPermissionsAsync(Guid userId)
        {
            return await _context.BranchPermissions
                .Include(bp => bp.User)
                .Include(bp => bp.Branch)
                .Where(bp => bp.UserId == userId && 
                           (bp.ExpiresAt == null || bp.ExpiresAt > DateTime.UtcNow))
                .ToListAsync();
        }

        public async Task<IEnumerable<BranchPermission>> GetBranchPermissionsAsync(Guid branchId)
        {
            return await _context.BranchPermissions
                .Include(bp => bp.User)
                .Include(bp => bp.Branch)
                .Where(bp => bp.BranchId == branchId && 
                           (bp.ExpiresAt == null || bp.ExpiresAt > DateTime.UtcNow))
                .ToListAsync();
        }

        public async Task<bool> HasPermissionAsync(Guid userId, Guid branchId, string permission)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            var branchPermission = userPermissions.FirstOrDefault(bp => bp.BranchId == branchId);

            if (branchPermission == null)
                return false;

            // Check if permission is explicitly granted
            if (branchPermission.Permissions.Contains(permission))
                return true;

            // Check if user is manager (managers have all permissions)
            if (branchPermission.IsManager)
                return true;

            // Check for wildcard permissions
            if (branchPermission.Permissions.Contains("*") || branchPermission.Permissions.Contains("all"))
                return true;

            return false;
        }

        public async Task<bool> HasAnyPermissionAsync(Guid userId, Guid branchId, List<string> permissions)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            var branchPermission = userPermissions.FirstOrDefault(bp => bp.BranchId == branchId);

            if (branchPermission == null)
                return false;

            // Check if user is manager
            if (branchPermission.IsManager)
                return true;

            // Check for wildcard permissions
            if (branchPermission.Permissions.Contains("*") || branchPermission.Permissions.Contains("all"))
                return true;

            // Check if any of the requested permissions are granted
            return permissions.Any(p => branchPermission.Permissions.Contains(p));
        }

        public async Task<bool> IsBranchManagerAsync(Guid userId, Guid branchId)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            var branchPermission = userPermissions.FirstOrDefault(bp => bp.BranchId == branchId);

            return branchPermission?.IsManager ?? false;
        }

        public async Task<bool> CanTransferStockAsync(Guid userId, Guid branchId)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            var branchPermission = userPermissions.FirstOrDefault(bp => bp.BranchId == branchId);

            if (branchPermission == null)
                return false;

            return branchPermission.IsManager || 
                   branchPermission.CanTransferStock || 
                   branchPermission.Permissions.Contains("stock_transfer");
        }

        public async Task<bool> CanApproveTransfersAsync(Guid userId, Guid branchId)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            var branchPermission = userPermissions.FirstOrDefault(bp => bp.BranchId == branchId);

            if (branchPermission == null)
                return false;

            return branchPermission.IsManager || 
                   branchPermission.CanApproveTransfers || 
                   branchPermission.Permissions.Contains("approve_transfers");
        }

        public async Task<bool> CanViewReportsAsync(Guid userId, Guid branchId)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            var branchPermission = userPermissions.FirstOrDefault(bp => bp.BranchId == branchId);

            if (branchPermission == null)
                return false;

            return branchPermission.IsManager || 
                   branchPermission.CanViewReports || 
                   branchPermission.Permissions.Contains("view_reports");
        }

        public async Task<bool> CanManageUsersAsync(Guid userId, Guid branchId)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            var branchPermission = userPermissions.FirstOrDefault(bp => bp.BranchId == branchId);

            if (branchPermission == null)
                return false;

            return branchPermission.IsManager || 
                   branchPermission.CanManageUsers || 
                   branchPermission.Permissions.Contains("manage_users");
        }

        public async Task<Dictionary<string, object>> GetUserPermissionSummaryAsync(Guid userId)
        {
            var userPermissions = await GetUserPermissionsAsync(userId);
            var user = await _context.Users.FindAsync(userId);

            var branchPermissions = new List<Dictionary<string, object>>();
            var allPermissions = new HashSet<string>();

            foreach (var permission in userPermissions)
            {
                allPermissions.UnionWith(permission.Permissions);

                branchPermissions.Add(new Dictionary<string, object>
                {
                    ["branch_id"] = permission.BranchId,
                    ["branch_name"] = permission.Branch?.Name,
                    ["is_manager"] = permission.IsManager,
                    ["can_transfer_stock"] = permission.CanTransferStock,
                    ["can_approve_transfers"] = permission.CanApproveTransfers,
                    ["can_view_reports"] = permission.CanViewReports,
                    ["can_manage_users"] = permission.CanManageUsers,
                    ["permissions"] = permission.Permissions,
                    ["expires_at"] = permission.ExpiresAt,
                    ["granted_at"] = permission.GrantedAt
                });
            }

            return new Dictionary<string, object>
            {
                ["user_id"] = userId,
                ["user_name"] = $"{user?.FirstName} {user?.LastName}",
                ["user_email"] = user?.Email,
                ["user_role"] = user?.Role,
                ["total_branches"] = userPermissions.Count,
                ["branch_permissions"] = branchPermissions,
                ["all_permissions"] = allPermissions.ToList(),
                ["is_manager_anywhere"] = userPermissions.Any(bp => bp.IsManager),
                ["can_transfer_stock_anywhere"] = userPermissions.Any(bp => bp.CanTransferStock),
                ["can_approve_transfers_anywhere"] = userPermissions.Any(bp => bp.CanApproveTransfers),
                ["can_view_reports_anywhere"] = userPermissions.Any(bp => bp.CanViewReports),
                ["can_manage_users_anywhere"] = userPermissions.Any(bp => bp.CanManageUsers)
            };
        }

        public async Task<bool> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request)
        {
            var permission = await _context.BranchPermissions.FindAsync(permissionId);
            if (permission == null)
                return false;

            if (request.Permissions != null)
                permission.Permissions = request.Permissions.Distinct().ToList();

            if (request.IsManager.HasValue)
                permission.IsManager = request.IsManager.Value;

            if (request.CanTransferStock.HasValue)
                permission.CanTransferStock = request.CanTransferStock.Value;

            if (request.CanApproveTransfers.HasValue)
                permission.CanApproveTransfers = request.CanApproveTransfers.Value;

            if (request.CanViewReports.HasValue)
                permission.CanViewReports = request.CanViewReports.Value;

            if (request.CanManageUsers.HasValue)
                permission.CanManageUsers = request.CanManageUsers.Value;

            if (request.Restrictions != null)
                permission.Restrictions = request.Restrictions;

            if (request.ExpiresAt.HasValue)
                permission.ExpiresAt = request.ExpiresAt.Value;

            permission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission {PermissionId} updated", permissionId);
            return true;
        }
    }

    // DTOs for permission operations
    public class GrantPermissionRequest
    {
        public Guid UserId { get; set; }
        public Guid BranchId { get; set; }
        public List<string> Permissions { get; set; } = new();
        public bool IsManager { get; set; } = false;
        public bool CanTransferStock { get; set; } = false;
        public bool CanApproveTransfers { get; set; } = false;
        public bool CanViewReports { get; set; } = false;
        public bool CanManageUsers { get; set; } = false;
        public Dictionary<string, object>? Restrictions { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class UpdatePermissionRequest
    {
        public List<string>? Permissions { get; set; }
        public bool? IsManager { get; set; }
        public bool? CanTransferStock { get; set; }
        public bool? CanApproveTransfers { get; set; }
        public bool? CanViewReports { get; set; }
        public bool? CanManageUsers { get; set; }
        public Dictionary<string, object>? Restrictions { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    // Permission constants for consistency
    public static class BranchPermissions
    {
        public const string INVENTORY_READ = "inventory_read";
        public const string INVENTORY_WRITE = "inventory_write";
        public const string INVENTORY_DELETE = "inventory_delete";
        public const string SALES_READ = "sales_read";
        public const string SALES_WRITE = "sales_write";
        public const string SALES_DELETE = "sales_delete";
        public const string PATIENTS_READ = "patients_read";
        public const string PATIENTS_WRITE = "patients_write";
        public const string PATIENTS_DELETE = "patients_delete";
        public const string PRESCRIPTIONS_READ = "prescriptions_read";
        public const string PRESCRIPTIONS_WRITE = "prescriptions_write";
        public const string PRESCRIPTIONS_DELETE = "prescriptions_delete";
        public const string STOCK_TRANSFER = "stock_transfer";
        public const string APPROVE_TRANSFERS = "approve_transfers";
        public const string VIEW_REPORTS = "view_reports";
        public const string MANAGE_USERS = "manage_users";
        public const string MANAGE_SETTINGS = "manage_settings";
        public const string PROCUREMENT_READ = "procurement_read";
        public const string PROCUREMENT_WRITE = "procurement_write";
        public const string PROCUREMENT_APPROVE = "procurement_approve";
        public const string ALL = "*";
    }
}
