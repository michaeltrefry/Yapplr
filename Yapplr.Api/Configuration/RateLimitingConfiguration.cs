namespace Yapplr.Api.Configuration;

/// <summary>
/// Configuration for API rate limiting system
/// </summary>
public class RateLimitingConfiguration
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Global enable/disable flag for all rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Enable/disable trust-based rate limiting multipliers
    /// </summary>
    public bool TrustBasedEnabled { get; set; } = true;

    /// <summary>
    /// Enable/disable burst protection
    /// </summary>
    public bool BurstProtectionEnabled { get; set; } = true;

    /// <summary>
    /// Enable/disable automatic user blocking for violations
    /// </summary>
    public bool AutoBlockingEnabled { get; set; } = true;

    /// <summary>
    /// Number of violations in 24 hours before auto-blocking
    /// </summary>
    public int AutoBlockViolationThreshold { get; set; } = 15;

    /// <summary>
    /// Duration in hours for auto-blocking
    /// </summary>
    public int AutoBlockDurationHours { get; set; } = 2;

    /// <summary>
    /// Enable/disable rate limiting for admin users
    /// </summary>
    public bool ApplyToAdmins { get; set; } = false;

    /// <summary>
    /// Enable/disable rate limiting for moderators
    /// </summary>
    public bool ApplyToModerators { get; set; } = false;

    /// <summary>
    /// Multiplier for rate limits when trust-based is disabled (fallback)
    /// </summary>
    public float FallbackMultiplier { get; set; } = 1.0f;
}
