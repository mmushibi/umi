using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace UmiHealth.API.Middleware
{
    /// <summary>
    /// Enhanced middleware to add comprehensive security headers to all responses
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            if (!context.Response.HasStarted)
            {
                // Enhanced Content Security Policy with configurable values
                var csp = _configuration["SecurityHeaders:ContentSecurityPolicy"] ??
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
                    "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
                    "img-src 'self' data: https:; " +
                    "connect-src 'self' wss:; " +
                    "frame-ancestors 'none'; " +
                    "form-action 'self'; " +
                    "base-uri 'self'; " +
                    "upgrade-insecure-requests";

                context.Response.Headers.Add("Content-Security-Policy", csp);

                // Prevent clickjacking
                context.Response.Headers.Add("X-Frame-Options", "DENY");

                // Prevent MIME type sniffing
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

                // XSS Protection
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

                // Referrer Policy
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

                // Enhanced Permissions Policy
                var permissionsPolicy = _configuration["SecurityHeaders:PermissionsPolicy"] ??
                    "geolocation=(), " +
                    "microphone=(), " +
                    "camera=(), " +
                    "payment=(), " +
                    "usb=(), " +
                    "magnetometer=(), " +
                    "gyroscope=(), " +
                    "accelerometer=(), " +
                    "interest-cohort=()";

                context.Response.Headers.Add("Permissions-Policy", permissionsPolicy);

                // HSTS (only in production with HTTPS)
                if (context.Request.IsHttps)
                {
                    var hstsMaxAge = _configuration["SecurityHeaders:HstsMaxAge"] ?? "31536000";
                    var includeSubDomains = _configuration["SecurityHeaders:IncludeSubDomains"] ?? "true";
                    var preload = _configuration["SecurityHeaders:Preload"] ?? "true";

                    var hsts = $"max-age={hstsMaxAge}";
                    if (bool.Parse(includeSubDomains))
                        hsts += "; includeSubDomains";
                    if (bool.Parse(preload))
                        hsts += "; preload";

                    context.Response.Headers.Add("Strict-Transport-Security", hsts);
                }

                // Remove server information
                context.Response.Headers.Remove("Server");
                context.Response.Headers.Add("Server", string.Empty);

                // Cache control for sensitive endpoints
                if (context.Request.Path.StartsWithSegments("/api/auth") ||
                    context.Request.Path.StartsWithSegments("/api/users") ||
                    context.Request.Path.StartsWithSegments("/admin"))
                {
                    context.Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
                    context.Response.Headers.Add("Pragma", "no-cache");
                    context.Response.Headers.Add("Expires", "0");
                }
            }

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }

        public static IServiceCollection AddSecurityHeaders(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SecurityHeadersOptions>(configuration.GetSection("SecurityHeaders"));
            return services;
        }
    }

    public class SecurityHeadersOptions
    {
        public string ContentSecurityPolicy { get; set; }
        public string PermissionsPolicy { get; set; }
        public string HstsMaxAge { get; set; } = "31536000";
        public bool IncludeSubDomains { get; set; } = true;
        public bool Preload { get; set; } = true;
    }
}
