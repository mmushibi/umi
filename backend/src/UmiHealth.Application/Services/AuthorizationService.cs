using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;

namespace UmiHealth.Application.Services
{
    public interface IAuthorizationService
    {
        Task<bool> CanAccessResourceAsync(ClaimsPrincipal user, string resource, string action);
        Task<bool> CanAccessBranchAsync(ClaimsPrincipal user, Guid branchId);
        Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission);
        Task<List<string>> GetUserPermissionsAsync(ClaimsPrincipal user);
        Task<bool> IsInRoleAsync(ClaimsPrincipal user, string role);
        Task<List<Guid>> GetAccessibleBranchesAsync(ClaimsPrincipal user);
        Task<bool> CanCrossBranchAccessAsync(ClaimsPrincipal user);
    }

    public class AuthorizationService : IAuthorizationService
    {
        private readonly IAuthenticationService _authService;

        public AuthorizationService(IAuthenticationService authService)
        {
            _authService = authService;
        }

        public async Task<bool> CanAccessResourceAsync(ClaimsPrincipal user, string resource, string action)
        {
            var permissions = await GetUserPermissionsAsync(user);
            var permission = $"{resource}:{action}";
            
            // Check for exact permission match
            if (permissions.Contains(permission))
                return true;

            // Check for wildcard permissions
            var wildcardPermissions = permissions.Where(p => p.EndsWith("*"));
            foreach (var wildcard in wildcardPermissions)
            {
                var prefix = wildcard.Substring(0, wildcard.Length - 1);
                if (permission.StartsWith(prefix))
                    return true;
            }

            // Check for system-wide access
            if (permissions.Contains("system:*"))
                return true;

            return false;
        }

        public async Task<bool> CanAccessBranchAsync(ClaimsPrincipal user, Guid branchId)
        {
            // Super Admin can access all branches
            if (await IsInRoleAsync(user, "super_admin"))
                return true;

            // Admin can access all branches within their tenant
            if (await IsInRoleAsync(user, "admin"))
            {
                var userTenantId = user.FindFirst("tenant_id")?.Value;
                var targetUser = await _authService.GetUserByIdAsync(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (targetUser != null && targetUser.TenantId.ToString() == userTenantId)
                    return true;
            }

            // Check explicit branch access
            var accessibleBranches = await GetAccessibleBranchesAsync(user);
            return accessibleBranches.Contains(branchId);
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
        {
            var permissions = await GetUserPermissionsAsync(user);
            
            // Check exact permission
            if (permissions.Contains(permission))
                return true;

            // Check wildcard permissions
            var wildcardPermissions = permissions.Where(p => p.EndsWith("*"));
            foreach (var wildcard in wildcardPermissions)
            {
                var prefix = wildcard.Substring(0, wildcard.Length - 1);
                if (permission.StartsWith(prefix))
                    return true;
            }

            return false;
        }

        public async Task<List<string>> GetUserPermissionsAsync(ClaimsPrincipal user)
        {
            var permissions = new List<string>();
            
            // Get role-based permissions
            var role = user.FindFirst(ClaimTypes.Role)?.Value?.ToLower();
            if (!string.IsNullOrEmpty(role))
            {
                permissions.AddRange(GetRolePermissions(role));
            }

            // Get custom permissions from claims
            var permissionClaims = user.FindAll("permission");
            permissions.AddRange(permissionClaims.Select(c => c.Value));

            // Get permissions from user profile (stored as JSON)
            var permissionsClaim = user.FindFirst("permissions")?.Value;
            if (!string.IsNullOrEmpty(permissionsClaim))
            {
                try
                {
                    var customPermissions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(permissionsClaim);
                    if (customPermissions != null)
                    {
                        permissions.AddRange(customPermissions.Keys.Select(k => k.ToString()));
                    }
                }
                catch
                {
                    // Ignore malformed permissions
                }
            }

            return await Task.FromResult(permissions.Distinct().ToList());
        }

        public async Task<bool> IsInRoleAsync(ClaimsPrincipal user, string role)
        {
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value?.ToLower();
            return await Task.FromResult(userRole == role.ToLower());
        }

        public async Task<List<Guid>> GetAccessibleBranchesAsync(ClaimsPrincipal user)
        {
            var accessibleBranches = new List<Guid>();

            // Super Admin can access all branches (theoretically)
            if (await IsInRoleAsync(user, "super_admin"))
            {
                // In practice, this would return all branches in the system
                return await Task.FromResult(accessibleBranches);
            }

            // Admin can access all branches within their tenant
            if (await IsInRoleAsync(user, "admin"))
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var userData = await _authService.GetUserByIdAsync(userId);
                    if (userData != null)
                    {
                        // Return all branches for the tenant
                        // This would require a branch service to get all tenant branches
                        accessibleBranches.Add(userData.BranchId ?? Guid.Empty);
                        if (userData.BranchAccess != null)
                        {
                            accessibleBranches.AddRange(userData.BranchAccess);
                        }
                    }
                }
                return await Task.FromResult(accessibleBranches.Distinct().ToList());
            }

            // Get branch access from claims
            var branchAccessClaim = user.FindFirst("branch_access")?.Value;
            if (!string.IsNullOrEmpty(branchAccessClaim))
            {
                try
                {
                    var branchIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(branchAccessClaim);
                    if (branchIds != null)
                    {
                        accessibleBranches.AddRange(branchIds);
                    }
                }
                catch
                {
                    // Ignore malformed branch access
                }
            }

            // Add current branch
            var currentBranchId = user.FindFirst("branch_id")?.Value;
            if (!string.IsNullOrEmpty(currentBranchId) && Guid.TryParse(currentBranchId, out var branchId))
            {
                accessibleBranches.Add(branchId);
            }

            return await Task.FromResult(accessibleBranches.Distinct().ToList());
        }

        public async Task<bool> CanCrossBranchAccessAsync(ClaimsPrincipal user)
        {
            // Super Admin and Admin can cross-branch access
            if (await IsInRoleAsync(user, "super_admin") || await IsInRoleAsync(user, "admin"))
                return true;

            // Check if user has explicit cross-branch permission
            return await HasPermissionAsync(user, "branches:cross_access");
        }

        private List<string> GetRolePermissions(string role)
        {
            return role switch
            {
                "super_admin" => new List<string>
                {
                    "system:*", "tenant:*", "user:*", "inventory:*", "reports:*", 
                    "pos:*", "prescriptions:*", "patients:*", "subscriptions:*", 
                    "branches:*", "operations:*"
                },
                "admin" => new List<string>
                {
                    "tenant:manage", "tenant:read", "user:*", "inventory:*", "reports:*", 
                    "pos:*", "prescriptions:*", "patients:*", "branches:*"
                },
                "pharmacist" => new List<string>
                {
                    "patients:*", "prescriptions:*", "inventory:read", "inventory:write",
                    "reports:read", "pos:read", "branches:read"
                },
                "cashier" => new List<string>
                {
                    "pos:*", "patients:read", "inventory:read", "reports:sales", "branches:read"
                },
                "operations" => new List<string>
                {
                    "tenant:create", "tenant:read", "subscriptions:*", "system:monitor", 
                    "reports:*", "branches:read"
                },
                _ => new List<string>()
            };
        }
    }

    // Permission constants for easy reference
    public static class Permissions
    {
        // System permissions
        public const string SYSTEM_ALL = "system:*";
        public const string SYSTEM_MONITOR = "system:monitor";

        // Tenant permissions
        public const string TENANT_ALL = "tenant:*";
        public const string TENANT_CREATE = "tenant:create";
        public const string TENANT_READ = "tenant:read";
        public const string TENANT_MANAGE = "tenant:manage";

        // User permissions
        public const string USER_ALL = "user:*";
        public const string USER_CREATE = "user:create";
        public const string USER_READ = "user:read";
        public const string USER_UPDATE = "user:update";
        public const string USER_DELETE = "user:delete";

        // Inventory permissions
        public const string INVENTORY_ALL = "inventory:*";
        public const string INVENTORY_READ = "inventory:read";
        public const string INVENTORY_WRITE = "inventory:write";

        // Reports permissions
        public const string REPORTS_ALL = "reports:*";
        public const string REPORTS_READ = "reports:read";
        public const string REPORTS_SALES = "reports:sales";

        // POS permissions
        public const string POS_ALL = "pos:*";
        public const string POS_READ = "pos:read";

        // Prescriptions permissions
        public const string PRESCRIPTIONS_ALL = "prescriptions:*";

        // Patients permissions
        public const string PATIENTS_ALL = "patients:*";
        public const string PATIENTS_READ = "patients:read";

        // Subscriptions permissions
        public const string SUBSCRIPTIONS_ALL = "subscriptions:*";

        // Branches permissions
        public const string BRANCHES_ALL = "branches:*";
        public const string BRANCHES_READ = "branches:read";
        public const string BRANCHES_CROSS_ACCESS = "branches:cross_access";

        // Operations permissions
        public const string OPERATIONS_ALL = "operations:*";
    }

    // Role constants for easy reference
    public static class Roles
    {
        public const string SUPER_ADMIN = "super_admin";
        public const string ADMIN = "admin";
        public const string PHARMACIST = "pharmacist";
        public const string CASHIER = "cashier";
        public const string OPERATIONS = "operations";
    }
}
