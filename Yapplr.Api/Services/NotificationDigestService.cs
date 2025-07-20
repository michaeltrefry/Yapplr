using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services.EmailTemplates;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.CQRS;

namespace Yapplr.Api.Services;

public class NotificationDigestService : INotificationDigestService
{
    private readonly YapplrDbContext _context;
    private readonly INotificationPreferencesService _preferencesService;
    private readonly ICommandPublisher _commandPublisher;
    private readonly ILogger<NotificationDigestService> _logger;

    public NotificationDigestService(
        YapplrDbContext context,
        INotificationPreferencesService preferencesService,
        ICommandPublisher commandPublisher,
        ILogger<NotificationDigestService> logger)
    {
        _context = context;
        _preferencesService = preferencesService;
        _commandPublisher = commandPublisher;
        _logger = logger;
    }

    public async Task ProcessDigestEmailsAsync(int frequencyHours)
    {
        _logger.LogInformation("Processing digest emails for frequency: {FrequencyHours} hours", frequencyHours);

        // Get users who have digest enabled with this frequency
        var usersWithDigest = await _context.NotificationPreferences
            .Where(np => np.EnableEmailDigest && 
                        np.EnableEmailNotifications && 
                        np.EmailDigestFrequencyHours == frequencyHours)
            .Include(np => np.User)
            .ToListAsync();

        _logger.LogInformation("Found {UserCount} users with {FrequencyHours}h digest enabled", 
            usersWithDigest.Count, frequencyHours);

        var periodEnd = DateTime.UtcNow;
        var periodStart = periodEnd.AddHours(-frequencyHours);

        var processedCount = 0;
        var errorCount = 0;

        foreach (var userPrefs in usersWithDigest)
        {
            try
            {
                if (await ShouldSendDigestAsync(userPrefs.UserId, frequencyHours))
                {
                    var notifications = await GenerateUserDigestAsync(userPrefs.UserId, periodStart, periodEnd);
                    
                    // Only send digest if there are notifications or it's been a while
                    if (notifications.Any() || await ShouldSendEmptyDigestAsync(userPrefs.UserId, frequencyHours))
                    {
                        await SendDigestEmailAsync(userPrefs.UserId, notifications, periodStart, periodEnd);
                        processedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process digest for user {UserId}", userPrefs.UserId);
                errorCount++;
            }
        }

        _logger.LogInformation("Digest processing complete. Processed: {ProcessedCount}, Errors: {ErrorCount}", 
            processedCount, errorCount);
    }

    public async Task<List<DigestNotification>> GenerateUserDigestAsync(int userId, DateTime periodStart, DateTime periodEnd)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId &&
                       n.CreatedAt >= periodStart &&
                       n.CreatedAt <= periodEnd &&
                       !n.IsRead) // Only include unread notifications
            .OrderByDescending(n => n.CreatedAt)
            .Include(n => n.ActorUser)
            .Include(n => n.Post)
            .Include(n => n.Comment)
            .ToListAsync();

        var digestNotifications = new List<DigestNotification>();

        foreach (var notification in notifications)
        {
            digestNotifications.Add(new DigestNotification
            {
                Type = notification.Type.ToString().ToLower(),
                Title = await GenerateNotificationTitleAsync(notification),
                Body = notification.Message,
                CreatedAt = notification.CreatedAt,
                ActionUrl = GenerateActionUrl(notification)
            });
        }

        return digestNotifications;
    }

