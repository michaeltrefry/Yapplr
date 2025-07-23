using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(YapplrDbContext context, ILogger<SubscriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<SubscriptionTierDto>> GetAllSubscriptionTiersAsync(bool includeInactive = false)
    {
        var query = _context.SubscriptionTiers.AsQueryable();
        
        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }
        
        var tiers = await query
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Price)
            .ToListAsync();
            
        return tiers.Select(MapToDto);
    }

    public async Task<IEnumerable<SubscriptionTierDto>> GetActiveSubscriptionTiersAsync()
    {
        return await GetAllSubscriptionTiersAsync(includeInactive: false);
    }

    public async Task<SubscriptionTierDto?> GetSubscriptionTierAsync(int id)
    {
        var tier = await _context.SubscriptionTiers.FindAsync(id);
        return tier == null ? null : MapToDto(tier);
    }

    public async Task<SubscriptionTierDto> CreateSubscriptionTierAsync(CreateSubscriptionTierDto createDto)
    {
        // If this is being set as default, unset any existing default
        if (createDto.IsDefault)
        {
            await UnsetExistingDefaultTierAsync();
        }

        var tier = new SubscriptionTier
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Price = createDto.Price,
            Currency = createDto.Currency,
            BillingCycleMonths = createDto.BillingCycleMonths,
            IsActive = createDto.IsActive,
            IsDefault = createDto.IsDefault,
            SortOrder = createDto.SortOrder,
            ShowAdvertisements = createDto.ShowAdvertisements,
            HasVerifiedBadge = createDto.HasVerifiedBadge,
            Features = createDto.Features,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SubscriptionTiers.Add(tier);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created subscription tier {TierName} with ID {TierId}", tier.Name, tier.Id);
        
        return MapToDto(tier);
    }

    public async Task<SubscriptionTierDto?> UpdateSubscriptionTierAsync(int id, UpdateSubscriptionTierDto updateDto)
    {
        var tier = await _context.SubscriptionTiers.FindAsync(id);
        if (tier == null)
        {
            return null;
        }

        // If this is being set as default, unset any existing default
        if (updateDto.IsDefault && !tier.IsDefault)
        {
            await UnsetExistingDefaultTierAsync();
        }

        tier.Name = updateDto.Name;
        tier.Description = updateDto.Description;
        tier.Price = updateDto.Price;
        tier.Currency = updateDto.Currency;
        tier.BillingCycleMonths = updateDto.BillingCycleMonths;
        tier.IsActive = updateDto.IsActive;
        tier.IsDefault = updateDto.IsDefault;
        tier.SortOrder = updateDto.SortOrder;
        tier.ShowAdvertisements = updateDto.ShowAdvertisements;
        tier.HasVerifiedBadge = updateDto.HasVerifiedBadge;
        tier.Features = updateDto.Features;
        tier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated subscription tier {TierName} with ID {TierId}", tier.Name, tier.Id);
        
        return MapToDto(tier);
    }

    public async Task<bool> DeleteSubscriptionTierAsync(int id)
    {
        var tier = await _context.SubscriptionTiers.FindAsync(id);
        if (tier == null)
        {
            return false;
        }

        // Check if any users are assigned to this tier
        var userCount = await _context.Users.CountAsync(u => u.SubscriptionTierId == id);
        if (userCount > 0)
        {
            _logger.LogWarning("Cannot delete subscription tier {TierId} - {UserCount} users are assigned to it", id, userCount);
            return false;
        }

        _context.SubscriptionTiers.Remove(tier);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted subscription tier {TierName} with ID {TierId}", tier.Name, tier.Id);
        
        return true;
    }

    public async Task<SubscriptionTierDto?> GetDefaultSubscriptionTierAsync()
    {
        var defaultTier = await _context.SubscriptionTiers
            .FirstOrDefaultAsync(t => t.IsDefault && t.IsActive);
            
        return defaultTier == null ? null : MapToDto(defaultTier);
    }

    public async Task<UserSubscriptionDto?> GetUserSubscriptionAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.SubscriptionTier)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user == null)
        {
            return null;
        }

        return new UserSubscriptionDto
        {
            UserId = user.Id,
            Username = user.Username,
            SubscriptionTier = user.SubscriptionTier == null ? null : MapToDto(user.SubscriptionTier)
        };
    }

    public async Task<bool> AssignSubscriptionTierAsync(int userId, int subscriptionTierId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        var tier = await _context.SubscriptionTiers.FindAsync(subscriptionTierId);
        if (tier == null || !tier.IsActive)
        {
            return false;
        }

        user.SubscriptionTierId = subscriptionTierId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned subscription tier {TierId} to user {UserId}", subscriptionTierId, userId);
        
        return true;
    }

    public async Task<bool> RemoveUserSubscriptionAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.SubscriptionTierId = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed subscription tier from user {UserId}", userId);
        
        return true;
    }

    public async Task<bool> EnsureDefaultTierExistsAsync()
    {
        var hasDefault = await _context.SubscriptionTiers.AnyAsync(t => t.IsDefault && t.IsActive);
        if (hasDefault)
        {
            return true;
        }

        // Create default free tier if none exists
        var freeTier = new SubscriptionTier
        {
            Name = "Free",
            Description = "Free tier with advertisements",
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
        };

        _context.SubscriptionTiers.Add(freeTier);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created default free subscription tier");
        
        return true;
    }

    public async Task<int> GetUserCountByTierAsync(int subscriptionTierId)
    {
        return await _context.Users.CountAsync(u => u.SubscriptionTierId == subscriptionTierId);
    }

    private async Task UnsetExistingDefaultTierAsync()
    {
        var existingDefault = await _context.SubscriptionTiers
            .FirstOrDefaultAsync(t => t.IsDefault);
            
        if (existingDefault != null)
        {
            existingDefault.IsDefault = false;
            existingDefault.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static SubscriptionTierDto MapToDto(SubscriptionTier tier)
    {
        return new SubscriptionTierDto
        {
            Id = tier.Id,
            Name = tier.Name,
            Description = tier.Description,
            Price = tier.Price,
            Currency = tier.Currency,
            BillingCycleMonths = tier.BillingCycleMonths,
            IsActive = tier.IsActive,
            IsDefault = tier.IsDefault,
            SortOrder = tier.SortOrder,
            ShowAdvertisements = tier.ShowAdvertisements,
            HasVerifiedBadge = tier.HasVerifiedBadge,
            Features = tier.Features,
            CreatedAt = tier.CreatedAt,
            UpdatedAt = tier.UpdatedAt
        };
    }
}
