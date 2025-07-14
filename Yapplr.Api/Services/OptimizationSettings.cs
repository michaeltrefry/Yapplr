namespace Yapplr.Api.Services;

/// <summary>
/// Notification payload optimization settings
/// </summary>
public class OptimizationSettings
{
    public bool EnableCompression { get; set; } = true;
    public int CompressionThreshold { get; set; } = 1024; // Only compress if payload > 1KB
    public bool TruncateLongMessages { get; set; } = true;
    public int MaxMessageLength { get; set; } = 200;
    public bool RemoveUnnecessaryFields { get; set; } = true;
    public bool UseShortFieldNames { get; set; } = true;
}