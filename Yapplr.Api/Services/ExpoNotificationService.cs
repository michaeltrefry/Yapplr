using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for sending push notifications via Expo Push API
/// </summary>
public class ExpoNotificationService : IRealtimeNotificationProvider
{
    private readonly ILogger<ExpoNotificationService> _logger;
    private readonly YapplrDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly bool _isEnabled;

    public string ProviderName => "Expo";

    public ExpoNotificationService(
        ILogger<ExpoNotificationService> logger,
        YapplrDbContext context,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _httpClient = httpClient;
        _isEnabled = configuration.GetValue<bool>("NotificationProviders:Expo:Enabled", false);

        // Configure HttpClient for Expo Push API
        _httpClient.BaseAddress = new Uri("https://exp.host/");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

        _logger.LogDebug("🚀 Expo notification service initialized. Enabled: {IsEnabled}", _isEnabled);
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(_isEnabled);
    }

    public async Task<bool> SendTestNotificationAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.ExpoPushToken == null)
        {
            _logger.LogInformation("No Expo push token available for user {UserId}", userId);
            return false;
        }

        return await SendNotificationAsync(user.ExpoPushToken, "Test Notification", "This is a test notification from Yapplr!");
    }

    public async Task<bool> SendNotificationAsync(int userId, string title, string body, Dictionary<string, string>? data = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.ExpoPushToken == null)
        {
            _logger.LogInformation("No Expo push token available for user {UserId}", userId);
            return false;
        }

        return await SendNotificationAsync(user.ExpoPushToken, title, body, data);
    }

    /// <summary>
    /// Send notification directly to an Expo push token
    /// </summary>
    public async Task<bool> SendNotificationAsync(string expoPushToken, string title, string body, Dictionary<string, string>? data = null)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("Expo service is disabled. Skipping notification: {Title}", title);
            return false;
        }

        if (string.IsNullOrWhiteSpace(expoPushToken))
        {
            _logger.LogInformation("No Expo push token provided, skipping notification: {Title}", title);
            return false;
        }

        if (!IsValidExpoPushToken(expoPushToken))
        {
            _logger.LogWarning("Invalid Expo push token format: {Token}", expoPushToken.Substring(0, Math.Min(30, expoPushToken.Length)) + "...");
            return false;
        }

        try
        {
            var message = new
            {
                to = expoPushToken,
                sound = "default",
                title = title,
                body = body,
                data = data ?? new Dictionary<string, string>()
            };

            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("🚀 Sending Expo notification to token: {Token}...", expoPushToken.Substring(0, Math.Min(30, expoPushToken.Length)) + "...");

            var response = await _httpClient.PostAsync("--/api/v2/push/send", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("✅ Expo notification sent successfully. Response: {Response}", responseContent);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ Expo notification failed. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Exception sending Expo notification: {Error}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Send notifications to multiple Expo push tokens
    /// </summary>
    public async Task<bool> SendMulticastNotificationAsync(List<string> expoPushTokens, string title, string body, Dictionary<string, string>? data = null)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("Expo service is disabled. Skipping multicast notification: {Title}", title);
            return false;
        }

        if (expoPushTokens == null || !expoPushTokens.Any())
        {
            _logger.LogInformation("No Expo push tokens provided, skipping multicast notification: {Title}", title);
            return false;
        }

        // Filter out invalid tokens
        var validTokens = expoPushTokens.Where(IsValidExpoPushToken).ToList();
        if (!validTokens.Any())
        {
            _logger.LogWarning("No valid Expo push tokens found for multicast notification: {Title}", title);
            return false;
        }

        try
        {
            var messages = validTokens.Select(token => new
            {
                to = token,
                sound = "default",
                title = title,
                body = body,
                data = data ?? new Dictionary<string, string>()
            }).ToArray();

            var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("🚀 Sending Expo multicast notification to {Count} tokens", validTokens.Count);

            var response = await _httpClient.PostAsync("--/api/v2/push/send", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("✅ Expo multicast notification sent successfully. Response: {Response}", responseContent);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ Expo multicast notification failed. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Exception sending Expo multicast notification: {Error}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Validate Expo push token format
    /// </summary>
    private static bool IsValidExpoPushToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // Expo push tokens start with "ExponentPushToken[" and end with "]"
        return token.StartsWith("ExponentPushToken[") && token.EndsWith("]") && token.Length > 20;
    }

    // Notification-specific methods for different types of notifications

    public async Task<bool> SendLikeNotificationAsync(int userId, string likerUsername, int postId)
    {
        var title = "New Like";
        var body = $"@{likerUsername} liked your post";
        var data = new Dictionary<string, string>
        {
            ["type"] = "like",
            ["postId"] = postId.ToString(),
            ["likerUsername"] = likerUsername
        };

        return await SendNotificationAsync(userId, title, body, data);
    }

    public async Task<bool> SendCommentNotificationAsync(int userId, string commenterUsername, int postId, int commentId)
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

        return await SendNotificationAsync(userId, title, body, data);
    }

    public async Task<bool> SendFollowNotificationAsync(int userId, string followerUsername)
    {
        var title = "New Follower";
        var body = $"@{followerUsername} started following you";
        var data = new Dictionary<string, string>
        {
            ["type"] = "follow",
            ["followerUsername"] = followerUsername
        };

        return await SendNotificationAsync(userId, title, body, data);
    }

    public async Task<bool> SendFollowRequestNotificationAsync(int userId, string requesterUsername)
    {
        var title = "Follow Request";
        var body = $"@{requesterUsername} wants to follow you";
        var data = new Dictionary<string, string>
        {
            ["type"] = "follow_request",
            ["requesterUsername"] = requesterUsername
        };

        return await SendNotificationAsync(userId, title, body, data);
    }

    public async Task<bool> SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId)
    {
        var title = "New Message";
        var body = $"@{senderUsername}: {TruncateMessage(messageContent)}";
        var data = new Dictionary<string, string>
        {
            ["type"] = "message",
            ["conversationId"] = conversationId.ToString(),
            ["senderUsername"] = senderUsername
        };

        return await SendNotificationAsync(userId, title, body, data);
    }

    private static string TruncateMessage(string message, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        return message.Length <= maxLength ? message : message.Substring(0, maxLength) + "...";
    }

    // Additional required interface methods

    public async Task<bool> SendMentionNotificationAsync(int userId, string mentionerUsername, int postId, int? commentId = null)
    {
        var title = "Mention";
        var body = $"@{mentionerUsername} mentioned you";
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

        return await SendNotificationAsync(userId, title, body, data);
    }

    public async Task<bool> SendReplyNotificationAsync(int userId, string replierUsername, int postId, int commentId)
    {
        var title = "Reply";
        var body = $"@{replierUsername} replied to your comment";
        var data = new Dictionary<string, string>
        {
            ["type"] = "reply",
            ["postId"] = postId.ToString(),
            ["commentId"] = commentId.ToString(),
            ["replierUsername"] = replierUsername
        };

        return await SendNotificationAsync(userId, title, body, data);
    }

    public async Task<bool> SendFollowRequestApprovedNotificationAsync(int userId, string approverUsername)
    {
        var title = "Follow Request Approved";
        var body = $"@{approverUsername} approved your follow request";
        var data = new Dictionary<string, string>
        {
            ["type"] = "follow_request_approved",
            ["approverUsername"] = approverUsername
        };

        return await SendNotificationAsync(userId, title, body, data);
    }

    public async Task<bool> SendRepostNotificationAsync(int userId, string reposterUsername, int postId)
    {
        var title = "Repost";
        var body = $"@{reposterUsername} reposted your post";
        var data = new Dictionary<string, string>
        {
            ["type"] = "repost",
            ["postId"] = postId.ToString(),
            ["reposterUsername"] = reposterUsername
        };

        return await SendNotificationAsync(userId, title, body, data);
    }

    public async Task<bool> SendMulticastNotificationAsync(List<int> userIds, string title, string body, Dictionary<string, string>? data = null)
    {
        if (userIds == null || !userIds.Any())
        {
            _logger.LogInformation("No user IDs provided for multicast notification: {Title}", title);
            return false;
        }

        // Get Expo push tokens for all users
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id) && !string.IsNullOrEmpty(u.ExpoPushToken))
            .Select(u => u.ExpoPushToken!)
            .ToListAsync();

        if (!users.Any())
        {
            _logger.LogInformation("No valid Expo push tokens found for multicast notification: {Title}", title);
            return false;
        }

        return await SendMulticastNotificationAsync(users, title, body, data);
    }
}
