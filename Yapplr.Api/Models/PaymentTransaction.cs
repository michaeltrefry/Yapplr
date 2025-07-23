using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models;

public class PaymentTransaction : IEntity
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [Required]
    public int SubscriptionTierId { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; } = null!;
    
    [Required]
    [StringLength(50)]
    public string PaymentProvider { get; set; } = string.Empty; // "PayPal", "Stripe", etc.
    
    [Required]
    [StringLength(255)]
    public string ExternalTransactionId { get; set; } = string.Empty; // Provider's transaction ID
    
    [StringLength(255)]
    public string? ExternalSubscriptionId { get; set; } // Provider's subscription ID
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "USD";
    
    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    [Required]
    public PaymentType Type { get; set; } = PaymentType.Subscription;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    // Billing period this payment covers
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    
    // Payment processing details
    public DateTime? ProcessedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    
    [StringLength(1000)]
    public string? FailureReason { get; set; }
    
    [StringLength(2000)]
    public string? ProviderResponse { get; set; } // JSON response from provider
    
    // Webhook verification
    [StringLength(255)]
    public string? WebhookEventId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public UserSubscription? UserSubscription { get; set; }
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}

public enum PaymentType
{
    Subscription = 0,
    OneTime = 1,
    Refund = 2,
    Upgrade = 3,
    Downgrade = 4
}
