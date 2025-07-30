using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Utils;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.CQRS;

namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Unified notification service that serves as the single entry point for all notification operations.
/// Consolidates functionality from NotificationService and CompositeNotificationService.
/// </summary>
public class UnifiedNotificationService : IUnifiedNotificationService
{
    private readonly YapplrDbContext _context;
    private readonly INotificationPreferencesService _preferencesService;
    private readonly ISignalRConnectionPool _connectionPool;
    private readonly ICountCacheService _countCache;
    private readonly IActiveConversationTracker _conversationTracker;
    private readonly ILogger<UnifiedNotificationService> _logger;
    
    // Optional services for enhanced functionality
    private readonly INotificationProviderManager? _providerManager;
    private readonly INotificationQueue? _notificationQueue;
    private readonly INotificationEnhancementService? _enhancementService;
    private readonly ICommandPublisher? _commandPublisher;
    
    // Statistics tracking
    private long _totalNotificationsSent;
    private long _totalNotificationsDelivered;
    private long _totalNotificationsFailed;
    private long _totalNotificationsQueued;
    private readonly ConcurrentDictionary<string, long> _notificationTypeBreakdown = new();
    private readonly object _statsLock = new();

    public UnifiedNotificationService(
        YapplrDbContext context,
        INotificationPreferencesService preferencesService,
        ISignalRConnectionPool connectionPool,
        ICountCacheService countCache,
        IActiveConversationTracker conversationTracker,
        ILogger<UnifiedNotificationService> logger,
        INotificationProviderManager? providerManager = null,
        INotificationQueue? notificationQueue = null,
        INotificationEnhancementService? enhancementService = null,
        ICommandPublisher? commandPublisher = null)
    {
        _context = context;
        _preferencesService = preferencesService;
        _connectionPool = connectionPool;
        _countCache = countCache;
        _conversationTracker = conversationTracker;
        _logger = logger;
        _providerManager = providerManager;
        _notificationQueue = notificationQueue;
        _enhancementService = enhancementService;
        _commandPublisher = commandPublisher;
    }

    #region Core Notification Methods

    public async Task<bool> SendNotificationAsync(NotificationRequest request)
    {
        try
        {
            _logger.LogDebug("Processing notification request for user {UserId}, type {NotificationType}", 
                request.UserId, request.NotificationType);

            // Validate request
            if (!await ValidateNotificationRequestAsync(request))
            {
                return false;
            }

            // Check user preferences
            if (!await ShouldSendNotificationAsync(request.UserId, request.NotificationType))
            {
                _logger.LogDebug("Notification blocked by user preferences for user {UserId}, type {NotificationType}", 
                    request.UserId, request.NotificationType);
                return false;
            }

            // Check rate limiting if enhancement service is available
            if (_enhancementService != null)
            {
                var rateLimitResult = await _enhancementService.CheckRateLimitAsync(request.UserId, request.NotificationType);
                if (!rateLimitResult.IsAllowed)
                {
                    _logger.LogWarning("Notification rate limited for user {UserId}, type {NotificationType}", 
                        request.UserId, request.NotificationType);
                    return false;
                }
            }

            // Create database notification (unless suppressed)
            Notification? notification = null;
            if (!request.SuppressDatabaseNotification)
            {
                _logger.LogDebug("Creating database notification for user {UserId}, type {NotificationType}", request.UserId, request.NotificationType);
                notification = await CreateDatabaseNotificationAsync(request);
                if (notification == null)
                {
                    _logger.LogError("Failed to create database notification for user {UserId}", request.UserId);
                    return false;
                }
                _logger.LogDebug("Database notification created successfully with ID {NotificationId} for user {UserId}", notification.Id, request.UserId);
            }
            else
            {
                _logger.LogDebug("Database notification suppressed for user {UserId}, type {NotificationType} (user active in conversation)", request.UserId, request.NotificationType);
            }

            // Increment sent counter for all successfully processed notifications
            Interlocked.Increment(ref _totalNotificationsSent);

            // Track notification type breakdown
            _notificationTypeBreakdown.AddOrUpdate(request.NotificationType, 1, (key, value) => value + 1);

            // Determine delivery strategy
            var isUserOnline = await _connectionPool.IsUserOnlineAsync(request.UserId);
            var deliveryMethod = await _preferencesService.GetPreferredDeliveryMethodAsync(request.UserId, request.NotificationType);

            // If user prefers email-only, send email immediately
            if (deliveryMethod == NotificationDeliveryMethod.EmailOnly)
            {
                var shouldSendEmail = await ShouldSendEmailNotificationAsync(request.UserId, request.NotificationType);
                if (shouldSendEmail)
                {
                    var emailSent = await TrySendEmailNotificationAsync(request);
                    if (emailSent)
                    {
                        Interlocked.Increment(ref _totalNotificationsDelivered);
                        _logger.LogDebug("Email-only notification sent to user {UserId}", request.UserId);
                        await RecordNotificationEventAsync("delivered", request, true);
                        return true;
                    }
                }
                _logger.LogWarning("Failed to send email-only notification to user {UserId}", request.UserId);
                return false;
            }

            // Try real-time delivery first if user is online
            bool realtimeDelivered = false;
            if (isUserOnline && _providerManager != null)
            {
                // Try immediate delivery
                var deliveryRequest = CreateDeliveryRequest(request);
                realtimeDelivered = await _providerManager.SendNotificationAsync(deliveryRequest);

                if (realtimeDelivered)
                {
                    Interlocked.Increment(ref _totalNotificationsDelivered);
                    _logger.LogDebug("Notification delivered immediately to user {UserId}", request.UserId);

                    // Record metrics
                    await RecordNotificationEventAsync("delivered", request, true);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Immediate delivery failed for user {UserId}, will try email fallback", request.UserId);
                }
            }

            // If real-time delivery failed or user is offline, try email as fallback
            if (!realtimeDelivered)
            {
                var shouldSendEmail = await ShouldSendEmailNotificationAsync(request.UserId, request.NotificationType);
                if (shouldSendEmail)
                {
                    var emailSent = await TrySendEmailNotificationAsync(request);
                    if (emailSent)
                    {
                        Interlocked.Increment(ref _totalNotificationsDelivered);
                        _logger.LogDebug("Email notification sent as fallback to user {UserId}", request.UserId);
                        await RecordNotificationEventAsync("delivered", request, true);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Email fallback also failed for user {UserId}", request.UserId);
                    }
                }
            }

            // Queue for later delivery if both real-time and email failed (only if we have a database notification)
            if (_notificationQueue != null && notification != null)
            {
                var queuedNotification = CreateQueuedNotification(request, notification.Id);
                await _notificationQueue.QueueNotificationAsync(queuedNotification);

                Interlocked.Increment(ref _totalNotificationsQueued);
                _logger.LogDebug("Notification queued for user {UserId}", request.UserId);

                // Record metrics
                await RecordNotificationEventAsync("queued", request, true);
                return true;
            }

            // If no provider manager or queue available, just create database notification
            await RecordNotificationEventAsync("sent", request, true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for user {UserId}, type {NotificationType}", 
                request.UserId, request.NotificationType);
            
            Interlocked.Increment(ref _totalNotificationsFailed);
            await RecordNotificationEventAsync("failed", request, false, ex.Message);
            return false;
        }
    }

