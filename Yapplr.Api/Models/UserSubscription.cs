using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models;

public class UserSubscription : IEntity
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
    
    [StringLength(255)]
    public string? ExternalSubscriptionId { get; set; } // Provider's subscription ID
    
    [Required]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    
    [Required]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? EndDate { get; set; } // Null for active subscriptions
    
    [Required]
    public DateTime NextBillingDate { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    [StringLength(500)]
    public string? CancellationReason { get; set; }
    
    // Trial period support
    public bool IsTrialPeriod { get; set; } = false;
    public DateTime? TrialEndDate { get; set; }
    
    // Payment method information
    [StringLength(255)]
    public string? PaymentMethodId { get; set; } // Provider's payment method ID
    
    [StringLength(100)]
    public string? PaymentMethodType { get; set; } // "credit_card", "paypal", etc.
    
    [StringLength(50)]
    public string? PaymentMethodLast4 { get; set; } // Last 4 digits for cards
    
    // Billing cycle tracking
    public int BillingCycleCount { get; set; } = 0; // Number of billing cycles completed
    
    // Grace period for failed payments
    public DateTime? GracePeriodEndDate { get; set; }

    // Background service tracking
    public bool TrialProcessed { get; set; } = false; // Whether trial expiration has been processed
    public int RetryCount { get; set; } = 0; // Number of payment retry attempts
    public DateTime? LastRetryDate { get; set; } // Last payment retry attempt
    public DateTime? LastSyncDate { get; set; } // Last time subscription was synced with provider

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}

public enum SubscriptionStatus
{
    Active = 0,
    Cancelled = 1,
    Expired = 2,
    PastDue = 3,
    Suspended = 4,
    Trial = 5,
    PendingCancellation = 6 // Cancelled but still active until end of billing period
}
