using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Yapplr.Api.Hubs;

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

public class SignalRConnectionPool : ISignalRConnectionPool
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRConnectionPool> _logger;
    
    // Thread-safe collections for managing user connections
    private readonly ConcurrentDictionary<int, ConcurrentBag<string>> _userConnections = new();
    private readonly ConcurrentDictionary<string, int> _connectionUsers = new();
    
    // Connection statistics
    private readonly ConcurrentDictionary<int, DateTime> _userLastActivity = new();
    private long _totalConnectionsCreated = 0;
    private long _totalConnectionsRemoved = 0;

    public SignalRConnectionPool(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRConnectionPool> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task AddUserConnectionAsync(int userId, string connectionId)
    {
        try
        {
            // Add connection to user's connection bag
            _userConnections.AddOrUpdate(
                userId,
                new ConcurrentBag<string> { connectionId },
                (key, existingBag) =>
                {
                    existingBag.Add(connectionId);
                    return existingBag;
                });

            // Map connection back to user
            _connectionUsers[connectionId] = userId;

            // Update last activity
            _userLastActivity[userId] = DateTime.UtcNow;

            // Add to SignalR group for targeted messaging
            await _hubContext.Groups.AddToGroupAsync(connectionId, $"user_{userId}");

            Interlocked.Increment(ref _totalConnectionsCreated);

            _logger.LogInformation("Added connection {ConnectionId} for user {UserId}. Total connections: {TotalConnections}",
                connectionId, userId, await GetActiveConnectionCountAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
    }

    public async Task RemoveUserConnectionAsync(int userId, string connectionId)
    {
        try
        {
            // Remove from connection-to-user mapping
            _connectionUsers.TryRemove(connectionId, out _);

            // Remove from user's connections
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                // Create new bag without the removed connection
                var newConnections = new ConcurrentBag<string>(
                    connections.Where(c => c != connectionId));
                
                if (newConnections.IsEmpty)
                {
                    // Remove user entirely if no connections left
                    _userConnections.TryRemove(userId, out _);
                    _userLastActivity.TryRemove(userId, out _);
                }
                else
                {
                    _userConnections[userId] = newConnections;
                }
            }

            // Remove from SignalR group
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"user_{userId}");

            Interlocked.Increment(ref _totalConnectionsRemoved);

            _logger.LogInformation("Removed connection {ConnectionId} for user {UserId}. Total connections: {TotalConnections}",
                connectionId, userId, await GetActiveConnectionCountAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
    }

    public async Task RemoveAllUserConnectionsAsync(int userId)
    {
        try
        {
            if (_userConnections.TryRemove(userId, out var connections))
            {
                foreach (var connectionId in connections)
                {
                    _connectionUsers.TryRemove(connectionId, out _);
                    await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"user_{userId}");
                    Interlocked.Increment(ref _totalConnectionsRemoved);
                }
            }

            _userLastActivity.TryRemove(userId, out _);

            _logger.LogInformation("Removed all connections for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove all connections for user {UserId}", userId);
        }
    }

    public Task<List<string>> GetUserConnectionsAsync(int userId)
    {
        if (_userConnections.TryGetValue(userId, out var connections))
        {
            return Task.FromResult(connections.ToList());
        }
        return Task.FromResult(new List<string>());
    }

    public Task<int> GetActiveConnectionCountAsync()
    {
        var totalConnections = _userConnections.Values.Sum(bag => bag.Count);
        return Task.FromResult(totalConnections);
    }

    public Task<Dictionary<int, int>> GetUserConnectionStatsAsync()
    {
        var stats = _userConnections.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Count);
        return Task.FromResult(stats);
    }

    public async Task SendToUserAsync(int userId, string method, object data)
    {
        try
        {
            var userGroup = $"user_{userId}";
            await _hubContext.Clients.Group(userGroup).SendAsync(method, data);
            
            _logger.LogDebug("Sent {Method} to user {UserId}", method, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {Method} to user {UserId}", method, userId);
        }
    }

    public async Task SendToUsersAsync(List<int> userIds, string method, object data)
    {
        try
        {
            var userGroups = userIds.Select(id => $"user_{id}").ToList();
            await _hubContext.Clients.Groups(userGroups).SendAsync(method, data);
            
            _logger.LogDebug("Sent {Method} to {UserCount} users", method, userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {Method} to {UserCount} users", method, userIds.Count);
        }
    }

    public Task<bool> IsUserOnlineAsync(int userId)
    {
        var isOnline = _userConnections.ContainsKey(userId);
        return Task.FromResult(isOnline);
    }

    /// <summary>
    /// Gets comprehensive connection pool statistics
    /// </summary>
    public Task<ConnectionPoolStats> GetStatsAsync()
    {
        var stats = new ConnectionPoolStats
        {
            ActiveUsers = _userConnections.Count,
            TotalConnections = _userConnections.Values.Sum(bag => bag.Count),
            TotalConnectionsCreated = _totalConnectionsCreated,
            TotalConnectionsRemoved = _totalConnectionsRemoved,
            UserConnectionCounts = _userConnections.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count),
            UserLastActivity = _userLastActivity.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value)
        };

        return Task.FromResult(stats);
    }

    /// <summary>
    /// Cleanup inactive connections (should be called periodically)
    /// </summary>
    public async Task CleanupInactiveConnectionsAsync(TimeSpan inactivityThreshold)
    {
        var cutoffTime = DateTime.UtcNow - inactivityThreshold;
        var inactiveUsers = _userLastActivity
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var userId in inactiveUsers)
        {
            _logger.LogInformation("Cleaning up inactive connections for user {UserId}", userId);
            await RemoveAllUserConnectionsAsync(userId);
        }

        if (inactiveUsers.Any())
        {
            _logger.LogInformation("Cleaned up {Count} inactive users", inactiveUsers.Count);
        }
    }
}

/// <summary>
/// Statistics about the SignalR connection pool
/// </summary>
public class ConnectionPoolStats
{
    public int ActiveUsers { get; set; }
    public int TotalConnections { get; set; }
    public long TotalConnectionsCreated { get; set; }
    public long TotalConnectionsRemoved { get; set; }
    public Dictionary<int, int> UserConnectionCounts { get; set; } = new();
    public Dictionary<int, DateTime> UserLastActivity { get; set; } = new();
}
