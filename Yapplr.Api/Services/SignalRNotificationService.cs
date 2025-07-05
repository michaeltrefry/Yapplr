using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Yapplr.Api.Data;
using Yapplr.Api.Hubs;
using Yapplr.Api.Models;
using Yapplr.Api.Configuration;

namespace Yapplr.Api.Services;

/// <summary>
/// SignalR-based notification service that implements real-time notifications
/// </summary>
public class SignalRNotificationService : IRealtimeNotificationProvider
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly YapplrDbContext _context;
    private readonly ILogger<SignalRNotificationService> _logger;
    private readonly INotificationMetricsService _metricsService;
    private readonly bool _isEnabled;

    public SignalRNotificationService(
        IHubContext<NotificationHub> hubContext,
        YapplrDbContext context,
        ILogger<SignalRNotificationService> logger,
        INotificationMetricsService metricsService,
        IOptions<NotificationProvidersConfiguration> notificationOptions)
    {
        _hubContext = hubContext;
        _context = context;
        _logger = logger;
        _metricsService = metricsService;
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

        // Start metrics tracking
        var trackingId = _metricsService.StartDeliveryTracking(userId, "generic", "SignalR");

        try
        {
            await _hubContext.SendNotificationToUserAsync(
                userId,
                "generic",
                title,
                body,
                data
            );

            // Record successful delivery
            await _metricsService.CompleteDeliveryTrackingAsync(trackingId, true);

            _logger.LogInformation("SignalR notification sent to user {UserId}: {Title}", userId, title);
            return true;
        }
        catch (Exception ex)
        {
            // Record failed delivery
            await _metricsService.CompleteDeliveryTrackingAsync(trackingId, false, ex.Message);

            _logger.LogError(ex, "Failed to send SignalR notification to user {UserId}: {Title}", userId, title);
            return false;
        }
    }

    public async Task<bool> SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId)
    {
        try
        {
            var title = "New Message";
            var body = $"@{senderUsername}: {TruncateMessage(messageContent)}";
            var data = new Dictionary<string, string>
            {
                ["type"] = "message",
                ["conversationId"] = conversationId.ToString(),
                ["senderUsername"] = senderUsername
            };

            await _hubContext.SendNotificationToUserAsync(
                userId,
                "message",
                title,
                body,
                data
            );

            _logger.LogInformation("SignalR message notification sent to user {UserId} from {SenderUsername}", userId, senderUsername);
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
            var title = "You were mentioned";
            var body = commentId.HasValue 
                ? $"@{mentionerUsername} mentioned you in a comment"
                : $"@{mentionerUsername} mentioned you in a post";
            
            var data = new Dictionary<string, string>
            {
                ["type"] = "mention",
                ["postId"] = postId.ToString(),
                ["mentionerUsername"] = mentionerUsername
            };

            if (commentId.HasValue)
            {
                data["commentId"] = commentId.Value.ToString();
            }

            await _hubContext.SendNotificationToUserAsync(
                userId,
                "mention",
                title,
                body,
                data
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
            var title = "New Reply";
            var body = $"@{replierUsername} replied to your comment";
            var data = new Dictionary<string, string>
            {
                ["type"] = "reply",
                ["postId"] = postId.ToString(),
                ["commentId"] = commentId.ToString(),
                ["replierUsername"] = replierUsername
            };

            await _hubContext.SendNotificationToUserAsync(
                userId,
                "reply",
                title,
                body,
                data
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
            var title = "New Comment";
            var body = $"@{commenterUsername} commented on your post";
            var data = new Dictionary<string, string>
            {
                ["type"] = "comment",
                ["postId"] = postId.ToString(),
                ["commentId"] = commentId.ToString(),
                ["commenterUsername"] = commenterUsername
            };

            await _hubContext.SendNotificationToUserAsync(
                userId,
                "comment",
                title,
                body,
                data
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
            var title = "New Follower";
            var body = $"@{followerUsername} started following you";
            var data = new Dictionary<string, string>
            {
                ["type"] = "follow",
                ["followerUsername"] = followerUsername
            };

            await _hubContext.SendNotificationToUserAsync(
                userId,
                "follow",
                title,
                body,
                data
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
            var title = "Follow Request";
            var body = $"@{requesterUsername} wants to follow you";
            var data = new Dictionary<string, string>
            {
                ["type"] = "follow_request",
                ["requesterUsername"] = requesterUsername
            };

            await _hubContext.SendNotificationToUserAsync(
                userId,
                "follow_request",
                title,
                body,
                data
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
            var title = "Follow Request Approved";
            var body = $"@{approverUsername} approved your follow request";
            var data = new Dictionary<string, string>
            {
                ["type"] = "follow_request_approved",
                ["approverUsername"] = approverUsername
            };

            await _hubContext.SendNotificationToUserAsync(
                userId,
                "follow_request_approved",
                title,
                body,
                data
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
            var title = "New Like";
            var body = $"@{likerUsername} liked your post";
            var data = new Dictionary<string, string>
            {
                ["type"] = "like",
                ["postId"] = postId.ToString(),
                ["likerUsername"] = likerUsername
            };

            await _hubContext.SendNotificationToUserAsync(
                userId,
                "like",
                title,
                body,
                data
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

    public async Task<bool> SendRepostNotificationAsync(int userId, string reposterUsername, int postId)
    {
        try
        {
            var title = "New Repost";
            var body = $"@{reposterUsername} reposted your post";
            var data = new Dictionary<string, string>
            {
                ["type"] = "repost",
                ["postId"] = postId.ToString(),
                ["reposterUsername"] = reposterUsername
            };

            await _hubContext.SendNotificationToUserAsync(
                userId,
                "repost",
                title,
                body,
                data
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
