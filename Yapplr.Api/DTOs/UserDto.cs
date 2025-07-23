using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record UserDto(
    int Id,
    string Email,
    string Username,
    string Bio,
    DateTime? Birthday,
    string Pronouns,
    string Tagline,
    string ProfileImageFileName,
    DateTime CreatedAt,
    string? FcmToken,
    string? ExpoPushToken,
    bool EmailVerified,
    UserRole Role,
    UserStatus Status,
    DateTime? SuspendedUntil,
    string? SuspensionReason,
    SubscriptionTierDto? SubscriptionTier
);