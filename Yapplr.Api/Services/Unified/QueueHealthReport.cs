namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Health report for the queue system
/// </summary>
public class QueueHealthReport
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}