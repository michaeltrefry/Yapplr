namespace Yapplr.Api.DTOs;

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
