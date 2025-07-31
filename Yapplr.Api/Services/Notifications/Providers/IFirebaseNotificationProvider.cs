namespace Yapplr.Api.Services.Notifications.Providers;

/// <summary>
/// Firebase-specific interface that extends the generic provider interface
/// </summary>
public interface IFirebaseNotificationProvider : IRealtimeNotificationProvider
{
    /// <summary>
    /// Firebase-specific method that uses FCM tokens directly
    /// </summary>
    Task<bool> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);

    /// <summary>
    /// Firebase-specific test method that uses FCM tokens directly
    /// </summary>
    Task<bool> SendTestNotificationAsync(string fcmToken);

    /// <summary>
    /// Firebase-specific multicast method that uses FCM tokens directly
    /// </summary>
    Task<bool> SendMulticastNotificationAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null);
}
