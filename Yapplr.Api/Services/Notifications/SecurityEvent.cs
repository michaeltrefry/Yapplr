namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Security event for auditing
/// </summary>
public class SecurityEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public SecurityEventSeverity Severity { get; set; } = SecurityEventSeverity.Low;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}