namespace Yapplr.Api.Services;

/// <summary>
/// Rate limit result
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RemainingRequests { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public string? ViolationType { get; set; }
    public DateTime? ResetTime { get; set; }
}