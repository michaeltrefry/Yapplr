namespace Yapplr.Api.DTOs;

public class ModerationTrendsDto
{
    public List<DailyStatsDto> DailyActions { get; set; } = new();
    public List<ActionTypeStatsDto> ActionBreakdown { get; set; } = new();
    public int TotalActions { get; set; }
    public double ActionsGrowthRate { get; set; }
    public int PeakDayActions { get; set; }
    public DateTime PeakDate { get; set; }
}
