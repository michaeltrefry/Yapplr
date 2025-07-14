using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for queuing notifications when users are offline
/// </summary>
public interface INotificationQueueService
{
    Task QueueNotificationAsync(QueuedNotificationDto notification);
    Task<List<QueuedNotificationDto>> GetPendingNotificationsAsync(int userId);
    Task<List<QueuedNotificationDto>> GetAllPendingNotificationsAsync();
    Task MarkAsDeliveredAsync(string notificationId);
    Task MarkAsFailedAsync(string notificationId, string error);
    Task ProcessPendingNotificationsAsync();
    Task CleanupOldNotificationsAsync(TimeSpan maxAge);
    Task<NotificationQueueStats> GetStatsAsync();
}