namespace Yapplr.Api.DTOs;

public class DailyStatsDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public string Label { get; set; } = string.Empty;
}
