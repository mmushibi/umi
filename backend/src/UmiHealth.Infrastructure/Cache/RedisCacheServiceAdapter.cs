using UmiHealth.Core.Interfaces;
using UmiHealth.Infrastructure.Cache;

namespace UmiHealth.Infrastructure.Cache;

// Adapter to bridge between ICacheService and IRedisCacheService
public class RedisCacheServiceAdapter : ICacheService
{
    private readonly IRedisCacheService _redisCacheService;

    public RedisCacheServiceAdapter(IRedisCacheService redisCacheService)
    {
        _redisCacheService = redisCacheService;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await _redisCacheService.GetAsync<T>(key, cancellationToken);
    }

    public async Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, CancellationToken cancellationToken = default)
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
            return cachedValue;

        var result = await factory();
        if (result != null)
            await SetAsync(key, result, cancellationToken: cancellationToken);

        return result;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        await _redisCacheService.SetAsync(key, value, expiry, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _redisCacheService.RemoveAsync(key, cancellationToken);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        await _redisCacheService.RemoveByPatternAsync(pattern, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _redisCacheService.ExistsAsync(key, cancellationToken);
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        await _redisCacheService.RefreshAsync(key, cancellationToken);
    }
}
