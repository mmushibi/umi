using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.API.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public TenantResolutionMiddleware(
            RequestDelegate next,
            ILogger<TenantResolutionMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var tenantId = ResolveTenant(context);
                
                if (string.IsNullOrEmpty(tenantId))
                {
                    _logger.LogWarning("Tenant could not be resolved for request: {RequestPath}", context.Request.Path);
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Tenant not found");
                    return;
                }

                // Add tenant to context items for downstream use
                context.Items["TenantId"] = tenantId;
                
                // Add tenant header for service communication
                context.Request.Headers["X-Tenant-ID"] = tenantId;

                _logger.LogDebug("Tenant resolved: {TenantId} for request: {RequestPath}", tenantId, context.Request.Path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving tenant for request: {RequestPath}", context.Request.Path);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Tenant resolution failed");
                return;
            }

            await _next(context);
        }

        private string? ResolveTenant(HttpContext context)
        {
            // Method 1: Subdomain extraction
            var host = context.Request.Host.Host;
            var subdomain = ExtractSubdomain(host);
            
            if (!string.IsNullOrEmpty(subdomain) && subdomain != "www" && subdomain != "api")
            {
                return ResolveTenantFromSubdomain(subdomain);
            }

            // Method 2: Header-based resolution
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantHeader))
            {
                return tenantHeader;
            }

            // Method 3: Query parameter (for development/testing)
            if (context.Request.Query.TryGetValue("tenant", out var tenantQuery))
            {
                return tenantQuery;
            }

            return null;
        }

        private string? ExtractSubdomain(string host)
        {
            var parts = host.Split('.');
            if (parts.Length >= 2)
            {
                return parts[0];
            }
            return null;
        }

        private string? ResolveTenantFromSubdomain(string subdomain)
        {
            // In a real implementation, this would query the database
            // For now, we'll use a simple mapping
            return subdomain.ToLower() switch
            {
                "demo" => "tenant-demo-uuid",
                "test" => "tenant-test-uuid",
                "staging" => "tenant-staging-uuid",
                _ => null
            };
        }
    }
}
