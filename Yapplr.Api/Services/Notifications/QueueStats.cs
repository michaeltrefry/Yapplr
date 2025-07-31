namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Statistics about the notification queue
/// </summary>
public class QueueStats
{
    public long TotalQueued { get; set; }
    public long TotalDelivered { get; set; }
    public long TotalFailed { get; set; }
    public long TotalExpired { get; set; }
    public long CurrentlyQueued { get; set; }
    public long CurrentlyProcessing { get; set; }
    public double AverageQueueTime { get; set; }
    public double DeliverySuccessRate { get; set; }
    public Dictionary<string, long> QueuedByPriority { get; set; } = new();
    public Dictionary<string, long> QueuedByType { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}