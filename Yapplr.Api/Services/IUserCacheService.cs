using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IUserCacheService
{
    /// <summary>
    /// Gets a user by ID from cache or database
    /// </summary>
    /// <param name="userId">The user ID to lookup</param>
    /// <returns>The user entity or null if not found</returns>
    Task<User?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Gets a user by username from cache or database
    /// </summary>
    /// <param name="username">The username to lookup</param>
    /// <returns>The user entity or null if not found</returns>
    Task<User?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Invalidates a user from the cache by ID
    /// </summary>
    /// <param name="userId">The user ID to remove from cache</param>
    void InvalidateUser(int userId);

    /// <summary>
    /// Invalidates a user from the cache by username
    /// </summary>
    /// <param name="username">The username to remove from cache</param>
    void InvalidateUser(string username);

    /// <summary>
    /// Clears all cached users
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    /// <returns>Cache statistics including hit rate, miss rate, and entry count</returns>
    UserCacheStatistics GetCacheStatistics();
    
    Task SaveUserAsync(User user);
}

/// <summary>
/// Statistics for monitoring cache performance
/// </summary>
public class UserCacheStatistics
{
    public int TotalRequests { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public int CachedEntries { get; set; }
    public double HitRate => TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0;
    public double MissRate => TotalRequests > 0 ? (double)CacheMisses / TotalRequests : 0;
}
