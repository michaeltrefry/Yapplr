namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Performance insights and recommendations
/// </summary>
public class PerformanceInsights
{
    public double OverallSuccessRate { get; set; }
    public double AverageDeliveryTime { get; set; }
    public string BestPerformingProvider { get; set; } = string.Empty;
    public string WorstPerformingProvider { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> DetailedMetrics { get; set; } = new();
}