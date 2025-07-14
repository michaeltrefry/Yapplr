using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Services;

namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Unified notification queue that consolidates NotificationQueueService, OfflineNotificationService, and SmartRetryService.
/// Provides hybrid in-memory/database queuing with smart retry logic and user connectivity tracking.
/// </summary>
public class NotificationQueue : INotificationQueue
{
    private readonly YapplrDbContext _context;
    private readonly INotificationProviderManager _providerManager;
    private readonly ISignalRConnectionPool _connectionPool;
    private readonly ILogger<NotificationQueue> _logger;

    #region Configuration
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromDays(7);
    private readonly TimeSpan _memoryQueueThreshold = TimeSpan.FromHours(1);
    private readonly int _maxMemoryQueueSize = 10000;
    private readonly int _batchProcessingSize = 100;
    #endregion

    #region In-Memory Collections
    // High-performance in-memory queue for immediate processing
    private readonly ConcurrentQueue<QueuedNotification> _memoryQueue = new();
    private readonly ConcurrentDictionary<string, QueuedNotification> _pendingNotifications = new();
    
    // User connectivity tracking
    private readonly ConcurrentDictionary<int, UserConnectivityStatus> _userConnectivity = new();
    private readonly ConcurrentDictionary<int, ConcurrentQueue<QueuedNotification>> _userQueues = new();
    #endregion

    #region Statistics
    private long _totalQueued = 0;
    private long _totalDelivered = 0;
    private long _totalFailed = 0;
    private long _totalExpired = 0;
    private long _totalRetried = 0;
    #endregion

