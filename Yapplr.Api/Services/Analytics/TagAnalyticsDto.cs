namespace Yapplr.Api.Services.Analytics;

public record TagAnalyticsDto(
    string Name,
    int TotalPosts,
    int PostsThisWeek,
    int PostsThisMonth,
    DateTime FirstUsed,
    DateTime LastUsed,
    int UniqueUsers
);