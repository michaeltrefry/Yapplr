namespace Yapplr.Api.Services;

/// <summary>
/// Rate limit configuration for different notification types
/// </summary>
public class RateLimitConfig
{
    public int MaxRequestsPerMinute { get; set; } = 10;
    public int MaxRequestsPerHour { get; set; } = 100;
    public int MaxRequestsPerDay { get; set; } = 1000;
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);
    public bool EnableBurstProtection { get; set; } = true;
    public int BurstThreshold { get; set; } = 5; // Max requests in 10 seconds
}