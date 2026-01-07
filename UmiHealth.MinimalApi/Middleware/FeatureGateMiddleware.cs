using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace UmiHealth.MinimalApi.Middleware;

public sealed class FeatureRequirement
{
    public string Feature { get; }
    public FeatureRequirement(string feature) => Feature = feature;
}

public class FeatureGateMiddleware
{
    private readonly RequestDelegate _next;

    public FeatureGateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, Services.ITierService tierService)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var req = endpoint.Metadata.GetMetadata<FeatureRequirement>();
            if (req != null)
            {
                // Resolve tenant id: try header, then query
                var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? context.Request.Query["tenantId"].FirstOrDefault();
                if (!tierService.HasFeature(tenantId, req.Feature))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { success = false, message = "Feature not available on your plan" });
                    return;
                }
            }
        }

        await _next(context);
    }
}
