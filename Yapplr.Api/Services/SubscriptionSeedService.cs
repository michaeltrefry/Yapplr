using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class SubscriptionSeedService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<SubscriptionSeedService> _logger;

    public SubscriptionSeedService(YapplrDbContext context, ILogger<SubscriptionSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedDefaultSubscriptionTiersAsync()
    {
        _logger.LogInformation("🎯 Seeding default subscription tiers...");

        // Check if any subscription tiers already exist
        var existingTiers = await _context.SubscriptionTiers.AnyAsync();
        if (existingTiers)
        {
            _logger.LogInformation("✅ Subscription tiers already exist, skipping seed");
            return;
        }

        var tiers = new List<SubscriptionTier>
        {
            new SubscriptionTier
            {
                Name = "Free",
                Description = "Free tier with advertisements. Perfect for getting started on Yapplr.",
                Price = 0,
                Currency = "USD",
                BillingCycleMonths = 1,
                IsActive = true,
                IsDefault = true,
                SortOrder = 0,
                ShowAdvertisements = true,
                HasVerifiedBadge = false,
                Features = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SubscriptionTier
            {
                Name = "Subscriber",
                Description = "No advertisements and enhanced experience. Support Yapplr while enjoying an ad-free experience.",
                Price = 3.00m,
                Currency = "USD",
                BillingCycleMonths = 1,
                IsActive = true,
                IsDefault = false,
                SortOrder = 1,
                ShowAdvertisements = false,
                HasVerifiedBadge = false,
                Features = "{\"adFree\": true, \"prioritySupport\": false}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SubscriptionTier
            {
                Name = "Verified Creator",
                Description = "No advertisements, verified badge, and creator tools. Perfect for content creators and influencers.",
                Price = 6.00m,
                Currency = "USD",
                BillingCycleMonths = 1,
                IsActive = true,
                IsDefault = false,
                SortOrder = 2,
                ShowAdvertisements = false,
                HasVerifiedBadge = true,
                Features = "{\"adFree\": true, \"verifiedBadge\": true, \"prioritySupport\": true, \"creatorTools\": true}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.SubscriptionTiers.AddRange(tiers);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Successfully seeded {Count} default subscription tiers", tiers.Count);
        
        foreach (var tier in tiers)
        {
            _logger.LogInformation("   - {Name}: {Price:C} / {Cycle} months (Default: {IsDefault})", 
                tier.Name, tier.Price, tier.BillingCycleMonths, tier.IsDefault);
        }
    }

    public async Task AssignDefaultTierToUsersWithoutSubscriptionAsync()
    {
        _logger.LogInformation("🎯 Assigning default subscription tier to users without subscriptions...");

        // Get the default tier
        var defaultTier = await _context.SubscriptionTiers
            .FirstOrDefaultAsync(t => t.IsDefault && t.IsActive);

        if (defaultTier == null)
        {
            _logger.LogWarning("⚠️ No default subscription tier found, skipping user assignment");
            return;
        }

        // Get users without a subscription tier
        var usersWithoutSubscription = await _context.Users
            .Where(u => u.SubscriptionTierId == null)
            .ToListAsync();

        if (!usersWithoutSubscription.Any())
        {
            _logger.LogInformation("✅ All users already have subscription tiers assigned");
            return;
        }

        // Assign default tier to users without subscription
        foreach (var user in usersWithoutSubscription)
        {
            user.SubscriptionTierId = defaultTier.Id;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Assigned default tier '{TierName}' to {UserCount} users", 
            defaultTier.Name, usersWithoutSubscription.Count);
    }

    public async Task SeedSubscriptionDataAsync()
    {
        await SeedDefaultSubscriptionTiersAsync();
        await AssignDefaultTierToUsersWithoutSubscriptionAsync();
    }
}
