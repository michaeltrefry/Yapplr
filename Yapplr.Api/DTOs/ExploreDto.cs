namespace Yapplr.Api.DTOs;

/// <summary>
/// Comprehensive explore page response with all discovery content
/// </summary>
public record ExplorePageDto(
    IEnumerable<PostDto> TrendingPosts,
    IEnumerable<TrendingHashtagDto> TrendingHashtags,
    IEnumerable<CategoryTrendingDto> TrendingCategories,
    IEnumerable<UserRecommendationDto> RecommendedUsers,
    IEnumerable<PostDto> PersonalizedPosts,
    ExploreMetricsDto Metrics,
    DateTime GeneratedAt
);

/// <summary>
/// User recommendation with similarity scoring
/// </summary>
public record UserRecommendationDto(
    UserDto User,
    double SimilarityScore,
    string RecommendationReason,
    IEnumerable<string> CommonInterests,
    IEnumerable<UserDto> MutualFollows,
    bool IsNewUser,
    double ActivityScore
);

/// <summary>
/// Content cluster for topic-based discovery
/// </summary>
public record ContentClusterDto(
    string Topic,
    string Description,
    IEnumerable<PostDto> Posts,
    IEnumerable<TrendingHashtagDto> RelatedHashtags,
    IEnumerable<UserDto> TopContributors,
    double ClusterScore,
    int TotalPosts
);

/// <summary>
/// Similar user discovery based on interaction patterns
/// </summary>
public record SimilarUserDto(
    UserDto User,
    double SimilarityScore,
    IEnumerable<string> SharedInterests,
    IEnumerable<UserDto> MutualConnections,
    string SimilarityReason
);

/// <summary>
/// Interest-based content recommendations
/// </summary>
public record InterestBasedContentDto(
    string Interest,
    IEnumerable<PostDto> RecommendedPosts,
    IEnumerable<UserDto> TopCreators,
    double InterestStrength,
    bool IsGrowing
);

/// <summary>
/// Explore page metrics and performance data
/// </summary>
public record ExploreMetricsDto(
    int TotalTrendingPosts,
    int TotalTrendingHashtags,
    int TotalRecommendedUsers,
    double AverageEngagementRate,
    double PersonalizationScore,
    TimeSpan GenerationTime,
    string AlgorithmVersion
);

/// <summary>
/// Personalized explore configuration
/// </summary>
public record ExploreConfigDto(
    int TrendingPostsLimit,
    int TrendingHashtagsLimit,
    int RecommendedUsersLimit,
    int TimeWindowHours,
    bool IncludePersonalizedContent,
    bool IncludeUserRecommendations,
    IEnumerable<string> PreferredCategories,
    double MinSimilarityScore
);

/// <summary>
/// Explore section for modular content display
/// </summary>
public record ExploreSectionDto(
    string SectionType,
    string Title,
    string Description,
    object Content,
    int Priority,
    bool IsPersonalized
);

/// <summary>
/// Trending topic with cross-content analysis
/// </summary>
public record TrendingTopicDto(
    string Topic,
    IEnumerable<PostDto> TrendingPosts,
    IEnumerable<TrendingHashtagDto> RelatedHashtags,
    IEnumerable<UserDto> TopContributors,
    double TopicScore,
    double GrowthRate,
    string Category
);

/// <summary>
/// User discovery based on network analysis
/// </summary>
public record NetworkBasedUserDto(
    UserDto User,
    double NetworkScore,
    IEnumerable<UserDto> ConnectionPath,
    string DiscoveryMethod,
    int DegreesOfSeparation
);

/// <summary>
/// Content recommendation with explanation
/// </summary>
public record ExplainedContentDto(
    PostDto Post,
    double RecommendationScore,
    string Explanation,
    IEnumerable<string> ReasonTags,
    bool IsPersonalized
);
