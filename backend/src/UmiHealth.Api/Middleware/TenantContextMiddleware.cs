using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Middleware
{
    public class TenantContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantContextMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public TenantContextMiddleware(
            RequestDelegate next,
            ILogger<TenantContextMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            // Skip tenant resolution for health checks and auth endpoints
            if (ShouldSkipTenantResolution(context.Request.Path))
            {
                await _next(context);
                return;
            }

            Guid? tenantId = null;
            Guid? branchId = null;

            try
            {
                // Method 1: Extract from JWT token claims
                var tenantClaim = context.User?.FindFirst("tenant_id")?.Value;
                var branchClaim = context.User?.FindFirst("branch_id")?.Value;

                if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var parsedTenantId))
                {
                    tenantId = parsedTenantId;
                }

                if (!string.IsNullOrEmpty(branchClaim) && Guid.TryParse(branchClaim, out var parsedBranchId))
                {
                    branchId = parsedBranchId;
                }

                // Method 2: Extract from subdomain (for web portal access)
                if (!tenantId.HasValue)
                {
                    var host = context.Request.Host.Host;
                    var subdomain = ExtractSubdomain(host);
                    
                    if (!string.IsNullOrEmpty(subdomain))
                    {
                        var tenant = await tenantService.GetBySubdomainAsync(subdomain);
                        if (tenant != null)
                        {
                            tenantId = tenant.Id;
                        }
                    }
                }

                // Method 3: Extract from header (for API access)
                if (!tenantId.HasValue && context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantHeader))
                {
                    if (Guid.TryParse(tenantHeader.FirstOrDefault(), out var headerTenantId))
                    {
                        tenantId = headerTenantId;
                    }
                }

                // Method 4: Extract from query parameter (for development/testing)
                if (!tenantId.HasValue && context.Request.Query.TryGetValue("tenant_id", out var tenantQuery))
                {
                    if (Guid.TryParse(tenantQuery.FirstOrDefault(), out var queryTenantId))
                    {
                        tenantId = queryTenantId;
                    }
                }

                // Validate tenant exists and is active
                if (tenantId.HasValue)
                {
                    var tenant = await tenantService.GetByIdAsync(tenantId.Value);
                    if (tenant == null)
                    {
                        _logger.LogWarning("Tenant not found: {TenantId}", tenantId.Value);
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Tenant not found");
                        return;
                    }

                    if (!await tenantService.IsTenantActiveAsync(tenantId.Value))
                    {
                        _logger.LogWarning("Tenant is not active: {TenantId}", tenantId.Value);
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync("Tenant is not active");
                        return;
                    }

                    // Set tenant context in HttpContext items
                    context.Items["TenantId"] = tenantId.Value;
                    context.Items["Tenant"] = tenant;
                    
                    if (branchId.HasValue)
                    {
                        context.Items["BranchId"] = branchId.Value;
                    }

                    // Set PostgreSQL session variable for RLS
                    if (context.Database?.GetDbConnection() is System.Data.Common.DbConnection connection)
                    {
                        if (connection.State != System.Data.ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        using var command = connection.CreateCommand();
                        command.CommandText = "SET app.current_tenant_id = @tenant_id";
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = "@tenant_id";
                        parameter.Value = tenantId.Value;
                        command.Parameters.Add(parameter);
                        await command.ExecuteNonQueryAsync();

                        if (branchId.HasValue)
                        {
                            command.CommandText = "SET app.current_branch_id = @branch_id";
                            var branchParameter = command.CreateParameter();
                            branchParameter.ParameterName = "@branch_id";
                            branchParameter.Value = branchId.Value;
                            command.Parameters.Clear();
                            command.Parameters.Add(branchParameter);
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    _logger.LogDebug("Tenant context set: {TenantId}, Branch: {BranchId}", tenantId.Value, branchId);
                }
                else
                {
                    _logger.LogWarning("Unable to resolve tenant for request: {RequestPath}", context.Request.Path);
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Tenant information required");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting tenant context for request: {RequestPath}", context.Request.Path);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal server error");
                return;
            }

            await _next(context);
        }

        private bool ShouldSkipTenantResolution(string path)
        {
            var skipPaths = new[]
            {
                "/health",
                "/api/v1/auth/login",
                "/api/v1/auth/register",
                "/api/v1/auth/refresh",
                "/swagger",
                "/swagger-ui",
                "/api-docs"
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase));
        }

        private string? ExtractSubdomain(string host)
        {
            if (string.IsNullOrEmpty(host))
                return null;

            var parts = host.Split('.');
            if (parts.Length >= 2)
            {
                // Extract first part as subdomain (e.g., tenant1.umihealth.com -> tenant1)
                return parts[0].ToLower();
            }

            return null;
        }
    }

    // Extension methods for easy access to tenant context
    public static class TenantContextExtensions
    {
        public static Guid? GetCurrentTenantId(this HttpContext context)
        {
            if (context.Items.TryGetValue("TenantId", out var tenantId) && tenantId is Guid id)
            {
                return id;
            }
            return null;
        }

        public static Guid? GetCurrentBranchId(this HttpContext context)
        {
            if (context.Items.TryGetValue("BranchId", out var branchId) && branchId is Guid id)
            {
                return id;
            }
            return null;
        }

        public static Tenant? GetCurrentTenant(this HttpContext context)
        {
            return context.Items["Tenant"] as Tenant;
        }

        public static bool HasTenantAccess(this HttpContext context, Guid tenantId)
        {
            var currentTenantId = context.GetCurrentTenantId();
            return currentTenantId.HasValue && currentTenantId.Value == tenantId;
        }

        public static bool HasBranchAccess(this HttpContext context, Guid branchId)
        {
            var currentBranchId = context.GetCurrentBranchId();
            var user = context.User;
            
            // Check if user has explicit branch access permissions
            var branchAccessClaim = user?.FindFirst("branch_access")?.Value;
            if (!string.IsNullOrEmpty(branchAccessClaim))
            {
                try
                {
                    var branchAccessList = System.Text.Json.JsonSerializer.Deserialize<Guid[]>(branchAccessClaim);
                    return branchAccessList?.Contains(branchId) == true;
                }
                catch
                {
                    // Fallback to current branch check
                }
            }

            return currentBranchId.HasValue && currentBranchId.Value == branchId;
        }
    }
}
