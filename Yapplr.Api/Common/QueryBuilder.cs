using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using Yapplr.Api.Data;

namespace Yapplr.Api.Common;

/// <summary>
/// Advanced query builder for complex database operations
/// </summary>
public class QueryBuilder<T> where T : class
{
    private IQueryable<T> _query;
    private readonly YapplrDbContext _context;

    public QueryBuilder(YapplrDbContext context)
    {
        _context = context;
        _query = context.Set<T>();
    }

    public QueryBuilder(IQueryable<T> query, YapplrDbContext context)
    {
        _query = query;
        _context = context;
    }

    /// <summary>
    /// Add a where condition
    /// </summary>
    public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        _query = _query.Where(predicate);
        return this;
    }

    /// <summary>
    /// Add includes for navigation properties
    /// </summary>
    public QueryBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
    {
        _query = _query.Include(navigationPropertyPath);
        return this;
    }

    /// <summary>
    /// Add ThenInclude for nested navigation properties
    /// </summary>
    public QueryBuilder<T> ThenInclude<TPreviousProperty, TProperty>(
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
        where TPreviousProperty : class
    {
        if (_query is IIncludableQueryable<T, TPreviousProperty> includableQuery)
        {
            _query = includableQuery.ThenInclude(navigationPropertyPath);
        }
        return this;
    }

    /// <summary>
    /// Order by ascending
    /// </summary>
    public QueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        _query = _query.OrderBy(keySelector);
        return this;
    }

    /// <summary>
    /// Order by descending
    /// </summary>
    public QueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        _query = _query.OrderByDescending(keySelector);
        return this;
    }

    /// <summary>
    /// Apply pagination
    /// WARNING: Ensure the query has an OrderBy clause before calling this method
    /// </summary>
    public QueryBuilder<T> Paginate(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        _query = _query.Skip((page - 1) * pageSize).Take(pageSize);
        return this;
    }



    /// <summary>
    /// Use split query for better performance with multiple includes
    /// </summary>
    public QueryBuilder<T> AsSplitQuery()
    {
        _query = _query.AsSplitQuery();
        return this;
    }

    /// <summary>
    /// Execute and return list
    /// </summary>
    public async Task<List<T>> ToListAsync()
    {
        return await _query.ToListAsync();
    }

    /// <summary>
    /// Execute and return first or default
    /// </summary>
    public async Task<T?> FirstOrDefaultAsync()
    {
        return await _query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Execute and return single or default
    /// </summary>
    public async Task<T?> SingleOrDefaultAsync()
    {
        return await _query.SingleOrDefaultAsync();
    }

    /// <summary>
    /// Get count
    /// </summary>
    public async Task<int> CountAsync()
    {
        return await _query.CountAsync();
    }

    /// <summary>
    /// Check if any records exist
    /// </summary>
    public async Task<bool> AnyAsync()
    {
        return await _query.AnyAsync();
    }

    /// <summary>
    /// Get the underlying queryable
    /// </summary>
    public IQueryable<T> AsQueryable()
    {
        return _query;
    }
}