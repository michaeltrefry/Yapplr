using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface INotificationService
{
    /// <summary>
    /// Creates a new notification
    /// </summary>
    Task<NotificationDto?> CreateNotificationAsync(CreateNotificationDto createDto);
    
    /// <summary>
    /// Creates mention notifications for users mentioned in content
    /// </summary>
    Task CreateMentionNotificationsAsync(string? content, int mentioningUserId, int? postId = null, int? commentId = null);
    
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
    /// Creates a like notification
    /// </summary>
    Task CreateLikeNotificationAsync(int likedUserId, int likingUserId, int postId);

    /// <summary>
    /// Creates a comment like notification
    /// </summary>
    Task CreateCommentLikeNotificationAsync(int commentOwnerId, int likingUserId, int postId, int commentId);

    /// <summary>
    /// Creates a reaction notification
    /// </summary>
    Task CreateReactionNotificationAsync(int reactedUserId, int reactingUserId, int postId, ReactionType reactionType);

    /// <summary>
    /// Creates a comment reaction notification
    /// </summary>
    Task CreateCommentReactionNotificationAsync(int commentOwnerId, int reactingUserId, int postId, int commentId, ReactionType reactionType);

    /// <summary>
    /// Creates a repost notification
    /// </summary>
    Task CreateRepostNotificationAsync(int originalUserId, int repostingUserId, int postId);

    /// <summary>
    /// Creates a follow notification
    /// </summary>
    Task CreateFollowNotificationAsync(int followedUserId, int followingUserId);

    /// <summary>
    /// Creates a follow request notification
    /// </summary>
    Task CreateFollowRequestNotificationAsync(int requestedUserId, int requesterUserId);
    
    /// <summary>
    /// Creates a comment notification
    /// </summary>
    Task CreateCommentNotificationAsync(int postOwnerId, int commentingUserId, int postId, int commentId, string commentContent);
    
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

    // Moderation notifications
    /// <summary>
    /// Creates a user suspension notification
    /// </summary>
    Task CreateUserSuspensionNotificationAsync(int userId, string reason, DateTime? suspendedUntil, string moderatorUsername);

    /// <summary>
    /// Creates a user ban notification
    /// </summary>
    Task CreateUserBanNotificationAsync(int userId, string reason, bool isShadowBan, string moderatorUsername);

    /// <summary>
    /// Creates a user unsuspension notification
    /// </summary>
    Task CreateUserUnsuspensionNotificationAsync(int userId, string moderatorUsername);

    /// <summary>
    /// Creates a user unban notification
    /// </summary>
    Task CreateUserUnbanNotificationAsync(int userId, string moderatorUsername);

    /// <summary>
    /// Creates a content hidden notification
    /// </summary>
    Task CreateContentHiddenNotificationAsync(int userId, string contentType, int contentId, string reason, string moderatorUsername);

    /// <summary>
    /// Creates a content deleted notification
    /// </summary>
    Task CreateContentDeletedNotificationAsync(int userId, string contentType, int contentId, string reason, string moderatorUsername);

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
    /// Creates a system message notification
    /// </summary>
    Task CreateSystemMessageNotificationAsync(int userId, string message);

    /// <summary>
    /// Creates a video processing completion notification
    /// </summary>
    Task CreateVideoProcessingCompletedNotificationAsync(int userId, int postId);
}
