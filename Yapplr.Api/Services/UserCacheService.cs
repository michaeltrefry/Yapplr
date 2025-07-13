using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using System.Collections.Concurrent;

namespace Yapplr.Api.Services;

public class UserCacheService : IUserCacheService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserCacheService> _logger;
    
    // Cache configuration
    private readonly TimeSpan _cacheExpiration;
    private readonly int _maxCacheSize;
    
    // Statistics tracking
    private readonly ConcurrentDictionary<string, bool> _cacheKeys = new();
    private long _totalRequests = 0;
    private long _cacheHits = 0;
    private long _cacheMisses = 0;

    // Cache key prefixes
    private const string USER_ID_PREFIX = "user_id_";
    private const string USERNAME_PREFIX = "user_username_";

    public UserCacheService(
        IServiceScopeFactory serviceScopeFactory,
        IMemoryCache memoryCache,
        IConfiguration configuration,
        ILogger<UserCacheService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _memoryCache = memoryCache;
        _configuration = configuration;
        _logger = logger;

        // Load configuration with defaults
        _cacheExpiration = TimeSpan.FromSeconds(
            _configuration.GetValue<int>("UserCache:ExpirationSeconds", 30));
        _maxCacheSize = _configuration.GetValue<int>("UserCache:MaxSize", 1000);

        _logger.LogInformation("UserCacheService initialized with expiration: {Expiration}, max size: {MaxSize}",
            _cacheExpiration, _maxCacheSize);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        Interlocked.Increment(ref _totalRequests);
        
        var cacheKey = $"{USER_ID_PREFIX}{userId}";
        
        // Try to get from cache first
        if (_memoryCache.TryGetValue(cacheKey, out User? cachedUser))
        {
            Interlocked.Increment(ref _cacheHits);
            _logger.LogDebug("Cache hit for user ID: {UserId}", userId);
            return cachedUser;
        }

        Interlocked.Increment(ref _cacheMisses);
        _logger.LogDebug("Cache miss for user ID: {UserId}, querying database", userId);

        // Query database using a new scope
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
        var user = await context.Users.FindAsync(userId);

        if (user != null)
        {
            // Cache the user with both ID and username keys
            await CacheUserAsync(user);
        }

        return user;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        Interlocked.Increment(ref _totalRequests);
        
        var cacheKey = $"{USERNAME_PREFIX}{username.ToLowerInvariant()}";
        
        // Try to get from cache first
        if (_memoryCache.TryGetValue(cacheKey, out User? cachedUser))
        {
            Interlocked.Increment(ref _cacheHits);
            _logger.LogDebug("Cache hit for username: {Username}", username);
            return cachedUser;
        }

        Interlocked.Increment(ref _cacheMisses);
        _logger.LogDebug("Cache miss for username: {Username}, querying database", username);

        // Query database using a new scope
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user != null)
        {
            // Cache the user with both ID and username keys
            await CacheUserAsync(user);
        }

        return user;
    }

    public void InvalidateUser(int userId)
    {
        var userIdKey = $"{USER_ID_PREFIX}{userId}";
        
        // Try to get the user to also remove the username key
        if (_memoryCache.TryGetValue(userIdKey, out User? user) && user != null)
        {
            var usernameKey = $"{USERNAME_PREFIX}{user.Username.ToLowerInvariant()}";
            _memoryCache.Remove(usernameKey);
            _cacheKeys.TryRemove(usernameKey, out _);
        }
        
        _memoryCache.Remove(userIdKey);
        _cacheKeys.TryRemove(userIdKey, out _);
        
        _logger.LogDebug("Invalidated cache for user ID: {UserId}", userId);
    }

    public void InvalidateUser(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;

        var usernameKey = $"{USERNAME_PREFIX}{username.ToLowerInvariant()}";
        
        // Try to get the user to also remove the ID key
        if (_memoryCache.TryGetValue(usernameKey, out User? user) && user != null)
        {
            var userIdKey = $"{USER_ID_PREFIX}{user.Id}";
            _memoryCache.Remove(userIdKey);
            _cacheKeys.TryRemove(userIdKey, out _);
        }
        
        _memoryCache.Remove(usernameKey);
        _cacheKeys.TryRemove(usernameKey, out _);
        
        _logger.LogDebug("Invalidated cache for username: {Username}", username);
    }

    public void ClearCache()
    {
        // Remove all tracked cache keys
        foreach (var key in _cacheKeys.Keys.ToList())
        {
            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
        }
        
        _logger.LogInformation("Cleared all user cache entries");
    }

    public UserCacheStatistics GetCacheStatistics()
    {
        return new UserCacheStatistics
        {
            TotalRequests = (int)_totalRequests,
            CacheHits = (int)_cacheHits,
            CacheMisses = (int)_cacheMisses,
            CachedEntries = _cacheKeys.Count
        };
    }

    public async Task SaveUserAsync(User user)
    {
        // Update database using a new scope
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
        context.Attach(user);
        await context.SaveChangesAsync();
    }

    private async Task CacheUserAsync(User user)
    {
        // Check cache size limit
        if (_cacheKeys.Count >= _maxCacheSize)
        {
            _logger.LogWarning("Cache size limit reached ({MaxSize}), not caching new user", _maxCacheSize);
            return;
        }

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Priority = CacheItemPriority.Normal,
            PostEvictionCallbacks = { new PostEvictionCallbackRegistration
            {
                EvictionCallback = OnCacheEntryEvicted
            }}
        };

        var userIdKey = $"{USER_ID_PREFIX}{user.Id}";
        var usernameKey = $"{USERNAME_PREFIX}{user.Username.ToLowerInvariant()}";

        // Cache with both keys
        _memoryCache.Set(userIdKey, user, cacheOptions);
        _memoryCache.Set(usernameKey, user, cacheOptions);
        
        // Track keys for statistics and cleanup
        _cacheKeys.TryAdd(userIdKey, true);
        _cacheKeys.TryAdd(usernameKey, true);

        _logger.LogDebug("Cached user: {UserId} ({Username})", user.Id, user.Username);
        
        await Task.CompletedTask;
    }

    private void OnCacheEntryEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (key is string keyString)
        {
            _cacheKeys.TryRemove(keyString, out _);
            _logger.LogDebug("Cache entry evicted: {Key}, reason: {Reason}", keyString, reason);
        }
    }
}
