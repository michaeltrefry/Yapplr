namespace Yapplr.Api.DTOs;

public class ContentTrendsDto
{
    public List<HashtagStatsDto> TrendingHashtags { get; set; } = new();
    public List<DailyStatsDto> EngagementTrends { get; set; } = new();
    public int TotalHashtags { get; set; }
    public double AverageEngagementRate { get; set; }
}
