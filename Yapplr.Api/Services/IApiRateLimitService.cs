namespace Yapplr.Api.Services;

/// <summary>
/// Service for rate limiting API requests based on user trust scores
/// </summary>
public interface IApiRateLimitService
{
    Task<RateLimitResult> CheckRateLimitAsync(int userId, ApiOperation operation);
    Task RecordRequestAsync(int userId, ApiOperation operation);
    Task<List<RateLimitViolation>> GetRecentViolationsAsync(int userId);
    Task<Dictionary<string, object>> GetRateLimitStatsAsync();
    Task ResetUserLimitsAsync(int userId);
    Task<bool> IsUserBlockedAsync(int userId);
    Task BlockUserAsync(int userId, TimeSpan duration, string reason);
    Task UnblockUserAsync(int userId);
}