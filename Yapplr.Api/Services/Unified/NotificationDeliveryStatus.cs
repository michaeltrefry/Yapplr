namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Delivery status for a notification
/// </summary>
public class NotificationDeliveryStatus
{
    public string NotificationId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryProvider { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
}