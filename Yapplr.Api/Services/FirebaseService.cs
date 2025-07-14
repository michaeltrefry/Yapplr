using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Yapplr.Api.Data;
using Yapplr.Api.Configuration;

namespace Yapplr.Api.Services;

public class FirebaseService : IFirebaseService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<FirebaseService> _logger;
    private readonly FirebaseMessaging? _messaging;
    private readonly bool _isEnabled;
    private static readonly object _lock = new object();
    private static bool _isInitialized;

    public string ProviderName => "Firebase";

    public FirebaseService(YapplrDbContext context, ILogger<FirebaseService> logger, IConfiguration configuration, IOptions<NotificationProvidersConfiguration> notificationOptions)
    {
        _context = context;
        _logger = logger;
        _isEnabled = notificationOptions.Value.Firebase.Enabled;

        if (!_isEnabled)
        {
            _logger.LogInformation("Firebase service is disabled via configuration");
            return;
        }

        // Initialize Firebase once using thread-safe singleton pattern
        lock (_lock)
        {
            if (!_isInitialized)
            {
                try
                {
                    var projectId = configuration["Firebase:ProjectId"] ?? "yapplr";
                    GoogleCredential credential;

                    // Try to use Service Account Key first (production)
                    var serviceAccountKey = configuration["Firebase:ServiceAccountKey"];
                    var serviceAccountKeyFile = configuration["Firebase:ServiceAccountKeyFile"];

                    if (!string.IsNullOrEmpty(serviceAccountKey))
                    {
                        // Use service account key from environment variable (production)
                        var serviceAccountJson = System.Text.Encoding.UTF8.GetBytes(serviceAccountKey);
                        credential = GoogleCredential.FromStream(new MemoryStream(serviceAccountJson));
                        _logger.LogInformation("Firebase initialized using Service Account Key from environment variable");
                    }
                    else if (!string.IsNullOrEmpty(serviceAccountKeyFile) && File.Exists(serviceAccountKeyFile))
                    {
                        // Use service account key from file (production alternative)
                        credential = GoogleCredential.FromFile(serviceAccountKeyFile);
                        _logger.LogInformation("Firebase initialized using Service Account Key from file: {File}", serviceAccountKeyFile);
                    }
                    else
                    {
                        // Fallback to Application Default Credentials (development)
                        credential = GoogleCredential.GetApplicationDefault();
                        _logger.LogInformation("Firebase initialized using Application Default Credentials (development mode)");
                    }

                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = credential,
                        ProjectId = projectId,
                    });

                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Firebase. Firebase notifications will be disabled. " +
                        "For development: run 'gcloud auth application-default login'. " +
                        "For production: set Firebase:ServiceAccountKey environment variable.");
                    _messaging = null;
                    _isInitialized = true; // Mark as initialized to prevent retries
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

    public async Task<bool> SendTestNotificationAsync(string fcmToken)
    {
        if (_messaging == null)
        {
            _logger.LogWarning("Firebase messaging not initialized");
            return false;
        }

        try
        {
            // Try the most basic possible message
            var message = new Message()
            {
                Token = fcmToken,
                Notification = new Notification()
                {
                    Title = "Test",
                    Body = "Test message",
                }
            };

            _logger.LogInformation("Sending basic test Firebase message to token: {Token}", fcmToken.Substring(0, Math.Min(30, fcmToken.Length)) + "...");

            string response = await _messaging.SendAsync(message);
            _logger.LogInformation("Firebase test message sent successfully. Response: {Response}", response);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Firebase test message failed. Error: {Error}", ex.Message);
            return false;
        }
    }

    public async Task<bool> SendNotificationAsync(string? fcmToken, string title, string body, Dictionary<string, string>? data = null)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("Firebase service is disabled. Skipping notification for: {Title}", title);
            return false;
        }

        if (_messaging == null)
        {
            _logger.LogWarning("Firebase messaging not initialized. Skipping notification for: {Title}", title);
            return false;
        }

        if (string.IsNullOrWhiteSpace(fcmToken))
        {
            _logger.LogInformation("No FCM token available for user, skipping notification: {Title}", title);
            return false;
        }

        _logger.LogInformation("Attempting to send Firebase notification: {Title} to token: {Token}", title, fcmToken.Substring(0, Math.Min(20, fcmToken.Length)) + "...");

        try
        {
            // Validate FCM token format
            if (fcmToken.Length < 10)
            {
                _logger.LogWarning("FCM token appears to be invalid: length={Length}, token={Token}", fcmToken.Length, fcmToken.Substring(0, Math.Min(10, fcmToken.Length)) + "...");
                return false;
            }

            // Log token details for debugging
            _logger.LogInformation("FCM Token validation - Length: {Length}, Starts with: {TokenStart}, Contains colon: {HasColon}",
                fcmToken.Length,
                fcmToken.Substring(0, Math.Min(20, fcmToken.Length)),
                fcmToken.Contains(':'));

            // Check if token looks like a valid FCM token (should be base64-like and not contain certain invalid characters)
            if (fcmToken.Contains(' ') || fcmToken.Contains('\n') || fcmToken.Contains('\r'))
            {
                _logger.LogWarning("FCM token contains invalid characters (spaces or newlines)");
                return false;
            }

            // Note: FCM tokens can have various formats, so we'll be less restrictive with validation

            // Validate and clean data dictionary
            Dictionary<string, string>? cleanData = null;
            if (data != null)
            {
                cleanData = new Dictionary<string, string>();
                foreach (var kvp in data)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value != null)
                    {
                        cleanData[kvp.Key] = kvp.Value.ToString();
                    }
                }
                _logger.LogInformation("Cleaned data dictionary: {CleanData}", string.Join(", ", cleanData.Select(kvp => $"{kvp.Key}={kvp.Value}")));
            }

            // Try a simple message first without platform-specific configs and without data payload
            var message = new Message()
            {
                Token = fcmToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body,
                },
                Data = cleanData
            };

            _logger.LogInformation("Created simple Firebase message without platform configs");

            _logger.LogInformation("Sending Firebase message - Title: {Title}, Body: {Body}, Token length: {TokenLength}, Data: {Data}",
                title, body, fcmToken.Length, data != null ? string.Join(", ", data.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "null");

            // Log the complete message structure for debugging
            _logger.LogInformation("Complete Firebase message structure: Token={Token}, Title={Title}, Body={Body}, DataCount={DataCount}",
                fcmToken.Substring(0, Math.Min(30, fcmToken.Length)) + "...",
                message.Notification?.Title,
                message.Notification?.Body,
                message.Data?.Count ?? 0);

            string response = await _messaging.SendAsync(message);
            _logger.LogInformation("Successfully sent Firebase notification: {Title} - Response: {Response}", title, response);
            return true;
        }
        catch (FirebaseMessagingException fmEx)
        {
            _logger.LogError(fmEx, "Firebase Messaging Error sending '{Title}' to token: {Token}. Error Code: {ErrorCode}, Message: {Error}",
                title,
                fcmToken.Substring(0, Math.Min(20, fcmToken.Length)) + "...",
                fmEx.ErrorCode,
                fmEx.Message);

            // Log additional details if available
            if (fmEx.HttpResponse != null)
            {
                _logger.LogError("HTTP Response Status: {StatusCode}, Reason: {ReasonPhrase}",
                    fmEx.HttpResponse.StatusCode,
                    fmEx.HttpResponse.ReasonPhrase);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "General error sending FCM notification '{Title}' to token: {Token}. Error: {ErrorMessage}",
                title, fcmToken.Substring(0, Math.Min(20, fcmToken.Length)) + "...", ex.Message);
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
        _logger.LogInformation("SendMentionNotificationAsync - User {UserId} found: {UserFound}, FcmToken: {FcmToken}",
            userId, user != null, user?.FcmToken?.Substring(0, Math.Min(20, user?.FcmToken?.Length ?? 0)) + "...");

        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            _logger.LogInformation("Skipping mention notification for user {UserId} - User null: {UserNull}, FcmToken null/empty: {TokenEmpty}",
                userId, user == null, string.IsNullOrEmpty(user?.FcmToken));
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

    public async Task<bool> SendCommentNotificationAsync(int userId, string commenterUsername, int postId, int commentId)
    {
        var user = await _context.Users.FindAsync(userId);
        _logger.LogInformation("SendCommentNotificationAsync - User {UserId} found: {UserFound}, FcmToken: {FcmToken}",
            userId, user != null, user?.FcmToken?.Substring(0, Math.Min(20, user?.FcmToken?.Length ?? 0)) + "...");

        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            _logger.LogInformation("Skipping comment notification for user {UserId} - User null: {UserNull}, FcmToken null/empty: {TokenEmpty}",
                userId, user == null, string.IsNullOrEmpty(user?.FcmToken));
            return false;
        }

        var title = "New Comment";
        var body = $"@{commenterUsername} commented on your post";
        var data = new Dictionary<string, string>
        {
            ["type"] = "comment",
            ["postId"] = postId.ToString(),
            ["commentId"] = commentId.ToString(),
            ["commenterUsername"] = commenterUsername
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

    public async Task<bool> SendFollowRequestNotificationAsync(int userId, string requesterUsername)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            return false;
        }

        var title = "Follow Request";
        var body = $"@{requesterUsername} wants to follow you";
        var data = new Dictionary<string, string>
        {
            ["type"] = "follow_request",
            ["userId"] = userId.ToString(),
            ["requesterUsername"] = requesterUsername
        };

        return await SendNotificationAsync(user.FcmToken, title, body, data);
    }

    public async Task<bool> SendFollowRequestApprovedNotificationAsync(int userId, string approverUsername)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            return false;
        }

        var title = "Follow Request Approved";
        var body = $"@{approverUsername} approved your follow request";
        var data = new Dictionary<string, string>
        {
            ["type"] = "follow",
            ["userId"] = userId.ToString(),
            ["approverUsername"] = approverUsername
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
                Notification = new Notification()
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

    // IRealtimeNotificationProvider implementation
    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(_isEnabled && _messaging != null);
    }

    public async Task<bool> SendTestNotificationAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.FcmToken == null)
        {
            _logger.LogInformation("No FCM token available for user {UserId}", userId);
            return false;
        }

        return await SendTestNotificationAsync(user.FcmToken);
    }

    public async Task<bool> SendNotificationAsync(int userId, string title, string body, Dictionary<string, string>? data = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.FcmToken == null)
        {
            _logger.LogInformation("No FCM token available for user {UserId}", userId);
            return false;
        }

        return await SendNotificationAsync(user.FcmToken, title, body, data);
    }

    public async Task<bool> SendMulticastNotificationAsync(List<int> userIds, string title, string body, Dictionary<string, string>? data = null)
    {
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id) && !string.IsNullOrEmpty(u.FcmToken))
            .Select(u => u.FcmToken!)
            .ToListAsync();

        if (!users.Any())
        {
            _logger.LogInformation("No FCM tokens available for users {UserIds}", string.Join(", ", userIds));
            return false;
        }

        return await SendMulticastNotificationAsync(users, title, body, data);
    }

    private static string TruncateMessage(string message, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        return message.Length <= maxLength ? message : message[..maxLength] + "...";
    }
}
