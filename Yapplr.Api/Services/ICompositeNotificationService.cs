namespace Yapplr.Api.Services;

/// <summary>
/// Composite notification service that manages multiple notification providers
/// with fallback logic (Firebase -> SignalR)
/// </summary>
public interface ICompositeNotificationService : IRealtimeNotificationProvider
{
    /// <summary>
    /// Gets the currently active notification provider
    /// </summary>
    IRealtimeNotificationProvider? ActiveProvider { get; }

    /// <summary>
    /// Gets all available notification providers
    /// </summary>
    IEnumerable<IRealtimeNotificationProvider> AvailableProviders { get; }

    /// <summary>
    /// Forces a refresh of provider availability status
    /// </summary>
    Task RefreshProviderStatusAsync();

    /// <summary>
    /// Gets the status of all notification providers
    /// </summary>
    Task<Dictionary<string, bool>> GetProviderStatusAsync();

    /// <summary>
    /// Send notification with user preferences consideration
    /// </summary>
    Task<bool> SendNotificationWithPreferencesAsync(int userId, string notificationType, string title, string body, Dictionary<string, string>? data = null);
}
