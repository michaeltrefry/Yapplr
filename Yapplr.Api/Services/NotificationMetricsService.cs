using System.Collections.Concurrent;
using System.Diagnostics;

namespace Yapplr.Api.Services;

/// <summary>
/// Metrics for notification delivery performance
/// </summary>
public class NotificationMetrics
{
    public long TotalNotificationsSent { get; set; }
    public long TotalNotificationsDelivered { get; set; }
    public long TotalNotificationsFailed { get; set; }
    public double AverageDeliveryTimeMs { get; set; }
    public double DeliverySuccessRate { get; set; }
    public Dictionary<string, long> NotificationTypeBreakdown { get; set; } = new();
    public Dictionary<string, long> ProviderBreakdown { get; set; } = new();
    public Dictionary<string, double> ProviderAverageLatency { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Real-time notification delivery metrics
/// </summary>
public class DeliveryMetric
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public double LatencyMs => EndTime.HasValue ? (EndTime.Value - StartTime).TotalMilliseconds : 0;
}

/// <summary>
/// Service for tracking and monitoring notification delivery metrics
/// </summary>
public interface INotificationMetricsService
{
    string StartDeliveryTracking(int userId, string notificationType, string provider);
    Task CompleteDeliveryTrackingAsync(string trackingId, bool success, string? error = null);
    Task<NotificationMetrics> GetMetricsAsync(TimeSpan? timeWindow = null);
    Task<List<DeliveryMetric>> GetRecentDeliveriesAsync(int count = 100);
    Task<Dictionary<string, object>> GetHealthCheckDataAsync();
    Task<Dictionary<string, object>> GetPerformanceInsightsAsync();
    Task ResetMetricsAsync();
}

public class NotificationMetricsService : INotificationMetricsService
{
    private readonly ILogger<NotificationMetricsService> _logger;
    
    // Thread-safe collections for metrics tracking
    private readonly ConcurrentDictionary<string, DeliveryMetric> _activeDeliveries = new();
    private readonly ConcurrentQueue<DeliveryMetric> _completedDeliveries = new();
    private readonly ConcurrentDictionary<string, long> _notificationTypeCounts = new();
    private readonly ConcurrentDictionary<string, long> _providerCounts = new();
    private readonly ConcurrentDictionary<string, List<double>> _providerLatencies = new();
    
    // Aggregate metrics
    private long _totalSent = 0;
    private long _totalDelivered = 0;
    private long _totalFailed = 0;
    private readonly object _metricsLock = new object();
    
    // Configuration
    private const int MaxCompletedDeliveries = 10000; // Keep last 10k deliveries in memory
    private const int MaxLatencyHistory = 1000; // Keep last 1k latencies per provider

    public NotificationMetricsService(ILogger<NotificationMetricsService> logger)
    {
        _logger = logger;
    }

    public string StartDeliveryTracking(int userId, string notificationType, string provider)
    {
        var metric = new DeliveryMetric
        {
            UserId = userId,
            NotificationType = notificationType,
            Provider = provider,
            StartTime = DateTime.UtcNow
        };

        _activeDeliveries[metric.Id] = metric;
        
        // Increment counters
        Interlocked.Increment(ref _totalSent);
        _notificationTypeCounts.AddOrUpdate(notificationType, 1, (key, value) => value + 1);
        _providerCounts.AddOrUpdate(provider, 1, (key, value) => value + 1);

        _logger.LogDebug("Started tracking delivery {TrackingId} for user {UserId} (type: {Type}, provider: {Provider})",
            metric.Id, userId, notificationType, provider);

        return metric.Id;
    }

    public async Task CompleteDeliveryTrackingAsync(string trackingId, bool success, string? error = null)
    {
        if (!_activeDeliveries.TryRemove(trackingId, out var metric))
        {
            _logger.LogWarning("Attempted to complete tracking for unknown delivery {TrackingId}", trackingId);
            return;
        }

        metric.EndTime = DateTime.UtcNow;
        metric.Success = success;
        metric.Error = error;

        // Update aggregate metrics
        if (success)
        {
            Interlocked.Increment(ref _totalDelivered);
        }
        else
        {
            Interlocked.Increment(ref _totalFailed);
        }

        // Track latency for provider
        _providerLatencies.AddOrUpdate(
            metric.Provider,
            new List<double> { metric.LatencyMs },
            (key, latencies) =>
            {
                lock (latencies)
                {
                    latencies.Add(metric.LatencyMs);
                    if (latencies.Count > MaxLatencyHistory)
                    {
                        latencies.RemoveAt(0); // Remove oldest
                    }
                }
                return latencies;
            });

        // Add to completed deliveries queue
        _completedDeliveries.Enqueue(metric);
        
        // Maintain queue size
        while (_completedDeliveries.Count > MaxCompletedDeliveries)
        {
            _completedDeliveries.TryDequeue(out _);
        }

        _logger.LogDebug("Completed tracking delivery {TrackingId}: {Success} in {LatencyMs}ms",
            trackingId, success ? "SUCCESS" : "FAILED", metric.LatencyMs);

        await Task.CompletedTask;
    }

