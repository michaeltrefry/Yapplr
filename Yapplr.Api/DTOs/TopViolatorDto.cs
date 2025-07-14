namespace Yapplr.Api.DTOs;

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
