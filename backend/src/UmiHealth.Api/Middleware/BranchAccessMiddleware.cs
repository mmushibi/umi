using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.API.Middleware
{
    public class BranchAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BranchAccessMiddleware> _logger;

        public BranchAccessMiddleware(RequestDelegate next, ILogger<BranchAccessMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, UmiHealthDbContext dbContext)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.Contains(MetadataKeys.AllowAnonymous) == true)
            {
                await _next(context);
                return;
            }

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            var tenantIdClaim = context.User.FindFirst("TenantId") ?? context.User.FindFirst("tenant_id");
            var branchIdClaim = context.User.FindFirst("BranchId") ?? context.User.FindFirst("branch_id");

            if (userIdClaim != null && tenantIdClaim != null)
            {
                var userId = Guid.Parse(userIdClaim.Value);
                var tenantId = Guid.Parse(tenantIdClaim.Value);
                var branchId = branchIdClaim?.Value != null ? Guid.Parse(branchIdClaim.Value) : (Guid?)null;

                if (branchId.HasValue)
                {
                    var hasBranchAccess = await dbContext.Users
                        .AnyAsync(u => u.Id == userId &&
                                       u.TenantId == tenantId &&
                                       (u.BranchId == branchId.Value || u.BranchId == null));

                    if (!hasBranchAccess)
                    {
                        _logger.LogWarning("User {UserId} attempted to access branch {BranchId} without permission", userId, branchId);
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsJsonAsync(new {
                            success = false,
                            message = "Access denied: You don't have permission to access this branch"
                        });
                        return;
                    }
                }

                context.Response.Headers.Add("X-User-Branch-Id", branchId?.ToString() ?? "");
                context.Response.Headers.Add("X-User-Tenant-Id", tenantId.ToString());
            }

            await _next(context);
        }
    }

    public static class MetadataKeys
    {
        public const string AllowAnonymous = "AllowAnonymous";
    }
}