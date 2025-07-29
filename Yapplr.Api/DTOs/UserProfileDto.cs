namespace Yapplr.Api.DTOs;

public record UserProfileDto(
    int Id,
    string Username,
    string Bio,
    DateTime? Birthday,
    string Pronouns,
    string Tagline,
    string? ProfileImageUrl,
    DateTime CreatedAt,
    int PostCount,
    int FollowerCount,
    int FollowingCount,
    bool IsFollowedByCurrentUser,
    bool HasPendingFollowRequest = false,
    bool RequiresFollowApproval = false,
    SubscriptionTierDto? SubscriptionTier = null
);