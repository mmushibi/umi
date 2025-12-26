using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace UmiHealth.Api.Middleware
{
    /// <summary>
    /// Middleware to add security headers to all responses
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            if (!context.Response.HasStarted)
            {
                // Content Security Policy
                context.Response.Headers.Add("Content-Security-Policy", 
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data: https:; " +
                    "font-src 'self' data:; " +
                    "connect-src 'self' wss:; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'");

                // Prevent clickjacking
                context.Response.Headers.Add("X-Frame-Options", "DENY");

                // Prevent MIME type sniffing
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

                // XSS Protection
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

                // Referrer Policy
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

                // Permissions Policy
                context.Response.Headers.Add("Permissions-Policy", 
                    "geolocation=(), " +
                    "microphone=(), " +
                    "camera=(), " +
                    "payment=(), " +
                    "usb=(), " +
                    "magnetometer=(), " +
                    "gyroscope=(), " +
                    "accelerometer=()");

                // HSTS (only in production with HTTPS)
                if (context.Request.IsHttps)
                {
                    context.Response.Headers.Add("Strict-Transport-Security", 
                        "max-age=31536000; includeSubDomains; preload");
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
}
