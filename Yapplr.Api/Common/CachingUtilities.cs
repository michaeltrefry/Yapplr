using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Yapplr.Api.Common;

/// <summary>
/// Utilities for caching database query results and other data
/// </summary>
public interface ICachingService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    string GenerateKey(string prefix, params object[] parameters);
}

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

/// <summary>
/// Cache key constants for consistent cache management
/// </summary>
public static class CacheKeys
{
    public const string USER_PREFIX = "user";
    public const string POST_PREFIX = "post";
    public const string TIMELINE_PREFIX = "timeline";
    public const string CONVERSATION_PREFIX = "conversation";
    public const string NOTIFICATION_PREFIX = "notification";
    public const string FOLLOW_PREFIX = "follow";
    public const string BLOCK_PREFIX = "block";

    public static string UserById(int userId) => $"{USER_PREFIX}:id:{userId}";
    public static string UserByUsername(string username) => $"{USER_PREFIX}:username:{username}";
    public static string PostById(int postId) => $"{POST_PREFIX}:id:{postId}";
    public static string UserTimeline(int userId, int page, int pageSize) => 
        $"{TIMELINE_PREFIX}:user:{userId}:page:{page}:size:{pageSize}";
    public static string PublicTimeline(int page, int pageSize) => 
        $"{TIMELINE_PREFIX}:public:page:{page}:size:{pageSize}";
    public static string UserFollowing(int userId) => $"{FOLLOW_PREFIX}:following:{userId}";
    public static string UserFollowers(int userId) => $"{FOLLOW_PREFIX}:followers:{userId}";
    public static string UserBlocked(int userId) => $"{BLOCK_PREFIX}:blocked:{userId}";
    public static string ConversationList(int userId, int page, int pageSize) => 
        $"{CONVERSATION_PREFIX}:list:{userId}:page:{page}:size:{pageSize}";
    public static string UnreadCount(int userId) => $"{NOTIFICATION_PREFIX}:unread:{userId}";
}

/// <summary>
/// Extension methods for easy caching of common operations
/// </summary>
public static class CachingExtensions
{
    /// <summary>
    /// Get or set cached value with a factory function
    /// </summary>
    public static async Task<T?> GetOrSetAsync<T>(
        this ICachingService cache,
        string key,
        Func<Task<T?>> factory,
        TimeSpan? expiration = null) where T : class
    {
        var cached = await cache.GetAsync<T>(key);
        if (cached != null)
            return cached;

        var value = await factory();
        if (value != null)
        {
            await cache.SetAsync(key, value, expiration);
        }

        return value;
    }

    /// <summary>
    /// Invalidate cache entries related to a user
    /// </summary>
    public static async Task InvalidateUserCacheAsync(this ICachingService cache, int userId)
    {
        await cache.RemoveByPatternAsync($"user:{userId}");
        await cache.RemoveByPatternAsync($"timeline:user:{userId}");
        await cache.RemoveByPatternAsync($"follow:following:{userId}");
        await cache.RemoveByPatternAsync($"follow:followers:{userId}");
        await cache.RemoveByPatternAsync($"conversation:list:{userId}");
    }

    /// <summary>
    /// Invalidate cache entries related to a post
    /// </summary>
    public static async Task InvalidatePostCacheAsync(this ICachingService cache, int postId, int userId)
    {
        await cache.RemoveByPatternAsync($"post:{postId}");
        await cache.RemoveByPatternAsync($"timeline:user:{userId}");
        await cache.RemoveByPatternAsync("timeline:public");
    }

    /// <summary>
    /// Invalidate timeline caches
    /// </summary>
    public static async Task InvalidateTimelineCacheAsync(this ICachingService cache)
    {
        await cache.RemoveByPatternAsync("timeline:");
    }
}
