using Yapplr.Api.DTOs.Payment;

namespace Yapplr.Api.Services.Payment;

/// <summary>
/// Service for payment administration functionality
/// </summary>
public interface IPaymentAdminService
{
    // Subscription Management
    Task<PagedResult<AdminSubscriptionDto>> GetSubscriptionsAsync(int page = 1, int pageSize = 25, string? status = null, string? provider = null);
    Task<AdminSubscriptionDto?> GetSubscriptionAsync(int subscriptionId);
    Task<PaymentServiceResult<bool>> CancelSubscriptionAsync(int subscriptionId, CancelSubscriptionRequest request);
    Task<PaymentServiceResult<AdminSubscriptionDto>> SyncSubscriptionAsync(int subscriptionId);

    // Transaction Management
    Task<PagedResult<AdminTransactionDto>> GetTransactionsAsync(int page = 1, int pageSize = 25, string? status = null, string? provider = null, int? userId = null);
    Task<AdminTransactionDto?> GetTransactionAsync(int transactionId);
    Task<PaymentServiceResult<RefundPaymentResult>> RefundTransactionAsync(int transactionId, RefundPaymentRequest request);

    // Analytics
    Task<PaymentAnalyticsDto> GetPaymentAnalyticsAsync(int days = 30);
    Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(int days = 30, string? provider = null);

    // Provider Management
    Task<IEnumerable<PaymentProviderStatusDto>> GetPaymentProvidersAsync();
    Task<PaymentServiceResult<bool>> TestPaymentProviderAsync(string providerName);

    // Failed Payments
    Task<PagedResult<FailedPaymentDto>> GetFailedPaymentsAsync(int page = 1, int pageSize = 25);
    Task<PaymentServiceResult<bool>> RetryFailedPaymentAsync(int failedPaymentId);

    // Subscription Tiers
    Task<IEnumerable<AdminSubscriptionTierDto>> GetSubscriptionTiersAsync();
    Task<PaymentServiceResult<AdminSubscriptionTierDto>> UpdateSubscriptionTierAsync(int tierId, UpdateSubscriptionTierRequest request);

    // Webhook Management
    Task<PagedResult<WebhookLogDto>> GetWebhookLogsAsync(int page = 1, int pageSize = 25, string? provider = null);
    Task<PaymentServiceResult<bool>> ReplayWebhookAsync(int webhookLogId);
}
