using System.Collections.Concurrent;

namespace UmiHealth.MinimalApi.Services;

/// <summary>
/// Simple in-memory tier/feature lookup for scaffolding.
/// Replace with DB-backed lookup or remote billing service in production.
/// </summary>
public class TierService : ITierService
{
    private readonly ConcurrentDictionary<string, string> _tenantPlans = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _planFeatures = new();

    public TierService()
    {
        // Seed sample plans and features
        _planFeatures["Free"] = new HashSet<string> { "inventory:view" };
        _planFeatures["Care"] = new HashSet<string> { "inventory:view", "inventory:create", "inventory:export" };
        _planFeatures["Enterprise"] = new HashSet<string> { "inventory:view", "inventory:create", "inventory:export", "reports:advanced" };

        // In-memory tenant -> plan mapping (demo). In production, read from tenants table.
        _tenantPlans["default-tenant"] = "Care";
    }

    public bool HasFeature(string? tenantId, string feature)
    {
        if (string.IsNullOrEmpty(tenantId)) return false;
        if (!_tenantPlans.TryGetValue(tenantId, out var plan)) return false;
        if (!_planFeatures.TryGetValue(plan, out var features)) return false;
        return features.Contains(feature);
    }

    public int GetRateLimit(string? tenantId, string resource)
    {
        // Very simple default limits per plan. Replace with dynamic limits.
        var plan = "Free";
        if (!string.IsNullOrEmpty(tenantId) && _tenantPlans.TryGetValue(tenantId, out var p)) plan = p;

        return plan switch
        {
            "Enterprise" => 1000,
            "Care" => 300,
            _ => 60
        };
    }
}
