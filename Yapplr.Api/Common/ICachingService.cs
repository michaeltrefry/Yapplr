namespace Yapplr.Api.Common;

/// <summary>
/// Utilities for caching database query results and other data
/// </summary>
public interface ICachingService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    string GenerateKey(string prefix, params object[] parameters);
}