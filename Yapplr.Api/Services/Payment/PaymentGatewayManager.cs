using Yapplr.Api.Configuration;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Services.Payment.Providers;

namespace Yapplr.Api.Services.Payment;

public class PaymentGatewayManager : IPaymentGatewayManager
{
    private readonly ILogger<PaymentGatewayManager> _logger;
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly IDynamicPaymentConfigurationService _configService;

    public PaymentGatewayManager(
        ILogger<PaymentGatewayManager> logger,
        IEnumerable<IPaymentProvider> providers,
        IDynamicPaymentConfigurationService configService)
    {
        _logger = logger;
        _providers = providers;
        _configService = configService;
    }

    public async Task<List<IPaymentProvider>> GetAvailableProvidersAsync()
    {
        var availableProviders = new List<IPaymentProvider>();
        var orderedProviders = await GetOrderedProvidersAsync();

        foreach (var provider in orderedProviders)
        {
            try
            {
                if (await provider.IsAvailableAsync())
                {
                    availableProviders.Add(provider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check availability for payment provider {ProviderName}", provider.ProviderName);
            }
        }

        return availableProviders;
    }

    public async Task<IPaymentProvider?> GetBestProviderAsync(string? preferredProvider = null)
    {
        var orderedProviders = await GetOrderedProvidersAsync();

        // If a preferred provider is specified and available, use it
        if (!string.IsNullOrEmpty(preferredProvider))
        {
            var preferred = orderedProviders.FirstOrDefault(p =>
                p.ProviderName.Equals(preferredProvider, StringComparison.OrdinalIgnoreCase));

            if (preferred != null)
            {
                try
                {
                    if (await preferred.IsAvailableAsync())
                    {
                        return preferred;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Preferred payment provider {ProviderName} is not available", preferredProvider);
                }
            }
        }

        // Fall back to the first available provider in priority order
        foreach (var provider in orderedProviders)
        {
            try
            {
                if (await provider.IsAvailableAsync())
                {
                    return provider;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payment provider {ProviderName} availability check failed", provider.ProviderName);
            }
        }

        _logger.LogError("No payment providers are available");
        return null;
    }

    public async Task<IPaymentProvider?> GetProviderByNameAsync(string providerName)
    {
        var orderedProviders = await GetOrderedProvidersAsync();
        var provider = orderedProviders.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            return null;
        }

        try
        {
            if (await provider.IsAvailableAsync())
            {
                return provider;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Payment provider {ProviderName} is not available", providerName);
        }

        return null;
    }

    public async Task<bool> HasAvailableProvidersAsync()
    {
        var orderedProviders = await GetOrderedProvidersAsync();

        foreach (var provider in orderedProviders)
        {
            try
            {
                if (await provider.IsAvailableAsync())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payment provider {ProviderName} availability check failed", provider.ProviderName);
            }
        }

        return false;
    }

    public async Task<List<PaymentProviderInfo>> GetProviderInfoAsync()
    {
        var providerInfos = new List<PaymentProviderInfo>();
        var orderedProviders = await GetOrderedProvidersAsync();

        foreach (var provider in orderedProviders)
        {
            try
            {
                var isAvailable = await provider.IsAvailableAsync();
                var info = new PaymentProviderInfo
                {
                    Name = provider.ProviderName,
                    IsAvailable = isAvailable,
                    SupportedPaymentMethods = await GetSupportedPaymentMethodsAsync(provider.ProviderName),
                    SupportedCurrencies = await GetSupportedCurrenciesAsync(provider.ProviderName)
                };

                providerInfos.Add(info);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get info for payment provider {ProviderName}", provider.ProviderName);

                // Add provider info with availability = false
                var info = new PaymentProviderInfo
                {
                    Name = provider.ProviderName,
                    IsAvailable = false,
                    SupportedPaymentMethods = new List<string>(),
                    SupportedCurrencies = new List<string>()
                };

                providerInfos.Add(info);
            }
        }

        return providerInfos;
    }

    private async Task<List<IPaymentProvider>> GetOrderedProvidersAsync()
    {
        var orderedProviders = new List<IPaymentProvider>();
        var providerPriority = await _configService.GetProviderPriorityAsync();

        // First, add providers in the configured priority order
        foreach (var providerName in providerPriority)
        {
            var provider = _providers.FirstOrDefault(p =>
                p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            if (provider != null)
            {
                orderedProviders.Add(provider);
            }
        }

        // Then add any remaining providers not in the priority list
        foreach (var provider in _providers)
        {
            if (!orderedProviders.Any(p => p.ProviderName.Equals(provider.ProviderName, StringComparison.OrdinalIgnoreCase)))
            {
                orderedProviders.Add(provider);
            }
        }

        return orderedProviders;
    }

    private async Task<List<string>> GetSupportedPaymentMethodsAsync(string providerName)
    {
        return providerName.ToLower() switch
        {
            "paypal" => new List<string> { "paypal", "credit_card", "debit_card" },
            "stripe" => new List<string> { "credit_card", "debit_card", "bank_account", "apple_pay", "google_pay" },
            _ => new List<string>()
        };
    }

    private async Task<List<string>> GetSupportedCurrenciesAsync(string providerName)
    {
        return providerName.ToLower() switch
        {
            "paypal" => (await _configService.GetPayPalConfigurationAsync())?.SupportedCurrencies ?? new List<string> { "USD" },
            "stripe" => (await _configService.GetStripeConfigurationAsync())?.SupportedCurrencies ?? new List<string> { "USD" },
            _ => new List<string> { "USD" }
        };
    }
}