using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for user-specific rate limiting settings
/// </summary>
public class UserRateLimitSettingsDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool? RateLimitingEnabled { get; set; }
    public bool? TrustBasedRateLimitingEnabled { get; set; }
    public bool IsCurrentlyBlocked { get; set; }
    public DateTime? BlockedUntil { get; set; }
    public int RecentViolations { get; set; }
    public float? TrustScore { get; set; }
    public DateTime? LastActivity { get; set; }
}
