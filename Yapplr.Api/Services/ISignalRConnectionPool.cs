namespace Yapplr.Api.Services;

/// <summary>
/// Manages SignalR connection pooling and user group management for efficient message delivery
/// </summary>
public interface ISignalRConnectionPool
{
    Task AddUserConnectionAsync(int userId, string connectionId);
    Task RemoveUserConnectionAsync(int userId, string connectionId);
    Task RemoveAllUserConnectionsAsync(int userId);
    Task<List<string>> GetUserConnectionsAsync(int userId);
    Task<int> GetActiveConnectionCountAsync();
    Task<Dictionary<int, int>> GetUserConnectionStatsAsync();
    Task SendToUserAsync(int userId, string method, object data);
    Task SendToUsersAsync(List<int> userIds, string method, object data);
    Task<bool> IsUserOnlineAsync(int userId);
    Task<ConnectionPoolStats> GetStatsAsync();
    Task CleanupInactiveConnectionsAsync(TimeSpan inactivityThreshold);
}