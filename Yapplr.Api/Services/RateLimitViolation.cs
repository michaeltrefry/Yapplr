namespace Yapplr.Api.Services;

/// <summary>
/// Rate limit violation information
/// </summary>
public class RateLimitViolation
{
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public DateTime ViolationTime { get; set; } = DateTime.UtcNow;
    public string LimitType { get; set; } = string.Empty; // "minute", "hour", "day", "burst"
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public TimeSpan RetryAfter { get; set; }
}