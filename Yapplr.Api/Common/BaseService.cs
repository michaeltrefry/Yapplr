using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Common;

/// <summary>
/// Base service class providing common functionality for all services
/// </summary>
public abstract class BaseService
{
    protected readonly YapplrDbContext _context;
    protected readonly ILogger _logger;

    protected BaseService(YapplrDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get user by ID with caching consideration
    /// </summary>
    protected async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    /// <summary>
    /// Check if user exists and is active
    /// </summary>
    protected async Task<bool> IsUserActiveAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null && user.Status == UserStatus.Active;
    }

    /// <summary>
    /// Check if user has required role
    /// </summary>
    protected async Task<bool> UserHasRoleAsync(int userId, UserRole requiredRole)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null && user.Role >= requiredRole;
    }

    /// <summary>
    /// Check if user is admin or moderator
    /// </summary>
    protected async Task<bool> IsUserAdminOrModeratorAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null && (user.Role == UserRole.Admin || user.Role == UserRole.Moderator);
    }

    /// <summary>
    /// Get blocked user IDs for a user (for filtering content)
    /// </summary>
    protected async Task<List<int>> GetBlockedUserIdsAsync(int userId)
    {
        return await _context.Blocks
            .Where(b => b.BlockerId == userId)
            .Select(b => b.BlockedId)
            .ToListAsync();
    }

    /// <summary>
    /// Check if one user is blocked by another
    /// </summary>
    protected async Task<bool> IsUserBlockedAsync(int userId, int potentialBlockerId)
    {
        return await _context.Blocks
            .AnyAsync(b => b.BlockerId == potentialBlockerId && b.BlockedId == userId);
    }

    /// <summary>
    /// Check if users are mutually blocking each other
    /// </summary>
    protected async Task<bool> AreUsersMutuallyBlockedAsync(int userId1, int userId2)
    {
        return await _context.Blocks
            .AnyAsync(b => (b.BlockerId == userId1 && b.BlockedId == userId2) ||
                          (b.BlockerId == userId2 && b.BlockedId == userId1));
    }

    /// <summary>
    /// Get following user IDs for a user
    /// </summary>
    protected async Task<List<int>> GetFollowingUserIdsAsync(int userId)
    {
        return await _context.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync();
    }

    /// <summary>
    /// Check if user is following another user
    /// </summary>
    protected async Task<bool> IsUserFollowingAsync(int followerId, int followingId)
    {
        return await _context.Follows
            .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
    }

    /// <summary>
    /// Apply pagination to a queryable
    /// </summary>
    protected static IQueryable<T> ApplyPagination<T>(IQueryable<T> query, int page, int pageSize)
    {
        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Log service operation
    /// </summary>
    protected void LogOperation(string operation, object? parameters = null)
    {
        _logger.LogInformation("Service operation: {Operation} with parameters: {@Parameters}", 
            operation, parameters);
    }

    /// <summary>
    /// Log service error
    /// </summary>
    protected void LogError(Exception ex, string operation, object? parameters = null)
    {
        _logger.LogError(ex, "Service operation failed: {Operation} with parameters: {@Parameters}", 
            operation, parameters);
    }

    /// <summary>
    /// Validate entity exists
    /// </summary>
    protected static void ValidateEntityExists<T>(T? entity, string entityName) where T : class
    {
        if (entity == null)
            throw new ArgumentException($"{entityName} not found");
    }

    /// <summary>
    /// Validate user authorization for resource
    /// </summary>
    protected static void ValidateUserAuthorization(int currentUserId, int resourceOwnerId, string operation)
    {
        if (currentUserId != resourceOwnerId)
            throw new UnauthorizedAccessException($"User {currentUserId} is not authorized to {operation}");
    }
}
