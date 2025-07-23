using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models;

public class PaymentMethod : IEntity
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [Required]
    [StringLength(50)]
    public string PaymentProvider { get; set; } = string.Empty; // "PayPal", "Stripe", etc.
    
    [Required]
    [StringLength(255)]
    public string ExternalPaymentMethodId { get; set; } = string.Empty; // Provider's payment method ID
    
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty; // "credit_card", "debit_card", "paypal", "bank_account"
    
    [StringLength(100)]
    public string? Brand { get; set; } // "visa", "mastercard", "amex", etc.
    
    [StringLength(10)]
    public string? Last4 { get; set; } // Last 4 digits for cards
    
    [StringLength(4)]
    public string? ExpiryMonth { get; set; } // MM format
    
    [StringLength(4)]
    public string? ExpiryYear { get; set; } // YYYY format
    
    [StringLength(100)]
    public string? HolderName { get; set; }
    
    [StringLength(100)]
    public string? BillingEmail { get; set; }
    
    // Address information
    [StringLength(200)]
    public string? BillingAddress { get; set; }
    
    [StringLength(100)]
    public string? BillingCity { get; set; }
    
    [StringLength(50)]
    public string? BillingState { get; set; }
    
    [StringLength(20)]
    public string? BillingPostalCode { get; set; }
    
    [StringLength(10)]
    public string? BillingCountry { get; set; }
    
    public bool IsDefault { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime? VerifiedAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; } // For cards, when they expire
    
    [StringLength(1000)]
    public string? ProviderData { get; set; } // JSON data from provider
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
}
