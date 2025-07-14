namespace Yapplr.Api.Common;

/// <summary>
/// Rate limiting error response
/// </summary>
public class RateLimitErrorResponse : ErrorResponse
{
    public int RetryAfterSeconds { get; set; }
    public string LimitType { get; set; } = string.Empty;

    public RateLimitErrorResponse(int retryAfterSeconds, string limitType)
    {
        Type = "rate_limit_exceeded";
        Message = $"Rate limit exceeded for {limitType}";
        RetryAfterSeconds = retryAfterSeconds;
        LimitType = limitType;
    }
}