namespace Yapplr.Api.DTOs;

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
