namespace Yapplr.Api.Services;

/// <summary>
/// Generic interface for real-time notification providers
/// </summary>
public interface IRealtimeNotificationProvider
{
    /// <summary>
    /// Gets the name of this notification provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Checks if this provider is available and properly configured
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Sends a test notification to verify the provider is working
    /// </summary>
    Task<bool> SendTestNotificationAsync(int userId);

    /// <summary>
    /// Sends a generic notification with title, body and optional data
    /// </summary>
    Task<bool> SendNotificationAsync(int userId, string title, string body, Dictionary<string, string>? data = null);

    /// <summary>
    /// Sends a message notification
    /// </summary>
    Task<bool> SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId);

    /// <summary>
    /// Sends a mention notification
    /// </summary>
    Task<bool> SendMentionNotificationAsync(int userId, string mentionerUsername, int postId, int? commentId = null);

    /// <summary>
    /// Sends a reply notification
    /// </summary>
    Task<bool> SendReplyNotificationAsync(int userId, string replierUsername, int postId, int commentId);

    /// <summary>
    /// Sends a comment notification
    /// </summary>
    Task<bool> SendCommentNotificationAsync(int userId, string commenterUsername, int postId, int commentId);

    /// <summary>
    /// Sends a follow notification
    /// </summary>
    Task<bool> SendFollowNotificationAsync(int userId, string followerUsername);

    /// <summary>
    /// Sends a follow request notification
    /// </summary>
    Task<bool> SendFollowRequestNotificationAsync(int userId, string requesterUsername);

    /// <summary>
    /// Sends a follow request approved notification
    /// </summary>
    Task<bool> SendFollowRequestApprovedNotificationAsync(int userId, string approverUsername);

    /// <summary>
    /// Sends a like notification
    /// </summary>
    Task<bool> SendLikeNotificationAsync(int userId, string likerUsername, int postId);

    /// <summary>
    /// Sends a repost notification
    /// </summary>
    Task<bool> SendRepostNotificationAsync(int userId, string reposterUsername, int postId);

    /// <summary>
    /// Sends notifications to multiple users
    /// </summary>
    Task<bool> SendMulticastNotificationAsync(List<int> userIds, string title, string body, Dictionary<string, string>? data = null);
}