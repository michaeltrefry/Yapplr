using Yapplr.Api.DTOs.Payment;

namespace Yapplr.Api.Services.Payment;

/// <summary>
/// Generic interface for payment providers (PayPal, Stripe, etc.)
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Gets the name of this payment provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Checks if this provider is available and properly configured
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Creates a subscription with the payment provider
    /// </summary>
    Task<CreateSubscriptionResult> CreateSubscriptionAsync(CreateSubscriptionRequest request);

    /// <summary>
    /// Cancels a subscription with the payment provider
    /// </summary>
    Task<CancelSubscriptionResult> CancelSubscriptionAsync(CancelSubscriptionRequest request);

    /// <summary>
    /// Updates a subscription (change plan, payment method, etc.)
    /// </summary>
    Task<UpdateSubscriptionResult> UpdateSubscriptionAsync(UpdateSubscriptionRequest request);

    /// <summary>
    /// Gets subscription details from the provider
    /// </summary>
    Task<GetSubscriptionResult> GetSubscriptionAsync(string externalSubscriptionId);

    /// <summary>
    /// Processes a one-time payment
    /// </summary>
    Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request);

    /// <summary>
    /// Refunds a payment
    /// </summary>
    Task<RefundPaymentResult> RefundPaymentAsync(RefundPaymentRequest request);

    /// <summary>
    /// Creates a payment method with the provider
    /// </summary>
    Task<CreatePaymentMethodResult> CreatePaymentMethodAsync(CreatePaymentMethodRequest request);

    /// <summary>
    /// Deletes a payment method from the provider
    /// </summary>
    Task<DeletePaymentMethodResult> DeletePaymentMethodAsync(string externalPaymentMethodId);

    /// <summary>
    /// Gets payment method details from the provider
    /// </summary>
    Task<GetPaymentMethodResult> GetPaymentMethodAsync(string externalPaymentMethodId);

    /// <summary>
    /// Handles webhook events from the payment provider
    /// </summary>
    Task<WebhookHandleResult> HandleWebhookAsync(WebhookRequest request);

    /// <summary>
    /// Verifies webhook signature/authenticity
    /// </summary>
    Task<bool> VerifyWebhookAsync(WebhookRequest request);

    /// <summary>
    /// Gets the redirect URL for payment authorization (for providers that require it)
    /// </summary>
    Task<string?> GetPaymentAuthorizationUrlAsync(PaymentAuthorizationRequest request);

    /// <summary>
    /// Handles payment authorization callback (for providers that require it)
    /// </summary>
    Task<PaymentAuthorizationResult> HandlePaymentAuthorizationAsync(PaymentAuthorizationCallbackRequest request);
}
