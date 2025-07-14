namespace Yapplr.Api.Services;

/// <summary>
/// Retry strategy configuration
/// </summary>
public class RetryStrategy
{
    public bool ShouldRetry { get; set; }
    public TimeSpan InitialDelay { get; set; }
    public TimeSpan MaxDelay { get; set; }
    public int MaxAttempts { get; set; }
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool UseJitter { get; set; } = true;
}