namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for topic information
/// </summary>
public record TopicDto(
    int Id,
    string Name,
    string Description,
    string Category,
    IEnumerable<string> RelatedHashtags,
    string Slug,
    string? Icon,
    string? Color,
    bool IsFeatured,
    int FollowerCount,
    bool IsFollowedByCurrentUser,
    DateTime CreatedAt
);

/// <summary>
/// DTO for user's topic follow preferences
/// </summary>
public record TopicFollowDto(
    int Id,
    int UserId,
    string TopicName,
    string? TopicDescription,
    string Category,
    IEnumerable<string> RelatedHashtags,
    float InterestLevel,
    bool IncludeInMainFeed,
    bool EnableNotifications,
    float NotificationThreshold,
    DateTime CreatedAt
);

/// <summary>
/// DTO for topic-based feed content
/// </summary>
public record TopicFeedDto(
    string TopicName,
    string Category,
    IEnumerable<PostDto> Posts,
    IEnumerable<TrendingHashtagDto> TrendingHashtags,
    IEnumerable<UserDto> TopContributors,
    TopicFeedMetricsDto Metrics,
    DateTime GeneratedAt
);

/// <summary>
/// DTO for topic feed metrics
/// </summary>
public record TopicFeedMetricsDto(
    int TotalPosts,
    int TotalEngagement,
    int UniqueContributors,
    float AvgEngagementRate,
    float TrendingScore,
    float GrowthRate,
    TimeSpan GenerationTime
);

/// <summary>
/// DTO for creating or updating topic follows
/// </summary>
public record CreateTopicFollowDto(
    string TopicName,
    string? TopicDescription,
    string Category,
    IEnumerable<string> RelatedHashtags,
    float InterestLevel = 1.0f,
    bool IncludeInMainFeed = true,
    bool EnableNotifications = false,
    float NotificationThreshold = 0.7f
);

/// <summary>
/// DTO for updating topic follow preferences
/// </summary>
public record UpdateTopicFollowDto(
    float? InterestLevel,
    bool? IncludeInMainFeed,
    bool? EnableNotifications,
    float? NotificationThreshold
);

/// <summary>
/// DTO for topic discovery and recommendations
/// </summary>
public record TopicRecommendationDto(
    TopicDto Topic,
    double RecommendationScore,
    string RecommendationReason,
    IEnumerable<string> MatchingInterests,
    IEnumerable<PostDto> SamplePosts,
    bool IsPersonalized
);

/// <summary>
/// DTO for topic analytics
/// </summary>
public record TopicAnalyticsDto(
    string TopicName,
    string Category,
    DateTime AnalyticsDate,
    int PostCount,
    int TotalEngagement,
    int UniquePosters,
    float AvgEngagementRate,
    float TrendingScore,
    float GrowthRate,
    IEnumerable<TrendingHashtagDto> TopHashtags
);

/// <summary>
/// DTO for personalized topic feed
/// </summary>
public record PersonalizedTopicFeedDto(
    int UserId,
    IEnumerable<TopicFeedDto> TopicFeeds,
    IEnumerable<PostDto> MixedFeed,
    PersonalizedFeedMetricsDto Metrics,
    DateTime GeneratedAt
);

/// <summary>
/// DTO for personalized feed metrics
/// </summary>
public record PersonalizedFeedMetricsDto(
    int TotalTopicsFollowed,
    int TotalPosts,
    float PersonalizationScore,
    IEnumerable<string> ActiveTopics,
    TimeSpan GenerationTime
);

/// <summary>
/// DTO for topic clustering results
/// </summary>
public record TopicClusterDto(
    string ClusterName,
    IEnumerable<string> RelatedTopics,
    IEnumerable<string> CommonHashtags,
    float SimilarityScore,
    int TotalPosts,
    string Category
);

/// <summary>
/// DTO for topic trending analysis
/// </summary>
public record TopicTrendingDto(
    string TopicName,
    string Category,
    float CurrentTrendingScore,
    float PreviousTrendingScore,
    float VelocityScore,
    int CurrentPosts,
    int PreviousPosts,
    IEnumerable<TrendingHashtagDto> DrivingHashtags,
    DateTime AnalyzedAt
);

/// <summary>
/// DTO for topic feed configuration
/// </summary>
public record TopicFeedConfigDto(
    int PostsPerTopic,
    int MaxTopics,
    bool IncludeTrendingContent,
    bool IncludePersonalizedContent,
    float MinInterestLevel,
    int TimeWindowHours,
    string SortBy // "trending", "recent", "engagement", "personalized"
);

/// <summary>
/// DTO for bulk topic operations
/// </summary>
public record BulkTopicOperationDto(
    IEnumerable<string> TopicNames,
    string Operation, // "follow", "unfollow", "update_preferences"
    object? OperationData
);

/// <summary>
/// DTO for topic search results
/// </summary>
public record TopicSearchResultDto(
    IEnumerable<TopicDto> ExactMatches,
    IEnumerable<TopicDto> PartialMatches,
    IEnumerable<TopicRecommendationDto> Recommendations,
    IEnumerable<string> SuggestedHashtags,
    int TotalResults
);

/// <summary>
/// DTO for topic statistics
/// </summary>
public record TopicStatsDto(
    string TopicName,
    int TotalFollowers,
    int PostsToday,
    int PostsThisWeek,
    int PostsThisMonth,
    float AvgDailyPosts,
    float EngagementRate,
    IEnumerable<UserDto> TopContributors,
    DateTime LastUpdated
);
