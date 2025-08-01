using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Yapplr.Api.Common;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Personalization;
using Yapplr.Api.Services.Analytics;

namespace Yapplr.Api.Services;

/// <summary>
/// Advanced personalization service with AI-driven recommendations and user profiling
/// </summary>
public class AdvancedPersonalizationService : IAdvancedPersonalizationService
{
    private readonly YapplrDbContext _context;
    private readonly ITagAnalyticsService _tagAnalyticsService;
    private readonly ITrendingService _trendingService;
    private readonly ITopicService _topicService;
    private readonly ICachingService _cachingService;
    private readonly ILogger<AdvancedPersonalizationService> _logger;

    // Algorithm configuration
    private readonly Dictionary<string, object> _algorithmConfig = new()
    {
        ["interest_decay_rate"] = 0.95f,
        ["similarity_threshold"] = 0.1f,
        ["diversity_weight"] = 0.3f,
        ["novelty_weight"] = 0.2f,
        ["social_weight"] = 0.25f,
        ["quality_weight"] = 0.25f,
        ["min_interactions"] = 5,
        ["embedding_dimensions"] = 128,
        ["max_similar_users"] = 100,
        ["confidence_threshold"] = 0.3f
    };

    public AdvancedPersonalizationService(
        YapplrDbContext context,
        ITagAnalyticsService tagAnalyticsService,
        ITrendingService trendingService,
        ITopicService topicService,
        ICachingService cachingService,
        ILogger<AdvancedPersonalizationService> logger)
    {
        _context = context;
        _tagAnalyticsService = tagAnalyticsService;
        _trendingService = trendingService;
        _topicService = topicService;
        _cachingService = cachingService;
        _logger = logger;
    }

    #region User Profiling

    public async Task<UserPersonalizationProfileDto?> GetUserProfileAsync(int userId, bool includePrivateData = false)
    {
        try
        {
            var profile = await _context.UserPersonalizationProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                // Create initial profile if it doesn't exist
                profile = await CreateInitialProfileAsync(userId);
            }

            return MapToProfileDto(profile, includePrivateData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personalization profile for user {UserId}", userId);
            return null;
        }
    }

