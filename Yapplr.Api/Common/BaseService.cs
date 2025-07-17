using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

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
    /// Check if user has required role using JWT claims (preferred method)
    /// </summary>
    protected static bool UserHasRole(ClaimsPrincipal user, UserRole requiredRole)
    {
        return user.HasRoleOrHigher(requiredRole);
    }

    /// <summary>
    /// Check if user is admin or moderator using JWT claims (preferred method)
    /// </summary>
    protected static bool IsUserAdminOrModerator(ClaimsPrincipal user)
    {
        return user.IsAdminOrModerator();
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
    /// WARNING: This method should only be used on queries that already have an OrderBy clause.
    /// </summary>
    protected static IQueryable<T> ApplyPagination<T>(IQueryable<T> query, int page, int pageSize)
    {
        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Log service operation with enhanced structured logging
    /// </summary>
    protected void LogOperation(string operation, object? parameters = null)
    {
        _logger.LogBusinessOperation(operation, parameters);
    }

    /// <summary>
    /// Log service error with enhanced structured logging
    /// </summary>
    protected void LogError(Exception ex, string operation, object? parameters = null)
    {
        _logger.LogBusinessError(ex, operation, parameters);
    }

    /// <summary>
    /// Log user action with context
    /// </summary>
    protected void LogUserAction(int userId, string action, object? details = null)
    {
        _logger.LogUserAction(userId, action, details);
    }

    /// <summary>
    /// Log entity operation with context
    /// </summary>
    protected void LogEntityOperation(string entityType, int entityId, string operation, object? parameters = null)
    {
        _logger.LogBusinessOperation(operation, parameters, entityType, entityId);
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
