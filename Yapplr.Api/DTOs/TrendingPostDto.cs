namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for trending posts with detailed scoring information
/// </summary>
public record TrendingPostDto(
    PostDto Post,
    TrendingScoreDto Score,
    DateTime CalculatedAt
);

/// <summary>
/// DTO for trending score breakdown
/// </summary>
public record TrendingScoreDto(
    double TotalScore,
    double EngagementVelocityScore,
    double RecencyScore,
    double QualityScore,
    double TrustScore,
    double DiversityScore,
    TrendingScoreBreakdownDto Breakdown
);

/// <summary>
/// Detailed breakdown of trending score components
/// </summary>
public record TrendingScoreBreakdownDto(
    // Engagement metrics
    int TotalEngagements,
    int LikesCount,
    int CommentsCount,
    int RepostsCount,
    int ViewsCount,
    double EngagementRate,
    double EngagementVelocity,
    
    // Quality metrics
    double AuthorTrustScore,
    double ContentQualityScore,
    double SpamProbability,
    
    // Timing metrics
    DateTime PostCreatedAt,
    double HoursSinceCreation,
    double RecencyMultiplier,
    
    // Diversity metrics
    bool HasMedia,
    bool HasHashtags,
    bool HasLinks,
    int HashtagCount,
    
    // Personalization (if applicable)
    bool IsFromFollowedUser,
    bool MatchesUserInterests,
    double PersonalizationBoost
);

/// <summary>
/// DTO for trending analytics dashboard
/// </summary>
public record TrendingAnalyticsDto(
    DateTime AnalyzedAt,
    int TimeWindowHours,
    TrendingStatsDto Stats,
    IEnumerable<TrendingCategoryDto> TopCategories,
    IEnumerable<TrendingAuthorDto> TopAuthors,
    TrendingMetricsDto Metrics
);

/// <summary>
/// Overall trending statistics
/// </summary>
public record TrendingStatsDto(
    int TotalTrendingPosts,
    int TotalEngagements,
    double AverageEngagementRate,
    double AverageTrendingScore,
    int UniqueAuthors,
    int UniqueHashtags
);

/// <summary>
/// Trending category information
/// </summary>
public record TrendingCategoryDto(
    string Hashtag,
    int PostCount,
    int TotalEngagements,
    double AverageScore,
    double GrowthRate
);

/// <summary>
/// Trending author information
/// </summary>
public record TrendingAuthorDto(
    UserDto Author,
    int TrendingPostsCount,
    double AverageTrendingScore,
    int TotalEngagements
);

/// <summary>
/// Trending metrics for performance monitoring
/// </summary>
public record TrendingMetricsDto(
    double CalculationTimeMs,
    int PostsAnalyzed,
    int PostsFiltered,
    double FilterRate,
    DateTime LastCalculation
);
