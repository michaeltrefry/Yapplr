using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Common;

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
    /// Get or set cached value type with a factory function
    /// </summary>
    public static async Task<T> GetOrSetValueAsync<T>(
        this ICachingService cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null) where T : struct
    {
        // For value types, we wrap them in a nullable wrapper class
        var wrapper = await cache.GetAsync<ValueWrapper<T>>(key);
        if (wrapper != null)
            return wrapper.Value;

        var value = await factory();
        await cache.SetAsync(key, new ValueWrapper<T>(value), expiration);

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
    /// Get user by ID from cache or database
    /// </summary>
    public static async Task<User?> GetUserByIdAsync(this ICachingService cache, int userId, IServiceScopeFactory serviceScopeFactory)
    {
        var key = cache.GenerateKey(CacheKeys.USER_PREFIX, "id", userId);

        return await cache.GetOrSetAsync(key, async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
            return await context.Users.FindAsync(userId);
        }, TimeSpan.FromSeconds(30)); // 30 seconds expiration like original UserCacheService
    }

    /// <summary>
    /// Get user by username from cache or database
    /// </summary>
    public static async Task<User?> GetUserByUsernameAsync(this ICachingService cache, string username, IServiceScopeFactory serviceScopeFactory)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        var key = cache.GenerateKey(CacheKeys.USER_PREFIX, "username", username.ToLowerInvariant());

        return await cache.GetOrSetAsync(key, async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
            return await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }, TimeSpan.FromSeconds(30)); // 30 seconds expiration like original UserCacheService
    }

    /// <summary>
    /// Invalidate user cache entries by user ID
    /// </summary>
    public static async Task InvalidateUserByIdAsync(this ICachingService cache, int userId)
    {
        var idKey = cache.GenerateKey(CacheKeys.USER_PREFIX, "id", userId);
        await cache.RemoveAsync(idKey);
    }

    /// <summary>
    /// Invalidate user cache entries by username
    /// </summary>
    public static async Task InvalidateUserByUsernameAsync(this ICachingService cache, string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;

        var usernameKey = cache.GenerateKey(CacheKeys.USER_PREFIX, "username", username.ToLowerInvariant());
        await cache.RemoveAsync(usernameKey);
    }

    /// <summary>
    /// Clear all user cache entries
    /// </summary>
    public static async Task ClearUserCacheAsync(this ICachingService cache)
    {
        await cache.RemoveByPatternAsync($"{CacheKeys.USER_PREFIX}:");
    }

    /// <summary>
    /// Save user to database and invalidate cache
    /// </summary>
    public static async Task SaveUserAsync(this ICachingService cache, User user, IServiceScopeFactory serviceScopeFactory)
    {
        // Update database using a new scope
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
        context.Attach(user);
        await context.SaveChangesAsync();

        // Invalidate cache entries for this user
        await cache.InvalidateUserByIdAsync(user.Id);
        await cache.InvalidateUserByUsernameAsync(user.Username);
    }

    /// <summary>
    /// Invalidate timeline caches
    /// </summary>
    public static async Task InvalidateTimelineCacheAsync(this ICachingService cache)
    {
        await cache.RemoveByPatternAsync("timeline:");
    }
}