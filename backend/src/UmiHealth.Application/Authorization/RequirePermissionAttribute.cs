using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.Application.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public string Permission { get; }
        public string Resource { get; }
        public string Action { get; }

        public RequirePermissionAttribute(string permission)
        {
            Permission = permission;
        }

        public RequirePermissionAttribute(string resource, string action)
        {
            Resource = resource;
            Action = action;
            Permission = $"{resource}:{action}";
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var authService = context.HttpContext.RequestServices
                .GetService(typeof(Services.IAuthorizationService)) as Services.IAuthorizationService;

            if (authService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var hasPermission = await authService.HasPermissionAsync(user, Permission);
            
            if (!hasPermission)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
