namespace Yapplr.Api.Services;

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