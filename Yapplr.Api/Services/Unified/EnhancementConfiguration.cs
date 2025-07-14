namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Configuration for enhancement features
/// </summary>
public class EnhancementConfiguration
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnableAuditing { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public bool EnableContentFiltering { get; set; } = true;
    public bool EnableCompression { get; set; } = true;
    public Dictionary<string, object> FeatureSettings { get; set; } = new();
}