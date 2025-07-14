namespace Yapplr.Api.Models;

/// <summary>
/// Notification delivery methods
/// </summary>
public enum NotificationDeliveryMethod
{
    Auto = 0,           // Use the best available method (Firebase -> SignalR -> Polling)
    FirebaseOnly = 1,   // Only use Firebase push notifications
    SignalROnly = 2,    // Only use SignalR real-time notifications
    PollingOnly = 3,    // Only use polling (no real-time)
    Disabled = 4        // Disable this notification type
}