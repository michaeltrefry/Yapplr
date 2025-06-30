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
    Task CreateMentionNotificationsAsync(string content, int mentioningUserId, int? postId = null, int? commentId = null);
    
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
    /// Creates a like notification
    /// </summary>
    Task CreateLikeNotificationAsync(int likedUserId, int likingUserId, int postId);
    
    /// <summary>
    /// Creates a repost notification
    /// </summary>
    Task CreateRepostNotificationAsync(int originalUserId, int repostingUserId, int postId);
    
    /// <summary>
    /// Creates a follow notification
    /// </summary>
    Task CreateFollowNotificationAsync(int followedUserId, int followingUserId);
    
    /// <summary>
    /// Creates a comment notification
    /// </summary>
    Task CreateCommentNotificationAsync(int postOwnerId, int commentingUserId, int postId, int commentId);
    
    /// <summary>
    /// Deletes notifications related to a post (when post is deleted)
    /// </summary>
    Task DeletePostNotificationsAsync(int postId);
    
    /// <summary>
    /// Deletes notifications related to a comment (when comment is deleted)
    /// </summary>
    Task DeleteCommentNotificationsAsync(int commentId);
}
