namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Notification history entry
/// </summary>
public class NotificationHistoryEntry
{
    public string NotificationId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
}