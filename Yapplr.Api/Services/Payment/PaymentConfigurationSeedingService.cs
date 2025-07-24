using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services.Payment;

/// <summary>
/// Service for seeding default payment configuration data
/// </summary>
public class PaymentConfigurationSeedingService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<PaymentConfigurationSeedingService> _logger;

    public PaymentConfigurationSeedingService(
        YapplrDbContext context,
        ILogger<PaymentConfigurationSeedingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds default payment configuration if none exists
    /// </summary>
    public async Task SeedDefaultConfigurationAsync()
    {
        try
        {
            // Check if any payment configuration already exists
            var existingProviders = await _context.PaymentProviderConfigurations.AnyAsync();
            var existingGlobalConfig = await _context.PaymentGlobalConfigurations.AnyAsync();

            if (existingProviders && existingGlobalConfig)
            {
                _logger.LogInformation("Payment configuration already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Seeding default payment configuration...");

            // Seed global configuration if it doesn't exist
            if (!existingGlobalConfig)
            {
                await SeedGlobalConfigurationAsync();
            }

            // Seed provider configurations if they don't exist
            if (!existingProviders)
            {
                await SeedProviderConfigurationsAsync();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Default payment configuration seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default payment configuration");
            throw;
        }
    }

    private async Task SeedGlobalConfigurationAsync()
    {
        var globalConfig = new PaymentGlobalConfiguration
        {
            DefaultProvider = "PayPal",
            DefaultCurrency = "USD",
            GracePeriodDays = 7,
            MaxPaymentRetries = 3,
            RetryIntervalDays = 3,
            EnableTrialPeriods = true,
            DefaultTrialDays = 14,
            EnableProration = true,
            WebhookTimeoutSeconds = 10,
            VerifyWebhookSignatures = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PaymentGlobalConfigurations.Add(globalConfig);
        _logger.LogInformation("Seeded global payment configuration");
    }

    private async Task SeedProviderConfigurationsAsync()
    {
        // Seed PayPal configuration
        var paypalProvider = new PaymentProviderConfiguration
        {
            ProviderName = "PayPal",
            IsEnabled = false, // Disabled by default until configured
            Environment = "sandbox",
            Priority = 1,
            TimeoutSeconds = 30,
            MaxRetries = 3,
            SupportedCurrencies = JsonSerializer.Serialize(new[] { "USD", "EUR", "GBP", "CAD", "AUD" }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Settings = new List<PaymentProviderSetting>
            {
                new PaymentProviderSetting
                {
                    Key = "ClientId",
                    Value = "",
                    IsSensitive = false,
                    Description = "PayPal Client ID from your PayPal Developer Dashboard",
                    Category = "Authentication",
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PaymentProviderSetting
                {
                    Key = "ClientSecret",
                    Value = "",
                    IsSensitive = true,
                    Description = "PayPal Client Secret from your PayPal Developer Dashboard",
                    Category = "Authentication",
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PaymentProviderSetting
                {
                    Key = "WebhookId",
                    Value = "",
                    IsSensitive = false,
                    Description = "PayPal Webhook ID for receiving payment notifications",
                    Category = "Webhooks",
                    IsRequired = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PaymentProviderSetting
                {
                    Key = "WebhookSecret",
                    Value = "",
                    IsSensitive = true,
                    Description = "PayPal Webhook Secret for verifying webhook signatures",
                    Category = "Webhooks",
                    IsRequired = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PaymentProviderSetting
                {
                    Key = "WebhookVerifySignature",
                    Value = "true",
                    IsSensitive = false,
                    Description = "Whether to verify PayPal webhook signatures",
                    Category = "Webhooks",
                    IsRequired = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PaymentProviderSetting
                {
                    Key = "WebhookEnabledEvents",
                    Value = JsonSerializer.Serialize(new[]
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
                    }),
                    IsSensitive = false,
                    Description = "List of PayPal webhook events to listen for",
                    Category = "Webhooks",
                    IsRequired = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        _context.PaymentProviderConfigurations.Add(paypalProvider);

        // Seed Stripe configuration
        var stripeProvider = new PaymentProviderConfiguration
        {
            ProviderName = "Stripe",
            IsEnabled = false, // Disabled by default until configured
            Environment = "test",
            Priority = 2,
            TimeoutSeconds = 30,
            MaxRetries = 3,
            SupportedCurrencies = JsonSerializer.Serialize(new[] { "USD", "EUR", "GBP", "CAD", "AUD" }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Settings = new List<PaymentProviderSetting>
            {
                new PaymentProviderSetting
                {
                    Key = "PublishableKey",
                    Value = "",
                    IsSensitive = false,
                    Description = "Stripe Publishable Key from your Stripe Dashboard",
                    Category = "Authentication",
                    IsRequired = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PaymentProviderSetting
                {
                    Key = "SecretKey",
                    Value = "",
                    IsSensitive = true,
                    Description = "Stripe Secret Key from your Stripe Dashboard",
                    Category = "Authentication",
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PaymentProviderSetting
                {
                    Key = "WebhookSecret",
                    Value = "",
                    IsSensitive = true,
                    Description = "Stripe Webhook Endpoint Secret for verifying webhook signatures",
                    Category = "Webhooks",
                    IsRequired = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        _context.PaymentProviderConfigurations.Add(stripeProvider);

        _logger.LogInformation("Seeded PayPal and Stripe provider configurations");
    }
}
