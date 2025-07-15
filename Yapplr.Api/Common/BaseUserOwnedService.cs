using System.Security.Claims;
using Yapplr.Api.Data;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Common;

/// <summary>
/// Base service for user-owned entities with automatic ownership validation
/// </summary>
public abstract class BaseUserOwnedService<TEntity, TDto, TCreateDto, TUpdateDto> : BaseCrudService<TEntity, TDto, TCreateDto, TUpdateDto>
    where TEntity : class, IUserOwnedEntity
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected BaseUserOwnedService(YapplrDbContext context, ILogger logger) : base(context, logger)
    {
    }

    protected override Task<bool> CanUpdateEntityAsync(TEntity entity, TUpdateDto updateDto, int currentUserId)
    {
        // This method is deprecated - use the ClaimsPrincipal overload instead
        // For backward compatibility, we'll return false to force usage of the new method
        return Task.FromResult(false);
    }

    protected override Task<bool> CanDeleteEntityAsync(TEntity entity, int currentUserId)
    {
        // This method is deprecated - use the ClaimsPrincipal overload instead
        // For backward compatibility, we'll return false to force usage of the new method
        return Task.FromResult(false);
    }

    /// <summary>
    /// Check if user can update entity using JWT claims (preferred method)
    /// </summary>
    protected virtual bool CanUpdateEntity(TEntity entity, TUpdateDto updateDto, ClaimsPrincipal user)
    {
        var currentUserId = user.GetUserIdOrNull();
        return currentUserId.HasValue &&
               (entity.UserId == currentUserId.Value || user.IsAdminOrModerator());
    }

    /// <summary>
    /// Check if user can delete entity using JWT claims (preferred method)
    /// </summary>
    protected virtual bool CanDeleteEntity(TEntity entity, ClaimsPrincipal user)
    {
        var currentUserId = user.GetUserIdOrNull();
        return currentUserId.HasValue &&
               (entity.UserId == currentUserId.Value || user.IsAdminOrModerator());
    }

    protected override Task<IQueryable<TEntity>> ApplyAccessFilterAsync(IQueryable<TEntity> query, int? currentUserId)
    {
        if (!currentUserId.HasValue)
            return Task.FromResult(query.Where(e => false)); // No access for unauthenticated users

        // Users can see their own entities only
        // Note: Admin/moderator access should be handled at the endpoint level using ClaimsPrincipal
        // This method is primarily for user-owned entity filtering
        return Task.FromResult(query.Where(e => e.UserId == currentUserId.Value));
    }

    /// <summary>
    /// Apply access filter using JWT claims (preferred method)
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyAccessFilter(IQueryable<TEntity> query, ClaimsPrincipal user)
    {
        var currentUserId = user.GetUserIdOrNull();
        if (!currentUserId.HasValue)
            return query.Where(e => false); // No access for unauthenticated users

        // Users can see their own entities, admins/moderators can see all
        if (user.IsAdminOrModerator())
            return query;

        return query.Where(e => e.UserId == currentUserId.Value);
    }
}