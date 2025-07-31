namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Health status information for a notification provider
/// </summary>
public class ProviderHealth
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsAvailable { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public DateTime? LastSuccessfulDelivery { get; set; }
    public DateTime? LastFailedDelivery { get; set; }
    public int ConsecutiveFailures { get; set; }
    public double SuccessRate { get; set; }
    public double AverageLatencyMs { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}