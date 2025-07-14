namespace Yapplr.Api.DTOs;

public class UserEngagementStatsDto
{
    public List<DailyStatsDto> DailyEngagement { get; set; } = new();
    public double AverageSessionDuration { get; set; }
    public int TotalSessions { get; set; }
    public double RetentionRate { get; set; }
    public List<EngagementTypeStatsDto> EngagementBreakdown { get; set; } = new();
}
