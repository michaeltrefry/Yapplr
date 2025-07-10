using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

/// <summary>
/// Queued notification for offline delivery
/// </summary>
public class QueuedNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Service for queuing notifications when users are offline
/// </summary>
public interface INotificationQueueService
{
    Task QueueNotificationAsync(QueuedNotification notification);
    Task<List<QueuedNotification>> GetPendingNotificationsAsync(int userId);
    Task<List<QueuedNotification>> GetAllPendingNotificationsAsync();
    Task MarkAsDeliveredAsync(string notificationId);
    Task MarkAsFailedAsync(string notificationId, string error);
    Task ProcessPendingNotificationsAsync();
    Task CleanupOldNotificationsAsync(TimeSpan maxAge);
    Task<NotificationQueueStats> GetStatsAsync();
}

public class NotificationQueueService : INotificationQueueService
{
    private readonly YapplrDbContext _context;
    private readonly ICompositeNotificationService _notificationService;
    private readonly ISignalRConnectionPool _connectionPool;
    private readonly ILogger<NotificationQueueService> _logger;
    
    // In-memory queue for high-performance scenarios
    private readonly ConcurrentQueue<QueuedNotification> _memoryQueue = new();
    private readonly ConcurrentDictionary<string, QueuedNotification> _pendingNotifications = new();
    
    // Statistics
    private long _totalQueued = 0;
    private long _totalDelivered = 0;
    private long _totalFailed = 0;

    public NotificationQueueService(
        YapplrDbContext context,
        ICompositeNotificationService notificationService,
        ISignalRConnectionPool connectionPool,
        ILogger<NotificationQueueService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _connectionPool = connectionPool;
        _logger = logger;
    }

