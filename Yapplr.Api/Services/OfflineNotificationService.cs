using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

/// <summary>
/// Offline notification entry
/// </summary>
public class OfflineNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAt { get; set; }
    public int AttemptCount { get; set; } = 0;
    public int MaxAttempts { get; set; } = 5;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public string? LastError { get; set; }
}

/// <summary>
/// Notification priority levels
/// </summary>
public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// User connectivity status
/// </summary>
public class UserConnectivityStatus
{
    public int UserId { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeenAt { get; set; }
    public string? LastKnownConnection { get; set; } // "firebase", "signalr", "polling"
    public int OfflineNotificationCount { get; set; }
}

/// <summary>
/// Service for handling offline notification scenarios
/// </summary>
public interface IOfflineNotificationService
{
    Task QueueOfflineNotificationAsync(OfflineNotification notification);
    Task<List<OfflineNotification>> GetOfflineNotificationsAsync(int userId);
    Task ProcessOfflineNotificationsAsync(int userId);
    Task ProcessAllOfflineNotificationsAsync();
    Task MarkUserOnlineAsync(int userId, string connectionType);
    Task MarkUserOfflineAsync(int userId);
    Task<UserConnectivityStatus> GetUserConnectivityStatusAsync(int userId);
    Task<List<UserConnectivityStatus>> GetAllUserConnectivityStatusAsync();
    Task CleanupExpiredNotificationsAsync();
    Task<Dictionary<string, object>> GetOfflineStatsAsync();
}

public class OfflineNotificationService : IOfflineNotificationService
{
    private readonly YapplrDbContext _context;
    private readonly ICompositeNotificationService _notificationService;
    private readonly ISignalRConnectionPool _connectionPool;
    private readonly ILogger<OfflineNotificationService> _logger;
    
    // In-memory queues for performance
    private readonly ConcurrentDictionary<int, ConcurrentQueue<OfflineNotification>> _userQueues = new();
    private readonly ConcurrentDictionary<int, UserConnectivityStatus> _connectivityStatus = new();
    
    // Statistics
    private long _totalQueued = 0;
    private long _totalProcessed = 0;
    private long _totalDelivered = 0;
    private long _totalExpired = 0;

    public OfflineNotificationService(
        YapplrDbContext context,
        ICompositeNotificationService notificationService,
        ISignalRConnectionPool connectionPool,
        ILogger<OfflineNotificationService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _connectionPool = connectionPool;
        _logger = logger;
    }

    public async Task QueueOfflineNotificationAsync(OfflineNotification notification)
    {
        try
        {
            // Set expiration based on priority
            if (notification.ExpiresAt == default)
            {
                notification.ExpiresAt = notification.Priority switch
                {
                    NotificationPriority.Critical => DateTime.UtcNow.AddDays(7),
                    NotificationPriority.High => DateTime.UtcNow.AddDays(3),
                    NotificationPriority.Normal => DateTime.UtcNow.AddDays(1),
                    NotificationPriority.Low => DateTime.UtcNow.AddHours(6),
                    _ => DateTime.UtcNow.AddDays(1)
                };
            }

            // Add to in-memory queue
            var userQueue = _userQueues.GetOrAdd(notification.UserId, _ => new ConcurrentQueue<OfflineNotification>());
            userQueue.Enqueue(notification);

            // Update connectivity status
            if (_connectivityStatus.TryGetValue(notification.UserId, out var status))
            {
                status.OfflineNotificationCount++;
            }

            Interlocked.Increment(ref _totalQueued);

            _logger.LogInformation("Queued offline notification {NotificationId} for user {UserId} (priority: {Priority})",
                notification.Id, notification.UserId, notification.Priority);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue offline notification for user {UserId}", notification.UserId);
            throw;
        }
    }

    public async Task<List<OfflineNotification>> GetOfflineNotificationsAsync(int userId)
    {
        await Task.CompletedTask;
        
        if (_userQueues.TryGetValue(userId, out var queue))
        {
            return queue.ToList().Where(n => !n.IsExpired).OrderBy(n => n.CreatedAt).ToList();
        }

        return new List<OfflineNotification>();
    }

