namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Status of a queued notification
/// </summary>
public enum QueuedNotificationStatus
{
    Pending = 0,
    Processing = 1,
    Delivered = 2,
    Failed = 3,
    Expired = 4,
    Cancelled = 5
}