namespace Yapplr.Api.Common;

/// <summary>
/// Extension methods for easy performance monitoring
/// </summary>
public static class PerformanceMonitoringExtensions
{
    /// <summary>
    /// Monitor a database query operation
    /// </summary>
    public static async Task<T> MonitorQueryAsync<T>(
        this IQueryPerformanceMonitor monitor,
        string queryName,
        Func<Task<T>> query,
        object? parameters = null)
    {
        return await monitor.MonitorAsync($"Query:{queryName}", query, parameters);
    }

    /// <summary>
    /// Monitor a service operation
    /// </summary>
    public static async Task<T> MonitorServiceAsync<T>(
        this IQueryPerformanceMonitor monitor,
        string serviceName,
        string operationName,
        Func<Task<T>> operation,
        object? parameters = null)
    {
        return await monitor.MonitorAsync($"Service:{serviceName}:{operationName}", operation, parameters);
    }
}