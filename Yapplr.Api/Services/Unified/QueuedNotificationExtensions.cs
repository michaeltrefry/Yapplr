namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Extension methods for QueuedNotification
/// </summary>
public static class QueuedNotificationExtensions
{
    public static bool IsExpired(this QueuedNotification notification)
    {
        return notification.ExpiresAt.HasValue && DateTime.UtcNow > notification.ExpiresAt.Value;
    }
}