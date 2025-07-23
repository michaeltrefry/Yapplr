namespace Yapplr.Api.DTOs.Payment;

// Subscription Request DTOs
public class CreateSubscriptionRequest
{
    public string PaymentProvider { get; set; } = string.Empty;
    public string? PaymentMethodId { get; set; } // External payment method ID
    public string? ReturnUrl { get; set; } // For providers that require redirect
    public string? CancelUrl { get; set; } // For providers that require redirect
    public bool StartTrial { get; set; } = false;
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CancelSubscriptionRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool CancelImmediately { get; set; } = false; // If false, cancel at end of billing period
    public Dictionary<string, string>? Metadata { get; set; }
}

public class UpdateSubscriptionRequest
{
    public int? NewSubscriptionTierId { get; set; }
    public string? NewPaymentMethodId { get; set; }
    public bool ProrateBilling { get; set; } = true;
    public Dictionary<string, string>? Metadata { get; set; }
}

// Payment Request DTOs
public class ProcessPaymentRequest
{
    public string PaymentProvider { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? PaymentMethodId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
}

public class RefundPaymentRequest
{
    public decimal? Amount { get; set; } // If null, full refund
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
}

// Payment Method Request DTOs
public class CreatePaymentMethodRequest
{
    public string PaymentProvider { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "credit_card", "paypal", etc.
    public string? Token { get; set; } // Provider-specific token
    public bool SetAsDefault { get; set; } = false;
    
    // Card details (if applicable)
    public string? CardNumber { get; set; }
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public string? Cvc { get; set; }
    public string? HolderName { get; set; }
    
    // Billing address
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCountry { get; set; }
    
    public Dictionary<string, string>? Metadata { get; set; }
}

// Webhook Request DTOs
public class WebhookRequest
{
    public string ProviderName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string RawBody { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Authorization Request DTOs
public class PaymentAuthorizationRequest
{
    public int UserId { get; set; }
    public int SubscriptionTierId { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
}

public class PaymentAuthorizationCallbackRequest
{
    public string AuthorizationCode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public Dictionary<string, string> QueryParameters { get; set; } = new();
}
