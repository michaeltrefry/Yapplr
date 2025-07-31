namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Enhancement health report
/// </summary>
public class EnhancementHealthReport
{
    public bool IsHealthy { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public Dictionary<string, bool> FeaturesEnabled { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object>? MetricsStats { get; set; }
    public Dictionary<string, object>? RateLimitStats { get; set; }
    public Dictionary<string, object>? ContentFilterStats { get; set; }
    public Dictionary<string, object>? CompressionStats { get; set; }
}