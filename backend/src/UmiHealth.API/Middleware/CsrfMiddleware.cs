using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace UmiHealth.API.Middleware
{
    /// <summary>
    /// Middleware for CSRF protection
    /// </summary>
    public class CsrfMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CsrfMiddleware> _logger;
        private readonly IAntiforgery _antiforgery;

        public CsrfMiddleware(RequestDelegate next, ILogger<CsrfMiddleware> logger, IAntiforgery antiforgery)
        {
            _next = next;
            _logger = logger;
            _antiforgery = antiforgery;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip CSRF validation for GET, HEAD, OPTIONS requests
            if (context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Skip for API endpoints that use JWT authentication
            if (context.Request.Path.StartsWithSegments("/api") && 
                context.Request.Headers.ContainsKey("Authorization"))
            {
                await _next(context);
                return;
            }

            // Skip for health checks and Swagger
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/swagger") ||
                context.Request.Path.StartsWithSegments("/hangfire"))
            {
                await _next(context);
                return;
            }

            try
            {
                // Validate CSRF token for state-changing requests
                if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                    context.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                    context.Request.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) ||
                    context.Request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
                {
                    await ValidateCsrfTokenAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CSRF validation failed for request from {RemoteIpAddress}", 
                    context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 403; // Forbidden
                await context.Response.WriteAsync("CSRF validation failed");
                return;
            }

            await _next(context);
        }

        private async Task ValidateCsrfTokenAsync(HttpContext context)
        {
            var tokens = _antiforgery.GetAndStoreTokens(context);
            
            // Get token from request header or form data
            var requestToken = context.Request.Headers["X-CSRF-Token"].FirstOrDefault() ??
                             context.Request.Form["__RequestVerificationToken"].FirstOrDefault();

            if (string.IsNullOrEmpty(requestToken))
            {
                throw new InvalidOperationException("CSRF token is missing");
            }

            // Validate the token
            await _antiforgery.ValidateRequestAsync(context);
        }
    }

    /// <summary>
    /// Extension methods for CSRF middleware
    /// </summary>
    public static class CsrfMiddlewareExtensions
    {
        public static IServiceCollection AddCsrfProtection(this IServiceCollection services)
        {
            services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-Token";
                options.Cookie.Name = "XSRF-COOKIE";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.SuppressXFrameOptionsHeader = true; // We handle this in SecurityHeadersMiddleware
            });

            return services;
        }
    }
}
