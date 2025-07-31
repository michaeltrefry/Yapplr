using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Unified notification service that serves as the single entry point for all notification operations.
/// Replaces NotificationService and CompositeNotificationService with a simplified, focused API.
/// </summary>
public interface IUnifiedNotificationService
{
    #region Core Notification Methods
    
    /// <summary>
    /// Sends a notification using the unified notification system
    /// </summary>
    /// <param name="request">The notification request containing all necessary information</param>
    /// <returns>True if the notification was successfully processed (sent or queued)</returns>
    Task<bool> SendNotificationAsync(NotificationRequest request);
    
    /// <summary>
    /// Sends a test notification to verify the system is working
    /// </summary>
    /// <param name="userId">The user ID to send the test notification to</param>
    /// <returns>True if the test notification was successfully sent</returns>
    Task<bool> SendTestNotificationAsync(int userId);
    
    /// <summary>
    /// Sends the same notification to multiple users efficiently
    /// </summary>
    /// <param name="userIds">List of user IDs to send the notification to</param>
    /// <param name="request">The notification request</param>
    /// <returns>True if all notifications were successfully processed</returns>
    Task<bool> SendMulticastNotificationAsync(List<int> userIds, NotificationRequest request);
    
    #endregion
    
    #region Specific Notification Types
    
