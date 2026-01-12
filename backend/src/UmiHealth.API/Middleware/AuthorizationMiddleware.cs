using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UmiHealth.API.Middleware
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationMiddleware> _logger;

        public AuthorizationMiddleware(
            RequestDelegate next,
            ILogger<AuthorizationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authorization for health checks and auth endpoints
            if (ShouldSkipAuthorization(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var user = context.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("Unauthorized access attempt for request: {RequestPath}", context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Authentication required");
                return;
            }

            // Check tenant access
            if (!ValidateTenantAccess(context))
            {
                _logger.LogWarning("Tenant access denied for user: {UserId}, tenant: {TenantId}", 
                    context.Items["UserId"], context.Items["TenantId"]);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Tenant access denied");
                return;
            }

            // Check role-based permissions
            if (!ValidateRolePermissions(context))
            {
                _logger.LogWarning("Role-based access denied for user: {UserId}, path: {RequestPath}", 
                    context.Items["UserId"], context.Request.Path);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access denied");
                return;
            }

            // Check branch access if applicable
            if (!ValidateBranchAccess(context))
            {
                _logger.LogWarning("Branch access denied for user: {UserId}, branch: {BranchId}", 
                    context.Items["UserId"], context.Items["BranchId"]);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Branch access denied");
                return;
            }

            await _next(context);
        }

        private bool ShouldSkipAuthorization(string path)
        {
            var skipPaths = new[]
            {
                "/health",
                "/api/v1/auth/login",
                "/api/v1/auth/register",
                "/api/v1/auth/refresh",
                "/api/v1/auth/me",
                "/api/v1/auth/subscription-status",
                "/api/v1/auth/check-setup",
                "/swagger",
                "/swagger-ui",
                "/api-docs"
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase));
        }

        private bool ValidateTenantAccess(HttpContext context)
        {
            // Get tenant from context (set by TenantResolutionMiddleware)
            var tenantId = context.Items["TenantId"]?.ToString();
            if (string.IsNullOrEmpty(tenantId))
            {
                return false;
            }

            // Get tenant from user claims
            var userTenantClaim = context.User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(userTenantClaim))
            {
                return false;
            }

            // Super admins can access any tenant
            var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "SuperAdmin")
            {
                return true;
            }

            // Regular users can only access their own tenant
            return tenantId == userTenantClaim;
        }

        private bool ValidateRolePermissions(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
            var method = context.Request.Method.ToUpper();
            var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userRole))
            {
                return false;
            }

            // Define role-based access rules
            var rolePermissions = GetRolePermissions();

            if (rolePermissions.TryGetValue(userRole, out var permissions))
            {
                return permissions.Any(permission => 
                    path.StartsWith(permission.PathPattern, StringComparison.OrdinalIgnoreCase) &&
                    permission.AllowedMethods.Contains(method));
            }

            return false;
        }

        private bool ValidateBranchAccess(HttpContext context)
        {
            // Get user's branch access from claims
            var branchAccessClaim = context.User.FindFirst("branch_access")?.Value;
            var userBranchId = context.User.FindFirst("branch_id")?.Value;

            // Super admins and users without branch restrictions can access any branch
            var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole == "SuperAdmin" || string.IsNullOrEmpty(branchAccessClaim))
            {
                return true;
            }

            // For branch-specific endpoints, validate access
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
            if (path.Contains("/branch/") || path.Contains("/branches/"))
            {
                var requestedBranchId = ExtractBranchIdFromPath(path);
                if (!string.IsNullOrEmpty(requestedBranchId))
                {
                    var accessibleBranches = branchAccessClaim.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    return accessibleBranches.Contains(requestedBranchId) || 
                           accessibleBranches.Contains("all") || 
                           requestedBranchId == userBranchId;
                }
            }

            return true;
        }

        private string? ExtractBranchIdFromPath(string path)
        {
            // Extract branch ID from URL patterns like /api/v1/branches/{branchId}/...
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i] == "branches" && i + 1 < segments.Length)
                {
                    return segments[i + 1];
                }
            }
            return null;
        }

        private Dictionary<string, List<RolePermission>> GetRolePermissions()
        {
            return new Dictionary<string, List<RolePermission>>
            {
                ["SuperAdmin"] = new List<RolePermission>
                {
                    new RolePermission { PathPattern = "/api/", AllowedMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" } }
                },
                ["Admin"] = new List<RolePermission>
                {
                    new RolePermission { PathPattern = "/api/v1/users", AllowedMethods = new[] { "GET", "POST", "PUT", "DELETE" } },
                    new RolePermission { PathPattern = "/api/v1/inventory", AllowedMethods = new[] { "GET", "POST", "PUT", "DELETE" } },
                    new RolePermission { PathPattern = "/api/v1/patients", AllowedMethods = new[] { "GET", "POST", "PUT", "DELETE" } },
                    new RolePermission { PathPattern = "/api/v1/sales", AllowedMethods = new[] { "GET", "POST", "PUT" } },
                    new RolePermission { PathPattern = "/api/v1/reports", AllowedMethods = new[] { "GET" } }
                },
                ["Pharmacist"] = new List<RolePermission>
                {
                    new RolePermission { PathPattern = "/api/v1/inventory", AllowedMethods = new[] { "GET", "PUT" } },
                    new RolePermission { PathPattern = "/api/v1/prescriptions", AllowedMethods = new[] { "GET", "POST", "PUT" } },
                    new RolePermission { PathPattern = "/api/v1/patients", AllowedMethods = new[] { "GET" } }
                },
                ["Cashier"] = new List<RolePermission>
                {
                    new RolePermission { PathPattern = "/api/v1/sales", AllowedMethods = new[] { "GET", "POST" } },
                    new RolePermission { PathPattern = "/api/v1/payments", AllowedMethods = new[] { "GET", "POST" } },
                    new RolePermission { PathPattern = "/api/v1/patients", AllowedMethods = new[] { "GET", "POST" } }
                },
                ["Operations"] = new List<RolePermission>
                {
                    new RolePermission { PathPattern = "/api/v1/subscriptions", AllowedMethods = new[] { "GET", "POST", "PUT" } },
                    new RolePermission { PathPattern = "/api/v1/tenants", AllowedMethods = new[] { "GET", "PUT" } },
                    new RolePermission { PathPattern = "/api/v1/reports", AllowedMethods = new[] { "GET" } }
                }
            };
        }
    }

    public class RolePermission
    {
        public string PathPattern { get; set; } = string.Empty;
        public string[] AllowedMethods { get; set; } = Array.Empty<string>();
    }
}
