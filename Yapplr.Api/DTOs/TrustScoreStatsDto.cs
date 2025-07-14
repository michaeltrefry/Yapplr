namespace Yapplr.Api.DTOs;

public class TrustScoreStatsDto
{
    public int TotalUsers { get; set; }
    public float AverageScore { get; set; }
    public float MedianScore { get; set; }
    public float MinScore { get; set; }
    public float MaxScore { get; set; }
    public Dictionary<string, int> Distribution { get; set; } = new();
}
