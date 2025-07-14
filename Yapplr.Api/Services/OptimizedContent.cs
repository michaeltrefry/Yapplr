namespace Yapplr.Api.Services;

/// <summary>
/// Optimized notification content for specific delivery methods
/// </summary>
public class OptimizedContent
{
    public string Content { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = string.Empty;
    public Dictionary<string, object> Optimizations { get; set; } = new();
    public int OriginalLength { get; set; }
    public int OptimizedLength { get; set; }
    public double CompressionRatio => OriginalLength > 0 ? (double)OptimizedLength / OriginalLength : 1.0;
}
