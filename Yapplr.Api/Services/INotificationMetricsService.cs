namespace Yapplr.Api.Services;

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