namespace Yapplr.Api.Configuration;

/// <summary>
/// Configuration for payment providers
/// </summary>
public class PaymentProvidersConfiguration
{
    public const string SectionName = "PaymentProviders";

    public PayPalConfiguration PayPal { get; set; } = new();
    public StripeConfiguration Stripe { get; set; } = new();
    
    /// <summary>
    /// Default provider to use when no specific provider is requested
    /// </summary>
    public string DefaultProvider { get; set; } = "PayPal";
    
    /// <summary>
    /// Provider priority order for fallback scenarios
    /// </summary>
    public List<string> ProviderPriority { get; set; } = new() { "PayPal", "Stripe" };
    
    /// <summary>
    /// Global payment settings
    /// </summary>
    public PaymentGlobalSettings Global { get; set; } = new();
}

/// <summary>
/// PayPal payment provider configuration
/// </summary>
public class PayPalConfiguration
{
    public bool Enabled { get; set; } = true;
    public string Environment { get; set; } = "sandbox"; // "sandbox" or "live"
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WebhookId { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public List<string> SupportedCurrencies { get; set; } = new() { "USD", "EUR", "GBP", "CAD", "AUD" };
    public PayPalWebhookConfiguration Webhooks { get; set; } = new();
}

/// <summary>
/// Stripe payment provider configuration (for future implementation)
/// </summary>
public class StripeConfiguration
{
    public bool Enabled { get; set; } = false;
    public string Environment { get; set; } = "test"; // "test" or "live"
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public List<string> SupportedCurrencies { get; set; } = new() { "USD", "EUR", "GBP", "CAD", "AUD" };
}

/// <summary>
/// PayPal webhook configuration
/// </summary>
public class PayPalWebhookConfiguration
{
    public bool VerifySignature { get; set; } = true;
    public List<string> EnabledEvents { get; set; } = new()
    {
        "BILLING.SUBSCRIPTION.CREATED",
        "BILLING.SUBSCRIPTION.ACTIVATED",
        "BILLING.SUBSCRIPTION.UPDATED",
        "BILLING.SUBSCRIPTION.CANCELLED",
        "BILLING.SUBSCRIPTION.SUSPENDED",
        "BILLING.SUBSCRIPTION.EXPIRED",
        "PAYMENT.SALE.COMPLETED",
        "PAYMENT.SALE.DENIED",
        "PAYMENT.SALE.REFUNDED"
    };
}

/// <summary>
/// Global payment settings
/// </summary>
public class PaymentGlobalSettings
{
    /// <summary>
    /// Default currency for payments
    /// </summary>
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
    /// Default trial period length in days
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
    /// Enable webhook signature verification
    /// </summary>
    public bool VerifyWebhookSignatures { get; set; } = true;
}
