using StackExchange.Redis;
using System.Text.Json;
using Yapplr.Api.Common;

namespace Yapplr.Api.Services;

/// <summary>
/// Redis-based implementation of the caching service for distributed caching
/// </summary>
public class RedisCachingService : ICachingService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCachingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCachingService(IConnectionMultiplexer redis, ILogger<RedisCachingService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from Redis cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var expiry = expiration ?? TimeSpan.FromMinutes(5); // Default 5 minutes

            await _database.StringSetAsync(key, json, expiry);
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Redis cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Removed cache entry for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing Redis cache for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: $"*{pattern}*").ToArray();
            
            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keys.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing Redis cache entries by pattern: {Pattern}", pattern);
        }
    }

    public string GenerateKey(string prefix, params object[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return prefix;

        var paramString = string.Join(":", parameters.Select(p => 
            p?.ToString()?.Replace(":", "_") ?? "null"));
        
        return $"{prefix}:{paramString}";
    }
}
