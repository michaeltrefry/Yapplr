using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for tracking notification delivery confirmations and history
/// </summary>
public interface INotificationDeliveryService
{
    Task<string> CreateDeliveryTrackingAsync(int userId, string notificationType, NotificationDeliveryMethod deliveryMethod);
    Task ConfirmDeliveryAsync(string notificationId);
    Task ConfirmReadAsync(string notificationId);
    Task RecordDeliveryErrorAsync(string notificationId, string error);
    Task<List<NotificationDeliveryConfirmation>> GetDeliveryStatusAsync(int userId, int count = 50);
    Task<NotificationHistory> SaveToHistoryAsync(int userId, string notificationType, string title, string body, Dictionary<string, string>? data = null);
    Task<List<NotificationHistory>> GetNotificationHistoryAsync(int userId, int count = 100);
    Task<List<NotificationHistory>> GetUndeliveredNotificationsAsync(int userId);
    Task ReplayMissedNotificationsAsync(int userId);
    Task CleanupOldHistoryAsync(TimeSpan maxAge);
    Task<Dictionary<string, object>> GetDeliveryStatsAsync(int userId, TimeSpan? timeWindow = null);
}

public class NotificationDeliveryService : INotificationDeliveryService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<NotificationDeliveryService> _logger;
    private readonly INotificationPreferencesService _preferencesService;

    public NotificationDeliveryService(
        YapplrDbContext context,
        ILogger<NotificationDeliveryService> logger,
        INotificationPreferencesService preferencesService)
    {
        _context = context;
        _logger = logger;
        _preferencesService = preferencesService;
    }

    public async Task<string> CreateDeliveryTrackingAsync(int userId, string notificationType, NotificationDeliveryMethod deliveryMethod)
    {
        var notificationId = Guid.NewGuid().ToString();
        
        var confirmation = new NotificationDeliveryConfirmation
        {
            UserId = userId,
            NotificationId = notificationId,
            NotificationType = notificationType,
            DeliveryMethod = deliveryMethod,
            SentAt = DateTime.UtcNow
        };

        _context.NotificationDeliveryConfirmations.Add(confirmation);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Created delivery tracking {NotificationId} for user {UserId} (type: {Type}, method: {Method})",
            notificationId, userId, notificationType, deliveryMethod);

        return notificationId;
    }

    public async Task ConfirmDeliveryAsync(string notificationId)
    {
        var confirmation = await _context.NotificationDeliveryConfirmations
            .FirstOrDefaultAsync(ndc => ndc.NotificationId == notificationId);

        if (confirmation != null)
        {
            confirmation.IsDelivered = true;
            confirmation.DeliveredAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Confirmed delivery for notification {NotificationId}", notificationId);
        }
        else
        {
            _logger.LogWarning("Attempted to confirm delivery for unknown notification {NotificationId}", notificationId);
        }
    }

    public async Task ConfirmReadAsync(string notificationId)
    {
        var confirmation = await _context.NotificationDeliveryConfirmations
            .FirstOrDefaultAsync(ndc => ndc.NotificationId == notificationId);

        if (confirmation != null)
        {
            confirmation.IsRead = true;
            confirmation.ReadAt = DateTime.UtcNow;
            
            // If not already marked as delivered, mark it now
            if (!confirmation.IsDelivered)
            {
                confirmation.IsDelivered = true;
                confirmation.DeliveredAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogDebug("Confirmed read for notification {NotificationId}", notificationId);
        }
        else
        {
            _logger.LogWarning("Attempted to confirm read for unknown notification {NotificationId}", notificationId);
        }
    }

    public async Task RecordDeliveryErrorAsync(string notificationId, string error)
    {
        var confirmation = await _context.NotificationDeliveryConfirmations
            .FirstOrDefaultAsync(ndc => ndc.NotificationId == notificationId);

        if (confirmation != null)
        {
            confirmation.DeliveryError = error;
            confirmation.RetryCount++;
            await _context.SaveChangesAsync();

            _logger.LogWarning("Recorded delivery error for notification {NotificationId}: {Error}", notificationId, error);
        }
        else
        {
            _logger.LogWarning("Attempted to record error for unknown notification {NotificationId}: {Error}", notificationId, error);
        }
    }

    public async Task<List<NotificationDeliveryConfirmation>> GetDeliveryStatusAsync(int userId, int count = 50)
    {
        return await _context.NotificationDeliveryConfirmations
            .Where(ndc => ndc.UserId == userId)
            .OrderByDescending(ndc => ndc.SentAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<NotificationHistory> SaveToHistoryAsync(int userId, string notificationType, string title, string body, Dictionary<string, string>? data = null)
    {
        var preferences = await _preferencesService.GetUserPreferencesAsync(userId);
        
        if (!preferences.EnableMessageHistory)
        {
            // User has disabled message history, return a dummy entry
            return new NotificationHistory
            {
                UserId = userId,
                NotificationId = Guid.NewGuid().ToString(),
                NotificationType = notificationType,
                Title = title,
                Body = body,
                Data = data
            };
        }

        var historyEntry = new NotificationHistory
        {
            UserId = userId,
            NotificationId = Guid.NewGuid().ToString(),
            NotificationType = notificationType,
            Title = title,
            Body = body,
            Data = data
        };

        _context.NotificationHistory.Add(historyEntry);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Saved notification to history {NotificationId} for user {UserId}", historyEntry.NotificationId, userId);

        return historyEntry;
    }

    public async Task<List<NotificationHistory>> GetNotificationHistoryAsync(int userId, int count = 100)
    {
        return await _context.NotificationHistory
            .Where(nh => nh.UserId == userId)
            .OrderByDescending(nh => nh.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<NotificationHistory>> GetUndeliveredNotificationsAsync(int userId)
    {
        var preferences = await _preferencesService.GetUserPreferencesAsync(userId);
        
        if (!preferences.EnableOfflineReplay)
        {
            return new List<NotificationHistory>();
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-preferences.MessageHistoryDays);

        return await _context.NotificationHistory
            .Where(nh => nh.UserId == userId && 
                        !nh.WasDelivered && 
                        !nh.WasReplayed &&
                        nh.CreatedAt >= cutoffDate)
            .OrderBy(nh => nh.CreatedAt)
            .ToListAsync();
    }

    public async Task ReplayMissedNotificationsAsync(int userId)
    {
        var undeliveredNotifications = await GetUndeliveredNotificationsAsync(userId);
        
        if (!undeliveredNotifications.Any())
        {
            _logger.LogDebug("No missed notifications to replay for user {UserId}", userId);
            return;
        }

        _logger.LogInformation("Replaying {Count} missed notifications for user {UserId}", undeliveredNotifications.Count, userId);

        foreach (var notification in undeliveredNotifications)
        {
            try
            {
                // Mark as replayed
                notification.WasReplayed = true;
                notification.ReplayedAt = DateTime.UtcNow;

                // Here you would trigger the actual notification delivery
                // This would integrate with your composite notification service
                _logger.LogDebug("Replayed notification {NotificationId} for user {UserId}: {Title}", 
                    notification.NotificationId, userId, notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to replay notification {NotificationId} for user {UserId}", 
                    notification.NotificationId, userId);
            }
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Completed replaying missed notifications for user {UserId}", userId);
    }

    public async Task CleanupOldHistoryAsync(TimeSpan maxAge)
    {
        var cutoffDate = DateTime.UtcNow - maxAge;

        // Clean up old notification history
        var oldHistory = await _context.NotificationHistory
            .Where(nh => nh.CreatedAt < cutoffDate)
            .ToListAsync();

        if (oldHistory.Any())
        {
            _context.NotificationHistory.RemoveRange(oldHistory);
            
            _logger.LogInformation("Cleaned up {Count} old notification history entries older than {CutoffDate}", 
                oldHistory.Count, cutoffDate);
        }

        // Clean up old delivery confirmations
        var oldConfirmations = await _context.NotificationDeliveryConfirmations
            .Where(ndc => ndc.SentAt < cutoffDate)
            .ToListAsync();

        if (oldConfirmations.Any())
        {
            _context.NotificationDeliveryConfirmations.RemoveRange(oldConfirmations);
            
            _logger.LogInformation("Cleaned up {Count} old delivery confirmations older than {CutoffDate}", 
                oldConfirmations.Count, cutoffDate);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets delivery statistics for a user
    /// </summary>
    public async Task<Dictionary<string, object>> GetDeliveryStatsAsync(int userId, TimeSpan? timeWindow = null)
    {
        var cutoffTime = timeWindow.HasValue ? DateTime.UtcNow - timeWindow.Value : DateTime.MinValue;

        var confirmations = await _context.NotificationDeliveryConfirmations
            .Where(ndc => ndc.UserId == userId && ndc.SentAt >= cutoffTime)
            .ToListAsync();

        var totalSent = confirmations.Count;
        var totalDelivered = confirmations.Count(c => c.IsDelivered);
        var totalRead = confirmations.Count(c => c.IsRead);
        var totalFailed = confirmations.Count(c => !string.IsNullOrEmpty(c.DeliveryError));

        var deliveryRate = totalSent > 0 ? (double)totalDelivered / totalSent * 100 : 0;
        var readRate = totalDelivered > 0 ? (double)totalRead / totalDelivered * 100 : 0;

        var averageDeliveryTime = confirmations
            .Where(c => c.IsDelivered && c.DeliveredAt.HasValue)
            .Select(c => (c.DeliveredAt!.Value - c.SentAt).TotalMilliseconds)
            .DefaultIfEmpty(0)
            .Average();

        return new Dictionary<string, object>
        {
            ["total_sent"] = totalSent,
            ["total_delivered"] = totalDelivered,
            ["total_read"] = totalRead,
            ["total_failed"] = totalFailed,
            ["delivery_rate"] = deliveryRate,
            ["read_rate"] = readRate,
            ["average_delivery_time_ms"] = averageDeliveryTime,
            ["delivery_method_breakdown"] = confirmations
                .GroupBy(c => c.DeliveryMethod)
                .ToDictionary(g => g.Key.ToString(), g => g.Count())
        };
    }
}
