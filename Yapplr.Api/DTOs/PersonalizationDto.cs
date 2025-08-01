namespace Yapplr.Api.DTOs;

/// <summary>
/// User personalization profile DTO
/// </summary>
public record UserPersonalizationProfileDto(
    int UserId,
    Dictionary<string, float> InterestScores,
    Dictionary<string, float> ContentTypePreferences,
    Dictionary<string, float> EngagementPatterns,
    Dictionary<string, float> SimilarUsers,
    float PersonalizationConfidence,
    float DiversityPreference,
    float NoveltyPreference,
    float SocialInfluenceFactor,
    float QualityThreshold,
    DateTime LastMLUpdate,
    int DataPointCount,
    string AlgorithmVersion
);

/// <summary>
/// Personalized content recommendation with explanation
/// </summary>
public record PersonalizedRecommendationDto(
    object Content, // Can be PostDto, UserDto, TopicDto, etc.
    string ContentType,
    double RecommendationScore,
    string PrimaryReason,
    IEnumerable<string> ReasonTags,
    Dictionary<string, float> ScoreBreakdown,
    float ConfidenceLevel,
    bool IsExperimental,
    DateTime GeneratedAt
);

/// <summary>
/// Personalization insights for user dashboard
/// </summary>
public record PersonalizationInsightsDto(
    int UserId,
    IEnumerable<InterestInsightDto> TopInterests,
    IEnumerable<ContentTypeInsightDto> ContentPreferences,
    IEnumerable<EngagementPatternDto> EngagementPatterns,
    IEnumerable<UserSimilarityDto> SimilarUsers,
    PersonalizationStatsDto Stats,
    IEnumerable<string> RecommendationTips,
    DateTime GeneratedAt
);

/// <summary>
/// Interest insight with trend information
/// </summary>
public record InterestInsightDto(
    string Interest,
    float Score,
    float TrendDirection, // -1.0 to 1.0 (decreasing to increasing)
    int PostCount,
    int EngagementCount,
    string Category,
    bool IsGrowing
);

/// <summary>
/// Content type preference insight
/// </summary>
public record ContentTypeInsightDto(
    string ContentType,
    float PreferenceScore,
    float EngagementRate,
    int ViewCount,
    int InteractionCount,
    TimeSpan AverageViewTime
);

/// <summary>
/// Engagement pattern insight
/// </summary>
public record EngagementPatternDto(
    string TimeOfDay,
    float EngagementScore,
    int ActivityCount,
    IEnumerable<string> PreferredContentTypes,
    float AverageSessionDuration
);

/// <summary>
/// User similarity insight
/// </summary>
public record UserSimilarityDto(
    UserDto SimilarUser,
    float SimilarityScore,
    IEnumerable<string> CommonInterests,
    IEnumerable<string> SharedFollows,
    string SimilarityReason
);

/// <summary>
/// Personalization statistics
/// </summary>
public record PersonalizationStatsDto(
    float OverallConfidence,
    int TotalInteractions,
    int UniqueInterests,
    int SimilarUsersCount,
    float DiversityScore,
    float NoveltyScore,
    DateTime ProfileCreatedAt,
    DateTime LastUpdated
);

/// <summary>
/// Personalized feed configuration
/// </summary>
public record PersonalizedFeedConfigDto(
    int UserId,
    int PostLimit,
    float DiversityWeight,
    float NoveltyWeight,
    float SocialWeight,
    float QualityThreshold,
    bool IncludeExperimental,
    IEnumerable<string> PreferredContentTypes,
    IEnumerable<string> ExcludedTopics,
    string FeedType // "main", "discover", "following", "topics"
);

/// <summary>
/// Interaction event for tracking user behavior
/// </summary>
public record UserInteractionEventDto(
    int UserId,
    string InteractionType,
    string? TargetEntityType,
    int? TargetEntityId,
    float InteractionStrength,
    int? DurationMs,
    string? Context,
    string? DeviceInfo,
    string? SessionId,
    bool IsImplicit,
    float Sentiment
);

/// <summary>
/// Personalization experiment DTO
/// </summary>
public record PersonalizationExperimentDto(
    int Id,
    string Name,
    string Description,
    Dictionary<string, object> Configuration,
    float TrafficAllocation,
    bool IsActive,
    DateTime StartDate,
    DateTime? EndDate,
    int ParticipantCount
);

/// <summary>
/// User's experiment participation
/// </summary>
public record UserExperimentParticipationDto(
    int ExperimentId,
    string ExperimentName,
    string Variant,
    DateTime AssignedAt,
    bool IsActive
);

/// <summary>
/// Content similarity result
/// </summary>
public record ContentSimilarityDto(
    string ContentType,
    int ContentId,
    object Content,
    float SimilarityScore,
    IEnumerable<string> SimilarityReasons,
    DateTime CalculatedAt
);

/// <summary>
/// Personalization algorithm performance metrics
/// </summary>
public record PersonalizationMetricsDto(
    string AlgorithmVersion,
    float AverageConfidence,
    float EngagementLift,
    float DiversityScore,
    float NoveltyScore,
    int TotalUsers,
    int ActiveUsers,
    DateTime CalculatedAt,
    Dictionary<string, float> PerformanceBreakdown
);

/// <summary>
/// Real-time personalization update
/// </summary>
public record PersonalizationUpdateDto(
    int UserId,
    string UpdateType, // "interest_boost", "preference_change", "similarity_update"
    Dictionary<string, object> UpdateData,
    float ConfidenceChange,
    DateTime UpdatedAt
);

/// <summary>
/// Personalized search results
/// </summary>
public record PersonalizedSearchResultDto(
    string Query,
    IEnumerable<PersonalizedRecommendationDto> Results,
    Dictionary<string, float> QueryExpansion,
    float PersonalizationStrength,
    int TotalResults,
    DateTime SearchedAt
);

/// <summary>
/// Content embedding DTO
/// </summary>
public record ContentEmbeddingDto(
    string ContentType,
    int ContentId,
    float[] EmbeddingVector,
    int Dimensions,
    string ModelVersion,
    float QualityScore,
    DateTime CreatedAt
);

/// <summary>
/// Batch personalization request
/// </summary>
public record BatchPersonalizationRequestDto(
    IEnumerable<int> UserIds,
    IEnumerable<object> ContentItems,
    string ContentType,
    PersonalizedFeedConfigDto? Config,
    bool IncludeExplanations
);

/// <summary>
/// Batch personalization response
/// </summary>
public record BatchPersonalizationResponseDto(
    Dictionary<int, IEnumerable<PersonalizedRecommendationDto>> UserRecommendations,
    PersonalizationMetricsDto Metrics,
    TimeSpan ProcessingTime,
    DateTime GeneratedAt
);

/// <summary>
/// Personalization training data point
/// </summary>
public record PersonalizationTrainingDataDto(
    int UserId,
    object Content,
    string ContentType,
    float EngagementScore,
    Dictionary<string, object> Features,
    Dictionary<string, object> Context,
    DateTime Timestamp
);

/// <summary>
/// A/B test result for personalization
/// </summary>
public record PersonalizationABTestResultDto(
    string ExperimentName,
    string Variant,
    int UserCount,
    float EngagementRate,
    float ClickThroughRate,
    float TimeSpent,
    float SatisfactionScore,
    bool IsStatisticallySignificant,
    DateTime TestPeriodStart,
    DateTime TestPeriodEnd
);
