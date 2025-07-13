using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;

namespace Yapplr.Api.Common;

/// <summary>
/// Base CRUD service providing common database operations
/// </summary>
public abstract class BaseCrudService<TEntity, TDto, TCreateDto, TUpdateDto> : BaseService
    where TEntity : class, IEntity
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected BaseCrudService(YapplrDbContext context, ILogger logger) : base(context, logger)
    {
    }

    /// <summary>
    /// Get entity by ID
    /// </summary>
    public virtual async Task<ServiceResult<TDto>> GetByIdAsync(int id, int? currentUserId = null)
    {
        try
        {
            var entity = await GetEntityByIdAsync(id);
            if (entity == null)
                return ServiceResult<TDto>.Failure("Entity not found");

            if (!await CanViewEntityAsync(entity, currentUserId))
                return ServiceResult<TDto>.Failure("Access denied");

            var dto = await MapToDto(entity, currentUserId);
            return ServiceResult<TDto>.Success(dto);
        }
        catch (Exception ex)
        {
            LogError(ex, nameof(GetByIdAsync), new { id, currentUserId });
            return ServiceResult<TDto>.Failure("An error occurred while retrieving the entity", ex);
        }
    }

    /// <summary>
    /// Get all entities with pagination
    /// </summary>
    public virtual async Task<ServiceResult<PaginatedResult<TDto>>> GetAllAsync(
        int page = 1, 
        int pageSize = 25, 
        int? currentUserId = null)
    {
        try
        {
            var query = GetBaseQuery();
            query = await ApplyAccessFilterAsync(query, currentUserId);
            
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
            LogError(ex, nameof(GetAllAsync), new { page, pageSize, currentUserId });
            return ServiceResult<PaginatedResult<TDto>>.Failure("An error occurred while retrieving entities", ex);
        }
    }

    /// <summary>
    /// Create new entity
    /// </summary>
    public virtual async Task<ServiceResult<TDto>> CreateAsync(TCreateDto createDto, int currentUserId)
    {
        try
        {
            if (!await CanCreateEntityAsync(createDto, currentUserId))
                return ServiceResult<TDto>.Failure("Access denied");

            var entity = await MapFromCreateDto(createDto, currentUserId);
            await BeforeCreateAsync(entity, createDto, currentUserId);

            _context.Set<TEntity>().Add(entity);
            await _context.SaveChangesAsync();

            await AfterCreateAsync(entity, createDto, currentUserId);

            var dto = await MapToDto(entity, currentUserId);
            LogOperation(nameof(CreateAsync), new { entityId = entity.Id, currentUserId });
            
            return ServiceResult<TDto>.Success(dto);
        }
        catch (Exception ex)
        {
            LogError(ex, nameof(CreateAsync), new { createDto, currentUserId });
            return ServiceResult<TDto>.Failure("An error occurred while creating the entity", ex);
        }
    }

    /// <summary>
    /// Update existing entity
    /// </summary>
    public virtual async Task<ServiceResult<TDto>> UpdateAsync(int id, TUpdateDto updateDto, int currentUserId)
    {
        try
        {
            var entity = await GetEntityByIdAsync(id);
            if (entity == null)
                return ServiceResult<TDto>.Failure("Entity not found");

            if (!await CanUpdateEntityAsync(entity, updateDto, currentUserId))
                return ServiceResult<TDto>.Failure("Access denied");

            await BeforeUpdateAsync(entity, updateDto, currentUserId);
            await MapFromUpdateDto(entity, updateDto, currentUserId);
            
            await _context.SaveChangesAsync();
            await AfterUpdateAsync(entity, updateDto, currentUserId);

            var dto = await MapToDto(entity, currentUserId);
            LogOperation(nameof(UpdateAsync), new { id, currentUserId });
            
            return ServiceResult<TDto>.Success(dto);
        }
        catch (Exception ex)
        {
            LogError(ex, nameof(UpdateAsync), new { id, updateDto, currentUserId });
            return ServiceResult<TDto>.Failure("An error occurred while updating the entity", ex);
        }
    }

    /// <summary>
    /// Delete entity
    /// </summary>
    public virtual async Task<ServiceResult> DeleteAsync(int id, int currentUserId)
    {
        try
        {
            var entity = await GetEntityByIdAsync(id);
            if (entity == null)
                return ServiceResult.Failure("Entity not found");

            if (!await CanDeleteEntityAsync(entity, currentUserId))
                return ServiceResult.Failure("Access denied");

            await BeforeDeleteAsync(entity, currentUserId);
            
            _context.Set<TEntity>().Remove(entity);
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

    // Abstract methods to be implemented by derived classes
    protected abstract Task<TEntity?> GetEntityByIdAsync(int id);
    protected abstract IQueryable<TEntity> GetBaseQuery();
    protected abstract Task<TDto> MapToDto(TEntity entity, int? currentUserId);
    protected abstract Task<TEntity> MapFromCreateDto(TCreateDto createDto, int currentUserId);
    protected abstract Task MapFromUpdateDto(TEntity entity, TUpdateDto updateDto, int currentUserId);

    // Virtual methods with default implementations
    protected virtual Task<IQueryable<TEntity>> ApplyAccessFilterAsync(IQueryable<TEntity> query, int? currentUserId)
    {
        return Task.FromResult(query);
    }

    protected virtual Task<bool> CanViewEntityAsync(TEntity entity, int? currentUserId)
    {
        return Task.FromResult(true);
    }

    protected virtual Task<bool> CanCreateEntityAsync(TCreateDto createDto, int currentUserId)
    {
        return Task.FromResult(true);
    }

    protected virtual Task<bool> CanUpdateEntityAsync(TEntity entity, TUpdateDto updateDto, int currentUserId)
    {
        return Task.FromResult(true);
    }

    protected virtual Task<bool> CanDeleteEntityAsync(TEntity entity, int currentUserId)
    {
        return Task.FromResult(true);
    }

    // Lifecycle hooks
    protected virtual Task BeforeCreateAsync(TEntity entity, TCreateDto createDto, int currentUserId)
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterCreateAsync(TEntity entity, TCreateDto createDto, int currentUserId)
    {
        return Task.CompletedTask;
    }

    protected virtual Task BeforeUpdateAsync(TEntity entity, TUpdateDto updateDto, int currentUserId)
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterUpdateAsync(TEntity entity, TUpdateDto updateDto, int currentUserId)
    {
        return Task.CompletedTask;
    }

    protected virtual Task BeforeDeleteAsync(TEntity entity, int currentUserId)
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterDeleteAsync(TEntity entity, int currentUserId)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Interface for entities that can be used with BaseCrudService
/// </summary>
public interface IEntity
{
    int Id { get; set; }
}

/// <summary>
/// Interface for entities that are owned by users
/// </summary>
public interface IUserOwnedEntity : IEntity
{
    int UserId { get; set; }
}

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

/// <summary>
/// Enhanced CRUD service with additional common operations
/// </summary>
public abstract class EnhancedCrudService<TEntity, TDto, TCreateDto, TUpdateDto> : BaseCrudService<TEntity, TDto, TCreateDto, TUpdateDto>
    where TEntity : class, IEntity
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected EnhancedCrudService(YapplrDbContext context, ILogger logger) : base(context, logger)
    {
    }

    /// <summary>
    /// Get entities by multiple IDs efficiently
    /// </summary>
    public virtual async Task<ServiceResult<IEnumerable<TDto>>> GetByIdsAsync(IEnumerable<int> ids, int? currentUserId = null)
    {
        try
        {
            var idList = ids.ToList();
            if (!idList.Any())
                return ServiceResult<IEnumerable<TDto>>.Success(Enumerable.Empty<TDto>());

            var query = GetBaseQuery().Where(e => idList.Contains(e.Id));
            query = await ApplyAccessFilterAsync(query, currentUserId);

            var entities = await query.ToListAsync();
            var dtos = new List<TDto>();

            foreach (var entity in entities)
            {
                if (await CanViewEntityAsync(entity, currentUserId))
                {
                    dtos.Add(await MapToDto(entity, currentUserId));
                }
            }

            return ServiceResult<IEnumerable<TDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            LogError(ex, nameof(GetByIdsAsync), new { ids, currentUserId });
            return ServiceResult<IEnumerable<TDto>>.Failure("An error occurred while retrieving entities", ex);
        }
    }

    /// <summary>
    /// Search entities with a search term
    /// </summary>
    public virtual async Task<ServiceResult<PaginatedResult<TDto>>> SearchAsync(
        string searchTerm,
        int page = 1,
        int pageSize = 25,
        int? currentUserId = null)
    {
        try
        {
            var query = GetBaseQuery();
            query = await ApplyAccessFilterAsync(query, currentUserId);
            query = ApplySearchFilter(query, searchTerm);

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
            LogError(ex, nameof(SearchAsync), new { searchTerm, page, pageSize, currentUserId });
            return ServiceResult<PaginatedResult<TDto>>.Failure("An error occurred while searching entities", ex);
        }
    }

    /// <summary>
    /// Bulk delete entities
    /// </summary>
    public virtual async Task<ServiceResult<int>> BulkDeleteAsync(IEnumerable<int> ids, int currentUserId)
    {
        try
        {
            var idList = ids.ToList();
            if (!idList.Any())
                return ServiceResult<int>.Success(0);

            var entities = await GetBaseQuery()
                .Where(e => idList.Contains(e.Id))
                .ToListAsync();

            var deletedCount = 0;
            foreach (var entity in entities)
            {
                if (await CanDeleteEntityAsync(entity, currentUserId))
                {
                    await BeforeDeleteAsync(entity, currentUserId);
                    _context.Set<TEntity>().Remove(entity);
                    await AfterDeleteAsync(entity, currentUserId);
                    deletedCount++;
                }
            }

            await _context.SaveChangesAsync();
            LogOperation(nameof(BulkDeleteAsync), new { deletedCount, currentUserId });

            return ServiceResult<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            LogError(ex, nameof(BulkDeleteAsync), new { ids, currentUserId });
            return ServiceResult<int>.Failure("An error occurred while bulk deleting entities", ex);
        }
    }

    /// <summary>
    /// Check if entity exists
    /// </summary>
    public virtual async Task<ServiceResult<bool>> ExistsAsync(int id, int? currentUserId = null)
    {
        try
        {
            var query = GetBaseQuery().Where(e => e.Id == id);
            query = await ApplyAccessFilterAsync(query, currentUserId);

            var exists = await query.AnyAsync();
            return ServiceResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            LogError(ex, nameof(ExistsAsync), new { id, currentUserId });
            return ServiceResult<bool>.Failure("An error occurred while checking entity existence", ex);
        }
    }

    /// <summary>
    /// Get count of entities
    /// </summary>
    public virtual async Task<ServiceResult<int>> GetCountAsync(int? currentUserId = null)
    {
        try
        {
            var query = GetBaseQuery();
            query = await ApplyAccessFilterAsync(query, currentUserId);

            var count = await query.CountAsync();
            return ServiceResult<int>.Success(count);
        }
        catch (Exception ex)
        {
            LogError(ex, nameof(GetCountAsync), new { currentUserId });
            return ServiceResult<int>.Failure("An error occurred while counting entities", ex);
        }
    }

    /// <summary>
    /// Apply search filter to query - override in derived classes for specific search logic
    /// </summary>
    protected virtual IQueryable<TEntity> ApplySearchFilter(IQueryable<TEntity> query, string searchTerm)
    {
        // Default implementation returns unfiltered query
        // Override in derived classes to implement specific search logic
        return query;
    }
}

/// <summary>
/// Interface for soft-deletable entities
/// </summary>
public interface ISoftDeletableEntity : IEntity
{
    bool IsDeletedByUser { get; set; }
    DateTime? DeletedByUserAt { get; set; }
}

/// <summary>
/// Interface for entities that can be hidden by moderators
/// </summary>
public interface IModeratedEntity : IEntity
{
    bool IsHidden { get; set; }
    int? HiddenByUserId { get; set; }
    DateTime? HiddenAt { get; set; }
    string? HiddenReason { get; set; }
}

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
