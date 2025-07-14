using Microsoft.Extensions.Caching.Memory;

namespace Yapplr.Api.Common;

public class MemoryCachingService : ICachingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCachingService> _logger;
    private readonly HashSet<string> _cacheKeys = new();
    private readonly object _lockObject = new();

    public MemoryCachingService(IMemoryCache cache, ILogger<MemoryCachingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            if (_cache.TryGetValue(key, out var value) && value is T typedValue)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult<T?>(typedValue);
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default expiration of 5 minutes
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            }

            // Add callback to remove from tracking when evicted
            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (key, value, reason, state) =>
                {
                    lock (_lockObject)
                    {
                        _cacheKeys.Remove(key.ToString()!);
                    }
                }
            });

            _cache.Set(key, value, options);
            
            lock (_lockObject)
            {
                _cacheKeys.Add(key);
            }

            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", 
                key, expiration?.ToString() ?? "5 minutes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            
            lock (_lockObject)
            {
                _cacheKeys.Remove(key);
            }

            _logger.LogDebug("Removed cache entry for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            List<string> keysToRemove;
            
            lock (_lockObject)
            {
                keysToRemove = _cacheKeys
                    .Where(key => key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            lock (_lockObject)
            {
                foreach (var key in keysToRemove)
                {
                    _cacheKeys.Remove(key);
                }
            }

            _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", 
                keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
        }

        return Task.CompletedTask;
    }

    public string GenerateKey(string prefix, params object[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return prefix;

        var paramString = string.Join(":", parameters.Select(p => 
            p?.ToString()?.Replace(":", "_") ?? "null"));
        
        return $"{prefix}:{paramString}";
    }
}