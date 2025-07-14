namespace Yapplr.Api.Services;

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
