namespace Yapplr.Api.DTOs;

/// <summary>
/// Enhanced trending hashtag DTO with velocity and quality metrics
/// </summary>
public record TrendingHashtagDto(
    string Name,
    int PostCount,
    int PreviousPeriodCount,
    double Velocity,
    double TrendingScore,
    int TotalEngagements,
    int UniqueUsers,
    float AverageUserTrustScore,
    string Category,
    double GrowthRate,
    double EngagementRate,
    double DiversityScore
);

/// <summary>
/// Geographic trending hashtag data
/// </summary>
public record GeographicTrendingDto(
    string Location,
    IEnumerable<TrendingHashtagDto> TrendingHashtags,
    int TotalPosts,
    double AverageEngagementRate
);

/// <summary>
/// Category-based trending hashtag data
/// </summary>
public record CategoryTrendingDto(
    string Category,
    IEnumerable<TrendingHashtagDto> TrendingHashtags,
    int TotalPosts,
    double CategoryGrowthRate,
    string? Description
);

/// <summary>
/// Personalized trending hashtags based on user interests
/// </summary>
public record PersonalizedTrendingDto(
    int UserId,
    IEnumerable<TrendingHashtagDto> RecommendedHashtags,
    IEnumerable<string> UserInterests,
    double PersonalizationScore,
    DateTime GeneratedAt
);

/// <summary>
/// Comprehensive trending analytics response
/// </summary>
public record TrendingHashtagAnalyticsDto(
    DateTime AnalyzedAt,
    int TimeWindowHours,
    IEnumerable<TrendingHashtagDto> GlobalTrending,
    IEnumerable<CategoryTrendingDto> CategoryTrending,
    IEnumerable<GeographicTrendingDto> GeographicTrending,
    TrendingHashtagStatsDto Stats
);

/// <summary>
/// Overall trending hashtag statistics
/// </summary>
public record TrendingHashtagStatsDto(
    int TotalTrendingHashtags,
    int TotalPosts,
    double AverageVelocity,
    double AverageEngagementRate,
    int UniqueCategories,
    string TopCategory,
    double OverallGrowthRate
);
