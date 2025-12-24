using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Threading.Tasks;

namespace UmiHealth.Infrastructure.Cache
{
    public interface IRedisCacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        Task RefreshAsync(string key, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    }

    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key, cancellationToken);
                if (string.IsNullOrEmpty(cachedData))
                    return default;

                return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent cache failures from breaking the application
                Console.WriteLine($"Cache get error for key '{key}': {ex.Message}");
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new DistributedCacheEntryOptions();
                
                if (expiry.HasValue)
                    options.AbsoluteExpirationRelativeToNow = expiry.Value;
                else
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // Default 1 hour

                var serializedData = JsonSerializer.Serialize(value, _jsonOptions);
                await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache set error for key '{key}': {ex.Message}");
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache remove error for key '{key}': {ex.Message}");
            }
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await GetKeysByPatternAsync(pattern, cancellationToken);
                foreach (var key in keys)
                {
                    await RemoveAsync(key, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache remove by pattern error for pattern '{pattern}': {ex.Message}");
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key, cancellationToken);
                return !string.IsNullOrEmpty(cachedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache exists check error for key '{key}': {ex.Message}");
                return false;
            }
        }

        public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _cache.RefreshAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache refresh error for key '{key}': {ex.Message}");
            }
        }

        public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            // Note: This implementation requires Redis server with SCAN command support
            // For basic implementation, we'll return empty list
            // In production, you might want to use StackExchange.Redis directly for this functionality
            await Task.CompletedTask;
            return Enumerable.Empty<string>();
        }
    }

    public class CacheKeys
    {
        // Tenant-specific cache keys
        public static string Tenant(string tenantId) => $"tenant:{tenantId}";
        public static string TenantUsers(string tenantId) => $"tenant:{tenantId}:users";
        public static string TenantBranches(string tenantId) => $"tenant:{tenantId}:branches";
        public static string TenantSubscription(string tenantId) => $"tenant:{tenantId}:subscription";
        
        // User-specific cache keys
        public static string UserProfile(string userId) => $"user:{userId}:profile";
        public static string UserPermissions(string userId) => $"user:{userId}:permissions";
        
        // Product cache keys
        public static string Products(string tenantId, string branchId = null) => 
            branchId != null ? $"tenant:{tenantId}:branch:{branchId}:products" : $"tenant:{tenantId}:products";
        public static string Product(string tenantId, string productId) => $"tenant:{tenantId}:product:{productId}";
        
        // Inventory cache keys
        public static string Inventory(string tenantId, string branchId) => $"tenant:{tenantId}:branch:{branchId}:inventory";
        public static string InventoryItem(string tenantId, string branchId, string productId) => 
            $"tenant:{tenantId}:branch:{branchId}:inventory:{productId}";
        
        // Patient cache keys
        public static string Patients(string tenantId, string branchId = null) => 
            branchId != null ? $"tenant:{tenantId}:branch:{branchId}:patients" : $"tenant:{tenantId}:patients";
        public static string Patient(string tenantId, string patientId) => $"tenant:{tenantId}:patient:{patientId}";
        
        // Sales cache keys
        public static string Sales(string tenantId, string branchId, DateTime? date = null) => 
            date.HasValue 
                ? $"tenant:{tenantId}:branch:{branchId}:sales:{date:yyyy-MM-dd}"
                : $"tenant:{tenantId}:branch:{branchId}:sales";
        
        // System cache keys
        public static string SystemSettings() => "system:settings";
        public static string SubscriptionPlans() => "system:subscription_plans";
        public static string Analytics(string date) => $"system:analytics:{date}";
    }
}
