using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs.Payment;

// Base result classes
public class PaymentServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    
    public static PaymentServiceResult<T> SuccessResult(T data) => new() { Success = true, Data = data };
    public static PaymentServiceResult<T> ErrorResult(string message, string? code = null) => new() { Success = false, ErrorMessage = message, ErrorCode = code };
}

// Subscription DTOs
public class SubscriptionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int SubscriptionTierId { get; set; }
    public string SubscriptionTierName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int BillingCycleMonths { get; set; }
    public string PaymentProvider { get; set; } = string.Empty;
    public string? ExternalSubscriptionId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime NextBillingDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public bool IsTrialPeriod { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public int BillingCycleCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Payment Transaction DTOs
public class PaymentTransactionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int SubscriptionTierId { get; set; }
    public string SubscriptionTierName { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string? ExternalSubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public PaymentType Type { get; set; }
    public string? Description { get; set; }
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Payment Method DTOs
public class PaymentMethodDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PaymentProvider { get; set; } = string.Empty;
    public string ExternalPaymentMethodId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Last4 { get; set; }
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public string? HolderName { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Provider Info DTO
public class PaymentProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public List<string> SupportedPaymentMethods { get; set; } = new();
    public List<string> SupportedCurrencies { get; set; } = new();
}
