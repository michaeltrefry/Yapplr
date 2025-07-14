namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Configuration settings for a notification provider
/// </summary>
public class ProviderConfiguration
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }
    public int MaxRetries { get; set; } = 3;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableCircuitBreaker { get; set; } = true;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, object> ProviderSpecificSettings { get; set; } = new();
}