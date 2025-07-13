using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for rate limiting configuration
/// </summary>
public class RateLimitConfigDto
{
    public bool Enabled { get; set; }
    public bool TrustBasedEnabled { get; set; }
    public bool BurstProtectionEnabled { get; set; }
    public bool AutoBlockingEnabled { get; set; }
    public int AutoBlockViolationThreshold { get; set; }
    public int AutoBlockDurationHours { get; set; }
    public bool ApplyToAdmins { get; set; }
    public bool ApplyToModerators { get; set; }
    public float FallbackMultiplier { get; set; }
}

/// <summary>
/// DTO for updating rate limiting configuration
/// </summary>
public class UpdateRateLimitConfigDto
{
    public bool? Enabled { get; set; }
    public bool? TrustBasedEnabled { get; set; }
    public bool? BurstProtectionEnabled { get; set; }
    public bool? AutoBlockingEnabled { get; set; }
    public int? AutoBlockViolationThreshold { get; set; }
    public int? AutoBlockDurationHours { get; set; }
    public bool? ApplyToAdmins { get; set; }
    public bool? ApplyToModerators { get; set; }
    public float? FallbackMultiplier { get; set; }
    public string? Reason { get; set; }
}

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

/// <summary>
/// DTO for updating user-specific rate limiting settings
/// </summary>
public class UpdateUserRateLimitSettingsDto
{
    public bool? RateLimitingEnabled { get; set; }
    public bool? TrustBasedRateLimitingEnabled { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for blocking/unblocking user rate limits
/// </summary>
public class BlockUserRateLimitDto
{
    public int DurationHours { get; set; } = 2;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for rate limiting violation details
/// </summary>
public class RateLimitViolationDto
{
    public string OperationType { get; set; } = string.Empty;
    public string LimitType { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public DateTime ViolationTime { get; set; }
    public TimeSpan RetryAfter { get; set; }
}

/// <summary>
/// DTO for rate limiting statistics
/// </summary>
public class RateLimitStatsDto
{
    public long TotalRequests { get; set; }
    public long TotalViolations { get; set; }
    public long TotalBlocked { get; set; }
    public int ActiveTrackers { get; set; }
    public int CurrentlyBlockedUsers { get; set; }
    public int RecentViolationsLastHour { get; set; }
    public Dictionary<string, object> BaseRateLimitConfigs { get; set; } = new();
    public List<TopViolatorDto> TopViolators { get; set; } = new();
    public Dictionary<string, int> ViolationsByOperation { get; set; } = new();
    public Dictionary<string, int> ViolationsByHour { get; set; } = new();
}

/// <summary>
/// DTO for top violators
/// </summary>
public class TopViolatorDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int ViolationCount { get; set; }
    public DateTime LastViolation { get; set; }
    public bool IsBlocked { get; set; }
    public float? TrustScore { get; set; }
}
