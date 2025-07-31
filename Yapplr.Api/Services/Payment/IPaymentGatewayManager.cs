using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Services.Payment.Providers;

namespace Yapplr.Api.Services.Payment;

public interface IPaymentGatewayManager
{
    Task<List<IPaymentProvider>> GetAvailableProvidersAsync();
    Task<IPaymentProvider?> GetBestProviderAsync(string? preferredProvider = null);
    Task<IPaymentProvider?> GetProviderByNameAsync(string providerName);
    Task<bool> HasAvailableProvidersAsync();
    Task<List<PaymentProviderInfo>> GetProviderInfoAsync();
}