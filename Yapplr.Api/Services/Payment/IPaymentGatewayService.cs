using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Services.Payment.Providers;

namespace Yapplr.Api.Services.Payment;

/// <summary>
/// High-level payment gateway service that orchestrates payment operations across providers
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Creates a subscription for a user
    /// </summary>
    Task<PaymentServiceResult<SubscriptionDto>> CreateSubscriptionAsync(int userId, int subscriptionTierId, CreateSubscriptionRequest request);

    /// <summary>
    /// Cancels a user's subscription
    /// </summary>
    Task<PaymentServiceResult<SubscriptionDto>> CancelSubscriptionAsync(int userId, CancelSubscriptionRequest request);

    /// <summary>
    /// Updates a user's subscription
    /// </summary>
    Task<PaymentServiceResult<SubscriptionDto>> UpdateSubscriptionAsync(int userId, UpdateSubscriptionRequest request);

    /// <summary>
    /// Gets a user's current subscription
    /// </summary>
    Task<PaymentServiceResult<SubscriptionDto>> GetUserSubscriptionAsync(int userId);

    /// <summary>
    /// Processes a one-time payment
    /// </summary>
    Task<PaymentServiceResult<PaymentTransactionDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request);

    /// <summary>
    /// Refunds a payment
    /// </summary>
    Task<PaymentServiceResult<PaymentTransactionDto>> RefundPaymentAsync(int userId, int transactionId, RefundPaymentRequest request);

    /// <summary>
    /// Adds a payment method for a user
    /// </summary>
    Task<PaymentServiceResult<PaymentMethodDto>> AddPaymentMethodAsync(int userId, CreatePaymentMethodRequest request);

    /// <summary>
    /// Removes a payment method for a user
    /// </summary>
    Task<PaymentServiceResult<bool>> RemovePaymentMethodAsync(int userId, int paymentMethodId);

    /// <summary>
    /// Gets all payment methods for a user
    /// </summary>
    Task<PaymentServiceResult<List<PaymentMethodDto>>> GetUserPaymentMethodsAsync(int userId);

    /// <summary>
    /// Sets a payment method as default for a user
    /// </summary>
    Task<PaymentServiceResult<PaymentMethodDto>> SetDefaultPaymentMethodAsync(int userId, int paymentMethodId);

    /// <summary>
    /// Gets payment history for a user
    /// </summary>
    Task<PaymentServiceResult<List<PaymentTransactionDto>>> GetPaymentHistoryAsync(int userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Handles webhook events from payment providers
    /// </summary>
    Task<PaymentServiceResult<bool>> HandleWebhookAsync(string providerName, WebhookRequest request);

    /// <summary>
    /// Gets available payment providers
    /// </summary>
    Task<List<PaymentProviderInfo>> GetAvailableProvidersAsync();

    /// <summary>
    /// Gets the best payment provider for a specific operation
    /// </summary>
    Task<IPaymentProvider?> GetBestProviderAsync(string? preferredProvider = null);

    /// <summary>
    /// Synchronizes subscription status with payment provider
    /// </summary>
    Task<PaymentServiceResult<SubscriptionDto>> SyncSubscriptionStatusAsync(int userId);
}
