using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace UmiHealth.MinimalApi.Middleware;

public class TierRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    // key = tenantId, value = (windowStartUtc, count)
    private readonly ConcurrentDictionary<string, (DateTime windowStart, int count)> _counters = new();

    public TierRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, Services.ITierService tierService)
    {
        var tenantId = context.Request.Headers["X-Tenant-Id"].ToString() ?? context.Request.Query["tenantId"].ToString();
        if (string.IsNullOrEmpty(tenantId)) tenantId = "default-tenant";

        var limit = tierService.GetRateLimit(tenantId, "request");
        var now = DateTime.UtcNow;
        var windowKey = tenantId;

        _counters.AddOrUpdate(windowKey,
            (now, 1),
            (k, old) => old.windowStart.AddMinutes(1) <= now ? (now, 1) : (old.windowStart, old.count + 1)
        );

        var current = _counters[windowKey];
        if (current.count > limit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new { success = false, message = "Rate limit exceeded for tier" });
            return;
        }

        await _next(context);
    }
}
