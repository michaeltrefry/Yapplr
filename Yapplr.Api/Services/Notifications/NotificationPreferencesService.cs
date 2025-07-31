using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services.Notifications;

public class NotificationPreferencesService : INotificationPreferencesService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<NotificationPreferencesService> _logger;

    public NotificationPreferencesService(
        YapplrDbContext context,
        ILogger<NotificationPreferencesService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationPreferences> GetUserPreferencesAsync(int userId)
    {
        var preferences = await _context.NotificationPreferences
            .FirstOrDefaultAsync(np => np.UserId == userId);

        if (preferences == null)
        {
            // Create default preferences for the user
            preferences = new NotificationPreferences
            {
                UserId = userId
            };

            _context.NotificationPreferences.Add(preferences);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created default notification preferences for user {UserId}", userId);
        }

        return preferences;
    }

    public async Task<NotificationPreferences> UpdateUserPreferencesAsync(int userId, UpdateNotificationPreferencesDto updateDto)
    {
        var preferences = await GetUserPreferencesAsync(userId);

        // Update only the fields that are provided
        if (updateDto.PreferredMethod.HasValue)
            preferences.PreferredMethod = updateDto.PreferredMethod.Value;

        if (updateDto.EnableMessageNotifications.HasValue)
            preferences.EnableMessageNotifications = updateDto.EnableMessageNotifications.Value;
        if (updateDto.EnableMentionNotifications.HasValue)
            preferences.EnableMentionNotifications = updateDto.EnableMentionNotifications.Value;
        if (updateDto.EnableReplyNotifications.HasValue)
            preferences.EnableReplyNotifications = updateDto.EnableReplyNotifications.Value;
        if (updateDto.EnableCommentNotifications.HasValue)
            preferences.EnableCommentNotifications = updateDto.EnableCommentNotifications.Value;
        if (updateDto.EnableFollowNotifications.HasValue)
            preferences.EnableFollowNotifications = updateDto.EnableFollowNotifications.Value;
        if (updateDto.EnableLikeNotifications.HasValue)
            preferences.EnableLikeNotifications = updateDto.EnableLikeNotifications.Value;
        if (updateDto.EnableRepostNotifications.HasValue)
            preferences.EnableRepostNotifications = updateDto.EnableRepostNotifications.Value;
        if (updateDto.EnableFollowRequestNotifications.HasValue)
            preferences.EnableFollowRequestNotifications = updateDto.EnableFollowRequestNotifications.Value;

        if (updateDto.MessageDeliveryMethod.HasValue)
            preferences.MessageDeliveryMethod = updateDto.MessageDeliveryMethod.Value;
        if (updateDto.MentionDeliveryMethod.HasValue)
            preferences.MentionDeliveryMethod = updateDto.MentionDeliveryMethod.Value;
        if (updateDto.ReplyDeliveryMethod.HasValue)
            preferences.ReplyDeliveryMethod = updateDto.ReplyDeliveryMethod.Value;
        if (updateDto.CommentDeliveryMethod.HasValue)
            preferences.CommentDeliveryMethod = updateDto.CommentDeliveryMethod.Value;
        if (updateDto.FollowDeliveryMethod.HasValue)
            preferences.FollowDeliveryMethod = updateDto.FollowDeliveryMethod.Value;
        if (updateDto.LikeDeliveryMethod.HasValue)
            preferences.LikeDeliveryMethod = updateDto.LikeDeliveryMethod.Value;
        if (updateDto.RepostDeliveryMethod.HasValue)
            preferences.RepostDeliveryMethod = updateDto.RepostDeliveryMethod.Value;
        if (updateDto.FollowRequestDeliveryMethod.HasValue)
            preferences.FollowRequestDeliveryMethod = updateDto.FollowRequestDeliveryMethod.Value;

        if (updateDto.EnableQuietHours.HasValue)
            preferences.EnableQuietHours = updateDto.EnableQuietHours.Value;
        if (updateDto.QuietHoursStart.HasValue)
            preferences.QuietHoursStart = updateDto.QuietHoursStart.Value;
        if (updateDto.QuietHoursEnd.HasValue)
            preferences.QuietHoursEnd = updateDto.QuietHoursEnd.Value;
        if (!string.IsNullOrEmpty(updateDto.QuietHoursTimezone))
            preferences.QuietHoursTimezone = updateDto.QuietHoursTimezone;

        if (updateDto.EnableFrequencyLimits.HasValue)
            preferences.EnableFrequencyLimits = updateDto.EnableFrequencyLimits.Value;
        if (updateDto.MaxNotificationsPerHour.HasValue)
            preferences.MaxNotificationsPerHour = updateDto.MaxNotificationsPerHour.Value;
        if (updateDto.MaxNotificationsPerDay.HasValue)
            preferences.MaxNotificationsPerDay = updateDto.MaxNotificationsPerDay.Value;

        if (updateDto.RequireDeliveryConfirmation.HasValue)
            preferences.RequireDeliveryConfirmation = updateDto.RequireDeliveryConfirmation.Value;
        if (updateDto.EnableReadReceipts.HasValue)
            preferences.EnableReadReceipts = updateDto.EnableReadReceipts.Value;

        if (updateDto.EnableMessageHistory.HasValue)
            preferences.EnableMessageHistory = updateDto.EnableMessageHistory.Value;
        if (updateDto.MessageHistoryDays.HasValue)
            preferences.MessageHistoryDays = updateDto.MessageHistoryDays.Value;
        if (updateDto.EnableOfflineReplay.HasValue)
            preferences.EnableOfflineReplay = updateDto.EnableOfflineReplay.Value;

        if (updateDto.EnableEmailNotifications.HasValue)
            preferences.EnableEmailNotifications = updateDto.EnableEmailNotifications.Value;
        if (updateDto.EnableEmailDigest.HasValue)
            preferences.EnableEmailDigest = updateDto.EnableEmailDigest.Value;
        if (updateDto.EmailDigestFrequencyHours.HasValue)
            preferences.EmailDigestFrequencyHours = updateDto.EmailDigestFrequencyHours.Value;
        if (updateDto.EnableInstantEmailNotifications.HasValue)
            preferences.EnableInstantEmailNotifications = updateDto.EnableInstantEmailNotifications.Value;

        preferences.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated notification preferences for user {UserId}", userId);

        return preferences;
    }

    public async Task<bool> ShouldSendNotificationAsync(int userId, string notificationType)
    {
        var preferences = await GetUserPreferencesAsync(userId);

        // Check if notification type is enabled
        var isEnabled = notificationType.ToLower() switch
        {
            "message" => preferences.EnableMessageNotifications,
            "mention" => preferences.EnableMentionNotifications,
            "reply" => preferences.EnableReplyNotifications,
            "comment" => preferences.EnableCommentNotifications,
            "follow" => preferences.EnableFollowNotifications,
            "like" => preferences.EnableLikeNotifications,
            "repost" => preferences.EnableRepostNotifications,
            "follow_request" => preferences.EnableFollowRequestNotifications,
            _ => true // Default to enabled for unknown types
        };

        if (!isEnabled)
        {
            _logger.LogDebug("Notification type {NotificationType} is disabled for user {UserId}", notificationType, userId);
            return false;
        }

        // Check quiet hours
        if (await IsInQuietHoursAsync(userId))
        {
            _logger.LogDebug("User {UserId} is in quiet hours, skipping notification", userId);
            return false;
        }

        // Check frequency limits
        if (await HasReachedFrequencyLimitAsync(userId))
        {
            _logger.LogDebug("User {UserId} has reached frequency limit, skipping notification", userId);
            return false;
        }

        return true;
    }

    public async Task<bool> ShouldSendEmailNotificationAsync(int userId, string notificationType)
    {
        var preferences = await GetUserPreferencesAsync(userId);

        // Check if email notifications are globally enabled
        if (!preferences.EnableEmailNotifications)
        {
            _logger.LogDebug("Email notifications are disabled for user {UserId}", userId);
            return false;
        }

        // Check if instant email notifications are enabled (for immediate notifications)
        if (!preferences.EnableInstantEmailNotifications)
        {
            _logger.LogDebug("Instant email notifications are disabled for user {UserId}", userId);
            return false;
        }

        // Use the same logic as regular notifications for type-specific checks
        return await ShouldSendNotificationAsync(userId, notificationType);
    }

    public async Task<NotificationDeliveryMethod> GetPreferredDeliveryMethodAsync(int userId, string notificationType)
    {
        var preferences = await GetUserPreferencesAsync(userId);

        var specificMethod = notificationType.ToLower() switch
        {
            "message" => preferences.MessageDeliveryMethod,
            "mention" => preferences.MentionDeliveryMethod,
            "reply" => preferences.ReplyDeliveryMethod,
            "comment" => preferences.CommentDeliveryMethod,
            "follow" => preferences.FollowDeliveryMethod,
            "like" => preferences.LikeDeliveryMethod,
            "repost" => preferences.RepostDeliveryMethod,
            "follow_request" => preferences.FollowRequestDeliveryMethod,
            _ => NotificationDeliveryMethod.Auto
        };

        // If specific method is Auto, use the general preference
        if (specificMethod == NotificationDeliveryMethod.Auto)
        {
            return preferences.PreferredMethod;
        }

        return specificMethod;
    }

    public async Task<bool> IsInQuietHoursAsync(int userId)
    {
        var preferences = await GetUserPreferencesAsync(userId);

        if (!preferences.EnableQuietHours)
        {
            return false;
        }

        try
        {
            // Convert to user's timezone
            var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(preferences.QuietHoursTimezone);
            var userTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);
            var currentTime = TimeOnly.FromDateTime(userTime);

            // Handle quiet hours that span midnight
            if (preferences.QuietHoursStart > preferences.QuietHoursEnd)
            {
                // Quiet hours span midnight (e.g., 22:00 to 08:00)
                return currentTime >= preferences.QuietHoursStart || currentTime <= preferences.QuietHoursEnd;
            }
            else
            {
                // Normal quiet hours (e.g., 01:00 to 06:00)
                return currentTime >= preferences.QuietHoursStart && currentTime <= preferences.QuietHoursEnd;
            }
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("Invalid timezone {Timezone} for user {UserId}, using UTC", preferences.QuietHoursTimezone, userId);
            // Fall back to UTC
            var currentTime = TimeOnly.FromDateTime(DateTime.UtcNow);
            
            if (preferences.QuietHoursStart > preferences.QuietHoursEnd)
            {
                return currentTime >= preferences.QuietHoursStart || currentTime <= preferences.QuietHoursEnd;
            }
            else
            {
                return currentTime >= preferences.QuietHoursStart && currentTime <= preferences.QuietHoursEnd;
            }
        }
    }

    public async Task<bool> HasReachedFrequencyLimitAsync(int userId)
    {
        var preferences = await GetUserPreferencesAsync(userId);

        if (!preferences.EnableFrequencyLimits)
        {
            return false;
        }

        var now = DateTime.UtcNow;

        // Check hourly limit
        var hourlyCount = await _context.NotificationHistory
            .Where(nh => nh.UserId == userId && nh.CreatedAt >= now.AddHours(-1))
            .CountAsync();

        if (hourlyCount >= preferences.MaxNotificationsPerHour)
        {
            _logger.LogInformation("User {UserId} has reached hourly notification limit ({Count}/{Limit})", 
                userId, hourlyCount, preferences.MaxNotificationsPerHour);
            return true;
        }

        // Check daily limit
        var dailyCount = await _context.NotificationHistory
            .Where(nh => nh.UserId == userId && nh.CreatedAt >= now.AddDays(-1))
            .CountAsync();

        if (dailyCount >= preferences.MaxNotificationsPerDay)
        {
            _logger.LogInformation("User {UserId} has reached daily notification limit ({Count}/{Limit})", 
                userId, dailyCount, preferences.MaxNotificationsPerDay);
            return true;
        }

        return false;
    }

    public async Task RecordNotificationSentAsync(int userId)
    {
        // This is used for frequency limiting - we'll implement this when we integrate with the notification history
        await Task.CompletedTask;
    }
}
