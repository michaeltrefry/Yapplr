namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for blocking/unblocking user rate limits
/// </summary>
public class BlockUserRateLimitDto
{
    public int DurationHours { get; set; } = 2;
    public string Reason { get; set; } = string.Empty;
}