    public async Task SendDigestEmailAsync(int userId, List<DigestNotification> notifications, DateTime periodStart, DateTime periodEnd)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            _logger.LogWarning("Cannot send digest email to user {UserId} - user not found or no email", userId);
            return;
        }

        var command = new SendNotificationDigestEmailCommand
        {
            ToEmail = user.Email,
            Username = user.Username,
            Notifications = notifications,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            UnsubscribeUrl = "https://yapplr.com/settings/notifications"
        };

        await _commandPublisher.PublishAsync(command);
        
        _logger.LogDebug("Digest email command published for user {UserId} with {NotificationCount} notifications", 
            userId, notifications.Count);
    }

    public async Task<bool> ShouldSendDigestAsync(int userId, int frequencyHours)
    {
        // Check if user has email notifications enabled
        var shouldSend = await _preferencesService.ShouldSendEmailNotificationAsync(userId, "digest");
        if (!shouldSend)
            return false;

        // Check if we've already sent a digest recently
        var lastDigestTime = await GetLastDigestTimeAsync(userId);
        if (lastDigestTime.HasValue)
        {
            var timeSinceLastDigest = DateTime.UtcNow - lastDigestTime.Value;
            if (timeSinceLastDigest.TotalHours < frequencyHours * 0.9) // 90% of frequency to avoid spam
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ShouldSendEmptyDigestAsync(int userId, int frequencyHours)
    {
        // Send empty digest weekly for daily/hourly users, or monthly for weekly users
        var emptyDigestFrequency = frequencyHours <= 24 ? 168 : 720; // 1 week or 1 month
        
        var lastDigestTime = await GetLastDigestTimeAsync(userId);
        if (!lastDigestTime.HasValue)
            return true;

        var timeSinceLastDigest = DateTime.UtcNow - lastDigestTime.Value;
        return timeSinceLastDigest.TotalHours >= emptyDigestFrequency;
    }

    private async Task<DateTime?> GetLastDigestTimeAsync(int userId)
    {
        // This would ideally be stored in a separate table to track digest sends
        // For now, we'll use a simple approach based on notification preferences update time
        var preferences = await _context.NotificationPreferences
            .Where(np => np.UserId == userId)
            .FirstOrDefaultAsync();

        // Return null if no preferences found (will send digest)
        return preferences?.UpdatedAt;
    }

    private async Task<string> GenerateNotificationTitleAsync(Notification notification)
    {
        var actorName = notification.ActorUser?.Username ?? "Someone";

        // For post-related notifications, use media-specific messages
        if (notification.PostId.HasValue &&
            (notification.Type == NotificationType.Like ||
             notification.Type == NotificationType.Comment ||
             notification.Type == NotificationType.Repost))
        {
            var mediaTypeText = await GetMediaTypeTextAsync(notification.PostId.Value);

            return notification.Type switch
            {
                NotificationType.Like => $"{actorName} liked your {mediaTypeText}",
                NotificationType.Comment => $"{actorName} commented on your {mediaTypeText}",
                NotificationType.Repost => $"{actorName} reposted your {mediaTypeText}",
                _ => $"{actorName} interacted with your {mediaTypeText}"
            };
        }

        // For non-post notifications, use standard messages
        return notification.Type switch
        {
            NotificationType.Like => $"{actorName} liked your post",
            NotificationType.Comment => $"{actorName} commented on your post",
            NotificationType.Repost => $"{actorName} reposted your post",
            NotificationType.Follow => $"{actorName} started following you",
            NotificationType.FollowRequest => $"{actorName} requested to follow you",
            NotificationType.Mention => $"{actorName} mentioned you",
            NotificationType.SystemMessage => "System notification",
            NotificationType.UserSuspended => "Account suspended",
            NotificationType.UserBanned => "Account banned",
            NotificationType.ContentHidden => "Content hidden",
            NotificationType.VideoProcessingCompleted => "Video processing completed",
            _ => "New notification"
        };
    }

    private string? GenerateActionUrl(Notification notification)
    {
        try
        {
            return notification.Type switch
            {
                NotificationType.Like or NotificationType.Comment or NotificationType.Repost or NotificationType.Mention
                    when notification.PostId.HasValue => $"https://yapplr.com/post/{notification.PostId}",
                NotificationType.Follow or NotificationType.FollowRequest
                    when notification.ActorUserId.HasValue => $"https://yapplr.com/user/{notification.ActorUserId}",
                NotificationType.Comment when notification.CommentId.HasValue => $"https://yapplr.com/post/{notification.PostId}#comment-{notification.CommentId}",
                _ => "https://yapplr.com/notifications"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate action URL for notification {NotificationId}", notification.Id);
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
}
