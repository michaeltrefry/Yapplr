using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services.Analytics;

public interface ITagAnalyticsService
{
    Task<IEnumerable<TagDto>> GetTrendingTagsAsync(int days = 7, int limit = 10);
    Task<IEnumerable<TagDto>> GetTopTagsAsync(int limit = 20);
    Task<TagAnalyticsDto?> GetTagAnalyticsAsync(string tagName);
    Task<IEnumerable<TagUsageDto>> GetTagUsageOverTimeAsync(string tagName, int days = 30);
    Task UpdateTagMetricsAsync(int tagId, string action, DateTime timestamp);

    // Enhanced trending methods
    Task<IEnumerable<TrendingHashtagDto>> GetTrendingHashtagsWithVelocityAsync(int timeWindow = 24, int limit = 20, string? category = null, string? location = null);
    Task<IEnumerable<CategoryTrendingDto>> GetTrendingHashtagsByCategoryAsync(int timeWindow = 24, int limit = 10);
    Task<PersonalizedTrendingDto> GetPersonalizedTrendingHashtagsAsync(int userId, int timeWindow = 24, int limit = 20);
    Task<TrendingHashtagAnalyticsDto> GetTrendingHashtagAnalyticsAsync(int timeWindow = 24);
}