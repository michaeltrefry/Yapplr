namespace Yapplr.Api.Services.Notifications.Providers;

/// <summary>
/// Manages notification providers with intelligent fallback and health monitoring.
/// Replaces the provider management logic from CompositeNotificationService.
/// </summary>
public interface INotificationProviderManager
{
    #region Provider Operations
    
    /// <summary>
    /// Sends a notification using the best available provider with automatic fallback
    /// </summary>
    /// <param name="request">The delivery request containing notification details</param>
    /// <returns>True if the notification was successfully delivered by any provider</returns>
    Task<bool> SendNotificationAsync(NotificationDeliveryRequest request);
    
    /// <summary>
    /// Sends a test notification to verify provider functionality
    /// </summary>
    /// <param name="userId">The user ID to send the test notification to</param>
    /// <param name="providerName">Optional specific provider to test, or null for best available</param>
    /// <returns>True if the test notification was successfully sent</returns>
    Task<bool> SendTestNotificationAsync(int userId, string? providerName = null);
    
    /// <summary>
    /// Sends the same notification to multiple users using the most efficient provider
    /// </summary>
    /// <param name="request">The multicast delivery request</param>
    /// <returns>True if all notifications were successfully delivered</returns>
    Task<bool> SendMulticastNotificationAsync(MulticastDeliveryRequest request);
    
    #endregion
    
    #region Provider Management
    
    /// <summary>
    /// Gets all currently available and healthy providers
    /// </summary>
    /// <returns>List of available providers ordered by priority</returns>
    Task<List<IRealtimeNotificationProvider>> GetAvailableProvidersAsync();
    
    /// <summary>
    /// Gets the best provider for a specific user and notification type
    /// </summary>
    /// <param name="userId">The target user ID</param>
    /// <param name="notificationType">The type of notification being sent</param>
    /// <returns>The best provider, or null if none are available</returns>
    Task<IRealtimeNotificationProvider?> GetBestProviderAsync(int userId, string notificationType);
    
    /// <summary>
    /// Gets all registered providers regardless of health status
    /// </summary>
    /// <returns>List of all registered providers</returns>
    Task<List<IRealtimeNotificationProvider>> GetAllProvidersAsync();
    
    #endregion
    
    #region Health Monitoring
    
    /// <summary>
    /// Forces a refresh of provider health status
    /// </summary>
    Task RefreshProviderHealthAsync();
    
    /// <summary>
    /// Gets the current health status of all providers
    /// </summary>
    /// <returns>Dictionary of provider names to their health status</returns>
    Task<Dictionary<string, ProviderHealth>> GetProviderHealthAsync();
    
    /// <summary>
    /// Gets detailed health information for a specific provider
    /// </summary>
    /// <param name="providerName">The name of the provider to check</param>
    /// <returns>Detailed health information, or null if provider not found</returns>
    Task<ProviderHealth?> GetProviderHealthAsync(string providerName);
    
    /// <summary>
    /// Checks if any providers are currently available
    /// </summary>
    /// <returns>True if at least one provider is healthy and available</returns>
    Task<bool> HasAvailableProvidersAsync();
    
    #endregion
    
    #region Configuration
    
    /// <summary>
    /// Updates the priority of a specific provider
    /// </summary>
    /// <param name="providerName">The name of the provider</param>
    /// <param name="priority">The new priority (lower number = higher priority)</param>
    Task UpdateProviderPriorityAsync(string providerName, int priority);
    
    /// <summary>
    /// Enables or disables a specific provider
    /// </summary>
    /// <param name="providerName">The name of the provider</param>
    /// <param name="enabled">True to enable, false to disable</param>
    Task EnableProviderAsync(string providerName, bool enabled);
    
    /// <summary>
    /// Gets the current configuration for all providers
    /// </summary>
    /// <returns>Dictionary of provider configurations</returns>
    Task<Dictionary<string, ProviderConfiguration>> GetProviderConfigurationsAsync();
    
    #endregion
}