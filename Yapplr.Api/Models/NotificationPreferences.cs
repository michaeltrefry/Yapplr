using System.ComponentModel.DataAnnotations;

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