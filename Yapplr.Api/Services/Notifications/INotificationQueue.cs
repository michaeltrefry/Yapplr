namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Unified notification queue that handles offline users, retry logic, and user connectivity.
/// Replaces NotificationQueueService, OfflineNotificationService, and SmartRetryService.
/// </summary>
public interface INotificationQueue
{
    #region Queuing Operations

    /// <summary>
    /// Queues a notification for delivery when the user comes online
    /// </summary>
    /// <param name="notification">The notification to queue</param>
    Task QueueNotificationAsync(QueuedNotification notification);
    
    /// <summary>
    /// Gets all pending notifications for a specific user
    /// </summary>
    /// <param name="userId">The user ID to get notifications for</param>
    /// <returns>List of pending notifications ordered by priority and creation time</returns>
    Task<List<QueuedNotification>> GetPendingNotificationsAsync(int userId);
    
    /// <summary>
    /// Gets all pending notifications across all users (for admin/monitoring)
    /// </summary>
    /// <param name="limit">Maximum number of notifications to return</param>
    /// <returns>List of pending notifications</returns>
    Task<List<QueuedNotification>> GetAllPendingNotificationsAsync(int limit = 1000);
    
    /// <summary>
    /// Processes all pending notifications for users who are currently online
    /// </summary>
    /// <returns>Number of notifications processed</returns>
    Task<int> ProcessPendingNotificationsAsync();
    
    /// <summary>
    /// Processes pending notifications for a specific user
    /// </summary>
    /// <param name="userId">The user ID to process notifications for</param>
    /// <returns>Number of notifications processed</returns>
    Task<int> ProcessUserNotificationsAsync(int userId);
    
    #endregion
    
    #region User Connectivity Management
    
    /// <summary>
    /// Marks a user as online and triggers processing of their queued notifications
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="connectionType">The type of connection (SignalR, Mobile, etc.)</param>
    Task MarkUserOnlineAsync(int userId, string connectionType);
    
    /// <summary>
    /// Marks a user as offline
    /// </summary>
    /// <param name="userId">The user ID</param>
    Task MarkUserOfflineAsync(int userId);
    
    /// <summary>
    /// Checks if a user is currently online
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <returns>True if the user is online</returns>
    Task<bool> IsUserOnlineAsync(int userId);
    
    /// <summary>
    /// Gets detailed connectivity information for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Connectivity status information</returns>
    Task<UserConnectivityStatus> GetUserConnectivityAsync(int userId);
    
    /// <summary>
    /// Gets connectivity information for all users (for monitoring)
    /// </summary>
    /// <returns>List of user connectivity statuses</returns>
    Task<List<UserConnectivityStatus>> GetAllUserConnectivityAsync();
    
    #endregion
    
    #region Queue Management
    
    /// <summary>
    /// Gets comprehensive statistics about the notification queue
    /// </summary>
    /// <returns>Queue statistics</returns>
    Task<QueueStats> GetQueueStatsAsync();
    
    /// <summary>
    /// Removes expired notifications from the queue
    /// </summary>
    /// <param name="maxAge">Maximum age of notifications to keep</param>
    /// <returns>Number of notifications cleaned up</returns>
    Task<int> CleanupExpiredNotificationsAsync(TimeSpan? maxAge = null);

    /// <summary>
    /// Removes old notifications from the queue (for background service cleanup)
    /// </summary>
    /// <param name="maxAge">Maximum age of notifications to keep</param>
    /// <returns>Number of notifications cleaned up</returns>
    Task<int> CleanupOldNotificationsAsync(TimeSpan maxAge);
    
    /// <summary>
    /// Retries failed notifications that are eligible for retry
    /// </summary>
    /// <returns>Number of notifications retried</returns>
    Task<int> RetryFailedNotificationsAsync();
    
    /// <summary>
    /// Marks a notification as successfully delivered
    /// </summary>
    /// <param name="notificationId">The notification ID</param>
    Task MarkAsDeliveredAsync(string notificationId);
    
    /// <summary>
    /// Marks a notification as failed with error information
    /// </summary>
    /// <param name="notificationId">The notification ID</param>
    /// <param name="error">The error message</param>
    Task MarkAsFailedAsync(string notificationId, string error);
    
    /// <summary>
    /// Cancels a queued notification
    /// </summary>
    /// <param name="notificationId">The notification ID to cancel</param>
    Task CancelNotificationAsync(string notificationId);
    
    #endregion
    
    #region Health and Monitoring
    
    /// <summary>
    /// Checks if the queue system is healthy and operational
    /// </summary>
    /// <returns>True if the queue is healthy</returns>
    Task<bool> IsHealthyAsync();
    
    /// <summary>
    /// Gets detailed health information about the queue system
    /// </summary>
    /// <returns>Health report</returns>
    Task<QueueHealthReport> GetHealthReportAsync();
    
    /// <summary>
    /// Forces a refresh of queue health status
    /// </summary>
    Task RefreshHealthAsync();
    
    #endregion
}