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
        return Task.FromResult(entity.UserId == currentUserId || IsUserAdminOrModeratorAsync(currentUserId).Result);
    }

    protected override Task<bool> CanDeleteEntityAsync(TEntity entity, int currentUserId)
    {
        return Task.FromResult(entity.UserId == currentUserId || IsUserAdminOrModeratorAsync(currentUserId).Result);
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

    protected override async Task<IQueryable<TEntity>> ApplyAccessFilterAsync(IQueryable<TEntity> query, int? currentUserId)
    {
        if (!currentUserId.HasValue)
            return query.Where(e => false); // No access for unauthenticated users

        // Users can see their own entities, admins/moderators can see all
        if (await IsUserAdminOrModeratorAsync(currentUserId.Value))
            return query;

        return query.Where(e => e.UserId == currentUserId.Value);
    }
}