    public async Task ProcessOfflineNotificationsAsync(int userId)
    {
        try
        {
            // Check if user is online
            var isOnline = await _connectionPool.IsUserOnlineAsync(userId);
            if (!isOnline)
            {
                _logger.LogDebug("User {UserId} is still offline, skipping notification processing", userId);
                return;
            }

            if (!_userQueues.TryGetValue(userId, out var queue))
            {
                return; // No notifications for this user
            }

            var processedCount = 0;
            var deliveredCount = 0;
            var notifications = new List<OfflineNotification>();

            // Collect all notifications from queue
            while (queue.TryDequeue(out var notification))
            {
                notifications.Add(notification);
            }

            // Sort by priority and creation time
            notifications = notifications
                .Where(n => !n.IsExpired)
                .OrderByDescending(n => n.Priority)
                .ThenBy(n => n.CreatedAt)
                .ToList();

            foreach (var notification in notifications)
            {
                processedCount++;
                Interlocked.Increment(ref _totalProcessed);

                try
                {
                    notification.AttemptCount++;
                    notification.LastAttemptAt = DateTime.UtcNow;

                    var success = await _notificationService.SendNotificationAsync(
                        notification.UserId,
                        notification.Title,
                        notification.Body,
                        notification.Data);

                    if (success)
                    {
                        deliveredCount++;
                        Interlocked.Increment(ref _totalDelivered);
                        
                        _logger.LogDebug("Delivered offline notification {NotificationId} to user {UserId}",
                            notification.Id, notification.UserId);
                    }
                    else
                    {
                        // Re-queue if not exceeded max attempts
                        if (notification.AttemptCount < notification.MaxAttempts)
                        {
                            queue.Enqueue(notification);
                            _logger.LogWarning("Failed to deliver offline notification {NotificationId}, re-queued (attempt {Attempt}/{MaxAttempts})",
                                notification.Id, notification.AttemptCount, notification.MaxAttempts);
                        }
                        else
                        {
                            _logger.LogWarning("Offline notification {NotificationId} exceeded max attempts, discarding",
                                notification.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    notification.LastError = ex.Message;
                    
                    if (notification.AttemptCount < notification.MaxAttempts)
                    {
                        queue.Enqueue(notification);
                    }
                    
                    _logger.LogError(ex, "Error processing offline notification {NotificationId} for user {UserId}",
                        notification.Id, notification.UserId);
                }
            }

            // Update connectivity status
            if (_connectivityStatus.TryGetValue(userId, out var status))
            {
                status.OfflineNotificationCount = queue.Count;
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {ProcessedCount} offline notifications for user {UserId}, delivered {DeliveredCount}",
                    processedCount, userId, deliveredCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process offline notifications for user {UserId}", userId);
        }
    }

    public async Task ProcessAllOfflineNotificationsAsync()
    {
        var userIds = _userQueues.Keys.ToList();
        var tasks = userIds.Select(ProcessOfflineNotificationsAsync);
        
        await Task.WhenAll(tasks);
        
        _logger.LogDebug("Processed offline notifications for {UserCount} users", userIds.Count);
    }

    public async Task MarkUserOnlineAsync(int userId, string connectionType)
    {
        var status = _connectivityStatus.AddOrUpdate(userId,
            new UserConnectivityStatus
            {
                UserId = userId,
                IsOnline = true,
                LastSeenAt = DateTime.UtcNow,
                LastKnownConnection = connectionType,
                OfflineNotificationCount = _userQueues.TryGetValue(userId, out var queue) ? queue.Count : 0
            },
            (key, existing) =>
            {
                existing.IsOnline = true;
                existing.LastSeenAt = DateTime.UtcNow;
                existing.LastKnownConnection = connectionType;
                return existing;
            });

        _logger.LogDebug("User {UserId} marked as online via {ConnectionType}", userId, connectionType);

        // Process any queued notifications
        await ProcessOfflineNotificationsAsync(userId);
    }

    public async Task MarkUserOfflineAsync(int userId)
    {
        _connectivityStatus.AddOrUpdate(userId,
            new UserConnectivityStatus
            {
                UserId = userId,
                IsOnline = false,
                LastSeenAt = DateTime.UtcNow,
                OfflineNotificationCount = _userQueues.TryGetValue(userId, out var queue) ? queue.Count : 0
            },
            (key, existing) =>
            {
                existing.IsOnline = false;
                existing.LastSeenAt = DateTime.UtcNow;
                return existing;
            });

        _logger.LogDebug("User {UserId} marked as offline", userId);
        await Task.CompletedTask;
    }

    public async Task<UserConnectivityStatus> GetUserConnectivityStatusAsync(int userId)
    {
        await Task.CompletedTask;
        
        if (_connectivityStatus.TryGetValue(userId, out var status))
        {
            // Update offline notification count
            status.OfflineNotificationCount = _userQueues.TryGetValue(userId, out var queue) ? queue.Count : 0;
            return status;
        }

        return new UserConnectivityStatus
        {
            UserId = userId,
            IsOnline = false,
            LastSeenAt = DateTime.MinValue,
            OfflineNotificationCount = 0
        };
    }

    public async Task<List<UserConnectivityStatus>> GetAllUserConnectivityStatusAsync()
    {
        await Task.CompletedTask;
        return _connectivityStatus.Values.ToList();
    }

    public async Task CleanupExpiredNotificationsAsync()
    {
        var expiredCount = 0;

        foreach (var kvp in _userQueues)
        {
            var userId = kvp.Key;
            var queue = kvp.Value;
            var validNotifications = new List<OfflineNotification>();

            while (queue.TryDequeue(out var notification))
            {
                if (notification.IsExpired)
                {
                    expiredCount++;
                    Interlocked.Increment(ref _totalExpired);
                }
                else
                {
                    validNotifications.Add(notification);
                }
            }

            // Re-queue valid notifications
            foreach (var notification in validNotifications)
            {
                queue.Enqueue(notification);
            }
        }

        if (expiredCount > 0)
        {
            _logger.LogInformation("Cleaned up {ExpiredCount} expired offline notifications", expiredCount);
        }

        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, object>> GetOfflineStatsAsync()
    {
        await Task.CompletedTask;
        
        var totalQueued = _userQueues.Values.Sum(q => q.Count);
        var onlineUsers = _connectivityStatus.Values.Count(s => s.IsOnline);
        var offlineUsers = _connectivityStatus.Values.Count(s => !s.IsOnline);

        return new Dictionary<string, object>
        {
            ["total_queued_notifications"] = totalQueued,
            ["total_users_tracked"] = _connectivityStatus.Count,
            ["online_users"] = onlineUsers,
            ["offline_users"] = offlineUsers,
            ["total_notifications_queued"] = _totalQueued,
            ["total_notifications_processed"] = _totalProcessed,
            ["total_notifications_delivered"] = _totalDelivered,
            ["total_notifications_expired"] = _totalExpired,
            ["delivery_success_rate"] = _totalProcessed > 0 ? (double)_totalDelivered / _totalProcessed * 100 : 0,
            ["users_with_pending_notifications"] = _userQueues.Count(kvp => !kvp.Value.IsEmpty)
        };
    }
}
