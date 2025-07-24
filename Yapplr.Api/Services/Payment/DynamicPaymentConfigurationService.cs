using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services.Payment;

/// <summary>
/// Service for managing dynamic payment provider configurations stored in the database
/// </summary>
public class DynamicPaymentConfigurationService : IDynamicPaymentConfigurationService
{
    private readonly YapplrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DynamicPaymentConfigurationService> _logger;
    private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);
    
    private const string CACHE_KEY_PREFIX = "PaymentConfig_";
    private const string CACHE_KEY_GLOBAL = "PaymentConfig_Global";
    private const string CACHE_KEY_PROVIDERS = "PaymentConfig_Providers";
    private const int CACHE_DURATION_MINUTES = 30;

    public event EventHandler<PaymentConfigurationChangedEventArgs>? ConfigurationChanged;

    public DynamicPaymentConfigurationService(
        YapplrDbContext context,
        IMemoryCache cache,
        ILogger<DynamicPaymentConfigurationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PaymentProvidersConfiguration> GetPaymentProvidersConfigurationAsync()
    {
        var cacheKey = CACHE_KEY_PROVIDERS;
        
        if (_cache.TryGetValue(cacheKey, out PaymentProvidersConfiguration? cachedConfig) && cachedConfig != null)
        {
            return cachedConfig;
        }

        await _cacheSemaphore.WaitAsync();
        try
        {
            // Double-check pattern
            if (_cache.TryGetValue(cacheKey, out cachedConfig) && cachedConfig != null)
            {
                return cachedConfig;
            }

            var config = new PaymentProvidersConfiguration();
            
            // Get global settings
            config.Global = await GetGlobalSettingsAsync();
            
            // Get PayPal configuration
            var paypalConfig = await GetPayPalConfigurationAsync();
            if (paypalConfig != null)
            {
                config.PayPal = paypalConfig;
            }
            
            // Get Stripe configuration
            var stripeConfig = await GetStripeConfigurationAsync();
            if (stripeConfig != null)
            {
                config.Stripe = stripeConfig;
            }
            
            // Get default provider and priority
            config.DefaultProvider = await GetDefaultProviderAsync();
            config.ProviderPriority = await GetProviderPriorityAsync();

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };

            _cache.Set(cacheKey, config, cacheOptions);
            return config;
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }

    public async Task<PayPalConfiguration?> GetPayPalConfigurationAsync()
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}PayPal";
        
        if (_cache.TryGetValue(cacheKey, out PayPalConfiguration? cachedConfig) && cachedConfig != null)
        {
            return cachedConfig;
        }

        var providerConfig = await _context.PaymentProviderConfigurations
            .Include(p => p.Settings)
            .FirstOrDefaultAsync(p => p.ProviderName == "PayPal");

        if (providerConfig == null)
        {
            return null;
        }

        var config = new PayPalConfiguration
        {
            Enabled = providerConfig.IsEnabled,
            Environment = providerConfig.Environment,
            TimeoutSeconds = providerConfig.TimeoutSeconds,
            MaxRetries = providerConfig.MaxRetries
        };

        // Parse supported currencies
        if (!string.IsNullOrEmpty(providerConfig.SupportedCurrencies))
        {
            try
            {
                config.SupportedCurrencies = JsonSerializer.Deserialize<List<string>>(providerConfig.SupportedCurrencies) ?? new List<string>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse supported currencies for PayPal: {Currencies}", providerConfig.SupportedCurrencies);
            }
        }

        // Map settings
        foreach (var setting in providerConfig.Settings)
        {
            switch (setting.Key)
            {
                case "ClientId":
                    config.ClientId = setting.Value;
                    break;
                case "ClientSecret":
                    config.ClientSecret = setting.Value;
                    break;
                case "WebhookId":
                    config.WebhookId = setting.Value;
                    break;
                case "WebhookSecret":
                    config.WebhookSecret = setting.Value;
                    break;
                case "WebhookVerifySignature":
                    if (bool.TryParse(setting.Value, out var verifySignature))
                    {
                        config.Webhooks.VerifySignature = verifySignature;
                    }
                    break;
                case "WebhookEnabledEvents":
                    try
                    {
                        config.Webhooks.EnabledEvents = JsonSerializer.Deserialize<List<string>>(setting.Value) ?? new List<string>();
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse webhook enabled events for PayPal: {Events}", setting.Value);
                    }
                    break;
            }
        }

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES),
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        _cache.Set(cacheKey, config, cacheOptions);
        return config;
    }

    public async Task<StripeConfiguration?> GetStripeConfigurationAsync()
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}Stripe";
        
        if (_cache.TryGetValue(cacheKey, out StripeConfiguration? cachedConfig) && cachedConfig != null)
        {
            return cachedConfig;
        }

        var providerConfig = await _context.PaymentProviderConfigurations
            .Include(p => p.Settings)
            .FirstOrDefaultAsync(p => p.ProviderName == "Stripe");

        if (providerConfig == null)
        {
            return null;
        }

        var config = new StripeConfiguration
        {
            Enabled = providerConfig.IsEnabled,
            Environment = providerConfig.Environment,
            TimeoutSeconds = providerConfig.TimeoutSeconds,
            MaxRetries = providerConfig.MaxRetries
        };

        // Parse supported currencies
        if (!string.IsNullOrEmpty(providerConfig.SupportedCurrencies))
        {
            try
            {
                config.SupportedCurrencies = JsonSerializer.Deserialize<List<string>>(providerConfig.SupportedCurrencies) ?? new List<string>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse supported currencies for Stripe: {Currencies}", providerConfig.SupportedCurrencies);
            }
        }

        // Map settings
        foreach (var setting in providerConfig.Settings)
        {
            switch (setting.Key)
            {
                case "PublishableKey":
                    config.PublishableKey = setting.Value;
                    break;
                case "SecretKey":
                    config.SecretKey = setting.Value;
                    break;
                case "WebhookSecret":
                    config.WebhookSecret = setting.Value;
                    break;
            }
        }

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES),
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        _cache.Set(cacheKey, config, cacheOptions);
        return config;
    }

    public async Task<PaymentGlobalSettings> GetGlobalSettingsAsync()
    {
        var cacheKey = CACHE_KEY_GLOBAL;
        
        if (_cache.TryGetValue(cacheKey, out PaymentGlobalSettings? cachedSettings) && cachedSettings != null)
        {
            return cachedSettings;
        }

        var globalConfig = await _context.PaymentGlobalConfigurations
            .FirstOrDefaultAsync();

        var settings = globalConfig != null ? new PaymentGlobalSettings
        {
            DefaultCurrency = globalConfig.DefaultCurrency,
            GracePeriodDays = globalConfig.GracePeriodDays,
            MaxPaymentRetries = globalConfig.MaxPaymentRetries,
            RetryIntervalDays = globalConfig.RetryIntervalDays,
            EnableTrialPeriods = globalConfig.EnableTrialPeriods,
            DefaultTrialDays = globalConfig.DefaultTrialDays,
            EnableProration = globalConfig.EnableProration,
            WebhookTimeoutSeconds = globalConfig.WebhookTimeoutSeconds,
            VerifyWebhookSignatures = globalConfig.VerifyWebhookSignatures
        } : new PaymentGlobalSettings(); // Use defaults if no config found

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES),
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        _cache.Set(cacheKey, settings, cacheOptions);
        return settings;
    }

    public async Task<bool> UpdateProviderConfigurationAsync(string providerName, object configuration)
    {
        try
        {
            var providerConfig = await _context.PaymentProviderConfigurations
                .Include(p => p.Settings)
                .FirstOrDefaultAsync(p => p.ProviderName == providerName);

            if (providerConfig == null)
            {
                _logger.LogWarning("Provider configuration not found: {ProviderName}", providerName);
                return false;
            }

            // Update based on provider type
            if (providerName == "PayPal" && configuration is PayPalConfiguration paypalConfig)
            {
                await UpdatePayPalConfigurationAsync(providerConfig, paypalConfig);
            }
            else if (providerName == "Stripe" && configuration is StripeConfiguration stripeConfig)
            {
                await UpdateStripeConfigurationAsync(providerConfig, stripeConfig);
            }
            else
            {
                _logger.LogWarning("Unsupported provider or configuration type: {ProviderName}", providerName);
                return false;
            }

            await _context.SaveChangesAsync();

            // Clear cache
            await RefreshCacheAsync();

            // Fire event
            ConfigurationChanged?.Invoke(this, new PaymentConfigurationChangedEventArgs
            {
                ProviderName = providerName,
                ChangeType = "Updated"
            });

            _logger.LogInformation("Updated payment provider configuration: {ProviderName}", providerName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider configuration: {ProviderName}", providerName);
            return false;
        }
    }

    public async Task<bool> UpdateGlobalSettingsAsync(PaymentGlobalSettings settings)
    {
        try
        {
            var globalConfig = await _context.PaymentGlobalConfigurations
                .FirstOrDefaultAsync();

            if (globalConfig == null)
            {
                globalConfig = new PaymentGlobalConfiguration();
                _context.PaymentGlobalConfigurations.Add(globalConfig);
            }

            globalConfig.DefaultCurrency = settings.DefaultCurrency;
            globalConfig.GracePeriodDays = settings.GracePeriodDays;
            globalConfig.MaxPaymentRetries = settings.MaxPaymentRetries;
            globalConfig.RetryIntervalDays = settings.RetryIntervalDays;
            globalConfig.EnableTrialPeriods = settings.EnableTrialPeriods;
            globalConfig.DefaultTrialDays = settings.DefaultTrialDays;
            globalConfig.EnableProration = settings.EnableProration;
            globalConfig.WebhookTimeoutSeconds = settings.WebhookTimeoutSeconds;
            globalConfig.VerifyWebhookSignatures = settings.VerifyWebhookSignatures;
            globalConfig.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Clear cache
            await RefreshCacheAsync();

            // Fire event
            ConfigurationChanged?.Invoke(this, new PaymentConfigurationChangedEventArgs
            {
                ProviderName = "Global",
                ChangeType = "Updated"
            });

            _logger.LogInformation("Updated global payment settings");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global payment settings");
            return false;
        }
    }

    public async Task<string> GetDefaultProviderAsync()
    {
        var globalConfig = await _context.PaymentGlobalConfigurations
            .FirstOrDefaultAsync();

        return globalConfig?.DefaultProvider ?? "PayPal";
    }

    public async Task<List<string>> GetProviderPriorityAsync()
    {
        var providers = await _context.PaymentProviderConfigurations
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority)
            .Select(p => p.ProviderName)
            .ToListAsync();

        return providers.Any() ? providers : new List<string> { "PayPal", "Stripe" };
    }

    public async Task RefreshCacheAsync()
    {
        await _cacheSemaphore.WaitAsync();
        try
        {
            // Remove all payment configuration cache entries
            _cache.Remove(CACHE_KEY_GLOBAL);
            _cache.Remove(CACHE_KEY_PROVIDERS);
            _cache.Remove($"{CACHE_KEY_PREFIX}PayPal");
            _cache.Remove($"{CACHE_KEY_PREFIX}Stripe");

            _logger.LogInformation("Payment configuration cache refreshed");
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }

    private async Task UpdatePayPalConfigurationAsync(PaymentProviderConfiguration providerConfig, PayPalConfiguration config)
    {
        providerConfig.IsEnabled = config.Enabled;
        providerConfig.Environment = config.Environment;
        providerConfig.TimeoutSeconds = config.TimeoutSeconds;
        providerConfig.MaxRetries = config.MaxRetries;
        providerConfig.SupportedCurrencies = JsonSerializer.Serialize(config.SupportedCurrencies);
        providerConfig.UpdatedAt = DateTime.UtcNow;

        // Update or create settings
        await UpdateOrCreateSettingAsync(providerConfig, "ClientId", config.ClientId, true, "PayPal Client ID", "Authentication");
        await UpdateOrCreateSettingAsync(providerConfig, "ClientSecret", config.ClientSecret, true, "PayPal Client Secret", "Authentication");
        await UpdateOrCreateSettingAsync(providerConfig, "WebhookId", config.WebhookId, false, "PayPal Webhook ID", "Webhooks");
        await UpdateOrCreateSettingAsync(providerConfig, "WebhookSecret", config.WebhookSecret, true, "PayPal Webhook Secret", "Webhooks");
        await UpdateOrCreateSettingAsync(providerConfig, "WebhookVerifySignature", config.Webhooks.VerifySignature.ToString(), false, "Verify webhook signatures", "Webhooks");
        await UpdateOrCreateSettingAsync(providerConfig, "WebhookEnabledEvents", JsonSerializer.Serialize(config.Webhooks.EnabledEvents), false, "Enabled webhook events", "Webhooks");
    }

    private async Task UpdateStripeConfigurationAsync(PaymentProviderConfiguration providerConfig, StripeConfiguration config)
    {
        providerConfig.IsEnabled = config.Enabled;
        providerConfig.Environment = config.Environment;
        providerConfig.TimeoutSeconds = config.TimeoutSeconds;
        providerConfig.MaxRetries = config.MaxRetries;
        providerConfig.SupportedCurrencies = JsonSerializer.Serialize(config.SupportedCurrencies);
        providerConfig.UpdatedAt = DateTime.UtcNow;

        // Update or create settings
        await UpdateOrCreateSettingAsync(providerConfig, "PublishableKey", config.PublishableKey, false, "Stripe Publishable Key", "Authentication");
        await UpdateOrCreateSettingAsync(providerConfig, "SecretKey", config.SecretKey, true, "Stripe Secret Key", "Authentication");
        await UpdateOrCreateSettingAsync(providerConfig, "WebhookSecret", config.WebhookSecret, true, "Stripe Webhook Secret", "Webhooks");
    }

    private async Task UpdateOrCreateSettingAsync(PaymentProviderConfiguration providerConfig, string key, string value, bool isSensitive, string description, string category)
    {
        var setting = providerConfig.Settings.FirstOrDefault(s => s.Key == key);

        if (setting == null)
        {
            setting = new PaymentProviderSetting
            {
                PaymentProviderConfigurationId = providerConfig.Id,
                Key = key,
                IsSensitive = isSensitive,
                Description = description,
                Category = category,
                IsRequired = isSensitive, // Sensitive settings are typically required
                CreatedAt = DateTime.UtcNow
            };
            providerConfig.Settings.Add(setting);
        }

        setting.Value = value;
        setting.UpdatedAt = DateTime.UtcNow;
    }
}
