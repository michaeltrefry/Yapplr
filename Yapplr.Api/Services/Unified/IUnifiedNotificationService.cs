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
    Task SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId);
    
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
    /// Sends a repost notification
    /// </summary>
    Task SendRepostNotificationAsync(int userId, string reposterUsername, int postId);
    
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

/// <summary>
/// Request object for sending notifications through the unified system
/// </summary>
public class NotificationRequest
{
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime? ScheduledFor { get; set; }
    public bool RequireDeliveryConfirmation { get; set; } = false;
    public TimeSpan? ExpiresAfter { get; set; }
}

/// <summary>
/// Comprehensive statistics about the notification system
/// </summary>
public class NotificationStats
{
    public long TotalNotificationsSent { get; set; }
    public long TotalNotificationsDelivered { get; set; }
    public long TotalNotificationsFailed { get; set; }
    public long TotalNotificationsQueued { get; set; }
    public double DeliverySuccessRate { get; set; }
    public double AverageDeliveryTimeMs { get; set; }
    public Dictionary<string, long> NotificationTypeBreakdown { get; set; } = new();
    public Dictionary<string, long> ProviderBreakdown { get; set; } = new();
    public Dictionary<string, double> ProviderSuccessRates { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health report for the notification system
/// </summary>
public class NotificationHealthReport
{
    public bool IsHealthy { get; set; }
    public Dictionary<string, ComponentHealth> ComponentHealth { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Health status for individual notification system components
/// </summary>
public class ComponentHealth
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Priority levels for notifications
/// </summary>
public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
