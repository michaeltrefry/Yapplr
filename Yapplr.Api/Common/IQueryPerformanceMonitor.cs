namespace Yapplr.Api.Common;

/// <summary>
/// Monitor and log database query performance
/// </summary>
public interface IQueryPerformanceMonitor
{
    Task<T> MonitorAsync<T>(string operationName, Func<Task<T>> operation, object? parameters = null);
    Task MonitorAsync(string operationName, Func<Task> operation, object? parameters = null);
    QueryPerformanceStats GetStats();
    void ResetStats();
}