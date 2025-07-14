namespace Yapplr.Api.DTOs;

public class ContentStatsDto
{
    public List<DailyStatsDto> DailyPosts { get; set; } = new();
    public List<DailyStatsDto> DailyComments { get; set; } = new();
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public double PostsGrowthRate { get; set; }
    public double CommentsGrowthRate { get; set; }
    public int AveragePostsPerDay { get; set; }
    public int AverageCommentsPerDay { get; set; }
}
