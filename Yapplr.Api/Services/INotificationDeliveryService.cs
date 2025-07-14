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