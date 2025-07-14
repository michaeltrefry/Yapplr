namespace Yapplr.Api.DTOs;

public class UserGrowthStatsDto
{
    public List<DailyStatsDto> DailyStats { get; set; } = new();
    public int TotalNewUsers { get; set; }
    public int TotalActiveUsers { get; set; }
    public double GrowthRate { get; set; }
    public int PeakDayNewUsers { get; set; }
    public DateTime PeakDate { get; set; }
}
