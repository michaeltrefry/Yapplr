using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

/// <summary>
/// Database model for storing payment provider configurations
/// </summary>
public class PaymentProviderConfiguration
{
    public int Id { get; set; }
    
    /// <summary>
    /// Payment provider name (e.g., "PayPal", "Stripe")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ProviderName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this provider is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = false;
    
    /// <summary>
    /// Environment (e.g., "sandbox", "live", "test")
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Environment { get; set; } = "sandbox";
    
    /// <summary>
    /// Priority order for provider selection (lower number = higher priority)
    /// </summary>
    public int Priority { get; set; } = 100;
    
    /// <summary>
    /// Timeout in seconds for API calls
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Supported currencies as JSON array
    /// </summary>
    [StringLength(500)]
    public string SupportedCurrencies { get; set; } = "[]";
    
    /// <summary>
    /// Provider-specific settings
    /// </summary>
    public virtual ICollection<PaymentProviderSetting> Settings { get; set; } = new List<PaymentProviderSetting>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database model for storing individual payment provider settings
/// </summary>
public class PaymentProviderSetting
{
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign key to PaymentProviderConfiguration
    /// </summary>
    public int PaymentProviderConfigurationId { get; set; }
    
    /// <summary>
    /// Navigation property to PaymentProviderConfiguration
    /// </summary>
    public virtual PaymentProviderConfiguration PaymentProviderConfiguration { get; set; } = null!;
    
    /// <summary>
    /// Setting key (e.g., "ClientId", "ClientSecret", "WebhookSecret")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Setting value (encrypted for sensitive data)
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this setting contains sensitive data (should be encrypted)
    /// </summary>
    public bool IsSensitive { get; set; } = false;
    
    /// <summary>
    /// Human-readable description of this setting
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Setting category for grouping (e.g., "Authentication", "Webhooks", "General")
    /// </summary>
    [StringLength(50)]
    public string Category { get; set; } = "General";
    
    /// <summary>
    /// Whether this setting is required for the provider to function
    /// </summary>
    public bool IsRequired { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database model for storing global payment configuration settings
/// </summary>
public class PaymentGlobalConfiguration
{
    public int Id { get; set; }
    
    /// <summary>
    /// Default payment provider name
    /// </summary>
    [StringLength(50)]
    public string DefaultProvider { get; set; } = "PayPal";
    
    /// <summary>
    /// Default currency for payments
    /// </summary>
    [StringLength(3)]
    public string DefaultCurrency { get; set; } = "USD";
    
    /// <summary>
    /// Grace period in days for failed payments before suspending subscription
    /// </summary>
    public int GracePeriodDays { get; set; } = 7;
    
    /// <summary>
    /// Maximum number of retry attempts for failed payments
    /// </summary>
    public int MaxPaymentRetries { get; set; } = 3;
    
    /// <summary>
    /// Days between payment retry attempts
    /// </summary>
    public int RetryIntervalDays { get; set; } = 3;
    
    /// <summary>
    /// Enable trial periods for new subscriptions
    /// </summary>
    public bool EnableTrialPeriods { get; set; } = true;
    
    /// <summary>
    /// Default trial period in days
    /// </summary>
    public int DefaultTrialDays { get; set; } = 14;
    
    /// <summary>
    /// Enable proration for subscription changes
    /// </summary>
    public bool EnableProration { get; set; } = true;
    
    /// <summary>
    /// Webhook timeout in seconds
    /// </summary>
    public int WebhookTimeoutSeconds { get; set; } = 10;
    
    /// <summary>
    /// Whether to verify webhook signatures
    /// </summary>
    public bool VerifyWebhookSignatures { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
