namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for updating user-specific rate limiting settings
/// </summary>
public class UpdateUserRateLimitSettingsDto
{
    public bool? RateLimitingEnabled { get; set; }
    public bool? TrustBasedRateLimitingEnabled { get; set; }
    public string? Reason { get; set; }
}
