namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Comprehensive statistics about the notification system
/// </summary>
public class NotificationStats
{
    public long TotalNotificationsSent { get; set; }
    public long TotalNotificationsDelivered { get; set; }
    public long TotalNotificationsFailed { get; set; }
    public long TotalNotificationsQueued { get; set; }
    public double DeliverySuccessRate { get; set; }
    public double AverageDeliveryTimeMs { get; set; }
    public Dictionary<string, long> NotificationTypeBreakdown { get; set; } = new();
    public Dictionary<string, long> ProviderBreakdown { get; set; } = new();
    public Dictionary<string, double> ProviderSuccessRates { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}