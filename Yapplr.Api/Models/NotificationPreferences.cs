using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Yapplr.Api.Models;

/// <summary>
/// User's notification preferences
/// </summary>
public class NotificationPreferences
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    // Preferred delivery method
    public NotificationDeliveryMethod PreferredMethod { get; set; } = NotificationDeliveryMethod.Auto;
    
    // Notification type preferences
    public bool EnableMessageNotifications { get; set; } = true;
    public bool EnableMentionNotifications { get; set; } = true;
    public bool EnableReplyNotifications { get; set; } = true;
    public bool EnableCommentNotifications { get; set; } = true;
    public bool EnableFollowNotifications { get; set; } = true;
    public bool EnableLikeNotifications { get; set; } = true;
    public bool EnableRepostNotifications { get; set; } = true;
    public bool EnableFollowRequestNotifications { get; set; } = true;
    
    // Delivery method preferences per notification type
    public NotificationDeliveryMethod MessageDeliveryMethod { get; set; } = NotificationDeliveryMethod.Auto;
    public NotificationDeliveryMethod MentionDeliveryMethod { get; set; } = NotificationDeliveryMethod.Auto;
    public NotificationDeliveryMethod ReplyDeliveryMethod { get; set; } = NotificationDeliveryMethod.Auto;
    public NotificationDeliveryMethod CommentDeliveryMethod { get; set; } = NotificationDeliveryMethod.Auto;
    public NotificationDeliveryMethod FollowDeliveryMethod { get; set; } = NotificationDeliveryMethod.Auto;
    public NotificationDeliveryMethod LikeDeliveryMethod { get; set; } = NotificationDeliveryMethod.Auto;
    public NotificationDeliveryMethod RepostDeliveryMethod { get; set; } = NotificationDeliveryMethod.Auto;
    public NotificationDeliveryMethod FollowRequestDeliveryMethod { get; set; } = NotificationDeliveryMethod.Auto;
    
    // Quiet hours
    public bool EnableQuietHours { get; set; } = false;
    public TimeOnly QuietHoursStart { get; set; } = new TimeOnly(22, 0); // 10 PM
    public TimeOnly QuietHoursEnd { get; set; } = new TimeOnly(8, 0); // 8 AM
    [StringLength(50)]
    public string QuietHoursTimezone { get; set; } = "UTC";
    
    // Frequency limits
    public bool EnableFrequencyLimits { get; set; } = false;
    public int MaxNotificationsPerHour { get; set; } = 10;
    public int MaxNotificationsPerDay { get; set; } = 100;
    
    // Delivery confirmation
    public bool RequireDeliveryConfirmation { get; set; } = false;
    public bool EnableReadReceipts { get; set; } = false;
    
    // Message history
    public bool EnableMessageHistory { get; set; } = true;
    public int MessageHistoryDays { get; set; } = 30;
    public bool EnableOfflineReplay { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}

/// <summary>
/// Notification delivery methods
/// </summary>
public enum NotificationDeliveryMethod
{
    Auto = 0,           // Use the best available method (Firebase -> SignalR -> Polling)
    FirebaseOnly = 1,   // Only use Firebase push notifications
    SignalROnly = 2,    // Only use SignalR real-time notifications
    PollingOnly = 3,    // Only use polling (no real-time)
    Disabled = 4        // Disable this notification type
}

/// <summary>
/// DTO for updating notification preferences
/// </summary>
public class UpdateNotificationPreferencesDto
{
    public NotificationDeliveryMethod? PreferredMethod { get; set; }
    
    public bool? EnableMessageNotifications { get; set; }
    public bool? EnableMentionNotifications { get; set; }
    public bool? EnableReplyNotifications { get; set; }
    public bool? EnableCommentNotifications { get; set; }
    public bool? EnableFollowNotifications { get; set; }
    public bool? EnableLikeNotifications { get; set; }
    public bool? EnableRepostNotifications { get; set; }
    public bool? EnableFollowRequestNotifications { get; set; }
    
    public NotificationDeliveryMethod? MessageDeliveryMethod { get; set; }
    public NotificationDeliveryMethod? MentionDeliveryMethod { get; set; }
    public NotificationDeliveryMethod? ReplyDeliveryMethod { get; set; }
    public NotificationDeliveryMethod? CommentDeliveryMethod { get; set; }
    public NotificationDeliveryMethod? FollowDeliveryMethod { get; set; }
    public NotificationDeliveryMethod? LikeDeliveryMethod { get; set; }
    public NotificationDeliveryMethod? RepostDeliveryMethod { get; set; }
    public NotificationDeliveryMethod? FollowRequestDeliveryMethod { get; set; }
    
    public bool? EnableQuietHours { get; set; }
    public TimeOnly? QuietHoursStart { get; set; }
    public TimeOnly? QuietHoursEnd { get; set; }
    public string? QuietHoursTimezone { get; set; }
    
    public bool? EnableFrequencyLimits { get; set; }
    public int? MaxNotificationsPerHour { get; set; }
    public int? MaxNotificationsPerDay { get; set; }
    
    public bool? RequireDeliveryConfirmation { get; set; }
    public bool? EnableReadReceipts { get; set; }
    
    public bool? EnableMessageHistory { get; set; }
    public int? MessageHistoryDays { get; set; }
    public bool? EnableOfflineReplay { get; set; }
}

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

/// <summary>
/// Notification history entry for replay functionality
/// </summary>
public class NotificationHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [StringLength(64)]
    public string NotificationId { get; set; } = string.Empty;
    [StringLength(100)]
    public string NotificationType { get; set; } = string.Empty;
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;
    [StringLength(1000)]
    public string Body { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? DataJson { get; set; } // JSON serialized data
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public bool WasDelivered { get; set; } = false;
    public bool WasReplayed { get; set; } = false;
    public DateTime? ReplayedAt { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
    
    // Helper property to deserialize data
    [NotMapped]
    public Dictionary<string, string>? Data
    {
        get => string.IsNullOrEmpty(DataJson) 
            ? null 
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(DataJson);
        set => DataJson = value == null 
            ? null 
            : System.Text.Json.JsonSerializer.Serialize(value);
    }
}
