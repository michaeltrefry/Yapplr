namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Request object for sending notifications through the unified system
/// </summary>
public class NotificationRequest
{
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime? ScheduledFor { get; set; }
    public bool RequireDeliveryConfirmation { get; set; } = false;
    public TimeSpan? ExpiresAfter { get; set; }
    public bool SuppressDatabaseNotification { get; set; } = false;
}