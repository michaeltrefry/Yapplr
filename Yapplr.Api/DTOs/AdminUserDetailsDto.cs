using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class AdminUserDetailsDto
{
    // Basic Profile Information
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
    public string Pronouns { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? ProfileImageFileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? LastLoginIp { get; set; }
    public bool EmailVerified { get; set; }
    public bool TermsAccepted { get; set; }
    public DateTime? TermsAcceptedAt { get; set; }

    // Account Status
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public DateTime? SuspendedUntil { get; set; }
    public string? SuspensionReason { get; set; }
    public string? SuspendedByUsername { get; set; }

    // Activity Statistics
    public int PostCount { get; set; }
    public int CommentCount { get; set; }
    public int LikeCount { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }

    // Trust and Moderation
    public float TrustScore { get; set; }
    public TrustScoreFactorsDto? TrustScoreFactors { get; set; }
    public List<TrustScoreHistoryDto> RecentTrustScoreHistory { get; set; } = new();
    public int ReportCount { get; set; }
    public int ModerationActionCount { get; set; }

    // Rate Limiting Settings
    public bool? RateLimitingEnabled { get; set; }
    public bool? TrustBasedRateLimitingEnabled { get; set; }
    public bool IsCurrentlyBlocked { get; set; }
    public bool IsCurrentlyRateLimited { get; set; }
    public DateTime? BlockedUntil { get; set; }
    public DateTime? RateLimitedUntil { get; set; }
    public int RecentViolations { get; set; }
    public int RecentRateLimitViolations { get; set; }

    // Recent Moderation Actions (last 10)
    public List<AuditLogDto> RecentModerationActions { get; set; } = new();
}
