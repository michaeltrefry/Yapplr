namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Represents a delivery event for monitoring
/// </summary>
public class DeliveryEvent
{
    public string Id { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime StartTime { get; set; }
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool Success { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public double LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
}