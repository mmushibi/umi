using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Middleware
{
    public class BranchFilterMiddleware
    {
        private readonly RequestDelegate _next;

        public BranchFilterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip branch filtering for certain endpoints
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (ShouldSkipBranchFilter(path))
            {
                await _next(context);
                return;
            }

            // Add branch context to the request if user is authenticated
            if (context.User.Identity.IsAuthenticated)
            {
                var branchAccessService = context.RequestServices.GetService<IBranchAccessService>();
                if (branchAccessService != null)
                {
                    var accessibleBranches = await branchAccessService.GetAccessibleBranchesAsync(context.User);
                    var currentBranch = await branchAccessService.GetCurrentUserBranchAsync(context.User);

                    // Add branch context to HttpContext items for use in controllers
                    context.Items["AccessibleBranches"] = accessibleBranches;
                    context.Items["CurrentBranchId"] = currentBranch;
                    context.Items["CanCrossBranchAccess"] = await branchAccessService.CanCrossBranchAccessAsync(context.User);
                }
            }

            await _next(context);
        }

        private static bool ShouldSkipBranchFilter(string path)
        {
            var skipPaths = new[]
            {
                "/api/v1/auth/login",
                "/api/v1/auth/register",
                "/api/v1/auth/refresh",
                "/api/v1/auth/logout",
                "/api/v1/auth/me",
                "/health",
                "/metrics",
                "/swagger",
                "/api-docs"
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath));
        }
    }

    public static class BranchFilterMiddlewareExtensions
    {
        public static IApplicationBuilder UseBranchFilter(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BranchFilterMiddleware>();
        }
    }
}
