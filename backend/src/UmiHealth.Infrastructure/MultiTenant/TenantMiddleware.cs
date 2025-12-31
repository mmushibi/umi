using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Infrastructure.MultiTenant
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public TenantMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
        {
            // Extract tenant from subdomain or header
            var tenantId = GetTenantIdentifier(context);
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                await tenantProvider.SetTenantAsync(tenantId);
            }

            await _next(context);
        }

        private string? GetTenantIdentifier(HttpContext context)
        {
            // Try subdomain first
            var host = context.Request.Host.Host;
            var subdomain = GetSubdomain(host);
            if (!string.IsNullOrEmpty(subdomain))
            {
                return subdomain;
            }

            // Fallback to header-based tenant identification
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader))
            {
                return tenantIdHeader;
            }

            // Fallback to query parameter (for development/testing)
            if (context.Request.Query.TryGetValue("tenant_id", out var tenantIdQuery))
            {
                return tenantIdQuery;
            }

            return null;
        }

        private string? GetSubdomain(string host)
        {
            if (string.IsNullOrEmpty(host))
                return null;

            var parts = host.Split('.');
            if (parts.Length >= 2)
            {
                // Skip common subdomains like www, api, etc.
                var subdomain = parts[0].ToLower();
                var excludedSubdomains = new[] { "www", "api", "app", "admin", "localhost" };
                
                if (!excludedSubdomains.Contains(subdomain))
                {
                    return subdomain;
                }
            }

            return null;
        }
    }

    public interface ITenantProvider
    {
        string? CurrentTenantId { get; }
        Task SetTenantAsync(string tenantId);
        void ClearTenant();
    }

    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly TenantDbContextFactory _tenantDbContextFactory;
        private string? _currentTenantId;

        public TenantProvider(
            IHttpContextAccessor httpContextAccessor,
            TenantDbContextFactory tenantDbContextFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _tenantDbContextFactory = tenantDbContextFactory;
        }

        public string? CurrentTenantId => _currentTenantId ?? 
            _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;

        public async Task SetTenantAsync(string tenantId)
        {
            // Validate tenant exists
            using var context = await _tenantDbContextFactory.CreateDbContextAsync(tenantId);
            
            // If we can create the context successfully, the tenant exists
            _currentTenantId = tenantId;
        }

        public void ClearTenant()
        {
            _currentTenantId = null;
        }
    }
}
