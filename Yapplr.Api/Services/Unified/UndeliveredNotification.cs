namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Undelivered notification
/// </summary>
public class UndeliveredNotification
{
    public string NotificationId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public NotificationPriority Priority { get; set; }
    public string? LastError { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public Dictionary<string, string>? Data { get; set; }
}