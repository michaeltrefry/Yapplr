using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;

namespace Yapplr.Api.Common;

/// <summary>
/// Service for entities that support soft deletion and moderation
/// </summary>
public abstract class SoftDeletableCrudService<TEntity, TDto, TCreateDto, TUpdateDto> : EnhancedCrudService<TEntity, TDto, TCreateDto, TUpdateDto>
    where TEntity : class, ISoftDeletableEntity, IUserOwnedEntity
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected SoftDeletableCrudService(YapplrDbContext context, ILogger logger) : base(context, logger)
    {
    }

    /// <summary>
    /// Override delete to use soft delete
    /// </summary>
    public override async Task<ServiceResult> DeleteAsync(int id, int currentUserId)
    {
        try
        {
            var entity = await GetEntityByIdAsync(id);
            if (entity == null)
                return ServiceResult.Failure("Entity not found");

            if (!await CanDeleteEntityAsync(entity, currentUserId))
                return ServiceResult.Failure("Access denied");

            await BeforeDeleteAsync(entity, currentUserId);

            // Soft delete instead of hard delete
            entity.IsDeletedByUser = true;
            entity.DeletedByUserAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await AfterDeleteAsync(entity, currentUserId);

            LogOperation(nameof(DeleteAsync), new { id, currentUserId });
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            LogError(ex, nameof(DeleteAsync), new { id, currentUserId });
            return ServiceResult.Failure("An error occurred while deleting the entity", ex);
        }
    }

    /// <summary>
    /// Restore a soft-deleted entity
    /// </summary>
    public virtual async Task<ServiceResult> RestoreAsync(int id, int currentUserId)
    {
        try
        {
            var entity = await GetEntityByIdAsync(id);
            if (entity == null)
                return ServiceResult.Failure("Entity not found");

            if (!await CanRestoreEntityAsync(entity, currentUserId))
                return ServiceResult.Failure("Access denied");

            entity.IsDeletedByUser = false;
            entity.DeletedByUserAt = null;

            await _context.SaveChangesAsync();
            LogOperation(nameof(RestoreAsync), new { id, currentUserId });

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            LogError(ex, nameof(RestoreAsync), new { id, currentUserId });
            return ServiceResult.Failure("An error occurred while restoring the entity", ex);
        }
    }

    /// <summary>
    /// Get deleted entities (admin/moderator only)
    /// </summary>
    public virtual async Task<ServiceResult<PaginatedResult<TDto>>> GetDeletedAsync(
        int page = 1,
        int pageSize = 25,
        int currentUserId = 0)
    {
        try
        {
            if (!await IsUserAdminOrModeratorAsync(currentUserId))
                return ServiceResult<PaginatedResult<TDto>>.Failure("Access denied");

            var query = GetBaseQuery().Where(e => e.IsDeletedByUser);

            var totalCount = await query.CountAsync();
            var entities = await query
                .ApplyPaginationWithOrdering(page, pageSize)
                .ToListAsync();

            var dtos = new List<TDto>();
            foreach (var entity in entities)
            {
                dtos.Add(await MapToDto(entity, currentUserId));
            }

            var result = PaginatedResult<TDto>.Create(dtos, page, pageSize, totalCount);
            return ServiceResult<PaginatedResult<TDto>>.Success(result);
        }
        catch (Exception ex)
        {
            LogError(ex, nameof(GetDeletedAsync), new { page, pageSize, currentUserId });
            return ServiceResult<PaginatedResult<TDto>>.Failure("An error occurred while retrieving deleted entities", ex);
        }
    }

    /// <summary>
    /// Override base query to exclude soft-deleted entities by default
    /// </summary>
    protected override IQueryable<TEntity> GetBaseQuery()
    {
        return GetBaseQueryWithDeleted().Where(e => !e.IsDeletedByUser);
    }

    /// <summary>
    /// Get base query including soft-deleted entities
    /// </summary>
    protected abstract IQueryable<TEntity> GetBaseQueryWithDeleted();

    /// <summary>
    /// Check if user can restore entity
    /// </summary>
    protected virtual async Task<bool> CanRestoreEntityAsync(TEntity entity, int currentUserId)
    {
        return entity.UserId == currentUserId || await IsUserAdminOrModeratorAsync(currentUserId);
    }
}