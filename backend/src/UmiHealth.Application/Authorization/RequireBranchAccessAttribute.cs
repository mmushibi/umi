using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace UmiHealth.Application.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireBranchAccessAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public string BranchParameter { get; }

        public RequireBranchAccessAttribute(string branchParameter = "branchId")
        {
            BranchParameter = branchParameter;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var branchAccessService = context.HttpContext.RequestServices
                .GetService(typeof(Services.IBranchAccessService)) as Services.IBranchAccessService;

            if (branchAccessService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            // Try to get branch ID from route data, query string, or request body
            Guid? branchId = null;

            // Check route data
            if (context.RouteData.Values.TryGetValue(BranchParameter, out var routeValue) && 
                Guid.TryParse(routeValue?.ToString(), out var routeBranchId))
            {
                branchId = routeBranchId;
            }
            // Check query string
            else if (context.HttpContext.Request.Query.TryGetValue(BranchParameter, out var queryValue) && 
                     Guid.TryParse(queryValue.FirstOrDefault(), out var queryBranchId))
            {
                branchId = queryBranchId;
            }
            // If no branch ID provided, check if user has any branch access
            else
            {
                var accessibleBranches = await branchAccessService.GetAccessibleBranchesAsync(user);
                if (!accessibleBranches.Any())
                {
                    context.Result = new ForbidResult();
                    return;
                }
                return; // Allow access if user has some branch access
            }

            if (branchId.HasValue)
            {
                var hasAccess = await branchAccessService.CanAccessBranchAsync(user, branchId.Value);
                if (!hasAccess)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }
    }
}
