using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.Extensions;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services.Notifications.Providers;

/// <summary>
/// SignalR-based notification service that implements real-time notifications
/// </summary>
public class SignalRNotificationProvider : IRealtimeNotificationProvider
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly YapplrDbContext _context;
    private readonly IActiveConversationTracker _conversationTracker;
    private readonly ILogger<SignalRNotificationProvider> _logger;
    private readonly INotificationContentBuilder _contentBuilder;

    private readonly bool _isEnabled;

    public SignalRNotificationProvider(
        IHubContext<NotificationHub> hubContext,
        YapplrDbContext context,
        IActiveConversationTracker conversationTracker,
        ILogger<SignalRNotificationProvider> logger,
        INotificationContentBuilder contentBuilder,
        IOptions<NotificationProvidersConfiguration> notificationOptions)
    {
        _hubContext = hubContext;
        _context = context;
        _conversationTracker = conversationTracker;
        _logger = logger;
        _contentBuilder = contentBuilder;

        _isEnabled = notificationOptions.Value.SignalR.Enabled;

        if (!_isEnabled)
        {
            _logger.LogInformation("SignalR notification service is disabled via configuration");
        }
    }

    public string ProviderName => "SignalR";

    public Task<bool> IsAvailableAsync()
    {
        // SignalR is available if enabled and the service is running
        return Task.FromResult(_isEnabled);
    }

    public async Task<bool> SendTestNotificationAsync(int userId)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("SignalR service is disabled. Skipping test notification for user {UserId}", userId);
            return false;
        }

        try
        {
            await _hubContext.SendNotificationToUserAsync(
                userId,
                "test",
                "Test Notification",
                "This is a test notification from SignalR!",
                new { userId = userId, timestamp = DateTime.UtcNow }
            );

            _logger.LogInformation("SignalR test notification sent to user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR test notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendNotificationAsync(int userId, string title, string body, Dictionary<string, string>? data = null)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("SignalR service is disabled. Skipping notification for user {UserId}: {Title}", userId, title);
            return false;
        }

        try
        {
            await _hubContext.SendNotificationToUserAsync(
                userId,
                data?["type"] ?? "generic",
                title,
                body,
                data
            );

            _logger.LogInformation("SignalR notification sent to user {UserId}: {Title}", userId, title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR notification to user {UserId}: {Title}", userId, title);
            return false;
        }
    }

    /// <summary>
    /// Sends a notification using centralized content
    /// </summary>
    public async Task<bool> SendNotificationAsync(int userId, NotificationContent content)
    {
        return await SendNotificationAsync(userId, content.Title, content.Body, content.Data);
    }

    public async Task<bool> SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId)
    {
        try
        {
            // Check if user is actively viewing this conversation
            var isUserActiveInConversation = await _conversationTracker.IsUserActiveInConversationAsync(userId, conversationId);

            var content = _contentBuilder.BuildMessageNotification(senderUsername, messageContent, conversationId);

            // Add SignalR-specific data
            content.WithData("suppressNotification", isUserActiveInConversation.ToString().ToLower());

            await _hubContext.SendNotificationToUserAsync(
                userId,
                content.NotificationType,
                content.Title,
                content.Body,
                content.Data
            );

            if (isUserActiveInConversation)
            {
                _logger.LogInformation("SignalR message notification sent to user {UserId} from {SenderUsername} (suppressed - user active in conversation)", userId, senderUsername);
            }
            else
            {
                _logger.LogInformation("SignalR message notification sent to user {UserId} from {SenderUsername}", userId, senderUsername);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR message notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendMentionNotificationAsync(int userId, string mentionerUsername, int postId, int? commentId = null)
    {
        try
        {
            var content = _contentBuilder.BuildMentionNotification(mentionerUsername, postId, commentId);

            await _hubContext.SendNotificationToUserAsync(
                userId,
                content.NotificationType,
                content.Title,
                content.Body,
                content.Data
            );

            _logger.LogInformation("SignalR mention notification sent to user {UserId} from {MentionerUsername}", userId, mentionerUsername);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR mention notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendReplyNotificationAsync(int userId, string replierUsername, int postId, int commentId)
    {
        try
        {
            var content = _contentBuilder.BuildReplyNotification(replierUsername, postId, commentId);

            await _hubContext.SendNotificationToUserAsync(
                userId,
                content.NotificationType,
                content.Title,
                content.Body,
                content.Data
            );

            _logger.LogInformation("SignalR reply notification sent to user {UserId} from {ReplierUsername}", userId, replierUsername);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR reply notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendCommentNotificationAsync(int userId, string commenterUsername, int postId, int commentId)
    {
        try
        {
            var content = _contentBuilder.BuildCommentNotification(commenterUsername, postId, commentId);

            await _hubContext.SendNotificationToUserAsync(
                userId,
                content.NotificationType,
                content.Title,
                content.Body,
                content.Data
            );

            _logger.LogInformation("SignalR comment notification sent to user {UserId} from {CommenterUsername}", userId, commenterUsername);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR comment notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendFollowNotificationAsync(int userId, string followerUsername)
    {
        try
        {
            var content = _contentBuilder.BuildFollowNotification(followerUsername);

            await _hubContext.SendNotificationToUserAsync(
                userId,
                content.NotificationType,
                content.Title,
                content.Body,
                content.Data
            );

            _logger.LogInformation("SignalR follow notification sent to user {UserId} from {FollowerUsername}", userId, followerUsername);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR follow notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendFollowRequestNotificationAsync(int userId, string requesterUsername)
    {
        try
        {
            var content = _contentBuilder.BuildFollowRequestNotification(requesterUsername);

            await _hubContext.SendNotificationToUserAsync(
                userId,
                content.NotificationType,
                content.Title,
                content.Body,
                content.Data
            );

            _logger.LogInformation("SignalR follow request notification sent to user {UserId} from {RequesterUsername}", userId, requesterUsername);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR follow request notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendFollowRequestApprovedNotificationAsync(int userId, string approverUsername)
    {
        try
        {
            var content = _contentBuilder.BuildFollowRequestApprovedNotification(approverUsername);

            await _hubContext.SendNotificationToUserAsync(
                userId,
                content.NotificationType,
                content.Title,
                content.Body,
                content.Data
            );

            _logger.LogInformation("SignalR follow request approved notification sent to user {UserId} from {ApproverUsername}", userId, approverUsername);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR follow request approved notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendLikeNotificationAsync(int userId, string likerUsername, int postId)
    {
        try
        {
            var content = _contentBuilder.BuildLikeNotification(likerUsername, postId);

            await _hubContext.SendNotificationToUserAsync(
                userId,
                content.NotificationType,
                content.Title,
                content.Body,
                content.Data
            );

            _logger.LogInformation("SignalR like notification sent to user {UserId} from {LikerUsername}", userId, likerUsername);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR like notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendCommentLikeNotificationAsync(int userId, string likerUsername, int postId, int commentId)
    {
        try
        {
            var content = _contentBuilder.BuildCommentLikeNotification(likerUsername, postId, commentId);

            await _hubContext.SendNotificationToUserAsync(
                userId,
                content.NotificationType,
                content.Title,
                content.Body,
                content.Data
            );

            _logger.LogInformation("SignalR comment like notification sent to user {UserId} from {LikerUsername}", userId, likerUsername);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR comment like notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendRepostNotificationAsync(int userId, string reposterUsername, int postId)
    {
        try
        {
            var content = _contentBuilder.BuildRepostNotification(reposterUsername, postId);

            await _hubContext.SendNotificationToUserAsync(
                userId,
                content.NotificationType,
                content.Title,
                content.Body,
                content.Data
            );

            _logger.LogInformation("SignalR repost notification sent to user {UserId} from {ReposterUsername}", userId, reposterUsername);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR repost notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendMulticastNotificationAsync(List<int> userIds, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            await _hubContext.SendNotificationToUsersAsync(
                userIds,
                "multicast",
                title,
                body,
                data
            );

            _logger.LogInformation("SignalR multicast notification sent to {UserCount} users: {Title}", userIds.Count, title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR multicast notification to {UserCount} users", userIds.Count);
            return false;
        }
    }

    private static string TruncateMessage(string message, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;
            
        return message.Length <= maxLength ? message : message[..maxLength] + "...";
    }
}
