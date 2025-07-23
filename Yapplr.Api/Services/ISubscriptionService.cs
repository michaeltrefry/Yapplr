using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public interface ISubscriptionService
{
    // Subscription Tier Management
    Task<IEnumerable<SubscriptionTierDto>> GetAllSubscriptionTiersAsync(bool includeInactive = false);
    Task<IEnumerable<SubscriptionTierDto>> GetActiveSubscriptionTiersAsync();
    Task<SubscriptionTierDto?> GetSubscriptionTierAsync(int id);
    Task<SubscriptionTierDto> CreateSubscriptionTierAsync(CreateSubscriptionTierDto createDto);
    Task<SubscriptionTierDto?> UpdateSubscriptionTierAsync(int id, UpdateSubscriptionTierDto updateDto);
    Task<bool> DeleteSubscriptionTierAsync(int id);
    Task<SubscriptionTierDto?> GetDefaultSubscriptionTierAsync();
    
    // User Subscription Management
    Task<UserSubscriptionDto?> GetUserSubscriptionAsync(int userId);
    Task<bool> AssignSubscriptionTierAsync(int userId, int subscriptionTierId);
    Task<bool> RemoveUserSubscriptionAsync(int userId);
    
    // Utility Methods
    Task<bool> EnsureDefaultTierExistsAsync();
    Task<int> GetUserCountByTierAsync(int subscriptionTierId);
}
