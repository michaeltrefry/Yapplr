namespace Yapplr.Api.DTOs;

public class AnalyticsDataSourceDto
{
    public string ConfiguredSource { get; set; } = string.Empty;
    public bool InfluxAvailable { get; set; }
    public string ActualSource { get; set; } = string.Empty;
    public bool DualWriteEnabled { get; set; }
    public DateTime LastChecked { get; set; }
    public Dictionary<string, object> HealthMetrics { get; set; } = new();
    public List<string> Issues { get; set; } = new();
}