    public async Task QueueNotificationAsync(QueuedNotification notification)
    {
        try
        {
            // Check if user is online first
            var isOnline = await _connectionPool.IsUserOnlineAsync(notification.UserId);
            
            if (isOnline)
            {
                // Try immediate delivery
                var delivered = await TryDeliverNotificationAsync(notification);
                if (delivered)
                {
                    Interlocked.Increment(ref _totalDelivered);
                    return;
                }
            }

            // Queue for later delivery
            _memoryQueue.Enqueue(notification);
            _pendingNotifications[notification.Id] = notification;
            
            // Also persist to database for durability
            await PersistNotificationAsync(notification);
            
            Interlocked.Increment(ref _totalQueued);
            
            _logger.LogInformation("Queued notification {NotificationId} for user {UserId} (type: {Type})",
                notification.Id, notification.UserId, notification.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue notification for user {UserId}", notification.UserId);
            throw;
        }
    }

    public async Task<List<QueuedNotification>> GetPendingNotificationsAsync(int userId)
    {
        try
        {
            // Get from memory first
            var memoryNotifications = _pendingNotifications.Values
                .Where(n => n.UserId == userId && n.DeliveredAt == null)
                .ToList();

            // Also check database for any missed notifications
            var dbNotifications = await GetPersistedNotificationsAsync(userId);
            
            // Merge and deduplicate
            var allNotifications = memoryNotifications
                .Concat(dbNotifications)
                .GroupBy(n => n.Id)
                .Select(g => g.First())
                .OrderBy(n => n.CreatedAt)
                .ToList();

            return allNotifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending notifications for user {UserId}", userId);
            return new List<QueuedNotification>();
        }
    }

    public async Task<List<QueuedNotification>> GetAllPendingNotificationsAsync()
    {
        try
        {
            var memoryNotifications = _pendingNotifications.Values
                .Where(n => n.DeliveredAt == null)
                .ToList();

            var dbNotifications = await GetAllPersistedNotificationsAsync();
            
            var allNotifications = memoryNotifications
                .Concat(dbNotifications)
                .GroupBy(n => n.Id)
                .Select(g => g.First())
                .OrderBy(n => n.CreatedAt)
                .ToList();

            return allNotifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all pending notifications");
            return new List<QueuedNotification>();
        }
    }

    public async Task MarkAsDeliveredAsync(string notificationId)
    {
        try
        {
            if (_pendingNotifications.TryGetValue(notificationId, out var notification))
            {
                notification.DeliveredAt = DateTime.UtcNow;
                _pendingNotifications.TryRemove(notificationId, out _);
            }

            await UpdatePersistedNotificationStatusAsync(notificationId, delivered: true);
            
            Interlocked.Increment(ref _totalDelivered);
            
            _logger.LogDebug("Marked notification {NotificationId} as delivered", notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as delivered", notificationId);
        }
    }

    public async Task MarkAsFailedAsync(string notificationId, string error)
    {
        try
        {
            if (_pendingNotifications.TryGetValue(notificationId, out var notification))
            {
                notification.RetryCount++;
                notification.LastError = error;
                notification.NextRetryAt = DateTime.UtcNow.Add(
                    TimeSpan.FromMinutes(Math.Pow(2, notification.RetryCount))); // Exponential backoff

                if (notification.RetryCount >= notification.MaxRetries)
                {
                    _pendingNotifications.TryRemove(notificationId, out _);
                    Interlocked.Increment(ref _totalFailed);
                }
            }

            await UpdatePersistedNotificationErrorAsync(notificationId, error);
            
            _logger.LogWarning("Marked notification {NotificationId} as failed: {Error}", notificationId, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as failed", notificationId);
        }
    }

    public async Task ProcessPendingNotificationsAsync()
    {
        try
        {
            var processedCount = 0;
            var deliveredCount = 0;
            var failedCount = 0;

            // Process memory queue
            while (_memoryQueue.TryDequeue(out var notification))
            {
                processedCount++;

                // Check if it's time to retry
                if (notification.NextRetryAt.HasValue && notification.NextRetryAt > DateTime.UtcNow)
                {
                    // Re-queue for later
                    _memoryQueue.Enqueue(notification);
                    continue;
                }

                // Check if user is now online
                var isOnline = await _connectionPool.IsUserOnlineAsync(notification.UserId);
                
                if (isOnline)
                {
                    var delivered = await TryDeliverNotificationAsync(notification);
                    if (delivered)
                    {
                        await MarkAsDeliveredAsync(notification.Id);
                        deliveredCount++;
                    }
                    else
                    {
                        await MarkAsFailedAsync(notification.Id, "Delivery failed");
                        failedCount++;
                    }
                }
                else
                {
                    // User still offline, re-queue
                    _memoryQueue.Enqueue(notification);
                }
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {ProcessedCount} notifications: {DeliveredCount} delivered, {FailedCount} failed",
                    processedCount, deliveredCount, failedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process pending notifications");
        }
    }

    public async Task CleanupOldNotificationsAsync(TimeSpan maxAge)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - maxAge;

            // Clean up memory
            var oldNotifications = _pendingNotifications.Values
                .Where(n => n.CreatedAt < cutoffTime)
                .ToList();

            foreach (var notification in oldNotifications)
            {
                _pendingNotifications.TryRemove(notification.Id, out _);
            }

            // Note: Database persistence is not implemented yet, so we only clean up memory
            // TODO: Implement QueuedNotifications table and migration if persistent queuing is needed

            _logger.LogInformation("Cleaned up {MemoryCount} memory notifications older than {MaxAge}",
                oldNotifications.Count, maxAge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old notifications");
        }
    }

    public Task<NotificationQueueStats> GetStatsAsync()
    {
        var stats = new NotificationQueueStats
        {
            TotalQueued = _totalQueued,
            TotalDelivered = _totalDelivered,
            TotalFailed = _totalFailed,
            PendingInMemory = _pendingNotifications.Count,
            QueueSize = _memoryQueue.Count
        };

        return Task.FromResult(stats);
    }

    private async Task<bool> TryDeliverNotificationAsync(QueuedNotification notification)
    {
        try
        {
            return await _notificationService.SendNotificationAsync(
                notification.UserId,
                notification.Title,
                notification.Body,
                notification.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver notification {NotificationId}", notification.Id);
            return false;
        }
    }

    private async Task PersistNotificationAsync(QueuedNotification notification)
    {
        // TODO: Implement database persistence for QueuedNotification
        // This would require:
        // 1. Adding DbSet<QueuedNotification> to YapplrDbContext
        // 2. Creating a migration for the QueuedNotifications table
        // 3. Implementing the actual persistence logic here
        await Task.CompletedTask;
    }

    private async Task<List<QueuedNotification>> GetPersistedNotificationsAsync(int userId)
    {
        // TODO: Implement database retrieval for user's queued notifications
        // This would query the QueuedNotifications table for the specific user
        await Task.CompletedTask;
        return new List<QueuedNotification>();
    }

    private async Task<List<QueuedNotification>> GetAllPersistedNotificationsAsync()
    {
        // TODO: Implement database retrieval for all queued notifications
        // This would query the QueuedNotifications table for all pending notifications
        await Task.CompletedTask;
        return new List<QueuedNotification>();
    }

    private async Task UpdatePersistedNotificationStatusAsync(string notificationId, bool delivered)
    {
        // Implementation would depend on your database schema
        await Task.CompletedTask;
    }

    private async Task UpdatePersistedNotificationErrorAsync(string notificationId, string error)
    {
        // Implementation would depend on your database schema
        await Task.CompletedTask;
    }
}

/// <summary>
/// Statistics about the notification queue
/// </summary>
public class NotificationQueueStats
{
    public long TotalQueued { get; set; }
    public long TotalDelivered { get; set; }
    public long TotalFailed { get; set; }
    public int PendingInMemory { get; set; }
    public int QueueSize { get; set; }
    public double DeliveryRate => TotalQueued > 0 ? (double)TotalDelivered / TotalQueued * 100 : 0;
    public double FailureRate => TotalQueued > 0 ? (double)TotalFailed / TotalQueued * 100 : 0;
}
