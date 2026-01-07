using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealth.Core.Entities;
using UmiHealth.Persistence;

namespace UmiHealth.Api.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, UmiHealthDbContext dbContext)
        {
            // Extract tenant information from JWT token or subdomain
            var tenantId = ExtractTenantId(context);
            var userId = ExtractUserId(context);
            var userRole = ExtractUserRole(context);
            var branchId = ExtractBranchId(context);

            if (tenantId.HasValue)
            {
                // Validate tenant exists and is active
                var tenant = await dbContext.Tenants
                    .FirstOrDefaultAsync(t => t.Id == tenantId.Value && t.IsActive);

                if (tenant == null)
                {
                    _logger.LogWarning("Invalid or inactive tenant: {TenantId}", tenantId.Value);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid tenant");
                    return;
                }

                // Set tenant context in the database
                await dbContext.BeginTenantTransactionAsync(
                    tenantId.Value, 
                    userId ?? Guid.Empty, 
                    userRole ?? string.Empty, 
                    branchId);

                // Add tenant information to HttpContext for later use
                context.Items["TenantId"] = tenantId.Value;
                context.Items["UserId"] = userId;
                context.Items["UserRole"] = userRole;
                context.Items["BranchId"] = branchId;
                context.Items["Tenant"] = tenant;

                _logger.LogDebug("Set tenant context: {TenantId}, User: {UserId}, Role: {UserRole}, Branch: {BranchId}",
                    tenantId.Value, userId, userRole, branchId);
            }

            try
            {
                await _next(context);
            }
            finally
            {
                // Clear tenant context after request
                if (tenantId.HasValue)
                {
                    await dbContext.ClearTenantContextAsync();
                }
            }
        }

        private Guid? ExtractTenantId(HttpContext context)
        {
            // First try to get from JWT token claims
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = context.User.FindFirst("tenant_id");
                if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
                {
                    return tenantId;
                }
            }

            // Try to get from subdomain
            var host = context.Request.Host.Host;
            if (!string.IsNullOrEmpty(host))
            {
                var parts = host.Split('.');
                if (parts.Length > 2)
                {
                    var subdomain = parts[0];
                    // You could map subdomain to tenant here
                    // For now, we'll rely on JWT token
                }
            }

            return null;
        }

        private Guid? ExtractUserId(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var nameIdentifierClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (nameIdentifierClaim != null && Guid.TryParse(nameIdentifierClaim.Value, out var userId))
                {
                    return userId;
                }
            }
            return null;
        }

        private string? ExtractUserRole(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                return context.User.FindFirst(ClaimTypes.Role)?.Value;
            }
            return null;
        }

        private Guid? ExtractBranchId(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var branchClaim = context.User.FindFirst("branch_id");
                if (branchClaim != null && Guid.TryParse(branchClaim.Value, out var branchId))
                {
                    return branchId;
                }
            }
            return null;
        }
    }

    // Extension method to register the middleware
    public static class TenantMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantMiddleware>();
        }
    }
}
