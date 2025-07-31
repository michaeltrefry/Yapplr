namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Represents a notification event for metrics tracking
/// </summary>
public class NotificationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string? TrackingId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty; // sent, delivered, failed, queued, etc.
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool Success { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
    public double? LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}