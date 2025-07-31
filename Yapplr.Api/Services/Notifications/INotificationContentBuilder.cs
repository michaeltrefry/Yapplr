using Yapplr.Api.Models;

namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Service for building consistent notification content across all delivery channels.
/// Centralizes notification content generation to eliminate duplication and ensure consistency.
/// </summary>
public interface INotificationContentBuilder
{
    /// <summary>
    /// Builds content for a like notification
    /// </summary>
    NotificationContent BuildLikeNotification(string likerUsername, int postId);

    /// <summary>
    /// Builds content for a comment like notification
    /// </summary>
    NotificationContent BuildCommentLikeNotification(string likerUsername, int postId, int commentId);

    /// <summary>
    /// Builds content for a comment notification
    /// </summary>
    NotificationContent BuildCommentNotification(string commenterUsername, int postId, int commentId);

    /// <summary>
    /// Builds content for a follow notification
    /// </summary>
    NotificationContent BuildFollowNotification(string followerUsername);

    /// <summary>
    /// Builds content for a follow request notification
    /// </summary>
    NotificationContent BuildFollowRequestNotification(string requesterUsername);

    /// <summary>
    /// Builds content for a follow request approved notification
    /// </summary>
    NotificationContent BuildFollowRequestApprovedNotification(string approverUsername);

    /// <summary>
    /// Builds content for a message notification
    /// </summary>
    NotificationContent BuildMessageNotification(string senderUsername, string messageContent, int conversationId);

    /// <summary>
    /// Builds content for a mention notification
    /// </summary>
    NotificationContent BuildMentionNotification(string mentionerUsername, int postId, int? commentId = null);

    /// <summary>
    /// Builds content for a reply notification
    /// </summary>
    NotificationContent BuildReplyNotification(string replierUsername, int postId, int commentId);

    /// <summary>
    /// Builds content for a repost notification
    /// </summary>
    NotificationContent BuildRepostNotification(string reposterUsername, int postId);

    /// <summary>
    /// Builds content for a system message notification
    /// </summary>
    NotificationContent BuildSystemMessageNotification(string title, string message, Dictionary<string, string>? additionalData = null);

    /// <summary>
    /// Builds content for a video processing completed notification
    /// </summary>
    NotificationContent BuildVideoProcessingCompletedNotification(int postId);

    /// <summary>
    /// Builds content for a test notification
    /// </summary>
    NotificationContent BuildTestNotification(string? customMessage = null);
}
