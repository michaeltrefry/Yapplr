namespace Yapplr.Api.DTOs.Payment;

// Base result class for provider operations
public abstract class PaymentProviderResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

// Subscription Result DTOs
public class CreateSubscriptionResult : PaymentProviderResult
{
    public string? ExternalSubscriptionId { get; set; }
    public string? PaymentMethodId { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public string? Status { get; set; }
    public string? AuthorizationUrl { get; set; } // For providers requiring redirect
    public bool RequiresAction { get; set; } = false;
}

public class CancelSubscriptionResult : PaymentProviderResult
{
    public string? ExternalSubscriptionId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? EffectiveEndDate { get; set; }
    public string? Status { get; set; }
}

public class UpdateSubscriptionResult : PaymentProviderResult
{
    public string? ExternalSubscriptionId { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public string? Status { get; set; }
    public decimal? ProrationAmount { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class GetSubscriptionResult : PaymentProviderResult
{
    public string? ExternalSubscriptionId { get; set; }
    public string? Status { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public string? PaymentMethodId { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
}

// Payment Result DTOs
public class ProcessPaymentResult : PaymentProviderResult
{
    public string? ExternalTransactionId { get; set; }
    public string? Status { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? AuthorizationUrl { get; set; } // For providers requiring redirect
    public bool RequiresAction { get; set; } = false;
}

public class RefundPaymentResult : PaymentProviderResult
{
    public string? ExternalRefundId { get; set; }
    public string? ExternalTransactionId { get; set; }
    public decimal? RefundedAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? Status { get; set; }
}

// Payment Method Result DTOs
public class CreatePaymentMethodResult : PaymentProviderResult
{
    public string? ExternalPaymentMethodId { get; set; }
    public string? Type { get; set; }
    public string? Brand { get; set; }
    public string? Last4 { get; set; }
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public bool IsVerified { get; set; }
}

public class DeletePaymentMethodResult : PaymentProviderResult
{
    public string? ExternalPaymentMethodId { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class GetPaymentMethodResult : PaymentProviderResult
{
    public string? ExternalPaymentMethodId { get; set; }
    public string? Type { get; set; }
    public string? Brand { get; set; }
    public string? Last4 { get; set; }
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

// Webhook Result DTOs
public class WebhookHandleResult : PaymentProviderResult
{
    public string? EventType { get; set; }
    public string? EventId { get; set; }
    public bool Processed { get; set; }
    public List<string> ActionsPerformed { get; set; } = new();
}

// Authorization Result DTOs
public class PaymentAuthorizationResult : PaymentProviderResult
{
    public string? ExternalSubscriptionId { get; set; }
    public string? PaymentMethodId { get; set; }
    public string? Status { get; set; }
    public DateTime? AuthorizedAt { get; set; }
}