    /// <summary>
    /// Sends a message notification
    /// </summary>
    Task SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId, bool suppressDatabaseNotification = false);
    
    /// <summary>
    /// Sends a mention notification
    /// </summary>
    Task SendMentionNotificationAsync(int userId, string mentionerUsername, int postId, int? commentId = null);
    
    /// <summary>
    /// Sends a reply notification
    /// </summary>
    Task SendReplyNotificationAsync(int userId, string replierUsername, int postId, int commentId);
    
    /// <summary>
    /// Sends a comment notification
    /// </summary>
    Task SendCommentNotificationAsync(int userId, string commenterUsername, int postId, int commentId);
    
    /// <summary>
    /// Sends a follow notification
    /// </summary>
    Task SendFollowNotificationAsync(int userId, string followerUsername);
    
    /// <summary>
    /// Sends a follow request notification
    /// </summary>
    Task SendFollowRequestNotificationAsync(int userId, string requesterUsername);
    
    /// <summary>
    /// Sends a follow request approved notification
    /// </summary>
    Task SendFollowRequestApprovedNotificationAsync(int userId, string approverUsername);
    
    /// <summary>
    /// Sends a like notification
    /// </summary>
    Task SendLikeNotificationAsync(int userId, string likerUsername, int postId);

    /// <summary>
    /// Sends a comment like notification
    /// </summary>
    Task SendCommentLikeNotificationAsync(int userId, string likerUsername, int postId, int commentId);

    /// <summary>
    /// Sends a repost notification
    /// </summary>
    Task SendRepostNotificationAsync(int userId, string reposterUsername, int postId);



    #endregion

    #region Legacy Compatibility Methods (for gradual migration)

    /// <summary>
    /// Creates a like notification with blocking and validation logic
    /// </summary>
    Task CreateLikeNotificationAsync(int likedUserId, int likingUserId, int postId);

    /// <summary>
    /// Creates a comment notification with blocking and validation logic
    /// </summary>
    Task CreateCommentNotificationAsync(int postOwnerId, int commentingUserId, int postId, int commentId, string commentContent);

    /// <summary>
    /// Creates a repost notification with blocking and validation logic
    /// </summary>
    Task CreateRepostNotificationAsync(int originalUserId, int repostingUserId, int postId);

    /// <summary>
    /// Creates mention notifications for users mentioned in content
    /// </summary>
    Task CreateMentionNotificationsAsync(string? content, int mentioningUserId, int? postId = null, int? commentId = null);

    /// <summary>
    /// Creates a follow notification
    /// </summary>
    Task CreateFollowNotificationAsync(int followedUserId, int followingUserId);

    /// <summary>
    /// Creates a follow request notification
    /// </summary>
    Task CreateFollowRequestNotificationAsync(int requestedUserId, int requesterUserId);

    /// <summary>
    /// Creates a reaction notification
    /// </summary>
    Task CreateReactionNotificationAsync(int reactedUserId, int reactingUserId, int postId, ReactionType reactionType);

    /// <summary>
    /// Creates a comment reaction notification
    /// </summary>
    Task CreateCommentReactionNotificationAsync(int commentOwnerId, int reactingUserId, int postId, int commentId, ReactionType reactionType);

    /// <summary>
    /// Creates a user suspension notification
    /// </summary>
    Task CreateUserSuspensionNotificationAsync(int userId, string reason, DateTime? suspendedUntil, string moderatorUsername);

    /// <summary>
    /// Creates a user unsuspension notification
    /// </summary>
    Task CreateUserUnsuspensionNotificationAsync(int userId, string moderatorUsername);

    /// <summary>
    /// Creates a user ban notification
    /// </summary>
    Task CreateUserBanNotificationAsync(int userId, string reason, bool isShadowBan, string moderatorUsername);

    /// <summary>
    /// Creates a user unban notification
    /// </summary>
    Task CreateUserUnbanNotificationAsync(int userId, string moderatorUsername);

    /// <summary>
    /// Creates a content hidden notification
    /// </summary>
    Task CreateContentHiddenNotificationAsync(int userId, string contentType, int contentId, string reason, string moderatorUsername);

    /// <summary>
    /// Creates a content restored notification
    /// </summary>
    Task CreateContentRestoredNotificationAsync(int userId, string contentType, int contentId, string moderatorUsername);

    /// <summary>
    /// Creates an appeal approved notification
    /// </summary>
    Task CreateAppealApprovedNotificationAsync(int userId, int appealId, string reviewNotes, string moderatorUsername);

    /// <summary>
    /// Creates an appeal denied notification
    /// </summary>
    Task CreateAppealDeniedNotificationAsync(int userId, int appealId, string reviewNotes, string moderatorUsername);

    /// <summary>
    /// Creates a video processing completion notification
    /// </summary>
    Task CreateVideoProcessingCompletedNotificationAsync(int userId, int postId);

    #endregion
    
    #region System and Moderation Notifications
    
    /// <summary>
    /// Sends a system message notification
    /// </summary>
    Task SendSystemMessageAsync(int userId, string title, string message, Dictionary<string, string>? data = null);
    
    /// <summary>
    /// Sends a user suspension notification
    /// </summary>
    Task SendUserSuspendedNotificationAsync(int userId, string reason, DateTime suspendedUntil);
    
    /// <summary>
    /// Sends a content hidden notification
    /// </summary>
    Task SendContentHiddenNotificationAsync(int userId, string contentType, int contentId, string reason);
    
    /// <summary>
    /// Sends an appeal approved notification
    /// </summary>
    Task SendAppealApprovedNotificationAsync(int userId, string appealType, int appealId);
    
    /// <summary>
    /// Sends an appeal denied notification
    /// </summary>
    Task SendAppealDeniedNotificationAsync(int userId, string appealType, int appealId, string reason);
    
    #endregion

    #region Notification Management

    /// <summary>
    /// Gets notifications for a user with pagination
    /// </summary>
    Task<NotificationListDto> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 25);

    /// <summary>
    /// Gets the count of unread notifications for a user
    /// </summary>
    Task<int> GetUnreadNotificationCountAsync(int userId);

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    Task<bool> MarkNotificationAsReadAsync(int notificationId, int userId);

    /// <summary>
    /// Marks all notifications as read for a user
    /// </summary>
    Task<bool> MarkAllNotificationsAsReadAsync(int userId);

    /// <summary>
    /// Marks a notification as seen
    /// </summary>
    Task<bool> MarkNotificationAsSeenAsync(int notificationId, int userId);

    /// <summary>
    /// Marks multiple notifications as seen
    /// </summary>
    Task<bool> MarkNotificationsAsSeenAsync(int[] notificationIds, int userId);

    /// <summary>
    /// Marks all notifications as seen for a user
    /// </summary>
    Task<bool> MarkAllNotificationsAsSeenAsync(int userId);

    /// <summary>
    /// Deletes notifications related to a post (when post is deleted)
    /// </summary>
    Task DeletePostNotificationsAsync(int postId);

    /// <summary>
    /// Deletes social interaction and post-specific notifications for a post, preserving user-level system/moderation notifications
    /// </summary>
    Task DeleteSocialNotificationsForPostAsync(int postId);

    /// <summary>
    /// Deletes notifications related to a comment (when comment is deleted)
    /// </summary>
    Task DeleteCommentNotificationsAsync(int commentId);

    #endregion

    #region Delivery Tracking and History

    /// <summary>
    /// Gets delivery status for recent notifications for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="count">Number of recent notifications to check</param>
    /// <returns>List of delivery statuses</returns>
    Task<List<NotificationDeliveryStatus>> GetDeliveryStatusAsync(int userId, int count);

    /// <summary>
    /// Gets notification history for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="count">Number of notifications to return</param>
    /// <returns>List of notification history entries</returns>
    Task<List<NotificationHistoryEntry>> GetNotificationHistoryAsync(int userId, int count);

    /// <summary>
    /// Gets undelivered notifications for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of undelivered notifications</returns>
    Task<List<UndeliveredNotification>> GetUndeliveredNotificationsAsync(int userId);

    /// <summary>
    /// Replays missed notifications for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Number of notifications replayed</returns>
    Task<int> ReplayMissedNotificationsAsync(int userId);

    /// <summary>
    /// Confirms that a notification was delivered
    /// </summary>
    /// <param name="notificationId">The notification ID</param>
    /// <returns>True if confirmation was successful</returns>
    Task<bool> ConfirmDeliveryAsync(string notificationId);

    /// <summary>
    /// Confirms that a notification was read
    /// </summary>
    /// <param name="notificationId">The notification ID</param>
    /// <returns>True if confirmation was successful</returns>
    Task<bool> ConfirmReadAsync(string notificationId);

    /// <summary>
    /// Gets delivery statistics for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="timeWindow">Optional time window to filter statistics</param>
    /// <returns>Delivery statistics</returns>
    Task<Dictionary<string, object>> GetDeliveryStatsAsync(int userId, TimeSpan? timeWindow);

    #endregion

    #region Management and Monitoring

    /// <summary>
    /// Gets comprehensive statistics about the notification system
    /// </summary>
    Task<NotificationStats> GetStatsAsync();

    /// <summary>
    /// Checks if the notification system is healthy and operational
    /// </summary>
    Task<bool> IsHealthyAsync();

    /// <summary>
    /// Gets detailed health information about all notification components
    /// </summary>
    Task<NotificationHealthReport> GetHealthReportAsync();

    /// <summary>
    /// Forces a refresh of all notification system components
    /// </summary>
    Task RefreshSystemAsync();

    #endregion
}