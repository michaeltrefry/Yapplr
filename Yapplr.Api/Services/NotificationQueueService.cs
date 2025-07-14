using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public class NotificationQueueService : INotificationQueueService
{
    private readonly YapplrDbContext _context;
    private readonly ICompositeNotificationService _notificationService;
    private readonly ISignalRConnectionPool _connectionPool;
    private readonly ILogger<NotificationQueueService> _logger;

    // In-memory queue for high-performance scenarios (using DTO)
    private readonly ConcurrentQueue<QueuedNotificationDto> _memoryQueue = new();
    private readonly ConcurrentDictionary<string, QueuedNotificationDto> _pendingNotifications = new();

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

    public async Task QueueNotificationAsync(QueuedNotificationDto notification)
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

    public async Task<List<QueuedNotificationDto>> GetPendingNotificationsAsync(int userId)
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
            return new List<QueuedNotificationDto>();
        }
    }

    public async Task<List<QueuedNotificationDto>> GetAllPendingNotificationsAsync()
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
            return new List<QueuedNotificationDto>();
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

            // Process memory queue first
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

            // Process database notifications that are ready for retry
            var dbNotificationsToProcess = await _context.QueuedNotifications
                .Where(n => n.DeliveredAt == null &&
                           n.RetryCount < n.MaxRetries &&
                           (n.NextRetryAt == null || n.NextRetryAt <= DateTime.UtcNow))
                .OrderBy(n => n.CreatedAt) // Ensure deterministic ordering - process oldest first
                .Take(100) // Process in batches to avoid overwhelming the system
                .ToListAsync();

            foreach (var dbNotification in dbNotificationsToProcess)
            {
                processedCount++;
                var notificationDto = ConvertToDto(dbNotification);

                // Check if user is now online
                var isOnline = await _connectionPool.IsUserOnlineAsync(notificationDto.UserId);

                if (isOnline)
                {
                    var delivered = await TryDeliverNotificationAsync(notificationDto);
                    if (delivered)
                    {
                        await MarkAsDeliveredAsync(notificationDto.Id);
                        deliveredCount++;
                    }
                    else
                    {
                        await MarkAsFailedAsync(notificationDto.Id, "Delivery failed");
                        failedCount++;
                    }
                }
                else
                {
                    // User still offline, update next retry time if needed
                    if (dbNotification.NextRetryAt == null || dbNotification.NextRetryAt <= DateTime.UtcNow)
                    {
                        dbNotification.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, dbNotification.RetryCount));
                        await _context.SaveChangesAsync();
                    }
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

            // Clean up database - remove old delivered or failed notifications
            var notificationsToDelete = await _context.QueuedNotifications
                .Where(n => n.CreatedAt < cutoffTime &&
                           (n.DeliveredAt != null || n.RetryCount >= n.MaxRetries))
                .ToListAsync();

            _context.QueuedNotifications.RemoveRange(notificationsToDelete);
            var deletedCount = notificationsToDelete.Count;

            // Also clean up very old undelivered notifications (older than 2x maxAge)
            var veryOldCutoff = DateTime.UtcNow - TimeSpan.FromTicks(maxAge.Ticks * 2);
            var veryOldNotifications = await _context.QueuedNotifications
                .Where(n => n.CreatedAt < veryOldCutoff)
                .ToListAsync();

            _context.QueuedNotifications.RemoveRange(veryOldNotifications);
            var veryOldDeletedCount = veryOldNotifications.Count;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {MemoryCount} memory notifications and {DbCount} database notifications older than {MaxAge}. Also removed {VeryOldCount} very old notifications.",
                oldNotifications.Count, deletedCount, maxAge, veryOldDeletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old notifications");
        }
    }

    public async Task<NotificationQueueStats> GetStatsAsync()
    {
        try
        {
            // Get database statistics
            var pendingInDb = await _context.QueuedNotifications
                .CountAsync(n => n.DeliveredAt == null && n.RetryCount < n.MaxRetries);

            var deliveredInDb = await _context.QueuedNotifications
                .CountAsync(n => n.DeliveredAt != null);

            var failedInDb = await _context.QueuedNotifications
                .CountAsync(n => n.RetryCount >= n.MaxRetries);

            var stats = new NotificationQueueStats
            {
                TotalQueued = _totalQueued,
                TotalDelivered = _totalDelivered,
                TotalFailed = _totalFailed,
                PendingInMemory = _pendingNotifications.Count,
                QueueSize = _memoryQueue.Count,
                PendingInDatabase = pendingInDb,
                DeliveredInDatabase = deliveredInDb,
                FailedInDatabase = failedInDb
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification queue stats");

            // Return basic stats if database query fails
            return new NotificationQueueStats
            {
                TotalQueued = _totalQueued,
                TotalDelivered = _totalDelivered,
                TotalFailed = _totalFailed,
                PendingInMemory = _pendingNotifications.Count,
                QueueSize = _memoryQueue.Count
            };
        }
    }

    private async Task<bool> TryDeliverNotificationAsync(QueuedNotificationDto notification)
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

    private async Task PersistNotificationAsync(QueuedNotificationDto notification)
    {
        try
        {
            // Convert Dictionary to JSON string
            string? dataJson = null;
            if (notification.Data != null)
            {
                dataJson = JsonSerializer.Serialize(notification.Data);
            }

            // Create database entity
            var dbNotification = new QueuedNotification
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Type = notification.Type,
                Title = notification.Title,
                Body = notification.Body,
                Data = dataJson,
                CreatedAt = notification.CreatedAt,
                DeliveredAt = notification.DeliveredAt,
                RetryCount = notification.RetryCount,
                MaxRetries = notification.MaxRetries,
                RetryDelayMinutes = (int)notification.RetryDelay.TotalMinutes,
                NextRetryAt = notification.NextRetryAt,
                LastError = notification.LastError
            };

            _context.QueuedNotifications.Add(dbNotification);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Persisted notification {NotificationId} to database", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist notification {NotificationId} to database", notification.Id);
            // Don't throw - we still have the in-memory queue as fallback
        }
    }

    private async Task<List<QueuedNotificationDto>> GetPersistedNotificationsAsync(int userId)
    {
        try
        {
            var dbNotifications = await _context.QueuedNotifications
                .Where(n => n.UserId == userId && n.DeliveredAt == null)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();

            return dbNotifications.Select(ConvertToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get persisted notifications for user {UserId}", userId);
            return new List<QueuedNotificationDto>();
        }
    }

    private async Task<List<QueuedNotificationDto>> GetAllPersistedNotificationsAsync()
    {
        try
        {
            var dbNotifications = await _context.QueuedNotifications
                .Where(n => n.DeliveredAt == null)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();

            return dbNotifications.Select(ConvertToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all persisted notifications");
            return new List<QueuedNotificationDto>();
        }
    }

    private async Task UpdatePersistedNotificationStatusAsync(string notificationId, bool delivered)
    {
        try
        {
            var notification = await _context.QueuedNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification != null)
            {
                if (delivered)
                {
                    notification.DeliveredAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification status for {NotificationId}", notificationId);
        }
    }

    private async Task UpdatePersistedNotificationErrorAsync(string notificationId, string error)
    {
        try
        {
            var notification = await _context.QueuedNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification != null)
            {
                notification.RetryCount++;
                notification.LastError = error;
                notification.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, notification.RetryCount));
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification error for {NotificationId}", notificationId);
        }
    }

    private QueuedNotificationDto ConvertToDto(QueuedNotification dbNotification)
    {
        Dictionary<string, string>? data = null;
        if (!string.IsNullOrEmpty(dbNotification.Data))
        {
            try
            {
                data = JsonSerializer.Deserialize<Dictionary<string, string>>(dbNotification.Data);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize notification data for {NotificationId}", dbNotification.Id);
            }
        }

        return new QueuedNotificationDto
        {
            Id = dbNotification.Id,
            UserId = dbNotification.UserId,
            Type = dbNotification.Type,
            Title = dbNotification.Title,
            Body = dbNotification.Body,
            Data = data,
            CreatedAt = dbNotification.CreatedAt,
            DeliveredAt = dbNotification.DeliveredAt,
            RetryCount = dbNotification.RetryCount,
            MaxRetries = dbNotification.MaxRetries,
            RetryDelay = TimeSpan.FromMinutes(dbNotification.RetryDelayMinutes),
            NextRetryAt = dbNotification.NextRetryAt,
            LastError = dbNotification.LastError
        };
    }
}