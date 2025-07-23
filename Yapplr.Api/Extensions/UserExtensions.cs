using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Extensions;

public static class UserExtensions
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.Username,
            user.Bio,
            user.Birthday,
            user.Pronouns,
            user.Tagline,
            user.ProfileImageFileName,
            user.CreatedAt,
            user.FcmToken,
            user.ExpoPushToken,
            user.EmailVerified,
            user.Role,
            user.Status,
            user.SuspendedUntil,
            user.SuspensionReason,
            user.SubscriptionTier?.ToDto()
        );
    }

    public static SubscriptionTierDto ToDto(this SubscriptionTier tier)
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