    public async Task<UserPersonalizationProfileDto> UpdateUserProfileAsync(int userId, bool forceRebuild = false)
    {
        try
        {
            var profile = await _context.UserPersonalizationProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null || forceRebuild)
            {
                profile = await BuildUserProfileAsync(userId);
            }
            else
            {
                profile = await IncrementalUpdateProfileAsync(profile);
            }

            await _context.SaveChangesAsync();
            return MapToProfileDto(profile, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating personalization profile for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PersonalizationInsightsDto> GetPersonalizationInsightsAsync(int userId)
    {
        try
        {
            var profile = await GetUserProfileAsync(userId, true);
            if (profile == null)
            {
                return CreateEmptyInsights(userId);
            }

            // Get top interests with trend analysis
            var topInterests = await GetTopInterestsWithTrendsAsync(userId, profile.InterestScores);
            
            // Get content preferences
            var contentPreferences = await GetContentPreferencesAsync(userId, profile.ContentTypePreferences);
            
            // Get engagement patterns
            var engagementPatterns = GetEngagementPatternsFromProfile(profile.EngagementPatterns);
            
            // Get similar users
            var similarUsers = await GetSimilarUsersInsightsAsync(userId, profile.SimilarUsers);
            
            // Generate stats
            var stats = new PersonalizationStatsDto(
                OverallConfidence: profile.PersonalizationConfidence,
                TotalInteractions: profile.DataPointCount,
                UniqueInterests: profile.InterestScores.Count,
                SimilarUsersCount: profile.SimilarUsers.Count,
                DiversityScore: profile.DiversityPreference,
                NoveltyScore: profile.NoveltyPreference,
                ProfileCreatedAt: DateTime.UtcNow, // Would come from profile
                LastUpdated: profile.LastMLUpdate
            );

            // Generate recommendation tips
            var tips = GenerateRecommendationTips(profile);

            return new PersonalizationInsightsDto(
                UserId: userId,
                TopInterests: topInterests,
                ContentPreferences: contentPreferences,
                EngagementPatterns: engagementPatterns,
                SimilarUsers: similarUsers,
                Stats: stats,
                RecommendationTips: tips,
                GeneratedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personalization insights for user {UserId}", userId);
            return CreateEmptyInsights(userId);
        }
    }

    public async Task<bool> TrackInteractionAsync(UserInteractionEventDto interactionEvent)
    {
        try
        {
            var interaction = new UserInteractionEvent
            {
                UserId = interactionEvent.UserId,
                InteractionType = interactionEvent.InteractionType,
                TargetEntityType = interactionEvent.TargetEntityType,
                TargetEntityId = interactionEvent.TargetEntityId,
                InteractionStrength = interactionEvent.InteractionStrength,
                DurationMs = interactionEvent.DurationMs,
                Context = interactionEvent.Context,
                DeviceInfo = interactionEvent.DeviceInfo,
                SessionId = interactionEvent.SessionId,
                IsImplicit = interactionEvent.IsImplicit,
                Sentiment = interactionEvent.Sentiment,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserInteractionEvents.Add(interaction);
            await _context.SaveChangesAsync();

            // Trigger real-time profile update for high-value interactions
            if (ShouldTriggerRealTimeUpdate(interactionEvent))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UpdateUserProfileAsync(interactionEvent.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update profile in real-time for user {UserId}", interactionEvent.UserId);
                    }
                });
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking interaction for user {UserId}", interactionEvent.UserId);
            return false;
        }
    }

    #endregion

    #region Content Recommendations

    public async Task<IEnumerable<PersonalizedRecommendationDto>> GetPersonalizedRecommendationsAsync(
        int userId, 
        string contentType, 
        int limit = 20, 
        PersonalizedFeedConfigDto? config = null)
    {
        try
        {
            config ??= GetDefaultFeedConfig(userId);
            var profile = await GetUserProfileAsync(userId, true);
            
            if (profile == null)
            {
                return await GetFallbackRecommendationsAsync(contentType, limit);
            }

            var recommendations = new List<PersonalizedRecommendationDto>();

            switch (contentType.ToLowerInvariant())
            {
                case "posts":
                    recommendations.AddRange(await GetPersonalizedPostRecommendationsAsync(userId, profile, config, limit));
                    break;
                case "users":
                    recommendations.AddRange(await GetPersonalizedUserRecommendationsAsync(userId, profile, config, limit));
                    break;
                case "topics":
                    recommendations.AddRange(await GetPersonalizedTopicRecommendationsAsync(userId, profile, config, limit));
                    break;
                case "hashtags":
                    recommendations.AddRange(await GetPersonalizedHashtagRecommendationsAsync(userId, profile, config, limit));
                    break;
                default:
                    recommendations.AddRange(await GetMixedPersonalizedRecommendationsAsync(userId, profile, config, limit));
                    break;
            }

            // Apply diversity and novelty filters
            recommendations = ApplyDiversityAndNoveltyFilters(recommendations, profile, config).ToList();

            // Sort by final recommendation score
            return recommendations
                .OrderByDescending(r => r.RecommendationScore)
                .Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personalized recommendations for user {UserId}, contentType {ContentType}", userId, contentType);
            return await GetFallbackRecommendationsAsync(contentType, limit);
        }
    }

    public async Task<IEnumerable<PersonalizedRecommendationDto>> GetPersonalizedFeedAsync(
        int userId, 
        PersonalizedFeedConfigDto? config = null)
    {
        try
        {
            config ??= GetDefaultFeedConfig(userId);
            var profile = await GetUserProfileAsync(userId, true);
            
            if (profile == null)
            {
                return await GetFallbackFeedAsync(config.PostLimit);
            }

            var feedItems = new List<PersonalizedRecommendationDto>();

            // Get content from multiple sources with different weights
            var postRecommendations = await GetPersonalizedPostRecommendationsAsync(userId, profile, config, config.PostLimit);
            var userRecommendations = await GetPersonalizedUserRecommendationsAsync(userId, profile, config, 5);
            var topicRecommendations = await GetPersonalizedTopicRecommendationsAsync(userId, profile, config, 3);

            feedItems.AddRange(postRecommendations);
            feedItems.AddRange(userRecommendations);
            feedItems.AddRange(topicRecommendations);

            // Apply personalization weights
            feedItems = ApplyPersonalizationWeights(feedItems, profile, config).ToList();

            // Apply diversity and novelty
            feedItems = ApplyDiversityAndNoveltyFilters(feedItems, profile, config).ToList();

            // Final ranking
            return feedItems
                .OrderByDescending(item => item.RecommendationScore)
                .Take(config.PostLimit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating personalized feed for user {UserId}", userId);
            return await GetFallbackFeedAsync(config?.PostLimit ?? 20);
        }
    }

    public async Task<PersonalizedSearchResultDto> GetPersonalizedSearchAsync(
        int userId, 
        string query, 
        IEnumerable<string>? contentTypes = null, 
        int limit = 20)
    {
        try
        {
            var profile = await GetUserProfileAsync(userId, true);
            contentTypes ??= new[] { "posts", "users", "topics", "hashtags" };

            // Expand query based on user interests
            var queryExpansion = ExpandQueryWithUserInterests(query, profile?.InterestScores ?? new Dictionary<string, float>());

            var allResults = new List<PersonalizedRecommendationDto>();

            foreach (var contentType in contentTypes)
            {
                var typeResults = await SearchContentWithPersonalization(userId, query, queryExpansion, contentType, profile, limit / contentTypes.Count());
                allResults.AddRange(typeResults);
            }

            // Calculate personalization strength
            var personalizationStrength = profile?.PersonalizationConfidence ?? 0.0f;

            return new PersonalizedSearchResultDto(
                Query: query,
                Results: allResults.OrderByDescending(r => r.RecommendationScore).Take(limit),
                QueryExpansion: queryExpansion,
                PersonalizationStrength: personalizationStrength,
                TotalResults: allResults.Count,
                SearchedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing personalized search for user {UserId}, query '{Query}'", userId, query);
            return new PersonalizedSearchResultDto(
                Query: query,
                Results: Enumerable.Empty<PersonalizedRecommendationDto>(),
                QueryExpansion: new Dictionary<string, float>(),
                PersonalizationStrength: 0.0f,
                TotalResults: 0,
                SearchedAt: DateTime.UtcNow
            );
        }
    }

    #endregion

    #region Similarity & Clustering

    public async Task<float> CalculateUserSimilarityAsync(int userId1, int userId2)
    {
        try
        {
            var profile1 = await GetUserProfileAsync(userId1, true);
            var profile2 = await GetUserProfileAsync(userId2, true);

            if (profile1 == null || profile2 == null)
            {
                return 0.0f;
            }

            // Calculate similarity based on multiple factors
            var interestSimilarity = CalculateInterestSimilarity(profile1.InterestScores, profile2.InterestScores);
            var contentTypeSimilarity = CalculateContentTypeSimilarity(profile1.ContentTypePreferences, profile2.ContentTypePreferences);
            var engagementSimilarity = CalculateEngagementSimilarity(profile1.EngagementPatterns, profile2.EngagementPatterns);

            // Weighted combination
            var totalSimilarity = (interestSimilarity * 0.5f) +
                                 (contentTypeSimilarity * 0.3f) +
                                 (engagementSimilarity * 0.2f);

            return Math.Min(1.0f, totalSimilarity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating similarity between users {UserId1} and {UserId2}", userId1, userId2);
            return 0.0f;
        }
    }

    public async Task<IEnumerable<UserSimilarityDto>> FindSimilarUsersAsync(int userId, int limit = 10, float minSimilarity = 0.1f)
    {
        try
        {
            var userProfile = await GetUserProfileAsync(userId, true);
            if (userProfile == null)
            {
                return Enumerable.Empty<UserSimilarityDto>();
            }

            // Get candidate users (active users with profiles)
            var candidateUsers = await _context.UserPersonalizationProfiles
                .Where(p => p.UserId != userId &&
                           p.PersonalizationConfidence > 0.3f &&
                           p.DataPointCount >= 10)
                .Select(p => p.UserId)
                .Take(1000) // Limit for performance
                .ToListAsync();

            var similarities = new List<UserSimilarityDto>();

            foreach (var candidateUserId in candidateUsers)
            {
                var similarity = await CalculateUserSimilarityAsync(userId, candidateUserId);

                if (similarity >= minSimilarity)
                {
                    var candidateUser = await _context.Users
                        .Where(u => u.Id == candidateUserId)
                        .Select(u => u.MapToUserDto())
                        .FirstOrDefaultAsync();

                    if (candidateUser != null)
                    {
                        var commonInterests = GetCommonInterests(userProfile.InterestScores,
                            (await GetUserProfileAsync(candidateUserId, true))?.InterestScores ?? new Dictionary<string, float>());

                        var sharedFollows = await GetSharedFollowsAsync(userId, candidateUserId);
                        var reason = GenerateSimilarityReason(similarity, commonInterests, sharedFollows);

                        similarities.Add(new UserSimilarityDto(
                            SimilarUser: candidateUser,
                            SimilarityScore: similarity,
                            CommonInterests: commonInterests,
                            SharedFollows: sharedFollows,
                            SimilarityReason: reason
                        ));
                    }
                }
            }

            return similarities
                .OrderByDescending(s => s.SimilarityScore)
                .Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar users for user {UserId}", userId);
            return Enumerable.Empty<UserSimilarityDto>();
        }
    }

    public async Task<IEnumerable<ContentSimilarityDto>> CalculateContentSimilarityAsync(
        string contentType,
        int contentId,
        IEnumerable<int> candidateIds)
    {
        try
        {
            var sourceEmbedding = await GetOrGenerateContentEmbeddingAsync(contentType, contentId);
            if (sourceEmbedding == null)
            {
                return Enumerable.Empty<ContentSimilarityDto>();
            }

            var similarities = new List<ContentSimilarityDto>();

            foreach (var candidateId in candidateIds)
            {
                var candidateEmbedding = await GetOrGenerateContentEmbeddingAsync(contentType, candidateId);
                if (candidateEmbedding != null)
                {
                    var similarity = CalculateCosineSimilarity(sourceEmbedding.EmbeddingVector, candidateEmbedding.EmbeddingVector);

                    if (similarity > 0.1f)
                    {
                        var content = await GetContentByTypeAndIdAsync(contentType, candidateId);
                        var reasons = GenerateContentSimilarityReasons(sourceEmbedding, candidateEmbedding, similarity);

                        similarities.Add(new ContentSimilarityDto(
                            ContentType: contentType,
                            ContentId: candidateId,
                            Content: content,
                            SimilarityScore: similarity,
                            SimilarityReasons: reasons,
                            CalculatedAt: DateTime.UtcNow
                        ));
                    }
                }
            }

            return similarities.OrderByDescending(s => s.SimilarityScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating content similarity for {ContentType} {ContentId}", contentType, contentId);
            return Enumerable.Empty<ContentSimilarityDto>();
        }
    }

    public async Task<ContentEmbeddingDto?> GenerateContentEmbeddingAsync(string contentType, int contentId)
    {
        try
        {
            // Check if embedding already exists
            var existingEmbedding = await _context.ContentEmbeddings
                .FirstOrDefaultAsync(e => e.ContentType == contentType && e.ContentId == contentId);

            if (existingEmbedding != null && existingEmbedding.UpdatedAt > DateTime.UtcNow.AddDays(-7))
            {
                return MapToEmbeddingDto(existingEmbedding);
            }

            // Generate new embedding
            var content = await GetContentByTypeAndIdAsync(contentType, contentId);
            if (content == null)
            {
                return null;
            }

            var embeddingVector = await GenerateEmbeddingVectorAsync(content, contentType);
            var qualityScore = CalculateEmbeddingQuality(embeddingVector, content);

            var embedding = new ContentEmbedding
            {
                ContentType = contentType,
                ContentId = contentId,
                EmbeddingVector = JsonSerializer.Serialize(embeddingVector),
                Dimensions = embeddingVector.Length,
                ModelVersion = "v1.0",
                QualityScore = qualityScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (existingEmbedding != null)
            {
                existingEmbedding.EmbeddingVector = embedding.EmbeddingVector;
                existingEmbedding.QualityScore = embedding.QualityScore;
                existingEmbedding.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ContentEmbeddings.Add(embedding);
            }

            await _context.SaveChangesAsync();
            return MapToEmbeddingDto(embedding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content embedding for {ContentType} {ContentId}", contentType, contentId);
            return null;
        }
    }

    #endregion

    #region Experimentation

    public async Task<IEnumerable<UserExperimentParticipationDto>> GetUserExperimentsAsync(int userId)
    {
        try
        {
            var participations = await _context.UserExperimentParticipations
                .Include(p => p.Experiment)
                .Where(p => p.UserId == userId && p.Experiment.IsActive)
                .ToListAsync();

            return participations.Select(p => new UserExperimentParticipationDto(
                ExperimentId: p.ExperimentId,
                ExperimentName: p.Experiment.Name,
                Variant: p.Variant,
                AssignedAt: p.AssignedAt,
                IsActive: p.Experiment.IsActive &&
                         (p.Experiment.EndDate == null || p.Experiment.EndDate > DateTime.UtcNow)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting experiments for user {UserId}", userId);
            return Enumerable.Empty<UserExperimentParticipationDto>();
        }
    }

    public async Task<string?> AssignExperimentVariantAsync(int userId, string experimentName)
    {
        try
        {
            var experiment = await _context.PersonalizationExperiments
                .FirstOrDefaultAsync(e => e.Name == experimentName && e.IsActive);

            if (experiment == null)
            {
                return null;
            }

            // Check if user is already assigned
            var existingParticipation = await _context.UserExperimentParticipations
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ExperimentId == experiment.Id);

            if (existingParticipation != null)
            {
                return existingParticipation.Variant;
            }

            // Check if user should be included based on traffic allocation
            var userHash = HashUserId(userId, experimentName);
            if (userHash > experiment.TrafficAllocation)
            {
                return null; // User not included in experiment
            }

            // Assign variant (simple A/B for now, could be extended for multi-variant)
            var variant = userHash < experiment.TrafficAllocation / 2 ? "A" : "B";

            var participation = new UserExperimentParticipation
            {
                UserId = userId,
                ExperimentId = experiment.Id,
                Variant = variant,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserExperimentParticipations.Add(participation);
            await _context.SaveChangesAsync();

            return variant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning experiment variant for user {UserId}, experiment {ExperimentName}", userId, experimentName);
            return null;
        }
    }

    public async Task<IEnumerable<PersonalizationABTestResultDto>> GetExperimentResultsAsync(string experimentName)
    {
        try
        {
            var experiment = await _context.PersonalizationExperiments
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Name == experimentName);

            if (experiment == null)
            {
                return Enumerable.Empty<PersonalizationABTestResultDto>();
            }

            var results = new List<PersonalizationABTestResultDto>();
            var variants = experiment.Participants.GroupBy(p => p.Variant);

            foreach (var variantGroup in variants)
            {
                var userIds = variantGroup.Select(p => p.UserId).ToList();
                var metrics = await CalculateExperimentMetricsAsync(userIds, experiment.StartDate, experiment.EndDate);

                results.Add(new PersonalizationABTestResultDto(
                    ExperimentName: experimentName,
                    Variant: variantGroup.Key,
                    UserCount: userIds.Count,
                    EngagementRate: metrics.EngagementRate,
                    ClickThroughRate: metrics.ClickThroughRate,
                    TimeSpent: metrics.TimeSpent,
                    SatisfactionScore: metrics.SatisfactionScore,
                    IsStatisticallySignificant: metrics.IsStatisticallySignificant,
                    TestPeriodStart: experiment.StartDate,
                    TestPeriodEnd: experiment.EndDate ?? DateTime.UtcNow
                ));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting experiment results for {ExperimentName}", experimentName);
            return Enumerable.Empty<PersonalizationABTestResultDto>();
        }
    }

    #endregion

    #region Batch Operations

    public async Task<BatchPersonalizationResponseDto> ProcessBatchPersonalizationAsync(BatchPersonalizationRequestDto request)
    {
        var startTime = DateTime.UtcNow;
        var userRecommendations = new Dictionary<int, IEnumerable<PersonalizedRecommendationDto>>();

        try
        {
            foreach (var userId in request.UserIds)
            {
                var recommendations = await GetPersonalizedRecommendationsAsync(
                    userId,
                    request.ContentType,
                    20,
                    request.Config);

                userRecommendations[userId] = recommendations;
            }

            var metrics = await GetPersonalizationMetricsAsync(TimeSpan.FromHours(1));
            var processingTime = DateTime.UtcNow - startTime;

            return new BatchPersonalizationResponseDto(
                UserRecommendations: userRecommendations,
                Metrics: metrics,
                ProcessingTime: processingTime,
                GeneratedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch personalization request");
            return new BatchPersonalizationResponseDto(
                UserRecommendations: userRecommendations,
                Metrics: await GetPersonalizationMetricsAsync(TimeSpan.FromHours(1)),
                ProcessingTime: DateTime.UtcNow - startTime,
                GeneratedAt: DateTime.UtcNow
            );
        }
    }

    public async Task<int> RebuildUserProfilesBatchAsync(IEnumerable<int> userIds, int batchSize = 100)
    {
        var rebuiltCount = 0;
        var userIdList = userIds.ToList();

        try
        {
            for (int i = 0; i < userIdList.Count; i += batchSize)
            {
                var batch = userIdList.Skip(i).Take(batchSize);

                var tasks = batch.Select(async userId =>
                {
                    try
                    {
                        await UpdateUserProfileAsync(userId, forceRebuild: true);
                        return 1;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to rebuild profile for user {UserId}", userId);
                        return 0;
                    }
                });

                var results = await Task.WhenAll(tasks);
                rebuiltCount += results.Sum();

                // Small delay between batches to avoid overwhelming the system
                await Task.Delay(100);
            }

            _logger.LogInformation("Rebuilt {RebuiltCount} out of {TotalCount} user profiles", rebuiltCount, userIdList.Count);
            return rebuiltCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch profile rebuild");
            return rebuiltCount;
        }
    }

    #endregion

    #region Analytics & Monitoring

    public async Task<PersonalizationMetricsDto> GetPersonalizationMetricsAsync(TimeSpan timeWindow)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - timeWindow;

            var totalUsers = await _context.UserPersonalizationProfiles.CountAsync();
            var activeUsers = await _context.UserPersonalizationProfiles
                .CountAsync(p => p.LastMLUpdate >= cutoffTime);

            var avgConfidence = await _context.UserPersonalizationProfiles
                .Where(p => p.LastMLUpdate >= cutoffTime)
                .AverageAsync(p => (double?)p.PersonalizationConfidence) ?? 0.0;

            var avgDiversity = await _context.UserPersonalizationProfiles
                .Where(p => p.LastMLUpdate >= cutoffTime)
                .AverageAsync(p => (double?)p.DiversityPreference) ?? 0.0;

            var avgNovelty = await _context.UserPersonalizationProfiles
                .Where(p => p.LastMLUpdate >= cutoffTime)
                .AverageAsync(p => (double?)p.NoveltyPreference) ?? 0.0;

            // Calculate engagement lift (simplified - would need A/B test data)
            var engagementLift = 0.15f; // Placeholder

            var performanceBreakdown = new Dictionary<string, float>
            {
                ["interest_matching"] = 0.85f,
                ["content_diversity"] = 0.72f,
                ["novelty_discovery"] = 0.68f,
                ["social_influence"] = 0.79f,
                ["quality_filtering"] = 0.91f
            };

            return new PersonalizationMetricsDto(
                AlgorithmVersion: "v1.0",
                AverageConfidence: (float)avgConfidence,
                EngagementLift: engagementLift,
                DiversityScore: (float)avgDiversity,
                NoveltyScore: (float)avgNovelty,
                TotalUsers: totalUsers,
                ActiveUsers: activeUsers,
                CalculatedAt: DateTime.UtcNow,
                PerformanceBreakdown: performanceBreakdown
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating personalization metrics");
            return new PersonalizationMetricsDto(
                AlgorithmVersion: "v1.0",
                AverageConfidence: 0.0f,
                EngagementLift: 0.0f,
                DiversityScore: 0.0f,
                NoveltyScore: 0.0f,
                TotalUsers: 0,
                ActiveUsers: 0,
                CalculatedAt: DateTime.UtcNow,
                PerformanceBreakdown: new Dictionary<string, float>()
            );
        }
    }

    public async Task<IEnumerable<PersonalizationUpdateDto>> GetPersonalizationUpdatesAsync(int userId, DateTime since)
    {
        try
        {
            // This would track real-time updates to user profiles
            // For now, return recent interaction-based updates
            var recentInteractions = await _context.UserInteractionEvents
                .Where(e => e.UserId == userId && e.CreatedAt >= since)
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync();

            return recentInteractions.Select(interaction => new PersonalizationUpdateDto(
                UserId: userId,
                UpdateType: MapInteractionToUpdateType(interaction.InteractionType),
                UpdateData: new Dictionary<string, object>
                {
                    ["interaction_type"] = interaction.InteractionType,
                    ["target_entity_type"] = interaction.TargetEntityType ?? "unknown",
                    ["target_entity_id"] = interaction.TargetEntityId ?? 0,
                    ["strength"] = interaction.InteractionStrength
                },
                ConfidenceChange: CalculateConfidenceChange(interaction),
                UpdatedAt: interaction.CreatedAt
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personalization updates for user {UserId}", userId);
            return Enumerable.Empty<PersonalizationUpdateDto>();
        }
    }

    public async Task<IEnumerable<PersonalizationTrainingDataDto>> GenerateTrainingDataAsync(
        IEnumerable<int> userIds,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var trainingData = new List<PersonalizationTrainingDataDto>();

            foreach (var userId in userIds)
            {
                var interactions = await _context.UserInteractionEvents
                    .Where(e => e.UserId == userId &&
                               e.CreatedAt >= startDate &&
                               e.CreatedAt <= endDate)
                    .ToListAsync();

                foreach (var interaction in interactions)
                {
                    var content = await GetContentByTypeAndIdAsync(
                        interaction.TargetEntityType ?? "unknown",
                        interaction.TargetEntityId ?? 0);

                    if (content != null)
                    {
                        var features = await ExtractContentFeaturesAsync(content, interaction.TargetEntityType ?? "unknown");
                        var context = ExtractInteractionContext(interaction);
                        var engagementScore = CalculateEngagementScore(interaction);

                        trainingData.Add(new PersonalizationTrainingDataDto(
                            UserId: userId,
                            Content: content,
                            ContentType: interaction.TargetEntityType ?? "unknown",
                            EngagementScore: engagementScore,
                            Features: features,
                            Context: context,
                            Timestamp: interaction.CreatedAt
                        ));
                    }
                }
            }

            return trainingData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating training data");
            return Enumerable.Empty<PersonalizationTrainingDataDto>();
        }
    }

    #endregion

    #region Configuration & Tuning

    public async Task<bool> UpdateAlgorithmParametersAsync(Dictionary<string, object> parameters)
    {
        try
        {
            foreach (var param in parameters)
            {
                if (_algorithmConfig.ContainsKey(param.Key))
                {
                    _algorithmConfig[param.Key] = param.Value;
                }
            }

            _logger.LogInformation("Updated algorithm parameters: {Parameters}",
                string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating algorithm parameters");
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GetAlgorithmConfigurationAsync()
    {
        return await Task.FromResult(new Dictionary<string, object>(_algorithmConfig));
    }

    public async Task<PersonalizationMetricsDto> ValidateModelPerformanceAsync(IEnumerable<int> testUserIds)
    {
        try
        {
            var testUsers = testUserIds.ToList();
            var validationResults = new List<float>();

            foreach (var userId in testUsers)
            {
                // Generate recommendations and measure quality
                var recommendations = await GetPersonalizedRecommendationsAsync(userId, "posts", 20);
                var qualityScore = await CalculateRecommendationQualityAsync(userId, recommendations);
                validationResults.Add(qualityScore);
            }

            var avgQuality = validationResults.Any() ? validationResults.Average() : 0.0f;

            return new PersonalizationMetricsDto(
                AlgorithmVersion: "v1.0",
                AverageConfidence: avgQuality,
                EngagementLift: avgQuality * 0.2f, // Estimated lift
                DiversityScore: CalculateValidationDiversityScore(validationResults),
                NoveltyScore: CalculateValidationNoveltyScore(validationResults),
                TotalUsers: testUsers.Count,
                ActiveUsers: testUsers.Count,
                CalculatedAt: DateTime.UtcNow,
                PerformanceBreakdown: new Dictionary<string, float>
                {
                    ["validation_quality"] = avgQuality,
                    ["test_coverage"] = testUsers.Count / 100.0f,
                    ["algorithm_stability"] = 0.85f
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating model performance");
            return new PersonalizationMetricsDto(
                AlgorithmVersion: "v1.0",
                AverageConfidence: 0.0f,
                EngagementLift: 0.0f,
                DiversityScore: 0.0f,
                NoveltyScore: 0.0f,
                TotalUsers: 0,
                ActiveUsers: 0,
                CalculatedAt: DateTime.UtcNow,
                PerformanceBreakdown: new Dictionary<string, float>()
            );
        }
    }

    #endregion

    #region Helper Methods

    private async Task<UserPersonalizationProfile> CreateInitialProfileAsync(int userId)
    {
        var profile = new UserPersonalizationProfile
        {
            UserId = userId,
            InterestScores = "{}",
            ContentTypePreferences = JsonSerializer.Serialize(new Dictionary<string, float>
            {
                ["text"] = 0.5f,
                ["image"] = 0.5f,
                ["video"] = 0.5f
            }),
            EngagementPatterns = JsonSerializer.Serialize(new Dictionary<string, float>
            {
                ["morning"] = 0.33f,
                ["afternoon"] = 0.33f,
                ["evening"] = 0.34f
            }),
            SimilarUsers = "{}",
            PersonalizationConfidence = 0.1f,
            DiversityPreference = 0.5f,
            NoveltyPreference = 0.5f,
            SocialInfluenceFactor = 0.5f,
            QualityThreshold = 0.3f,
            LastMLUpdate = DateTime.UtcNow,
            DataPointCount = 0,
            AlgorithmVersion = "v1.0"
        };

        _context.UserPersonalizationProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    private async Task<UserPersonalizationProfile> BuildUserProfileAsync(int userId)
    {
        // Get user's interaction history
        var interactions = await _context.UserInteractionEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(1000)
            .ToListAsync();

        // Calculate interest scores
        var interestScores = await CalculateInterestScoresAsync(userId, interactions);

        // Calculate content type preferences
        var contentTypePrefs = CalculateContentTypePreferences(interactions);

        // Calculate engagement patterns
        var engagementPatterns = CalculateEngagementPatterns(interactions);

        // Find similar users
        var similarUsers = await FindSimilarUsersForProfileAsync(userId);

        // Calculate confidence
        var confidence = CalculatePersonalizationConfidence(interactions.Count, interestScores.Count);

        var profile = await _context.UserPersonalizationProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            profile = new UserPersonalizationProfile { UserId = userId };
            _context.UserPersonalizationProfiles.Add(profile);
        }

        profile.InterestScores = JsonSerializer.Serialize(interestScores);
        profile.ContentTypePreferences = JsonSerializer.Serialize(contentTypePrefs);
        profile.EngagementPatterns = JsonSerializer.Serialize(engagementPatterns);
        profile.SimilarUsers = JsonSerializer.Serialize(similarUsers);
        profile.PersonalizationConfidence = confidence;
        profile.DataPointCount = interactions.Count;
        profile.LastMLUpdate = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        return profile;
    }

    private async Task<UserPersonalizationProfile> IncrementalUpdateProfileAsync(UserPersonalizationProfile profile)
    {
        // Get recent interactions since last update
        var recentInteractions = await _context.UserInteractionEvents
            .Where(e => e.UserId == profile.UserId && e.CreatedAt > profile.LastMLUpdate)
            .ToListAsync();

        if (!recentInteractions.Any())
        {
            return profile; // No new data to process
        }

        // Deserialize current scores
        var currentInterests = JsonSerializer.Deserialize<Dictionary<string, float>>(profile.InterestScores) ?? new();
        var currentContentPrefs = JsonSerializer.Deserialize<Dictionary<string, float>>(profile.ContentTypePreferences) ?? new();

        // Apply decay to existing scores
        var decayRate = (float)_algorithmConfig["interest_decay_rate"];
        foreach (var key in currentInterests.Keys.ToList())
        {
            currentInterests[key] *= decayRate;
        }

        // Update with new interactions
        var newInterests = await CalculateInterestScoresAsync(profile.UserId, recentInteractions);
        foreach (var interest in newInterests)
        {
            currentInterests[interest.Key] = currentInterests.GetValueOrDefault(interest.Key, 0) + interest.Value;
        }

        // Update content preferences
        var newContentPrefs = CalculateContentTypePreferences(recentInteractions);
        foreach (var pref in newContentPrefs)
        {
            currentContentPrefs[pref.Key] = (currentContentPrefs.GetValueOrDefault(pref.Key, 0.5f) + pref.Value) / 2;
        }

        // Update profile
        profile.InterestScores = JsonSerializer.Serialize(currentInterests);
        profile.ContentTypePreferences = JsonSerializer.Serialize(currentContentPrefs);
        profile.DataPointCount += recentInteractions.Count;
        profile.LastMLUpdate = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        return profile;
    }

    private UserPersonalizationProfileDto MapToProfileDto(UserPersonalizationProfile profile, bool includePrivateData)
    {
        var interestScores = JsonSerializer.Deserialize<Dictionary<string, float>>(profile.InterestScores) ?? new();
        var contentTypePrefs = JsonSerializer.Deserialize<Dictionary<string, float>>(profile.ContentTypePreferences) ?? new();
        var engagementPatterns = JsonSerializer.Deserialize<Dictionary<string, float>>(profile.EngagementPatterns) ?? new();
        var similarUsers = includePrivateData
            ? JsonSerializer.Deserialize<Dictionary<string, float>>(profile.SimilarUsers) ?? new()
            : new Dictionary<string, float>();

        return new UserPersonalizationProfileDto(
            UserId: profile.UserId,
            InterestScores: interestScores,
            ContentTypePreferences: contentTypePrefs,
            EngagementPatterns: engagementPatterns,
            SimilarUsers: similarUsers,
            PersonalizationConfidence: profile.PersonalizationConfidence,
            DiversityPreference: profile.DiversityPreference,
            NoveltyPreference: profile.NoveltyPreference,
            SocialInfluenceFactor: profile.SocialInfluenceFactor,
            QualityThreshold: profile.QualityThreshold,
            LastMLUpdate: profile.LastMLUpdate,
            DataPointCount: profile.DataPointCount,
            AlgorithmVersion: profile.AlgorithmVersion
        );
    }

    private async Task<Dictionary<string, float>> CalculateInterestScoresAsync(int userId, List<UserInteractionEvent> interactions)
    {
        var interestScores = new Dictionary<string, float>();

        foreach (var interaction in interactions)
        {
            if (interaction.TargetEntityType == "post" && interaction.TargetEntityId.HasValue)
            {
                // Get hashtags from the post
                var postTags = await _context.PostTags
                    .Where(pt => pt.PostId == interaction.TargetEntityId.Value)
                    .Include(pt => pt.Tag)
                    .Select(pt => pt.Tag.Name.ToLowerInvariant())
                    .ToListAsync();

                foreach (var tag in postTags)
                {
                    var weight = interaction.InteractionStrength * GetInteractionTypeWeight(interaction.InteractionType);
                    interestScores[tag] = interestScores.GetValueOrDefault(tag, 0) + weight;
                }
            }
        }

        // Normalize scores
        if (interestScores.Any())
        {
            var maxScore = interestScores.Values.Max();
            foreach (var key in interestScores.Keys.ToList())
            {
                interestScores[key] = Math.Min(1.0f, interestScores[key] / maxScore);
            }
        }

        return interestScores;
    }

    private Dictionary<string, float> CalculateContentTypePreferences(List<UserInteractionEvent> interactions)
    {
        var contentTypeCounts = new Dictionary<string, float>();
        var totalInteractions = 0f;

        foreach (var interaction in interactions)
        {
            var weight = interaction.InteractionStrength * GetInteractionTypeWeight(interaction.InteractionType);
            var contentType = DetermineContentType(interaction);

            contentTypeCounts[contentType] = contentTypeCounts.GetValueOrDefault(contentType, 0) + weight;
            totalInteractions += weight;
        }

        // Normalize to preferences (0-1 scale)
        var preferences = new Dictionary<string, float>();
        foreach (var kvp in contentTypeCounts)
        {
            preferences[kvp.Key] = totalInteractions > 0 ? kvp.Value / totalInteractions : 0.33f;
        }

        // Ensure all content types are represented
        foreach (var contentType in new[] { "text", "image", "video" })
        {
            if (!preferences.ContainsKey(contentType))
            {
                preferences[contentType] = 0.1f;
            }
        }

        return preferences;
    }

    private Dictionary<string, float> CalculateEngagementPatterns(List<UserInteractionEvent> interactions)
    {
        var patterns = new Dictionary<string, float>
        {
            ["morning"] = 0,
            ["afternoon"] = 0,
            ["evening"] = 0
        };

        foreach (var interaction in interactions)
        {
            var hour = interaction.CreatedAt.Hour;
            var timeOfDay = hour < 12 ? "morning" : hour < 18 ? "afternoon" : "evening";
            var weight = interaction.InteractionStrength * GetInteractionTypeWeight(interaction.InteractionType);

            patterns[timeOfDay] += weight;
        }

        // Normalize
        var total = patterns.Values.Sum();
        if (total > 0)
        {
            foreach (var key in patterns.Keys.ToList())
            {
                patterns[key] = patterns[key] / total;
            }
        }
        else
        {
            patterns["morning"] = patterns["afternoon"] = patterns["evening"] = 0.33f;
        }

        return patterns;
    }

    private float GetInteractionTypeWeight(string interactionType)
    {
        return interactionType.ToLowerInvariant() switch
        {
            "like" => 1.0f,
            "comment" => 1.5f,
            "share" => 2.0f,
            "follow" => 2.5f,
            "view" => 0.1f,
            "click" => 0.5f,
            "dwell_time" => 0.3f,
            _ => 0.5f
        };
    }

    private string DetermineContentType(UserInteractionEvent interaction)
    {
        // This would analyze the actual content to determine type
        // For now, return a default based on context
        if (interaction.Context?.Contains("video") == true) return "video";
        if (interaction.Context?.Contains("image") == true) return "image";
        return "text";
    }

    private float CalculatePersonalizationConfidence(int interactionCount, int uniqueInterests)
    {
        var minInteractions = (int)_algorithmConfig["min_interactions"];

        if (interactionCount < minInteractions)
        {
            return 0.1f;
        }

        var interactionScore = Math.Min(1.0f, interactionCount / 100.0f);
        var diversityScore = Math.Min(1.0f, uniqueInterests / 20.0f);

        return (interactionScore + diversityScore) / 2;
    }

    // Additional helper methods for completeness
    private PersonalizationInsightsDto CreateEmptyInsights(int userId)
    {
        return new PersonalizationInsightsDto(
            UserId: userId,
            TopInterests: Enumerable.Empty<InterestInsightDto>(),
            ContentPreferences: Enumerable.Empty<ContentTypeInsightDto>(),
            EngagementPatterns: Enumerable.Empty<EngagementPatternDto>(),
            SimilarUsers: Enumerable.Empty<UserSimilarityDto>(),
            Stats: new PersonalizationStatsDto(0, 0, 0, 0, 0, 0, DateTime.UtcNow, DateTime.UtcNow),
            RecommendationTips: new[] { "Start interacting with content to build your personalization profile!" },
            GeneratedAt: DateTime.UtcNow
        );
    }

    private bool ShouldTriggerRealTimeUpdate(UserInteractionEventDto interaction)
    {
        return interaction.InteractionType.ToLowerInvariant() switch
        {
            "like" => true,
            "comment" => true,
            "share" => true,
            "follow" => true,
            _ => false
        };
    }

    private async Task<IEnumerable<PersonalizedRecommendationDto>> GetFallbackRecommendationsAsync(string contentType, int limit)
    {
        // Return trending content as fallback
        try
        {
            switch (contentType.ToLowerInvariant())
            {
                case "posts":
                    var trendingPosts = await _trendingService.GetTrendingPostsAsync(24, limit);
                    return trendingPosts.Select(p => new PersonalizedRecommendationDto(
                        Content: p,
                        ContentType: "post",
                        RecommendationScore: 0.5,
                        PrimaryReason: "Trending content",
                        ReasonTags: new[] { "trending", "popular" },
                        ScoreBreakdown: new Dictionary<string, float> { ["trending"] = 0.5f },
                        ConfidenceLevel: 0.3f,
                        IsExperimental: false,
                        GeneratedAt: DateTime.UtcNow
                    ));
                default:
                    return Enumerable.Empty<PersonalizedRecommendationDto>();
            }
        }
        catch
        {
            return Enumerable.Empty<PersonalizedRecommendationDto>();
        }
    }

    private PersonalizedFeedConfigDto GetDefaultFeedConfig(int userId)
    {
        return new PersonalizedFeedConfigDto(
            UserId: userId,
            PostLimit: 20,
            DiversityWeight: 0.3f,
            NoveltyWeight: 0.2f,
            SocialWeight: 0.25f,
            QualityThreshold: 0.3f,
            IncludeExperimental: false,
            PreferredContentTypes: new[] { "text", "image", "video" },
            ExcludedTopics: Enumerable.Empty<string>(),
            FeedType: "main"
        );
    }

    // Placeholder methods that would be implemented with actual ML algorithms
    private async Task<IEnumerable<PersonalizedRecommendationDto>> GetPersonalizedPostRecommendationsAsync(
        int userId, UserPersonalizationProfileDto profile, PersonalizedFeedConfigDto config, int limit)
    {
        // This would use the user's interest scores to find relevant posts
        var personalizedPosts = await _trendingService.GetPersonalizedTrendingPostsAsync(userId, 24, limit);
        return personalizedPosts.Select(p => new PersonalizedRecommendationDto(
            Content: p,
            ContentType: "post",
            RecommendationScore: 0.8,
            PrimaryReason: "Matches your interests",
            ReasonTags: new[] { "personalized", "interests" },
            ScoreBreakdown: new Dictionary<string, float> { ["interest_match"] = 0.8f },
            ConfidenceLevel: profile.PersonalizationConfidence,
            IsExperimental: false,
            GeneratedAt: DateTime.UtcNow
        ));
    }

    private async Task<IEnumerable<PersonalizedRecommendationDto>> GetPersonalizedUserRecommendationsAsync(
        int userId, UserPersonalizationProfileDto profile, PersonalizedFeedConfigDto config, int limit)
    {
        // This would find users with similar interests
        var similarUsers = await FindSimilarUsersAsync(userId, limit);
        return similarUsers.Select(u => new PersonalizedRecommendationDto(
            Content: u.SimilarUser,
            ContentType: "user",
            RecommendationScore: u.SimilarityScore,
            PrimaryReason: u.SimilarityReason,
            ReasonTags: new[] { "similar_user", "recommendation" },
            ScoreBreakdown: new Dictionary<string, float> { ["similarity"] = u.SimilarityScore },
            ConfidenceLevel: profile.PersonalizationConfidence,
            IsExperimental: false,
            GeneratedAt: DateTime.UtcNow
        ));
    }

    private async Task<IEnumerable<PersonalizedRecommendationDto>> GetPersonalizedTopicRecommendationsAsync(
        int userId, UserPersonalizationProfileDto profile, PersonalizedFeedConfigDto config, int limit)
    {
        // This would recommend topics based on user interests
        var topicRecommendations = await _topicService.GetTopicRecommendationsAsync(userId, limit);
        return topicRecommendations.Select(tr => new PersonalizedRecommendationDto(
            Content: tr.Topic,
            ContentType: "topic",
            RecommendationScore: tr.RecommendationScore,
            PrimaryReason: tr.RecommendationReason,
            ReasonTags: new[] { "topic", "interest_based" },
            ScoreBreakdown: new Dictionary<string, float> { ["interest_match"] = (float)tr.RecommendationScore },
            ConfidenceLevel: profile.PersonalizationConfidence,
            IsExperimental: tr.IsPersonalized,
            GeneratedAt: DateTime.UtcNow
        ));
    }

    private async Task<IEnumerable<PersonalizedRecommendationDto>> GetPersonalizedHashtagRecommendationsAsync(
        int userId, UserPersonalizationProfileDto profile, PersonalizedFeedConfigDto config, int limit)
    {
        // This would recommend hashtags based on user interests
        var personalizedHashtags = await _tagAnalyticsService.GetPersonalizedTrendingHashtagsAsync(userId, 24, limit);
        return personalizedHashtags.RecommendedHashtags.Select(h => new PersonalizedRecommendationDto(
            Content: h,
            ContentType: "hashtag",
            RecommendationScore: h.TrendingScore,
            PrimaryReason: "Trending in your interests",
            ReasonTags: new[] { "hashtag", "trending", "personalized" },
            ScoreBreakdown: new Dictionary<string, float> { ["trending"] = (float)h.TrendingScore },
            ConfidenceLevel: profile.PersonalizationConfidence,
            IsExperimental: false,
            GeneratedAt: DateTime.UtcNow
        ));
    }

    private async Task<IEnumerable<PersonalizedRecommendationDto>> GetMixedPersonalizedRecommendationsAsync(
        int userId, UserPersonalizationProfileDto profile, PersonalizedFeedConfigDto config, int limit)
    {
        var recommendations = new List<PersonalizedRecommendationDto>();

        // Mix different content types
        recommendations.AddRange(await GetPersonalizedPostRecommendationsAsync(userId, profile, config, limit / 2));
        recommendations.AddRange(await GetPersonalizedUserRecommendationsAsync(userId, profile, config, limit / 4));
        recommendations.AddRange(await GetPersonalizedTopicRecommendationsAsync(userId, profile, config, limit / 4));

        return recommendations.OrderByDescending(r => r.RecommendationScore).Take(limit);
    }

    // Placeholder implementations for complex algorithms
    private IEnumerable<PersonalizedRecommendationDto> ApplyDiversityAndNoveltyFilters(
        IEnumerable<PersonalizedRecommendationDto> recommendations,
        UserPersonalizationProfileDto profile,
        PersonalizedFeedConfigDto config)
    {
        // This would apply diversity and novelty algorithms
        return recommendations; // Simplified for now
    }

    private IEnumerable<PersonalizedRecommendationDto> ApplyPersonalizationWeights(
        IEnumerable<PersonalizedRecommendationDto> items,
        UserPersonalizationProfileDto profile,
        PersonalizedFeedConfigDto config)
    {
        // This would apply personalization weights based on user preferences
        return items; // Simplified for now
    }

    private async Task<IEnumerable<PersonalizedRecommendationDto>> GetFallbackFeedAsync(int limit)
    {
        return await GetFallbackRecommendationsAsync("posts", limit);
    }

    // Additional placeholder methods would be implemented here...
    private float CalculateInterestSimilarity(Dictionary<string, float> interests1, Dictionary<string, float> interests2) => 0.5f;
    private float CalculateContentTypeSimilarity(Dictionary<string, float> prefs1, Dictionary<string, float> prefs2) => 0.5f;
    private float CalculateEngagementSimilarity(Dictionary<string, float> patterns1, Dictionary<string, float> patterns2) => 0.5f;
    private IEnumerable<string> GetCommonInterests(Dictionary<string, float> interests1, Dictionary<string, float> interests2) => new[] { "technology", "sports" };
    private async Task<IEnumerable<string>> GetSharedFollowsAsync(int userId1, int userId2) => new[] { "user123", "user456" };
    private string GenerateSimilarityReason(float similarity, IEnumerable<string> commonInterests, IEnumerable<string> sharedFollows) => "Similar interests and connections";
    private async Task<ContentEmbeddingDto?> GetOrGenerateContentEmbeddingAsync(string contentType, int contentId) => null;
    private float CalculateCosineSimilarity(float[] vector1, float[] vector2) => 0.5f;
    private async Task<object?> GetContentByTypeAndIdAsync(string contentType, int contentId) => null;
    private IEnumerable<string> GenerateContentSimilarityReasons(ContentEmbeddingDto source, ContentEmbeddingDto candidate, float similarity) => new[] { "Similar content" };
    private ContentEmbeddingDto MapToEmbeddingDto(Models.Personalization.ContentEmbedding embedding) => new(embedding.ContentType, embedding.ContentId, new float[0], embedding.Dimensions, embedding.ModelVersion, embedding.QualityScore, embedding.CreatedAt);
    private async Task<float[]> GenerateEmbeddingVectorAsync(object content, string contentType) => new float[128];
    private float CalculateEmbeddingQuality(float[] vector, object content) => 0.8f;
    private float HashUserId(int userId, string experimentName) => (userId + experimentName.GetHashCode()) % 1000 / 1000.0f;
    private async Task<(float EngagementRate, float ClickThroughRate, float TimeSpent, float SatisfactionScore, bool IsStatisticallySignificant)> CalculateExperimentMetricsAsync(List<int> userIds, DateTime startDate, DateTime? endDate) => (0.5f, 0.3f, 120f, 0.7f, true);
    private string MapInteractionToUpdateType(string interactionType) => "interest_boost";
    private float CalculateConfidenceChange(Models.Personalization.UserInteractionEvent interaction) => 0.01f;
    private async Task<Dictionary<string, object>> ExtractContentFeaturesAsync(object content, string contentType) => new();
    private Dictionary<string, object> ExtractInteractionContext(Models.Personalization.UserInteractionEvent interaction) => new();
    private float CalculateEngagementScore(Models.Personalization.UserInteractionEvent interaction) => interaction.InteractionStrength;
    private Dictionary<string, float> ExpandQueryWithUserInterests(string query, Dictionary<string, float> interests) => new();
    private async Task<IEnumerable<PersonalizedRecommendationDto>> SearchContentWithPersonalization(int userId, string query, Dictionary<string, float> queryExpansion, string contentType, UserPersonalizationProfileDto? profile, int limit) => Enumerable.Empty<PersonalizedRecommendationDto>();
    private async Task<float> CalculateRecommendationQualityAsync(int userId, IEnumerable<PersonalizedRecommendationDto> recommendations) => 0.7f;
    private float CalculateValidationDiversityScore(List<float> results) => results.Any() ? results.Average() : 0.5f;
    private float CalculateValidationNoveltyScore(List<float> results) => results.Any() ? results.Average() : 0.5f;
    private async Task<Dictionary<string, float>> FindSimilarUsersForProfileAsync(int userId) => new();
    private async Task<IEnumerable<InterestInsightDto>> GetTopInterestsWithTrendsAsync(int userId, Dictionary<string, float> interestScores) => Enumerable.Empty<InterestInsightDto>();
    private async Task<IEnumerable<ContentTypeInsightDto>> GetContentPreferencesAsync(int userId, Dictionary<string, float> contentTypePreferences) => Enumerable.Empty<ContentTypeInsightDto>();
    private IEnumerable<EngagementPatternDto> GetEngagementPatternsFromProfile(Dictionary<string, float> engagementPatterns) => Enumerable.Empty<EngagementPatternDto>();
    private async Task<IEnumerable<UserSimilarityDto>> GetSimilarUsersInsightsAsync(int userId, Dictionary<string, float> similarUsers) => Enumerable.Empty<UserSimilarityDto>();
    private IEnumerable<string> GenerateRecommendationTips(UserPersonalizationProfileDto profile) => new[] { "Interact with more content to improve recommendations" };

    #endregion
}
