namespace Yapplr.Api.DTOs;

public class SubscriptionTierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int BillingCycleMonths { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public bool ShowAdvertisements { get; set; }
    public bool HasVerifiedBadge { get; set; }
    public string? Features { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSubscriptionTierDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int BillingCycleMonths { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public bool ShowAdvertisements { get; set; } = true;
    public bool HasVerifiedBadge { get; set; } = false;
    public string? Features { get; set; }
}

public class UpdateSubscriptionTierDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int BillingCycleMonths { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public bool ShowAdvertisements { get; set; }
    public bool HasVerifiedBadge { get; set; }
    public string? Features { get; set; }
}

public class UserSubscriptionDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public SubscriptionTierDto? SubscriptionTier { get; set; }
}

public class AssignSubscriptionTierDto
{
    public int SubscriptionTierId { get; set; }
}
