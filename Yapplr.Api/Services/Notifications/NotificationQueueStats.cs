namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Statistics about the notification queue
/// </summary>
public class NotificationQueueStats
{
    public long TotalQueued { get; set; }
    public long TotalDelivered { get; set; }
    public long TotalFailed { get; set; }
    public int PendingInMemory { get; set; }
    public int QueueSize { get; set; }

    // Database statistics
    public int PendingInDatabase { get; set; }
    public int DeliveredInDatabase { get; set; }
    public int FailedInDatabase { get; set; }

    // Calculated properties
    public double DeliveryRate => TotalQueued > 0 ? (double)TotalDelivered / TotalQueued * 100 : 0;
    public double FailureRate => TotalQueued > 0 ? (double)TotalFailed / TotalQueued * 100 : 0;
    public int TotalPending => PendingInMemory + PendingInDatabase;
    public int TotalInDatabase => PendingInDatabase + DeliveredInDatabase + FailedInDatabase;
}