    public async Task<bool> SendTestNotificationAsync(int userId)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "This is a test notification from the unified notification service!",
            Data = new Dictionary<string, string>
            {
                ["type"] = "test",
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            }
        };

        return await SendNotificationAsync(request);
    }

    public async Task<bool> SendMulticastNotificationAsync(List<int> userIds, NotificationRequest request)
    {
        var tasks = userIds.Select(async userId =>
        {
            var userRequest = new NotificationRequest
            {
                UserId = userId,
                NotificationType = request.NotificationType,
                Title = request.Title,
                Body = request.Body,
                Data = request.Data,
                Priority = request.Priority,
                ScheduledFor = request.ScheduledFor,
                RequireDeliveryConfirmation = request.RequireDeliveryConfirmation,
                ExpiresAfter = request.ExpiresAfter
            };
            
            return await SendNotificationAsync(userRequest);
        });

        var results = await Task.WhenAll(tasks);
        return results.All(r => r);
    }

    #endregion

    #region Helper Methods

    private async Task<bool> ValidateNotificationRequestAsync(NotificationRequest request)
    {
        if (request.UserId <= 0)
        {
            _logger.LogWarning("Invalid user ID in notification request: {UserId}", request.UserId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.NotificationType))
        {
            _logger.LogWarning("Missing notification type in request for user {UserId}", request.UserId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Body))
        {
            _logger.LogWarning("Missing title and body in notification request for user {UserId}", request.UserId);
            return false;
        }

        // Check if user exists
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
        if (!userExists)
        {
            _logger.LogWarning("User {UserId} not found for notification", request.UserId);
            return false;
        }

        return true;
    }

    private async Task<bool> ShouldSendNotificationAsync(int userId, string notificationType)
    {
        try
        {
            return await _preferencesService.ShouldSendNotificationAsync(userId, notificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check notification preferences for user {UserId}, type {NotificationType}", 
                userId, notificationType);
            // Default to allowing notification if preference check fails
            return true;
        }
    }

    private async Task<Notification?> CreateDatabaseNotificationAsync(NotificationRequest request)
    {
        try
        {
            _logger.LogDebug("Creating notification entity for user {UserId}, type {NotificationType}", request.UserId, request.NotificationType);
            var notification = new Notification
            {
                Type = ParseNotificationType(request.NotificationType),
                Message = !string.IsNullOrWhiteSpace(request.Body) ? request.Body : request.Title,
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Adding notification to context for user {UserId}", request.UserId);
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Notification saved to database with ID {NotificationId} for user {UserId}", notification.Id, request.UserId);

            // Invalidate notification count cache
            _logger.LogDebug("Invalidating notification count cache for user {UserId}", request.UserId);
            await _countCache.InvalidateNotificationCountsAsync(request.UserId);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database notification for user {UserId}", request.UserId);
            return null;
        }
    }

    private NotificationDeliveryRequest CreateDeliveryRequest(NotificationRequest request)
    {
        return new NotificationDeliveryRequest
        {
            UserId = request.UserId,
            NotificationType = request.NotificationType,
            Title = request.Title,
            Body = request.Body,
            Data = request.Data,
            Priority = request.Priority,
            RequireDeliveryConfirmation = request.RequireDeliveryConfirmation
        };
    }

    private QueuedNotification CreateQueuedNotification(NotificationRequest request, int notificationId)
    {
        var queuedNotification = new QueuedNotification
        {
            UserId = request.UserId,
            NotificationType = request.NotificationType,
            Title = request.Title,
            Body = request.Body,
            Data = request.Data,
            Priority = request.Priority,
            ScheduledFor = request.ScheduledFor,
            ExpiresAt = request.ExpiresAfter.HasValue ? DateTime.UtcNow.Add(request.ExpiresAfter.Value) : null
        };

        // Add database notification ID to data for reference
        queuedNotification.Data ??= new Dictionary<string, string>();
        queuedNotification.Data["notificationId"] = notificationId.ToString();

        return queuedNotification;
    }

    private NotificationType ParseNotificationType(string notificationTypeString)
    {
        return notificationTypeString.ToLower() switch
        {
            "mention" => NotificationType.Mention,
            "like" => NotificationType.Like,
            "repost" => NotificationType.Repost,
            "follow" => NotificationType.Follow,
            "comment" => NotificationType.Comment,
            "follow_request" => NotificationType.FollowRequest,
            "message" => NotificationType.SystemMessage,
            "user_suspended" => NotificationType.UserSuspended,
            "user_banned" => NotificationType.UserBanned,
            "content_hidden" => NotificationType.ContentHidden,
            "appeal_approved" => NotificationType.AppealApproved,
            "appeal_denied" => NotificationType.AppealDenied,
            "video_processing_completed" => NotificationType.VideoProcessingCompleted,
            "test" => NotificationType.SystemMessage,
            "systemmessage" => NotificationType.SystemMessage,
            _ => NotificationType.SystemMessage
        };
    }

    private async Task RecordNotificationEventAsync(string eventType, NotificationRequest request, bool success, string? errorMessage = null)
    {
        if (_enhancementService == null) return;

        try
        {
            var notificationEvent = new NotificationEvent
            {
                EventType = eventType,
                UserId = request.UserId,
                NotificationType = request.NotificationType,
                Success = success,
                ErrorMessage = errorMessage,
                Metadata = new Dictionary<string, object>
                {
                    ["title"] = request.Title,
                    ["priority"] = request.Priority.ToString()
                }
            };

            await _enhancementService.RecordNotificationEventAsync(notificationEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record notification event for user {UserId}", request.UserId);
        }
    }

    #endregion

    #region Legacy Compatibility Methods (for gradual migration)

    /// <summary>
    /// Creates a like notification with blocking and validation logic
    /// </summary>
    public async Task CreateLikeNotificationAsync(int likedUserId, int likingUserId, int postId)
    {
        // Don't notify if user likes their own post
        if (likedUserId == likingUserId)
            return;

        // Check if user has blocked the liking user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == likedUserId && b.BlockedId == likingUserId);

        if (isBlocked)
            return;

        var likingUser = await _context.Users.FindAsync(likingUserId);
        if (likingUser == null)
            return;

        // Get media type for notification message
        var mediaTypeText = await GetMediaTypeTextAsync(postId);

        // Create database notification with proper foreign keys
        var notification = new Notification
        {
            Type = NotificationType.Like,
            Message = $"@{likingUser.Username} liked your {mediaTypeText}",
            UserId = likedUserId,
            ActorUserId = likingUserId,
            PostId = postId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(likedUserId);

        // Send real-time notification
        await SendLikeNotificationAsync(likedUserId, likingUser.Username, postId);
    }

    /// <summary>
    /// Creates a repost notification with blocking and validation logic
    /// </summary>
    public async Task CreateRepostNotificationAsync(int originalUserId, int repostingUserId, int postId)
    {
        // Don't notify if user reposts their own post
        if (originalUserId == repostingUserId)
            return;

        // Check if user has blocked the reposting user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == originalUserId && b.BlockedId == repostingUserId);

        if (isBlocked)
            return;

        var repostingUser = await _context.Users.FindAsync(repostingUserId);
        if (repostingUser == null)
            return;

        // Get media type for notification message
        var mediaTypeText = await GetMediaTypeTextAsync(postId);

        // Create database notification with proper foreign keys
        var notification = new Notification
        {
            Type = NotificationType.Repost,
            Message = $"@{repostingUser.Username} reposted your {mediaTypeText}",
            UserId = originalUserId,
            ActorUserId = repostingUserId,
            PostId = postId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(originalUserId);

        // Send real-time notification
        await SendRepostNotificationAsync(originalUserId, repostingUser.Username, postId);
    }

    /// <summary>
    /// Creates a follow notification with blocking and validation logic
    /// </summary>
    public async Task CreateFollowNotificationAsync(int followedUserId, int followingUserId)
    {
        // Check if user has blocked the following user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == followedUserId && b.BlockedId == followingUserId);

        if (isBlocked)
            return;

        var followingUser = await _context.Users.FindAsync(followingUserId);
        if (followingUser == null)
            return;

        // Create database notification with proper foreign keys
        var notification = new Notification
        {
            Type = NotificationType.Follow,
            Message = $"@{followingUser.Username} started following you",
            UserId = followedUserId,
            ActorUserId = followingUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(followedUserId);

        // Send real-time notification
        await SendFollowNotificationAsync(followedUserId, followingUser.Username);
    }

    /// <summary>
    /// Creates a follow request notification with blocking and validation logic
    /// </summary>
    public async Task CreateFollowRequestNotificationAsync(int requestedUserId, int requesterUserId)
    {
        // Check if user has blocked the requesting user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == requestedUserId && b.BlockedId == requesterUserId);

        if (isBlocked)
            return;

        var requesterUser = await _context.Users.FindAsync(requesterUserId);
        if (requesterUser == null)
            return;

        // Create database notification with proper foreign keys
        var notification = new Notification
        {
            Type = NotificationType.FollowRequest,
            Message = $"@{requesterUser.Username} wants to follow you",
            UserId = requestedUserId,
            ActorUserId = requesterUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(requestedUserId);

        // Send real-time notification
        await SendFollowRequestNotificationAsync(requestedUserId, requesterUser.Username);
    }

    /// <summary>
    /// Creates a comment notification with blocking and validation logic
    /// </summary>
    public async Task CreateCommentNotificationAsync(int postOwnerId, int commentingUserId, int postId, int commentId, string commentContent)
    {
        // Don't notify if user comments on their own post
        if (postOwnerId == commentingUserId)
            return;

        // Check if user has blocked the commenting user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == postOwnerId && b.BlockedId == commentingUserId);

        if (isBlocked)
            return;

        var commentingUser = await _context.Users.FindAsync(commentingUserId);
        if (commentingUser == null)
            return;

        // Get media type for notification message
        var mediaTypeText = await GetMediaTypeTextAsync(postId);

        // Create database notification with proper foreign keys
        var notification = new Notification
        {
            Type = NotificationType.Comment,
            Message = $"@{commentingUser.Username} commented on your {mediaTypeText}",
            UserId = postOwnerId,
            ActorUserId = commentingUserId,
            PostId = postId,
            CommentId = commentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(postOwnerId);

        // Send real-time notification
        await SendCommentNotificationAsync(postOwnerId, commentingUser.Username, postId, commentId);
    }

    /// <summary>
    /// Creates mention notifications for users mentioned in content
    /// </summary>
    public async Task CreateMentionNotificationsAsync(string content, int mentioningUserId, int? postId = null, int? commentId = null)
    {
        var mentionedUsernames = MentionParser.ExtractMentions(content);
        if (!mentionedUsernames.Any())
            return;

        // Get users that exist and are not the mentioning user
        var mentionedUsers = await _context.Users
            .Where(u => mentionedUsernames.Contains(u.Username.ToLower()) && u.Id != mentioningUserId)
            .ToListAsync();

        var mentioningUser = await _context.Users.FindAsync(mentioningUserId);
        if (mentioningUser == null)
            return;

        foreach (var mentionedUser in mentionedUsers)
        {
            // Check if user has blocked the mentioning user
            var isBlocked = await _context.Blocks
                .AnyAsync(b => b.BlockerId == mentionedUser.Id && b.BlockedId == mentioningUserId);

            if (isBlocked)
                continue;

            // Create database notification with proper foreign keys
            var message = postId.HasValue
                ? $"@{mentioningUser.Username} mentioned you in a post"
                : $"@{mentioningUser.Username} mentioned you in a comment";

            var notification = new Notification
            {
                Type = NotificationType.Mention,
                Message = message,
                UserId = mentionedUser.Id,
                ActorUserId = mentioningUserId,
                PostId = postId,
                CommentId = commentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Invalidate notification count cache
            await _countCache.InvalidateNotificationCountsAsync(mentionedUser.Id);

            // Create mention record
            var mention = new Mention
            {
                MentioningUserId = mentioningUserId,
                MentionedUserId = mentionedUser.Id,
                PostId = postId,
                CommentId = commentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Mentions.Add(mention);
            await _context.SaveChangesAsync();

            // Send real-time notification
            await SendMentionNotificationAsync(mentionedUser.Id, mentioningUser.Username, postId ?? 0, commentId);
        }
    }

    /// <summary>
    /// Creates a system message notification
    /// </summary>
    public async Task CreateSystemMessageNotificationAsync(int userId, string message)
    {
        // Create database notification
        var notification = new Notification
        {
            Type = NotificationType.SystemMessage,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(userId);

        // Send real-time notification
        await SendSystemMessageAsync(userId, "System Message", message);
    }

    /// <summary>
    /// Creates a user ban notification
    /// </summary>
    public async Task CreateUserBanNotificationAsync(int userId, string reason, bool isShadowBan, string moderatorUsername)
    {
        var banType = isShadowBan ? "shadow banned" : "banned";
        var message = $"Your account has been {banType} by @{moderatorUsername}. Reason: {reason}";

        // Create database notification
        var notification = new Notification
        {
            Type = NotificationType.UserBanned,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(userId);

        // Send real-time notification (only for regular bans, not shadow-bans)
        if (!isShadowBan)
        {
            await SendSystemMessageAsync(userId, "Account Banned", message);
        }
    }

    /// <summary>
    /// Creates a content hidden notification
    /// </summary>
    public async Task CreateContentHiddenNotificationAsync(int userId, string contentType, int contentId, string reason, string moderatorUsername)
    {
        var message = $"Your {contentType} #{contentId} has been hidden by @{moderatorUsername}. Reason: {reason}";

        // Create database notification
        var notification = new Notification
        {
            Type = NotificationType.ContentHidden,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            PostId = contentType == "post" ? contentId : null,
            CommentId = contentType == "comment" ? contentId : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(userId);

        // Send real-time notification
        await SendContentHiddenNotificationAsync(userId, contentType, contentId, reason);
    }

    /// <summary>
    /// Deletes notifications related to a post (when post is deleted)
    /// </summary>
    public async Task DeletePostNotificationsAsync(int postId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.PostId == postId)
            .ToListAsync();

        if (notifications.Any())
        {
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();

            // Invalidate notification count cache for affected users
            var affectedUserIds = notifications.Select(n => n.UserId).Distinct();
            foreach (var userId in affectedUserIds)
            {
                await _countCache.InvalidateNotificationCountsAsync(userId);
            }
        }
    }

    /// <summary>
    /// Deletes social interaction and post-specific notifications for a post, preserving user-level system/moderation notifications
    /// </summary>
    public async Task DeleteSocialNotificationsForPostAsync(int postId)
    {
        // Delete social interaction notifications and post-specific system notifications
        // Preserve user-level system/moderation notifications (suspensions, bans, appeals, etc.)
        var notificationTypesToDelete = new[]
        {
            NotificationType.Mention,                    // 1
            NotificationType.Like,                       // 2
            NotificationType.Repost,                     // 3
            NotificationType.Follow,                     // 4 (shouldn't be post-related but included for completeness)
            NotificationType.Comment,                    // 5
            NotificationType.FollowRequest,              // 6 (shouldn't be post-related but included for completeness)
            NotificationType.VideoProcessingCompleted   // 110 (post-specific, should be deleted with the post)
        };

        var notificationsToDelete = await _context.Notifications
            .Where(n => n.PostId == postId && notificationTypesToDelete.Contains(n.Type))
            .ToListAsync();

        if (notificationsToDelete.Any())
        {
            _context.Notifications.RemoveRange(notificationsToDelete);
            await _context.SaveChangesAsync();

            // Invalidate notification count cache for affected users
            var affectedUserIds = notificationsToDelete.Select(n => n.UserId).Distinct();
            foreach (var userId in affectedUserIds)
            {
                await _countCache.InvalidateNotificationCountsAsync(userId);
            }

            _logger.LogInformation("Deleted {Count} post-related notifications for deleted post {PostId}",
                notificationsToDelete.Count, postId);
        }
    }

    /// <summary>
    /// Deletes notifications related to a comment (when comment is deleted)
    /// </summary>
    public async Task DeleteCommentNotificationsAsync(int commentId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.CommentId == commentId)
            .ToListAsync();

        if (notifications.Any())
        {
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();

            // Invalidate notification count cache for affected users
            var affectedUserIds = notifications.Select(n => n.UserId).Distinct();
            foreach (var userId in affectedUserIds)
            {
                await _countCache.InvalidateNotificationCountsAsync(userId);
            }
        }
    }

    #endregion

    #region Specific Notification Types

    public async Task SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId, bool suppressDatabaseNotification = false)
    {
        // Check if user is actively viewing this conversation (if not already suppressed)
        if (!suppressDatabaseNotification)
        {
            suppressDatabaseNotification = await _conversationTracker.IsUserActiveInConversationAsync(userId, conversationId);
        }

        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "message",
            Title = "New Message",
            Body = $"@{senderUsername}: {TruncateMessage(messageContent)}",
            Data = new Dictionary<string, string>
            {
                ["type"] = "message",
                ["conversationId"] = conversationId.ToString(),
                ["senderUsername"] = senderUsername,
                ["suppressNotification"] = suppressDatabaseNotification.ToString().ToLower()
            },
            SuppressDatabaseNotification = suppressDatabaseNotification
        };

        await SendNotificationAsync(request);
    }

    public async Task SendMentionNotificationAsync(int userId, string mentionerUsername, int postId, int? commentId = null)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "mention",
            Title = "You were mentioned",
            Body = $"@{mentionerUsername} mentioned you in a {(commentId.HasValue ? "comment" : "post")}",
            Data = new Dictionary<string, string>
            {
                ["type"] = "mention",
                ["postId"] = postId.ToString(),
                ["mentionerUsername"] = mentionerUsername
            }
        };

        if (commentId.HasValue)
        {
            request.Data["commentId"] = commentId.Value.ToString();
        }

        await SendNotificationAsync(request);
    }

    public async Task SendReplyNotificationAsync(int userId, string replierUsername, int postId, int commentId)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "reply",
            Title = "New Reply",
            Body = $"@{replierUsername} replied to your comment",
            Data = new Dictionary<string, string>
            {
                ["type"] = "reply",
                ["postId"] = postId.ToString(),
                ["commentId"] = commentId.ToString(),
                ["replierUsername"] = replierUsername
            }
        };

        await SendNotificationAsync(request);
    }

    public async Task SendCommentNotificationAsync(int userId, string commenterUsername, int postId, int commentId)
    {
        var mediaTypeText = await GetMediaTypeTextAsync(postId);
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "comment",
            Title = "New Comment",
            Body = $"@{commenterUsername} commented on your {mediaTypeText}",
            Data = new Dictionary<string, string>
            {
                ["type"] = "comment",
                ["postId"] = postId.ToString(),
                ["commentId"] = commentId.ToString(),
                ["commenterUsername"] = commenterUsername
            }
        };

        await SendNotificationAsync(request);
    }

    public async Task SendFollowNotificationAsync(int userId, string followerUsername)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "follow",
            Title = "New Follower",
            Body = $"@{followerUsername} started following you",
            Data = new Dictionary<string, string>
            {
                ["type"] = "follow",
                ["followerUsername"] = followerUsername
            }
        };

        await SendNotificationAsync(request);
    }

    public async Task SendFollowRequestNotificationAsync(int userId, string requesterUsername)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "follow_request",
            Title = "Follow Request",
            Body = $"@{requesterUsername} wants to follow you",
            Data = new Dictionary<string, string>
            {
                ["type"] = "follow_request",
                ["requesterUsername"] = requesterUsername
            }
        };

        await SendNotificationAsync(request);
    }

    public async Task SendFollowRequestApprovedNotificationAsync(int userId, string approverUsername)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "follow_request_approved",
            Title = "Follow Request Approved",
            Body = $"@{approverUsername} approved your follow request",
            Data = new Dictionary<string, string>
            {
                ["type"] = "follow_request_approved",
                ["approverUsername"] = approverUsername
            }
        };

        await SendNotificationAsync(request);
    }

    public async Task SendLikeNotificationAsync(int userId, string likerUsername, int postId)
    {
        var mediaTypeText = await GetMediaTypeTextAsync(postId);
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "like",
            Title = "Post Liked",
            Body = $"@{likerUsername} liked your {mediaTypeText}",
            Data = new Dictionary<string, string>
            {
                ["type"] = "like",
                ["postId"] = postId.ToString(),
                ["likerUsername"] = likerUsername
            }
        };

        await SendNotificationAsync(request);
    }

    public async Task SendCommentLikeNotificationAsync(int userId, string likerUsername, int postId, int commentId)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "comment_like",
            Title = "Comment Liked",
            Body = $"@{likerUsername} liked your comment",
            Data = new Dictionary<string, string>
            {
                ["type"] = "comment_like",
                ["postId"] = postId.ToString(),
                ["commentId"] = commentId.ToString(),
                ["likerUsername"] = likerUsername
            }
        };

        await SendNotificationAsync(request);
    }

    public async Task SendRepostNotificationAsync(int userId, string reposterUsername, int postId)
    {
        var mediaTypeText = await GetMediaTypeTextAsync(postId);
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "repost",
            Title = "Post Reposted",
            Body = $"@{reposterUsername} reposted your {mediaTypeText}",
            Data = new Dictionary<string, string>
            {
                ["type"] = "repost",
                ["postId"] = postId.ToString(),
                ["reposterUsername"] = reposterUsername
            }
        };

        await SendNotificationAsync(request);
    }



    #endregion

    #region System and Moderation Notifications

    public async Task SendSystemMessageAsync(int userId, string title, string message, Dictionary<string, string>? data = null)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "system_message",
            Title = title,
            Body = message,
            Data = data ?? new Dictionary<string, string> { ["type"] = "system_message" },
            Priority = NotificationPriority.High
        };

        await SendNotificationAsync(request);
    }

    public async Task SendUserSuspendedNotificationAsync(int userId, string reason, DateTime suspendedUntil)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "user_suspended",
            Title = "Account Suspended",
            Body = $"Your account has been suspended until {suspendedUntil:yyyy-MM-dd}. Reason: {reason}",
            Data = new Dictionary<string, string>
            {
                ["type"] = "user_suspended",
                ["reason"] = reason,
                ["suspendedUntil"] = suspendedUntil.ToString("O")
            },
            Priority = NotificationPriority.Critical
        };

        await SendNotificationAsync(request);
    }

    public async Task SendContentHiddenNotificationAsync(int userId, string contentType, int contentId, string reason)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "content_hidden",
            Title = "Content Hidden",
            Body = $"Your {contentType} #{contentId} has been hidden. Reason: {reason}",
            Data = new Dictionary<string, string>
            {
                ["type"] = "content_hidden",
                ["contentType"] = contentType,
                ["contentId"] = contentId.ToString(),
                ["reason"] = reason
            },
            Priority = NotificationPriority.High
        };

        await SendNotificationAsync(request);
    }

    public async Task SendAppealApprovedNotificationAsync(int userId, string appealType, int appealId)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "appeal_approved",
            Title = "Appeal Approved",
            Body = $"Your {appealType} appeal has been approved",
            Data = new Dictionary<string, string>
            {
                ["type"] = "appeal_approved",
                ["appealType"] = appealType,
                ["appealId"] = appealId.ToString()
            },
            Priority = NotificationPriority.High
        };

        await SendNotificationAsync(request);
    }

    public async Task SendAppealDeniedNotificationAsync(int userId, string appealType, int appealId, string reason)
    {
        var request = new NotificationRequest
        {
            UserId = userId,
            NotificationType = "appeal_denied",
            Title = "Appeal Denied",
            Body = $"Your {appealType} appeal has been denied. Reason: {reason}",
            Data = new Dictionary<string, string>
            {
                ["type"] = "appeal_denied",
                ["appealType"] = appealType,
                ["appealId"] = appealId.ToString(),
                ["reason"] = reason
            },
            Priority = NotificationPriority.High
        };

        await SendNotificationAsync(request);
    }

    #endregion

    #region Delivery Tracking and History

    public async Task<List<NotificationDeliveryStatus>> GetDeliveryStatusAsync(int userId, int count)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .Select(n => new NotificationDeliveryStatus
                {
                    NotificationId = n.Id.ToString(),
                    UserId = n.UserId,
                    NotificationType = n.Type.ToString(),
                    CreatedAt = n.CreatedAt,
                    IsDelivered = n.IsRead, // Using IsRead as a proxy for delivered
                    DeliveredAt = n.ReadAt,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    AttemptCount = 1 // Default value
                })
                .ToListAsync();

            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get delivery status for user {UserId}", userId);
            return new List<NotificationDeliveryStatus>();
        }
    }

    public async Task<List<NotificationHistoryEntry>> GetNotificationHistoryAsync(int userId, int count)
    {
        try
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .Select(n => new NotificationHistoryEntry
                {
                    NotificationId = n.Id.ToString(),
                    UserId = n.UserId,
                    NotificationType = n.Type.ToString(),
                    Title = n.Type.ToString(), // Use type as title since no separate title field
                    Body = n.Message,
                    CreatedAt = n.CreatedAt,
                    DeliveredAt = n.ReadAt, // Using ReadAt as proxy for delivered
                    ReadAt = n.ReadAt,
                    Status = n.IsRead ? "Read" : "Unread"
                })
                .ToListAsync();

            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification history for user {UserId}", userId);
            return new List<NotificationHistoryEntry>();
        }
    }

    public async Task<List<UndeliveredNotification>> GetUndeliveredNotificationsAsync(int userId)
    {
        try
        {
            // Get unread notifications as undelivered
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new UndeliveredNotification
                {
                    NotificationId = n.Id.ToString(),
                    UserId = n.UserId,
                    NotificationType = n.Type.ToString(),
                    Title = n.Type.ToString(), // Use type as title since no separate title field
                    Body = n.Message,
                    CreatedAt = n.CreatedAt,
                    Priority = NotificationPriority.Normal, // Default priority
                    AttemptCount = 1
                })
                .ToListAsync();

            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get undelivered notifications for user {UserId}", userId);
            return new List<UndeliveredNotification>();
        }
    }

    public async Task<int> ReplayMissedNotificationsAsync(int userId)
    {
        try
        {
            var undeliveredNotifications = await GetUndeliveredNotificationsAsync(userId);
            int replayedCount = 0;

            foreach (var notification in undeliveredNotifications)
            {
                var request = new NotificationRequest
                {
                    UserId = userId,
                    NotificationType = notification.NotificationType,
                    Title = notification.Title,
                    Body = notification.Body,
                    Data = notification.Data,
                    Priority = notification.Priority
                };

                var success = await SendNotificationAsync(request);
                if (success)
                {
                    replayedCount++;
                }
            }

            _logger.LogInformation("Replayed {Count} notifications for user {UserId}", replayedCount, userId);
            return replayedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replay missed notifications for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<bool> ConfirmDeliveryAsync(string notificationId)
    {
        try
        {
            if (int.TryParse(notificationId, out int id))
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification != null)
                {
                    // Check if there's already a delivery confirmation record
                    var deliveryConfirmation = await _context.NotificationDeliveryConfirmations
                        .FirstOrDefaultAsync(dc => dc.NotificationId == notificationId);

                    if (deliveryConfirmation != null)
                    {
                        // Update existing delivery confirmation
                        deliveryConfirmation.IsDelivered = true;
                        deliveryConfirmation.DeliveredAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Create new delivery confirmation record
                        deliveryConfirmation = new NotificationDeliveryConfirmation
                        {
                            UserId = notification.UserId,
                            NotificationId = notificationId,
                            NotificationType = notification.Type.ToString(),
                            DeliveryMethod = NotificationDeliveryMethod.Auto, // Default to auto
                            SentAt = notification.CreatedAt,
                            DeliveredAt = DateTime.UtcNow,
                            IsDelivered = true
                        };
                        _context.NotificationDeliveryConfirmations.Add(deliveryConfirmation);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Confirmed delivery for notification {NotificationId}", notificationId);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm delivery for notification {NotificationId}", notificationId);
            return false;
        }
    }

    public async Task<bool> ConfirmReadAsync(string notificationId)
    {
        try
        {
            if (int.TryParse(notificationId, out int id))
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification != null)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Confirmed read for notification {NotificationId}", notificationId);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm read for notification {NotificationId}", notificationId);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetDeliveryStatsAsync(int userId, TimeSpan? timeWindow)
    {
        try
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);

            if (timeWindow.HasValue)
            {
                var cutoffTime = DateTime.UtcNow - timeWindow.Value;
                query = query.Where(n => n.CreatedAt >= cutoffTime);
            }

            var notifications = await query.ToListAsync();
            var totalCount = notifications.Count;
            var readCount = notifications.Count(n => n.IsRead);
            var unreadCount = totalCount - readCount;

            return new Dictionary<string, object>
            {
                ["total_notifications"] = totalCount,
                ["delivered_notifications"] = readCount, // Using read as proxy for delivered
                ["undelivered_notifications"] = unreadCount,
                ["delivery_rate"] = totalCount > 0 ? (double)readCount / totalCount * 100 : 0,
                ["time_window_hours"] = timeWindow?.TotalHours ?? 0,
                ["last_updated"] = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get delivery stats for user {UserId}", userId);
            return new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["last_updated"] = DateTime.UtcNow
            };
        }
    }

    #endregion

    #region Management and Monitoring

    public Task<NotificationStats> GetStatsAsync()
    {
        lock (_statsLock)
        {
            return Task.FromResult(new NotificationStats
            {
                TotalNotificationsSent = _totalNotificationsSent,
                TotalNotificationsDelivered = _totalNotificationsDelivered,
                TotalNotificationsFailed = _totalNotificationsFailed,
                TotalNotificationsQueued = _totalNotificationsQueued,
                DeliverySuccessRate = _totalNotificationsSent > 0
                    ? (double)_totalNotificationsDelivered / _totalNotificationsSent * 100
                    : 0,
                NotificationTypeBreakdown = _notificationTypeBreakdown.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                LastUpdated = DateTime.UtcNow
            });
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();

            // Check if preferences service is working
            await _preferencesService.GetUserPreferencesAsync(1); // Test with user ID 1

            // Check provider manager health if available
            if (_providerManager != null)
            {
                var hasProviders = await _providerManager.HasAvailableProvidersAsync();
                if (!hasProviders)
                {
                    _logger.LogWarning("No notification providers are available");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for UnifiedNotificationService");
            return false;
        }
    }

    public async Task<NotificationHealthReport> GetHealthReportAsync()
    {
        var report = new NotificationHealthReport();
        var issues = new List<string>();

        try
        {
            // Check database
            var canConnect = await _context.Database.CanConnectAsync();
            report.ComponentHealth["Database"] = new ComponentHealth
            {
                IsHealthy = canConnect,
                Status = canConnect ? "Connected" : "Disconnected",
                LastChecked = DateTime.UtcNow
            };

            if (!canConnect)
            {
                issues.Add("Database connection failed");
            }

            // Check preferences service
            try
            {
                await _preferencesService.GetUserPreferencesAsync(1);
                report.ComponentHealth["PreferencesService"] = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "Operational",
                    LastChecked = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                report.ComponentHealth["PreferencesService"] = new ComponentHealth
                {
                    IsHealthy = false,
                    Status = "Failed",
                    ErrorMessage = ex.Message,
                    LastChecked = DateTime.UtcNow
                };
                issues.Add("Preferences service failed");
            }

            // Check provider manager if available
            if (_providerManager != null)
            {
                var hasProviders = await _providerManager.HasAvailableProvidersAsync();
                report.ComponentHealth["ProviderManager"] = new ComponentHealth
                {
                    IsHealthy = hasProviders,
                    Status = hasProviders ? "Available" : "No providers available",
                    LastChecked = DateTime.UtcNow
                };

                if (!hasProviders)
                {
                    issues.Add("No notification providers available");
                }
            }

            report.IsHealthy = issues.Count == 0;
            report.Issues = issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate health report");
            report.IsHealthy = false;
            report.Issues.Add($"Health check failed: {ex.Message}");
        }

        return report;
    }

    public async Task RefreshSystemAsync()
    {
        _logger.LogInformation("Refreshing unified notification system");

        try
        {
            // Refresh provider health if available
            if (_providerManager != null)
            {
                await _providerManager.RefreshProviderHealthAsync();
            }

            // Refresh queue health if available
            if (_notificationQueue != null)
            {
                await _notificationQueue.RefreshHealthAsync();
            }

            _logger.LogInformation("System refresh completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh notification system");
            throw;
        }
    }

    #endregion

    #region Utility Methods

    private string TruncateMessage(string message, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(message) || message.Length <= maxLength)
            return message;

        return message.Substring(0, maxLength - 3) + "...";
    }

    private async Task<bool> ShouldSendEmailNotificationAsync(int userId, string notificationType)
    {
        try
        {
            return await _preferencesService.ShouldSendEmailNotificationAsync(userId, notificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check email notification preferences for user {UserId}", userId);
            return false;
        }
    }

    private async Task<bool> TrySendEmailNotificationAsync(NotificationRequest request)
    {
        try
        {
            // Check if command publisher is available
            if (_commandPublisher == null)
            {
                _logger.LogWarning("Command publisher not available - cannot send email notification to user {UserId}", request.UserId);
                return false;
            }

            // Get user information for email
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                _logger.LogWarning("Cannot send email notification to user {UserId} - no email address", request.UserId);
                return false;
            }

            // Create email notification command
            var emailCommand = new SendNotificationEmailCommand
            {
                ToEmail = user.Email,
                Username = user.Username,
                Subject = request.Title,
                Message = request.Body,
                NotificationType = request.NotificationType,
                ActionUrl = GenerateActionUrl(request)
            };

            // Send via CQRS command
            await _commandPublisher.PublishAsync(emailCommand);

            _logger.LogDebug("Email notification command published for user {UserId}", request.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification to user {UserId}", request.UserId);
            return false;
        }
    }

    private string? GenerateActionUrl(NotificationRequest request)
    {
        try
        {
            // Generate appropriate action URLs based on notification type and data
            return request.NotificationType?.ToLower() switch
            {
                "like" or "comment" or "repost" when request.Data?.ContainsKey("postId") == true
                    => $"https://yapplr.com/post/{request.Data["postId"]}",
                "follow" or "follow_request" when request.Data?.ContainsKey("fromUserId") == true
                    => $"https://yapplr.com/user/{request.Data["fromUserId"]}",
                "message" when request.Data?.ContainsKey("conversationId") == true
                    => $"https://yapplr.com/messages/{request.Data["conversationId"]}",
                "mention" when request.Data?.ContainsKey("postId") == true
                    => $"https://yapplr.com/post/{request.Data["postId"]}",
                "reply" when request.Data?.ContainsKey("postId") == true
                    => $"https://yapplr.com/post/{request.Data["postId"]}",
                _ => "https://yapplr.com/notifications"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate action URL for notification type {NotificationType}", request.NotificationType);
            return "https://yapplr.com/notifications";
        }
    }

    /// <summary>
    /// Determines the appropriate text to use in notifications based on the post's media content
    /// </summary>
    private async Task<string> GetMediaTypeTextAsync(int postId)
    {
        try
        {
            var post = await _context.Posts
                .Include(p => p.PostMedia)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post?.PostMedia == null || !post.PostMedia.Any())
            {
                return "post";
            }

            // Check if post has only videos
            var hasVideo = post.PostMedia.Any(m => m.MediaType == MediaType.Video);
            var hasImage = post.PostMedia.Any(m => m.MediaType == MediaType.Image);
            var hasGif = post.PostMedia.Any(m => m.MediaType == MediaType.Gif);

            // If only videos, call it "video"
            if (hasVideo && !hasImage && !hasGif)
            {
                return "video";
            }

            // If only images (including GIFs), call it "photo"
            if ((hasImage || hasGif) && !hasVideo)
            {
                return "photo";
            }

            // If mixed media or unknown, default to "post"
            return "post";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to determine media type for post {PostId}, defaulting to 'post'", postId);
            return "post";
        }
    }

    #endregion
}
