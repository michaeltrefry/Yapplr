using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs.Payment;

// Admin Subscription DTOs
public class AdminSubscriptionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int SubscriptionTierId { get; set; }
    public string SubscriptionTierName { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public string? ExternalSubscriptionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime NextBillingDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public bool IsTrialPeriod { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public string? PaymentMethodType { get; set; }
    public string? PaymentMethodLast4 { get; set; }
    public int BillingCycleCount { get; set; }
    public DateTime? GracePeriodEndDate { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryDate { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public decimal TotalPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Admin Transaction DTOs
public class AdminTransactionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string? ExternalSubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? WebhookEventId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Analytics DTOs
public class PaymentAnalyticsDto
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int CancelledSubscriptions { get; set; }
    public int TrialSubscriptions { get; set; }
    public int PastDueSubscriptions { get; set; }
    public int SuspendedSubscriptions { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal AverageRevenuePerUser { get; set; }
    public double ChurnRate { get; set; }
    public int TotalTransactions { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public double SuccessRate { get; set; }
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    public List<ProviderStatsDto> ProviderStats { get; set; } = new();
}

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}

public class ProviderStatsDto
{
    public string ProviderName { get; set; } = string.Empty;
    public int SubscriptionCount { get; set; }
    public decimal Revenue { get; set; }
    public double SuccessRate { get; set; }
}

public class RevenueAnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal PreviousPeriodRevenue { get; set; }
    public double GrowthRate { get; set; }
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
    public List<TierRevenueDto> RevenueByTier { get; set; } = new();
}

public class MonthlyRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Revenue { get; set; }
    public int SubscriptionCount { get; set; }
}

public class TierRevenueDto
{
    public string TierName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int SubscriptionCount { get; set; }
    public double Percentage { get; set; }
}

// Provider Status DTOs
public class PaymentProviderStatusDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsAvailable { get; set; }
    public string Environment { get; set; } = string.Empty;
    public List<string> SupportedCurrencies { get; set; } = new();
    public List<string> SupportedPaymentMethods { get; set; } = new();
    public DateTime? LastHealthCheck { get; set; }
    public string? HealthStatus { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal TotalRevenue { get; set; }
}

// Failed Payment DTOs
public class FailedPaymentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public string? ExternalSubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime? LastRetryDate { get; set; }
    public DateTime? NextRetryDate { get; set; }
    public DateTime FailedAt { get; set; }
    public bool CanRetry { get; set; }
}

// Subscription Tier Admin DTOs
public class AdminSubscriptionTierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int BillingCycleMonths { get; set; }
    public bool IsActive { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateSubscriptionTierRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public bool? IsActive { get; set; }
}

// Webhook Log DTOs
public class WebhookLogDto
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string RawPayload { get; set; } = string.Empty;
}

// Paged Result DTO
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
