using System.Diagnostics;

namespace Yapplr.Api.Common;

public class QueryPerformanceMonitor : IQueryPerformanceMonitor
{
    private readonly ILogger<QueryPerformanceMonitor> _logger;
    private readonly QueryPerformanceStats _stats = new();
    private readonly object _lockObject = new();

    public QueryPerformanceMonitor(ILogger<QueryPerformanceMonitor> logger)
    {
        _logger = logger;
    }

    public async Task<T> MonitorAsync<T>(string operationName, Func<Task<T>> operation, object? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogDebug("Starting operation: {OperationName} with parameters: {@Parameters}", 
                operationName, parameters);

            var result = await operation();
            
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;

            RecordSuccess(operationName, duration);
            
            if (duration.TotalMilliseconds > 1000) // Log slow queries
            {
                _logger.LogWarning("Slow operation detected: {OperationName} took {Duration}ms with parameters: {@Parameters}",
                    operationName, duration.TotalMilliseconds, parameters);
            }
            else
            {
                _logger.LogDebug("Operation completed: {OperationName} took {Duration}ms",
                    operationName, duration.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            
            RecordFailure(operationName, duration, ex);
            
            _logger.LogError(ex, "Operation failed: {OperationName} took {Duration}ms with parameters: {@Parameters}",
                operationName, duration.TotalMilliseconds, parameters);
            
            throw;
        }
    }

    public async Task MonitorAsync(string operationName, Func<Task> operation, object? parameters = null)
    {
        await MonitorAsync(operationName, async () =>
        {
            await operation();
            return true; // Dummy return value
        }, parameters);
    }

    private void RecordSuccess(string operationName, TimeSpan duration)
    {
        lock (_lockObject)
        {
            _stats.TotalOperations++;
            _stats.SuccessfulOperations++;
            _stats.TotalDuration += duration;

            if (!_stats.OperationStats.ContainsKey(operationName))
            {
                _stats.OperationStats[operationName] = new OperationStats();
            }

            var opStats = _stats.OperationStats[operationName];
            opStats.TotalCalls++;
            opStats.SuccessfulCalls++;
            opStats.TotalDuration += duration;
            
            if (duration > opStats.MaxDuration)
                opStats.MaxDuration = duration;
            
            if (opStats.MinDuration == TimeSpan.Zero || duration < opStats.MinDuration)
                opStats.MinDuration = duration;

            opStats.LastExecuted = DateTime.UtcNow;
        }
    }

    private void RecordFailure(string operationName, TimeSpan duration, Exception exception)
    {
        lock (_lockObject)
        {
            _stats.TotalOperations++;
            _stats.FailedOperations++;
            _stats.TotalDuration += duration;

            if (!_stats.OperationStats.ContainsKey(operationName))
            {
                _stats.OperationStats[operationName] = new OperationStats();
            }

            var opStats = _stats.OperationStats[operationName];
            opStats.TotalCalls++;
            opStats.FailedCalls++;
            opStats.TotalDuration += duration;
            opStats.LastError = exception.Message;
            opStats.LastExecuted = DateTime.UtcNow;
        }
    }

    public QueryPerformanceStats GetStats()
    {
        lock (_lockObject)
        {
            return new QueryPerformanceStats
            {
                TotalOperations = _stats.TotalOperations,
                SuccessfulOperations = _stats.SuccessfulOperations,
                FailedOperations = _stats.FailedOperations,
                TotalDuration = _stats.TotalDuration,
                AverageDuration = _stats.TotalOperations > 0 
                    ? TimeSpan.FromTicks(_stats.TotalDuration.Ticks / _stats.TotalOperations) 
                    : TimeSpan.Zero,
                OperationStats = _stats.OperationStats.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new OperationStats
                    {
                        TotalCalls = kvp.Value.TotalCalls,
                        SuccessfulCalls = kvp.Value.SuccessfulCalls,
                        FailedCalls = kvp.Value.FailedCalls,
                        TotalDuration = kvp.Value.TotalDuration,
                        AverageDuration = kvp.Value.TotalCalls > 0 
                            ? TimeSpan.FromTicks(kvp.Value.TotalDuration.Ticks / kvp.Value.TotalCalls)
                            : TimeSpan.Zero,
                        MinDuration = kvp.Value.MinDuration,
                        MaxDuration = kvp.Value.MaxDuration,
                        LastExecuted = kvp.Value.LastExecuted,
                        LastError = kvp.Value.LastError
                    })
            };
        }
    }

    public void ResetStats()
    {
        lock (_lockObject)
        {
            _stats.TotalOperations = 0;
            _stats.SuccessfulOperations = 0;
            _stats.FailedOperations = 0;
            _stats.TotalDuration = TimeSpan.Zero;
            _stats.OperationStats.Clear();
        }
    }
}