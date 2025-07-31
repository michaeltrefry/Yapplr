namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Represents a notification in the queue system
/// </summary>
public class QueuedNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledFor { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int AttemptCount { get; set; } = 0;
    public int MaxAttempts { get; set; } = 5;
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
    public QueuedNotificationStatus Status { get; set; } = QueuedNotificationStatus.Pending;
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryProvider { get; set; }
}