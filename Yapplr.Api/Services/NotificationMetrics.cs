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