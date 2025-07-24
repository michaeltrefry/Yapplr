using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs.Payment;

/// <summary>
/// DTO for payment provider configuration
/// </summary>
public class PaymentProviderConfigurationDto
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string Environment { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int TimeoutSeconds { get; set; }
    public int MaxRetries { get; set; }
    public List<string> SupportedCurrencies { get; set; } = new();
    public List<PaymentProviderSettingDto> Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for payment provider setting
/// </summary>
public class PaymentProviderSettingDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for global payment configuration
/// </summary>
public class PaymentGlobalConfigurationDto
{
    public int Id { get; set; }
    public string DefaultProvider { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = string.Empty;
    public int GracePeriodDays { get; set; }
    public int MaxPaymentRetries { get; set; }
    public int RetryIntervalDays { get; set; }
    public bool EnableTrialPeriods { get; set; }
    public int DefaultTrialDays { get; set; }
    public bool EnableProration { get; set; }
    public int WebhookTimeoutSeconds { get; set; }
    public bool VerifyWebhookSignatures { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating/updating payment provider configuration
/// </summary>
public class CreateUpdatePaymentProviderConfigurationDto
{
    [Required]
    [StringLength(50)]
    public string ProviderName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    [Required]
    [StringLength(20)]
    public string Environment { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Priority { get; set; }

    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    public List<string> SupportedCurrencies { get; set; } = new();
    public List<CreateUpdatePaymentProviderSettingDto> Settings { get; set; } = new();
}

/// <summary>
/// DTO for creating/updating payment provider setting
/// </summary>
public class CreateUpdatePaymentProviderSettingDto
{
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Value { get; set; } = string.Empty;

    public bool IsSensitive { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    public bool IsRequired { get; set; }
}

/// <summary>
/// DTO for updating global payment configuration
/// </summary>
public class UpdatePaymentGlobalConfigurationDto
{
    [Required]
    [StringLength(50)]
    public string DefaultProvider { get; set; } = string.Empty;

    [Required]
    [StringLength(3)]
    public string DefaultCurrency { get; set; } = string.Empty;

    [Range(0, 365)]
    public int GracePeriodDays { get; set; }

    [Range(0, 10)]
    public int MaxPaymentRetries { get; set; }

    [Range(1, 30)]
    public int RetryIntervalDays { get; set; }

    public bool EnableTrialPeriods { get; set; }

    [Range(1, 365)]
    public int DefaultTrialDays { get; set; }

    public bool EnableProration { get; set; }

    [Range(5, 300)]
    public int WebhookTimeoutSeconds { get; set; }

    public bool VerifyWebhookSignatures { get; set; }
}

/// <summary>
/// DTO for payment provider test result
/// </summary>
public class PaymentProviderTestResultDto
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for payment configuration summary
/// </summary>
public class PaymentConfigurationSummaryDto
{
    public PaymentGlobalConfigurationDto GlobalConfiguration { get; set; } = new();
    public List<PaymentProviderConfigurationDto> Providers { get; set; } = new();
    public string ActiveProvider { get; set; } = string.Empty;
    public List<string> ProviderPriority { get; set; } = new();
    public int TotalProviders { get; set; }
    public int EnabledProviders { get; set; }
    public DateTime LastUpdated { get; set; }
}
