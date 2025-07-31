namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Health status for individual notification system components
/// </summary>
public class ComponentHealth
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metrics { get; set; } = new();
}