    #region Retry Configuration
    private readonly Dictionary<NotificationErrorType, RetryStrategy> _retryStrategies = new()
    {
        [NotificationErrorType.NetworkTimeout] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromMinutes(5),
            MaxAttempts = 5,
            BackoffMultiplier = 2.0,
            UseJitter = true
        },
        [NotificationErrorType.NetworkUnavailable] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromSeconds(5),
            MaxDelay = TimeSpan.FromMinutes(10),
            MaxAttempts = 3,
            BackoffMultiplier = 3.0,
            UseJitter = true
        },
        [NotificationErrorType.ServiceUnavailable] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromSeconds(10),
            MaxDelay = TimeSpan.FromMinutes(15),
            MaxAttempts = 4,
            BackoffMultiplier = 2.5,
            UseJitter = true
        },
        [NotificationErrorType.RateLimited] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromMinutes(1),
            MaxDelay = TimeSpan.FromHours(1),
            MaxAttempts = 3,
            BackoffMultiplier = 4.0,
            UseJitter = false
        },
        [NotificationErrorType.ServerError] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromMinutes(5),
            MaxAttempts = 3,
            BackoffMultiplier = 2.0,
            UseJitter = true
        },
        [NotificationErrorType.InvalidToken] = new()
        {
            ShouldRetry = false,
            MaxAttempts = 0
        },
        [NotificationErrorType.PermissionDenied] = new()
        {
            ShouldRetry = false,
            MaxAttempts = 0
        },
        [NotificationErrorType.InvalidPayload] = new()
        {
            ShouldRetry = false,
            MaxAttempts = 0
        },
        [NotificationErrorType.ClientError] = new()
        {
            ShouldRetry = false,
            MaxAttempts = 0
        },
        [NotificationErrorType.QuotaExceeded] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromHours(1),
            MaxDelay = TimeSpan.FromHours(24),
            MaxAttempts = 2,
            BackoffMultiplier = 24.0,
            UseJitter = false
        }
    };

    private readonly Random _random = new();
    #endregion

    public NotificationQueue(
        YapplrDbContext context,
        INotificationProviderManager providerManager,
        ISignalRConnectionPool connectionPool,
        ILogger<NotificationQueue> logger)
    {
        _context = context;
        _providerManager = providerManager;
        _connectionPool = connectionPool;
        _logger = logger;
    }

    #region Queuing Operations

    public async Task QueueNotificationAsync(QueuedNotification notification)
    {
        try
        {
            // Set default expiration if not provided
            if (notification.ExpiresAt == null)
            {
                notification.ExpiresAt = notification.Priority switch
                {
                    NotificationPriority.Critical => DateTime.UtcNow.Add(_defaultExpiration),
                    NotificationPriority.High => DateTime.UtcNow.AddDays(3),
                    NotificationPriority.Normal => DateTime.UtcNow.AddDays(1),
                    NotificationPriority.Low => DateTime.UtcNow.AddHours(6),
                    _ => DateTime.UtcNow.Add(_defaultExpiration)
                };
            }

            // Check if user is online for immediate delivery
            var isOnline = await IsUserOnlineAsync(notification.UserId);
            
            if (isOnline)
            {
                // Try immediate delivery
                var delivered = await TryDeliverNotificationAsync(notification);
                if (delivered)
                {
                    Interlocked.Increment(ref _totalDelivered);
                    _logger.LogDebug("Immediately delivered notification {NotificationId} to online user {UserId}",
                        notification.Id, notification.UserId);
                    return;
                }
            }

            // Queue for later delivery
            await QueueForLaterDeliveryAsync(notification);
            
            Interlocked.Increment(ref _totalQueued);
            _logger.LogInformation("Queued notification {NotificationId} for user {UserId} (priority: {Priority})",
                notification.Id, notification.UserId, notification.Priority);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue notification {NotificationId} for user {UserId}",
                notification.Id, notification.UserId);
            throw;
        }
    }

    private async Task QueueForLaterDeliveryAsync(QueuedNotification notification)
    {
        // Determine storage strategy based on expected delivery time
        var expectedDeliveryTime = notification.ScheduledFor ?? DateTime.UtcNow;
        var timeUntilDelivery = expectedDeliveryTime - DateTime.UtcNow;

        if (timeUntilDelivery <= _memoryQueueThreshold && _memoryQueue.Count < _maxMemoryQueueSize)
        {
            // Use in-memory queue for short-term storage
            _memoryQueue.Enqueue(notification);
            _pendingNotifications[notification.Id] = notification;
            
            // Also add to user-specific queue for efficient user-based processing
            var userQueue = _userQueues.GetOrAdd(notification.UserId, _ => new ConcurrentQueue<QueuedNotification>());
            userQueue.Enqueue(notification);
        }
        else
        {
            // Use database for long-term storage
            await PersistNotificationAsync(notification);
        }
    }

    private async Task PersistNotificationAsync(QueuedNotification notification)
    {
        try
        {
            var dbNotification = new Models.QueuedNotification
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Type = notification.NotificationType,
                Title = notification.Title,
                Body = notification.Body,
                Data = notification.Data != null ? System.Text.Json.JsonSerializer.Serialize(notification.Data) : null,
                CreatedAt = notification.CreatedAt,
                RetryCount = notification.AttemptCount,
                MaxRetries = notification.MaxAttempts,
                RetryDelayMinutes = 1, // Default retry delay
                NextRetryAt = notification.NextRetryAt
            };

            _context.QueuedNotifications.Add(dbNotification);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist notification {NotificationId} to database", notification.Id);
            throw;
        }
    }

    private async Task<bool> TryDeliverNotificationAsync(QueuedNotification notification)
    {
        try
        {
            notification.AttemptCount++;
            notification.Status = QueuedNotificationStatus.Processing;

            // Use the provider manager for delivery
            var request = new NotificationDeliveryRequest
            {
                UserId = notification.UserId,
                NotificationType = notification.NotificationType,
                Title = notification.Title,
                Body = notification.Body,
                Data = notification.Data,
                Priority = notification.Priority
            };

            var success = await _providerManager.SendNotificationAsync(request);

            if (success)
            {
                notification.Status = QueuedNotificationStatus.Delivered;
                notification.DeliveredAt = DateTime.UtcNow;
                return true;
            }
            else
            {
                notification.Status = QueuedNotificationStatus.Failed;
                return false;
            }
        }
        catch (Exception ex)
        {
            notification.Status = QueuedNotificationStatus.Failed;
            notification.LastError = ex.Message;
            _logger.LogWarning(ex, "Failed to deliver notification {NotificationId} to user {UserId}",
                notification.Id, notification.UserId);
            return false;
        }
    }

    #endregion

    #region Retrieval Operations

    public async Task<List<QueuedNotification>> GetPendingNotificationsAsync(int userId)
    {
        try
        {
            var notifications = new List<QueuedNotification>();

            // Get from user-specific memory queue
            if (_userQueues.TryGetValue(userId, out var userQueue))
            {
                var tempList = new List<QueuedNotification>();
                while (userQueue.TryDequeue(out var notification))
                {
                    if (notification.Status == QueuedNotificationStatus.Pending && !notification.IsExpired())
                    {
                        notifications.Add(notification);
                    }
                    tempList.Add(notification); // Re-queue non-expired notifications
                }

                // Re-queue non-expired notifications
                foreach (var notification in tempList.Where(n => !n.IsExpired()))
                {
                    userQueue.Enqueue(notification);
                }
            }

            // Get from database
            var dbNotifications = await _context.QueuedNotifications
                .Where(n => n.UserId == userId && n.DeliveredAt == null)
                .OrderBy(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();

            foreach (var dbNotification in dbNotifications)
            {
                notifications.Add(ConvertFromDbModel(dbNotification));
            }

            // Remove duplicates and sort by priority and creation time
            return notifications
                .GroupBy(n => n.Id)
                .Select(g => g.First())
                .OrderByDescending(n => n.Priority)
                .ThenBy(n => n.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending notifications for user {UserId}", userId);
            return new List<QueuedNotification>();
        }
    }

    public async Task<List<QueuedNotification>> GetAllPendingNotificationsAsync(int limit = 1000)
    {
        try
        {
            var notifications = new List<QueuedNotification>();

            // Get from memory queue
            var memoryNotifications = _pendingNotifications.Values
                .Where(n => n.Status == QueuedNotificationStatus.Pending && !n.IsExpired())
                .ToList();
            notifications.AddRange(memoryNotifications);

            // Get from database
            var dbNotifications = await _context.QueuedNotifications
                .Where(n => n.DeliveredAt == null)
                .OrderBy(n => n.CreatedAt)
                .Take(limit) // Use provided limit
                .ToListAsync();

            foreach (var dbNotification in dbNotifications)
            {
                notifications.Add(ConvertFromDbModel(dbNotification));
            }

            // Remove duplicates and sort
            return notifications
                .GroupBy(n => n.Id)
                .Select(g => g.First())
                .OrderByDescending(n => n.Priority)
                .ThenBy(n => n.CreatedAt)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all pending notifications");
            return new List<QueuedNotification>();
        }
    }

    private QueuedNotification ConvertFromDbModel(Models.QueuedNotification dbNotification)
    {
        var data = !string.IsNullOrEmpty(dbNotification.Data)
            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(dbNotification.Data)
            : null;

        return new QueuedNotification
        {
            Id = dbNotification.Id,
            UserId = dbNotification.UserId,
            NotificationType = dbNotification.Type,
            Title = dbNotification.Title,
            Body = dbNotification.Body,
            Data = data,
            CreatedAt = dbNotification.CreatedAt,
            AttemptCount = dbNotification.RetryCount,
            MaxAttempts = dbNotification.MaxRetries,
            NextRetryAt = dbNotification.NextRetryAt,
            LastError = dbNotification.LastError,
            Status = dbNotification.DeliveredAt.HasValue ? QueuedNotificationStatus.Delivered : QueuedNotificationStatus.Pending,
            DeliveredAt = dbNotification.DeliveredAt
        };
    }

    #endregion

    #region User Connectivity Management

    public async Task MarkUserOnlineAsync(int userId, string connectionType)
    {
        var status = _userConnectivity.AddOrUpdate(userId,
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
                existing.OfflineNotificationCount = _userQueues.TryGetValue(userId, out var queue) ? queue.Count : 0;
                return existing;
            });

        _logger.LogDebug("User {UserId} marked as online via {ConnectionType}", userId, connectionType);

        // Process any queued notifications for this user
        await ProcessUserNotificationsAsync(userId);
    }

    public async Task MarkUserOfflineAsync(int userId)
    {
        _userConnectivity.AddOrUpdate(userId,
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
                existing.OfflineNotificationCount = _userQueues.TryGetValue(userId, out var queue) ? queue.Count : 0;
                return existing;
            });

        _logger.LogDebug("User {UserId} marked as offline", userId);
        await Task.CompletedTask;
    }

    public async Task<bool> IsUserOnlineAsync(int userId)
    {
        // First check our connectivity tracking
        if (_userConnectivity.TryGetValue(userId, out var status) && status.IsOnline)
        {
            return true;
        }

        // Fallback to SignalR connection pool
        return await _connectionPool.IsUserOnlineAsync(userId);
    }

    public async Task<UserConnectivityStatus> GetUserConnectivityAsync(int userId)
    {
        await Task.CompletedTask;

        if (_userConnectivity.TryGetValue(userId, out var status))
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

    #endregion

    #region Processing Operations

    public async Task<int> ProcessPendingNotificationsAsync()
    {
        try
        {
            var processedCount = 0;
            var deliveredCount = 0;
            var failedCount = 0;

            _logger.LogDebug("Starting to process pending notifications");

            // Process memory queue first (higher priority)
            processedCount += await ProcessMemoryQueueAsync();

            // Process database notifications in batches
            var dbNotifications = await _context.QueuedNotifications
                .Where(n => n.DeliveredAt == null &&
                           n.RetryCount < n.MaxRetries &&
                           (n.NextRetryAt == null || n.NextRetryAt <= DateTime.UtcNow))
                .OrderBy(n => n.CreatedAt)
                .Take(_batchProcessingSize)
                .ToListAsync();

            foreach (var dbNotification in dbNotifications)
            {
                processedCount++;
                var notification = ConvertFromDbModel(dbNotification);

                // Check if user is online
                var isOnline = await IsUserOnlineAsync(notification.UserId);

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
                        await HandleFailedDeliveryAsync(notification);
                        failedCount++;
                    }
                }
                else
                {
                    // User still offline, update next retry time
                    await ScheduleRetryAsync(notification);
                }
            }

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {ProcessedCount} notifications: {DeliveredCount} delivered, {FailedCount} failed",
                    processedCount, deliveredCount, failedCount);
            }

            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending notifications");
            return 0;
        }
    }

    private async Task<int> ProcessMemoryQueueAsync()
    {
        var processedCount = 0;
        var tempQueue = new Queue<QueuedNotification>();

        // Process all items in memory queue
        while (_memoryQueue.TryDequeue(out var notification))
        {
            processedCount++;

            // Check if expired
            if (notification.IsExpired())
            {
                _pendingNotifications.TryRemove(notification.Id, out _);
                Interlocked.Increment(ref _totalExpired);
                continue;
            }

            // Check if it's time to retry
            if (notification.NextRetryAt.HasValue && notification.NextRetryAt > DateTime.UtcNow)
            {
                tempQueue.Enqueue(notification);
                continue;
            }

            // Check if user is online
            var isOnline = await IsUserOnlineAsync(notification.UserId);

            if (isOnline)
            {
                var delivered = await TryDeliverNotificationAsync(notification);
                if (delivered)
                {
                    _pendingNotifications.TryRemove(notification.Id, out _);
                    Interlocked.Increment(ref _totalDelivered);
                }
                else
                {
                    await HandleFailedDeliveryAsync(notification);
                    if (notification.AttemptCount < notification.MaxAttempts)
                    {
                        tempQueue.Enqueue(notification);
                    }
                    else
                    {
                        _pendingNotifications.TryRemove(notification.Id, out _);
                        Interlocked.Increment(ref _totalFailed);
                    }
                }
            }
            else
            {
                // User still offline, re-queue
                tempQueue.Enqueue(notification);
            }
        }

        // Re-queue items that need to be processed later
        while (tempQueue.Count > 0)
        {
            _memoryQueue.Enqueue(tempQueue.Dequeue());
        }

        return processedCount;
    }

    public async Task<int> ProcessUserNotificationsAsync(int userId)
    {
        try
        {
            if (!_userQueues.TryGetValue(userId, out var userQueue))
            {
                return 0; // No notifications for this user
            }

            var processedCount = 0;
            var deliveredCount = 0;
            var tempQueue = new Queue<QueuedNotification>();

            // Process all notifications for this user
            while (userQueue.TryDequeue(out var notification))
            {
                processedCount++;

                if (notification.IsExpired())
                {
                    Interlocked.Increment(ref _totalExpired);
                    continue;
                }

                var delivered = await TryDeliverNotificationAsync(notification);
                if (delivered)
                {
                    _pendingNotifications.TryRemove(notification.Id, out _);
                    deliveredCount++;
                    Interlocked.Increment(ref _totalDelivered);
                }
                else
                {
                    await HandleFailedDeliveryAsync(notification);
                    if (notification.AttemptCount < notification.MaxAttempts)
                    {
                        tempQueue.Enqueue(notification);
                    }
                    else
                    {
                        _pendingNotifications.TryRemove(notification.Id, out _);
                        Interlocked.Increment(ref _totalFailed);
                    }
                }
            }

            // Re-queue failed notifications that can be retried
            while (tempQueue.Count > 0)
            {
                userQueue.Enqueue(tempQueue.Dequeue());
            }

            if (processedCount > 0)
            {
                _logger.LogDebug("Processed {ProcessedCount} notifications for user {UserId}: {DeliveredCount} delivered",
                    processedCount, userId, deliveredCount);
            }

            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notifications for user {UserId}", userId);
            return 0;
        }
    }

    #endregion

    #region Retry Logic and Error Handling

    private async Task HandleFailedDeliveryAsync(QueuedNotification notification)
    {
        try
        {
            // Note: AttemptCount is already incremented by TryDeliverNotificationAsync
            // Don't increment it again here to avoid double counting

            // Classify the error and determine retry strategy
            var errorType = ClassifyError(notification.LastError);
            var strategy = GetRetryStrategy(errorType);

            if (strategy.ShouldRetry && notification.AttemptCount < strategy.MaxAttempts)
            {
                // Calculate next retry time
                var delay = CalculateRetryDelay(strategy, notification.AttemptCount);
                notification.NextRetryAt = DateTime.UtcNow.Add(delay);
                notification.Status = QueuedNotificationStatus.Pending;

                Interlocked.Increment(ref _totalRetried);

                _logger.LogWarning("Scheduling retry for notification {NotificationId} in {Delay} (attempt {Attempt}/{MaxAttempts})",
                    notification.Id, delay, notification.AttemptCount, strategy.MaxAttempts);
            }
            else
            {
                notification.Status = QueuedNotificationStatus.Failed;
                _logger.LogWarning("Notification {NotificationId} failed permanently after {AttemptCount} attempts",
                    notification.Id, notification.AttemptCount);
            }

            // Update database record with new retry count and next retry time
            await UpdateDatabaseRetryInfoAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling failed delivery for notification {NotificationId}", notification.Id);
        }
    }

    private async Task UpdateDatabaseRetryInfoAsync(QueuedNotification notification)
    {
        try
        {
            var dbNotification = await _context.QueuedNotifications
                .FirstOrDefaultAsync(n => n.Id == notification.Id);

            if (dbNotification != null)
            {
                dbNotification.RetryCount = notification.AttemptCount;
                dbNotification.NextRetryAt = notification.NextRetryAt;
                dbNotification.LastError = notification.LastError;

                await _context.SaveChangesAsync();

                _logger.LogDebug("Updated database retry info for notification {NotificationId}: RetryCount={RetryCount}, NextRetryAt={NextRetryAt}",
                    notification.Id, notification.AttemptCount, notification.NextRetryAt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update database retry info for notification {NotificationId}", notification.Id);
        }
    }

    private NotificationErrorType ClassifyError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return NotificationErrorType.Unknown;

        var message = errorMessage.ToLowerInvariant();

        return message switch
        {
            var msg when msg.Contains("timeout") => NotificationErrorType.NetworkTimeout,
            var msg when msg.Contains("network") => NotificationErrorType.NetworkUnavailable,
            var msg when msg.Contains("unavailable") => NotificationErrorType.ServiceUnavailable,
            var msg when msg.Contains("rate limit") => NotificationErrorType.RateLimited,
            var msg when msg.Contains("unauthorized") => NotificationErrorType.PermissionDenied,
            var msg when msg.Contains("invalid token") => NotificationErrorType.InvalidToken,
            var msg when msg.Contains("invalid payload") => NotificationErrorType.InvalidPayload,
            var msg when msg.Contains("quota") => NotificationErrorType.QuotaExceeded,
            var msg when msg.Contains("server error") => NotificationErrorType.ServerError,
            var msg when msg.Contains("client error") => NotificationErrorType.ClientError,
            _ => NotificationErrorType.Unknown
        };
    }

    private RetryStrategy GetRetryStrategy(NotificationErrorType errorType)
    {
        if (_retryStrategies.TryGetValue(errorType, out var strategy))
        {
            return strategy;
        }

        // Default strategy for unknown errors
        return new RetryStrategy
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromMinutes(2),
            MaxAttempts = 2,
            BackoffMultiplier = 2.0,
            UseJitter = true
        };
    }

    private TimeSpan CalculateRetryDelay(RetryStrategy strategy, int attemptNumber)
    {
        if (!strategy.ShouldRetry)
            return TimeSpan.Zero;

        var delay = TimeSpan.FromMilliseconds(
            strategy.InitialDelay.TotalMilliseconds * Math.Pow(strategy.BackoffMultiplier, attemptNumber - 1));

        // Cap at max delay
        if (delay > strategy.MaxDelay)
            delay = strategy.MaxDelay;

        // Add jitter to prevent thundering herd
        if (strategy.UseJitter)
        {
            var jitterMs = _random.Next(0, (int)(delay.TotalMilliseconds * 0.1)); // Up to 10% jitter
            delay = delay.Add(TimeSpan.FromMilliseconds(jitterMs));
        }

        return delay;
    }

    private async Task ScheduleRetryAsync(QueuedNotification notification)
    {
        var errorType = ClassifyError(notification.LastError);
        var strategy = GetRetryStrategy(errorType);

        if (strategy.ShouldRetry && notification.AttemptCount < strategy.MaxAttempts)
        {
            var delay = CalculateRetryDelay(strategy, notification.AttemptCount);
            notification.NextRetryAt = DateTime.UtcNow.Add(delay);

            // Update database record if it exists
            var dbNotification = await _context.QueuedNotifications
                .FirstOrDefaultAsync(n => n.Id == notification.Id);

            if (dbNotification != null)
            {
                dbNotification.NextRetryAt = notification.NextRetryAt;
                await _context.SaveChangesAsync();
            }
        }
    }

    #endregion

    #region Management Operations

    public async Task MarkAsDeliveredAsync(string notificationId)
    {
        try
        {
            // Remove from memory collections
            _pendingNotifications.TryRemove(notificationId, out _);

            // Update database record
            var dbNotification = await _context.QueuedNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (dbNotification != null)
            {
                dbNotification.DeliveredAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

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
            // Update memory collection
            if (_pendingNotifications.TryGetValue(notificationId, out var notification))
            {
                notification.Status = QueuedNotificationStatus.Failed;
                notification.LastError = error;
            }

            // Update database record
            var dbNotification = await _context.QueuedNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (dbNotification != null)
            {
                dbNotification.LastError = error;
                dbNotification.RetryCount++;
                await _context.SaveChangesAsync();
            }

            _logger.LogDebug("Marked notification {NotificationId} as failed: {Error}", notificationId, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as failed", notificationId);
        }
    }

    #endregion

    #region Cleanup and Maintenance

    public async Task<int> CleanupExpiredNotificationsAsync(TimeSpan? maxAge = null)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.Subtract(maxAge ?? _defaultExpiration);
            var cleanedCount = 0;

            // Clean memory collections
            var expiredIds = new List<string>();
            foreach (var kvp in _pendingNotifications)
            {
                if (kvp.Value.IsExpired() || kvp.Value.CreatedAt < cutoffTime)
                {
                    expiredIds.Add(kvp.Key);
                }
            }

            foreach (var id in expiredIds)
            {
                _pendingNotifications.TryRemove(id, out _);
                cleanedCount++;
            }

            // Clean user queues
            foreach (var userQueue in _userQueues.Values)
            {
                var tempQueue = new Queue<QueuedNotification>();
                while (userQueue.TryDequeue(out var notification))
                {
                    if (!notification.IsExpired() && notification.CreatedAt >= cutoffTime)
                    {
                        tempQueue.Enqueue(notification);
                    }
                    else
                    {
                        cleanedCount++;
                    }
                }

                // Re-queue non-expired notifications
                while (tempQueue.Count > 0)
                {
                    userQueue.Enqueue(tempQueue.Dequeue());
                }
            }

            // Clean database
            var expiredDbNotifications = await _context.QueuedNotifications
                .Where(n => n.CreatedAt < cutoffTime ||
                           (n.DeliveredAt.HasValue && n.DeliveredAt < cutoffTime))
                .ToListAsync();

            if (expiredDbNotifications.Any())
            {
                _context.QueuedNotifications.RemoveRange(expiredDbNotifications);
                await _context.SaveChangesAsync();
            }

            var dbCleanedCount = expiredDbNotifications.Count;
            cleanedCount += dbCleanedCount;

            if (cleanedCount > 0)
            {
                _logger.LogInformation("Cleaned up {CleanedCount} expired notifications", cleanedCount);
            }

            return cleanedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired notifications");
            return 0;
        }
    }

    public async Task<int> CleanupOldNotificationsAsync(TimeSpan maxAge)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.Subtract(maxAge);
            var cleanedCount = 0;

            _logger.LogDebug("Starting cleanup of notifications older than {MaxAge}", maxAge);

            // Clean memory collections
            var oldIds = new List<string>();
            foreach (var kvp in _pendingNotifications)
            {
                if (kvp.Value.CreatedAt < cutoffTime)
                {
                    oldIds.Add(kvp.Key);
                }
            }

            foreach (var id in oldIds)
            {
                _pendingNotifications.TryRemove(id, out _);
                cleanedCount++;
            }

            // Clean user queues
            foreach (var userQueue in _userQueues.Values)
            {
                var tempQueue = new Queue<QueuedNotification>();
                while (userQueue.TryDequeue(out var notification))
                {
                    if (notification.CreatedAt >= cutoffTime)
                    {
                        tempQueue.Enqueue(notification);
                    }
                    else
                    {
                        cleanedCount++;
                    }
                }

                // Re-queue non-old notifications
                while (tempQueue.Count > 0)
                {
                    userQueue.Enqueue(tempQueue.Dequeue());
                }
            }

            // Clean database
            var oldDbNotifications = await _context.QueuedNotifications
                .Where(n => n.CreatedAt < cutoffTime)
                .ToListAsync();

            if (oldDbNotifications.Any())
            {
                _context.QueuedNotifications.RemoveRange(oldDbNotifications);
                await _context.SaveChangesAsync();
            }

            var dbCleanedCount = oldDbNotifications.Count;
            cleanedCount += dbCleanedCount;

            if (cleanedCount > 0)
            {
                _logger.LogInformation("Cleaned up {CleanedCount} old notifications older than {MaxAge}", cleanedCount, maxAge);
            }

            return cleanedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
            return 0;
        }
    }

    public async Task<int> RetryFailedNotificationsAsync()
    {
        try
        {
            var retriedCount = 0;

            // Retry failed notifications in database that are eligible
            var failedNotifications = await _context.QueuedNotifications
                .Where(n => n.DeliveredAt == null &&
                           n.RetryCount < n.MaxRetries &&
                           (n.NextRetryAt == null || n.NextRetryAt <= DateTime.UtcNow))
                .Take(_batchProcessingSize)
                .ToListAsync();

            foreach (var dbNotification in failedNotifications)
            {
                var notification = ConvertFromDbModel(dbNotification);

                // Check if user is online
                var isOnline = await IsUserOnlineAsync(notification.UserId);

                if (isOnline)
                {
                    var delivered = await TryDeliverNotificationAsync(notification);
                    if (delivered)
                    {
                        await MarkAsDeliveredAsync(notification.Id);
                        retriedCount++;
                    }
                    else
                    {
                        await HandleFailedDeliveryAsync(notification);
                    }
                }
            }

            if (retriedCount > 0)
            {
                _logger.LogInformation("Retried {RetriedCount} failed notifications", retriedCount);
            }

            return retriedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed notifications");
            return 0;
        }
    }

    #endregion

    #region Statistics and Monitoring

    public async Task<QueueStats> GetQueueStatsAsync()
    {
        try
        {
            // Get database statistics
            var dbStats = await _context.QueuedNotifications
                .GroupBy(n => 1)
                .Select(g => new
                {
                    TotalInDb = g.Count(),
                    DeliveredInDb = g.Count(n => n.DeliveredAt != null),
                    PendingInDb = g.Count(n => n.DeliveredAt == null)
                })
                .FirstOrDefaultAsync();

            var memoryQueueSize = _memoryQueue.Count;
            var pendingInMemory = _pendingNotifications.Count;

            // Calculate queue time statistics
            var averageQueueTime = 0.0;
            if (_totalDelivered > 0)
            {
                var deliveredNotifications = _pendingNotifications.Values
                    .Where(n => n.DeliveredAt.HasValue)
                    .Take(100) // Sample for performance
                    .ToList();

                if (deliveredNotifications.Any())
                {
                    averageQueueTime = deliveredNotifications
                        .Average(n => (n.DeliveredAt!.Value - n.CreatedAt).TotalMinutes);
                }
            }

            // Calculate success rate
            var totalProcessed = _totalDelivered + _totalFailed;
            var successRate = totalProcessed > 0 ? (double)_totalDelivered / totalProcessed * 100 : 0;

            // Get priority and type breakdowns
            var queuedByPriority = _pendingNotifications.Values
                .GroupBy(n => n.Priority.ToString())
                .ToDictionary(g => g.Key, g => (long)g.Count());

            var queuedByType = _pendingNotifications.Values
                .GroupBy(n => n.NotificationType)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            return new QueueStats
            {
                TotalQueued = _totalQueued,
                TotalDelivered = _totalDelivered,
                TotalFailed = _totalFailed,
                TotalExpired = _totalExpired,
                CurrentlyQueued = pendingInMemory + (dbStats?.PendingInDb ?? 0),
                CurrentlyProcessing = 0, // Would need additional tracking for this
                AverageQueueTime = averageQueueTime,
                DeliverySuccessRate = successRate,
                QueuedByPriority = queuedByPriority,
                QueuedByType = queuedByType,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue statistics");
            return new QueueStats { LastUpdated = DateTime.UtcNow };
        }
    }

    #endregion

    #region Missing Interface Methods

    public async Task<List<UserConnectivityStatus>> GetAllUserConnectivityAsync()
    {
        await Task.CompletedTask;
        return _userConnectivity.Values.ToList();
    }

    public async Task CancelNotificationAsync(string notificationId)
    {
        try
        {
            // Remove from memory collections
            if (_pendingNotifications.TryRemove(notificationId, out var notification))
            {
                notification.Status = QueuedNotificationStatus.Cancelled;
                _logger.LogDebug("Cancelled notification {NotificationId} from memory queue", notificationId);
            }

            // Update database record
            var dbNotification = await _context.QueuedNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (dbNotification != null)
            {
                _context.QueuedNotifications.Remove(dbNotification);
                await _context.SaveChangesAsync();
                _logger.LogDebug("Cancelled notification {NotificationId} from database", notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel notification {NotificationId}", notificationId);
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();

            // Check provider manager health
            var hasAvailableProviders = await _providerManager.HasAvailableProvidersAsync();

            // Check memory queue size
            var memoryQueueSize = _memoryQueue.Count;
            var isMemoryHealthy = memoryQueueSize < _maxMemoryQueueSize * 0.9; // 90% threshold

            // Check for stuck notifications
            var oldestPending = _pendingNotifications.Values
                .Where(n => n.Status == QueuedNotificationStatus.Pending)
                .OrderBy(n => n.CreatedAt)
                .FirstOrDefault();

            var hasStuckNotifications = oldestPending != null &&
                (DateTime.UtcNow - oldestPending.CreatedAt) > TimeSpan.FromHours(24);

            return hasAvailableProviders && isMemoryHealthy && !hasStuckNotifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking queue health");
            return false;
        }
    }

    public async Task<QueueHealthReport> GetHealthReportAsync()
    {
        var report = new QueueHealthReport
        {
            LastChecked = DateTime.UtcNow
        };

        try
        {
            var issues = new List<string>();
            var metrics = new Dictionary<string, object>();

            // Check database connectivity
            var canConnectToDb = await _context.Database.CanConnectAsync();
            metrics["DatabaseConnectivity"] = canConnectToDb;
            if (!canConnectToDb)
            {
                issues.Add("Cannot connect to database");
            }

            // Check provider health
            var hasAvailableProviders = await _providerManager.HasAvailableProvidersAsync();
            var providerHealthStatuses = await _providerManager.GetProviderHealthAsync();
            metrics["HasAvailableProviders"] = hasAvailableProviders;
            metrics["ProviderCount"] = providerHealthStatuses.Count;
            metrics["HealthyProviderCount"] = providerHealthStatuses.Values.Count(p => p.IsHealthy);

            if (!hasAvailableProviders)
            {
                issues.Add("No healthy providers available");
            }

            var unhealthyProviders = providerHealthStatuses.Values.Where(p => !p.IsHealthy).ToList();
            if (unhealthyProviders.Any())
            {
                issues.Add($"Unhealthy providers: {string.Join(", ", unhealthyProviders.Select(p => p.ProviderName))}");
            }

            // Check memory queue
            var memoryQueueSize = _memoryQueue.Count;
            metrics["MemoryQueueSize"] = memoryQueueSize;
            metrics["MemoryQueueCapacity"] = _maxMemoryQueueSize;
            if (memoryQueueSize > _maxMemoryQueueSize * 0.9)
            {
                issues.Add($"Memory queue near capacity: {memoryQueueSize}/{_maxMemoryQueueSize}");
            }

            // Check for stuck notifications
            var oldestPending = _pendingNotifications.Values
                .Where(n => n.Status == QueuedNotificationStatus.Pending)
                .OrderBy(n => n.CreatedAt)
                .FirstOrDefault();

            if (oldestPending != null)
            {
                var age = DateTime.UtcNow - oldestPending.CreatedAt;
                metrics["OldestPendingNotificationAge"] = age.TotalHours;
                if (age > TimeSpan.FromHours(24))
                {
                    issues.Add($"Stuck notification detected: {age.TotalHours:F1} hours old");
                }
            }

            // Add statistics
            metrics["TotalQueued"] = _totalQueued;
            metrics["TotalDelivered"] = _totalDelivered;
            metrics["TotalFailed"] = _totalFailed;
            metrics["TotalExpired"] = _totalExpired;
            metrics["PendingInMemory"] = _pendingNotifications.Count;

            report.IsHealthy = issues.Count == 0;
            report.Status = report.IsHealthy ? "Healthy" : "Unhealthy";
            report.Issues = issues;
            report.Metrics = metrics;
        }
        catch (Exception ex)
        {
            report.IsHealthy = false;
            report.Status = "Error";
            report.Issues.Add($"Health check failed: {ex.Message}");
            _logger.LogError(ex, "Error generating health report");
        }

        return report;
    }

    public async Task RefreshHealthAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing notification queue health");

            // Refresh provider health
            await _providerManager.RefreshProviderHealthAsync();

            // Clean up expired notifications
            await CleanupExpiredNotificationsAsync();

            // Process any stuck notifications
            await ProcessPendingNotificationsAsync();

            _logger.LogInformation("Queue health refresh completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing queue health");
        }
    }

    #endregion

    #region Helper Properties

    /// <summary>
    /// Extension property to check if a notification has expired
    /// </summary>
    private static bool IsExpired(QueuedNotification notification)
    {
        return notification.ExpiresAt.HasValue && DateTime.UtcNow > notification.ExpiresAt.Value;
    }

    #endregion
}