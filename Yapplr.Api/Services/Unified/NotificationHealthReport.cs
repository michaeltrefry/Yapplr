namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Health report for the notification system
/// </summary>
public class NotificationHealthReport
{
    public bool IsHealthy { get; set; }
    public Dictionary<string, ComponentHealth> ComponentHealth { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}