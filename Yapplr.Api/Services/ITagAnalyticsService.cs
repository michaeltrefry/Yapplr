using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public interface ITagAnalyticsService
{
    Task<IEnumerable<TagDto>> GetTrendingTagsAsync(int days = 7, int limit = 10);
    Task<IEnumerable<TagDto>> GetTopTagsAsync(int limit = 20);
    Task<TagAnalyticsDto?> GetTagAnalyticsAsync(string tagName);
    Task<IEnumerable<TagUsageDto>> GetTagUsageOverTimeAsync(string tagName, int days = 30);
    Task UpdateTagMetricsAsync(int tagId, string action, DateTime timestamp);
}

public record TagAnalyticsDto(
    string Name,
    int TotalPosts,
    int PostsThisWeek,
    int PostsThisMonth,
    DateTime FirstUsed,
    DateTime LastUsed,
    int UniqueUsers
);

public record TagUsageDto(
    DateTime Date,
    int PostCount
);
