using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models;

public class SubscriptionTier : IEntity
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public decimal Price { get; set; }
    
    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "USD";
    
    [Required]
    public int BillingCycleMonths { get; set; } = 1; // 1 = monthly, 12 = yearly, etc.
    
    public bool IsActive { get; set; } = true;
    
    public bool IsDefault { get; set; } = false; // Only one tier should be default
    
    public int SortOrder { get; set; } = 0; // For ordering tiers in UI
    
    // Feature flags - these will be used for future features
    public bool ShowAdvertisements { get; set; } = true;
    public bool HasVerifiedBadge { get; set; } = false;
    
    // Additional features can be added here as JSON or separate properties
    [StringLength(2000)]
    public string? Features { get; set; } // JSON string for flexible feature storage
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}
