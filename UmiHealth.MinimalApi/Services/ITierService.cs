namespace UmiHealth.MinimalApi.Services;

public interface ITierService
{
    bool HasFeature(string? tenantId, string feature);
    int GetRateLimit(string? tenantId, string resource);
}