    public Task<NotificationMetrics> GetMetricsAsync(TimeSpan? timeWindow = null)
    {
        var cutoffTime = timeWindow.HasValue ? DateTime.UtcNow - timeWindow.Value : DateTime.MinValue;
        
        var recentDeliveries = _completedDeliveries
            .Where(d => d.StartTime >= cutoffTime)
            .ToList();

        var totalSent = timeWindow.HasValue ? recentDeliveries.Count : _totalSent;
        var totalDelivered = timeWindow.HasValue 
            ? recentDeliveries.Count(d => d.Success) 
            : _totalDelivered;
        var totalFailed = timeWindow.HasValue 
            ? recentDeliveries.Count(d => !d.Success) 
            : _totalFailed;

        var averageLatency = recentDeliveries.Any() 
            ? recentDeliveries.Average(d => d.LatencyMs) 
            : 0;

        var successRate = totalSent > 0 ? (double)totalDelivered / totalSent * 100 : 0;

        var typeBreakdown = timeWindow.HasValue
            ? recentDeliveries.GroupBy(d => d.NotificationType)
                .ToDictionary(g => g.Key, g => (long)g.Count())
            : _notificationTypeCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var providerBreakdown = timeWindow.HasValue
            ? recentDeliveries.GroupBy(d => d.Provider)
                .ToDictionary(g => g.Key, g => (long)g.Count())
            : _providerCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var providerLatencies = new Dictionary<string, double>();
        foreach (var kvp in _providerLatencies)
        {
            lock (kvp.Value)
            {
                if (kvp.Value.Any())
                {
                    providerLatencies[kvp.Key] = kvp.Value.Average();
                }
            }
        }

        var metrics = new NotificationMetrics
        {
            TotalNotificationsSent = totalSent,
            TotalNotificationsDelivered = totalDelivered,
            TotalNotificationsFailed = totalFailed,
            AverageDeliveryTimeMs = averageLatency,
            DeliverySuccessRate = successRate,
            NotificationTypeBreakdown = typeBreakdown,
            ProviderBreakdown = providerBreakdown,
            ProviderAverageLatency = providerLatencies,
            LastUpdated = DateTime.UtcNow
        };

        return Task.FromResult(metrics);
    }

    public Task<List<DeliveryMetric>> GetRecentDeliveriesAsync(int count = 100)
    {
        var recentDeliveries = _completedDeliveries
            .TakeLast(count)
            .OrderByDescending(d => d.StartTime)
            .ToList();

        return Task.FromResult(recentDeliveries);
    }

    public async Task<Dictionary<string, object>> GetHealthCheckDataAsync()
    {
        var metrics = await GetMetricsAsync(TimeSpan.FromMinutes(5)); // Last 5 minutes
        
        var healthData = new Dictionary<string, object>
        {
            ["status"] = metrics.DeliverySuccessRate >= 95 ? "healthy" : 
                        metrics.DeliverySuccessRate >= 80 ? "degraded" : "unhealthy",
            ["success_rate"] = metrics.DeliverySuccessRate,
            ["average_latency_ms"] = metrics.AverageDeliveryTimeMs,
            ["total_sent_last_5min"] = metrics.TotalNotificationsSent,
            ["total_failed_last_5min"] = metrics.TotalNotificationsFailed,
            ["active_deliveries"] = _activeDeliveries.Count,
            ["provider_status"] = metrics.ProviderBreakdown,
            ["provider_latencies"] = metrics.ProviderAverageLatency,
            ["last_updated"] = DateTime.UtcNow
        };

        return healthData;
    }

    public Task ResetMetricsAsync()
    {
        lock (_metricsLock)
        {
            _activeDeliveries.Clear();
            
            // Clear completed deliveries queue
            while (_completedDeliveries.TryDequeue(out _)) { }
            
            _notificationTypeCounts.Clear();
            _providerCounts.Clear();
            _providerLatencies.Clear();
            
            _totalSent = 0;
            _totalDelivered = 0;
            _totalFailed = 0;
        }

        _logger.LogInformation("Notification metrics have been reset");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets performance insights and recommendations
    /// </summary>
    public async Task<Dictionary<string, object>> GetPerformanceInsightsAsync()
    {
        var metrics = await GetMetricsAsync();
        var insights = new Dictionary<string, object>();

        // Provider performance comparison
        var bestProvider = metrics.ProviderAverageLatency
            .OrderBy(kvp => kvp.Value)
            .FirstOrDefault();

        var worstProvider = metrics.ProviderAverageLatency
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();

        insights["best_performing_provider"] = bestProvider.Key;
        insights["worst_performing_provider"] = worstProvider.Key;
        insights["latency_difference_ms"] = worstProvider.Value - bestProvider.Value;

        // Success rate analysis
        if (metrics.DeliverySuccessRate < 95)
        {
            insights["recommendations"] = new[]
            {
                "Consider implementing retry logic for failed deliveries",
                "Monitor provider health and implement circuit breaker pattern",
                "Review notification payload sizes to reduce latency"
            };
        }

        // Load distribution
        var totalNotifications = metrics.NotificationTypeBreakdown.Values.Sum();
        var topNotificationType = metrics.NotificationTypeBreakdown
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();

        insights["most_common_notification_type"] = topNotificationType.Key;
        insights["notification_type_percentage"] = totalNotifications > 0 
            ? (double)topNotificationType.Value / totalNotifications * 100 
            : 0;

        return insights;
    }
}
