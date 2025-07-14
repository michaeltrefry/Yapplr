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