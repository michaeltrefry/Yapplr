using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

/// <summary>
/// Notification delivery confirmation
/// </summary>
public class NotificationDeliveryConfirmation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [StringLength(64)]
    public string NotificationId { get; set; } = string.Empty;
    [StringLength(64)]
    public string NotificationType { get; set; } = string.Empty;
    public NotificationDeliveryMethod DeliveryMethod { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsDelivered { get; set; } = false;
    public bool IsRead { get; set; } = false;
    [StringLength(256)]
    public string? DeliveryError { get; set; }
    public int RetryCount { get; set; } = 0;
    
    // Navigation property
    public User User { get; set; } = null!;
}
