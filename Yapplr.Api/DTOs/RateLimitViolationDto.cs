namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for rate limiting violation details
/// </summary>
public class RateLimitViolationDto
{
    public string OperationType { get; set; } = string.Empty;
    public string LimitType { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public DateTime ViolationTime { get; set; }
    public TimeSpan RetryAfter { get; set; }
}
