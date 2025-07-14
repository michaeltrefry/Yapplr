using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;

namespace Yapplr.Api.Common;

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