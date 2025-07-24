using Yapplr.Api.Configuration;

namespace Yapplr.Api.Services.Payment;

/// <summary>
/// Service for managing dynamic payment provider configurations stored in the database
/// </summary>
public interface IDynamicPaymentConfigurationService
{
    /// <summary>
    /// Gets the current payment providers configuration
    /// </summary>
    Task<PaymentProvidersConfiguration> GetPaymentProvidersConfigurationAsync();
    
    /// <summary>
    /// Gets configuration for a specific payment provider
    /// </summary>
    Task<PayPalConfiguration?> GetPayPalConfigurationAsync();
    
    /// <summary>
    /// Gets configuration for Stripe payment provider
    /// </summary>
    Task<StripeConfiguration?> GetStripeConfigurationAsync();
    
    /// <summary>
    /// Gets global payment settings
    /// </summary>
    Task<PaymentGlobalSettings> GetGlobalSettingsAsync();
    
    /// <summary>
    /// Updates configuration for a specific payment provider
    /// </summary>
    Task<bool> UpdateProviderConfigurationAsync(string providerName, object configuration);
    
    /// <summary>
    /// Updates global payment settings
    /// </summary>
    Task<bool> UpdateGlobalSettingsAsync(PaymentGlobalSettings settings);
    
    /// <summary>
    /// Gets the default payment provider name
    /// </summary>
    Task<string> GetDefaultProviderAsync();
    
    /// <summary>
    /// Gets the provider priority list
    /// </summary>
    Task<List<string>> GetProviderPriorityAsync();
    
    /// <summary>
    /// Refreshes the configuration cache
    /// </summary>
    Task RefreshCacheAsync();
    
    /// <summary>
    /// Event fired when configuration changes
    /// </summary>
    event EventHandler<PaymentConfigurationChangedEventArgs>? ConfigurationChanged;
}

/// <summary>
/// Event arguments for payment configuration changes
/// </summary>
public class PaymentConfigurationChangedEventArgs : EventArgs
{
    public string ProviderName { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty; // "Updated", "Enabled", "Disabled"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
