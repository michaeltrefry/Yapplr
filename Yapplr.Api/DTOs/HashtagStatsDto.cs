namespace Yapplr.Api.DTOs;

public class HashtagStatsDto
{
    public string Hashtag { get; set; } = string.Empty;
    public int Count { get; set; }
    public double GrowthRate { get; set; }
    public int UniqueUsers { get; set; }
}
