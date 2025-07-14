namespace Yapplr.Api.Services;

/// <summary>
/// Service for rate limiting notification requests to prevent spam
/// </summary>
public interface INotificationRateLimitService
{
    Task<RateLimitResult> CheckRateLimitAsync(int userId, string notificationType);
    Task RecordRequestAsync(int userId, string notificationType);
    Task<List<RateLimitViolation>> GetRecentViolationsAsync(int userId);
    Task<Dictionary<string, object>> GetRateLimitStatsAsync();
    Task ResetUserLimitsAsync(int userId);
    Task<bool> IsUserBlockedAsync(int userId);
    Task BlockUserAsync(int userId, TimeSpan duration, string reason);
    Task UnblockUserAsync(int userId);
}