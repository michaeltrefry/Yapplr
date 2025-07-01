using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using System.Text.Json;

namespace Yapplr.Api.Services;

public class FirebaseService : IFirebaseService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<FirebaseService> _logger;
    private readonly FirebaseMessaging? _messaging;
    private static readonly object _lock = new object();
    private static bool _isInitialized = false;

    public FirebaseService(YapplrDbContext context, ILogger<FirebaseService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;

        // Initialize Firebase once using thread-safe singleton pattern
        lock (_lock)
        {
            if (!_isInitialized)
            {
                try
                {
                    var projectId = configuration["Firebase:ProjectId"] ?? "yapplr";

                    // Try to use Application Default Credentials first
                    var credential = GoogleCredential.GetApplicationDefault();

                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = credential,
                        ProjectId = projectId,
                    });

                    _logger.LogInformation("Firebase initialized successfully using Application Default Credentials");
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Firebase with Application Default Credentials. Firebase notifications will be disabled.");
                    _messaging = null;
                    return;
                }
            }
        }

        // Get the messaging instance (this is safe to call multiple times)
        try
        {
            _messaging = FirebaseMessaging.DefaultInstance;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Firebase messaging instance. Firebase notifications will be disabled.");
            _messaging = null;
        }
    }

    public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
    {
        if (_messaging == null)
        {
            _logger.LogWarning("Firebase messaging not initialized. Skipping notification.");
            return false;
        }

        if (string.IsNullOrEmpty(fcmToken))
        {
            _logger.LogWarning("FCM token is null or empty");
            return false;
        }

        try
        {
            var message = new FirebaseAdmin.Messaging.Message()
            {
                Token = fcmToken,
                Notification = new FirebaseAdmin.Messaging.Notification()
                {
                    Title = title,
                    Body = body,
                },
                Data = data,
                Android = new AndroidConfig()
                {
                    Notification = new AndroidNotification()
                    {
                        Icon = "ic_notification",
                        Color = "#1DA1F2", // Twitter blue
                        ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                    }
                },
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Icon = "/next.svg",
                        Badge = "/next.svg",
                    }
                }
            };

            string response = await _messaging.SendAsync(message);
            _logger.LogInformation($"Successfully sent message: {response}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending FCM notification to token: {fcmToken}");
            return false;
        }
    }

    public async Task<bool> SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            return false;
        }

        var title = "New Message";
        var body = $"@{senderUsername}: {TruncateMessage(messageContent)}";
        var data = new Dictionary<string, string>
        {
            ["type"] = "message",
            ["conversationId"] = conversationId.ToString(),
            ["senderUsername"] = senderUsername
        };

        return await SendNotificationAsync(user.FcmToken, title, body, data);
    }

    public async Task<bool> SendMentionNotificationAsync(int userId, string mentionerUsername, int postId, int? commentId = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            return false;
        }

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

        return await SendNotificationAsync(user.FcmToken, title, body, data);
    }

    public async Task<bool> SendReplyNotificationAsync(int userId, string replierUsername, int postId, int commentId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            return false;
        }

        var title = "New Reply";
        var body = $"@{replierUsername} replied to your comment";
        var data = new Dictionary<string, string>
        {
            ["type"] = "reply",
            ["postId"] = postId.ToString(),
            ["commentId"] = commentId.ToString(),
            ["replierUsername"] = replierUsername
        };

        return await SendNotificationAsync(user.FcmToken, title, body, data);
    }

    public async Task<bool> SendFollowNotificationAsync(int userId, string followerUsername)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            return false;
        }

        var title = "New Follower";
        var body = $"@{followerUsername} started following you";
        var data = new Dictionary<string, string>
        {
            ["type"] = "follow",
            ["userId"] = userId.ToString(),
            ["followerUsername"] = followerUsername
        };

        return await SendNotificationAsync(user.FcmToken, title, body, data);
    }

    public async Task<bool> SendLikeNotificationAsync(int userId, string likerUsername, int postId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            return false;
        }

        var title = "Post Liked";
        var body = $"@{likerUsername} liked your post";
        var data = new Dictionary<string, string>
        {
            ["type"] = "like",
            ["postId"] = postId.ToString(),
            ["likerUsername"] = likerUsername
        };

        return await SendNotificationAsync(user.FcmToken, title, body, data);
    }

    public async Task<bool> SendRepostNotificationAsync(int userId, string reposterUsername, int postId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            return false;
        }

        var title = "Post Reposted";
        var body = $"@{reposterUsername} reposted your post";
        var data = new Dictionary<string, string>
        {
            ["type"] = "repost",
            ["postId"] = postId.ToString(),
            ["reposterUsername"] = reposterUsername
        };

        return await SendNotificationAsync(user.FcmToken, title, body, data);
    }

    public async Task<bool> SendMulticastNotificationAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null)
    {
        if (_messaging == null)
        {
            _logger.LogWarning("Firebase messaging not initialized. Skipping multicast notification.");
            return false;
        }

        if (fcmTokens == null || !fcmTokens.Any())
        {
            _logger.LogWarning("FCM tokens list is null or empty");
            return false;
        }

        try
        {
            var message = new MulticastMessage()
            {
                Tokens = fcmTokens,
                Notification = new FirebaseAdmin.Messaging.Notification()
                {
                    Title = title,
                    Body = body,
                },
                Data = data,
                Android = new AndroidConfig()
                {
                    Notification = new AndroidNotification()
                    {
                        Icon = "ic_notification",
                        Color = "#1DA1F2",
                        ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                    }
                },
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Icon = "/next.svg",
                        Badge = "/next.svg",
                    }
                }
            };

            var response = await _messaging.SendEachForMulticastAsync(message);
            _logger.LogInformation($"Successfully sent multicast message. Success count: {response.SuccessCount}, Failure count: {response.FailureCount}");
            return response.SuccessCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending multicast FCM notification");
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
