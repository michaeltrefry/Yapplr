using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

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
