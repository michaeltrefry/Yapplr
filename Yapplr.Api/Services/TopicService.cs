using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Common;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Services.Analytics;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for topic-based feed functionality
/// </summary>
public class TopicService : ITopicService
{
    private readonly YapplrDbContext _context;
    private readonly ITagAnalyticsService _tagAnalyticsService;
    private readonly ITrendingService _trendingService;
    private readonly ILogger<TopicService> _logger;

    // Predefined topic categories and their hashtag patterns
    private readonly Dictionary<string, string[]> _topicCategories = new()
    {
        ["Technology"] = new[] { "tech", "ai", "programming", "code", "software", "development", "startup", "innovation" },
        ["Sports"] = new[] { "sports", "football", "basketball", "soccer", "tennis", "baseball", "fitness", "workout" },
        ["Arts & Entertainment"] = new[] { "art", "music", "movie", "film", "photography", "creative", "design", "entertainment" },
        ["News & Politics"] = new[] { "news", "politics", "breaking", "election", "government", "policy", "current" },
        ["Food & Lifestyle"] = new[] { "food", "recipe", "cooking", "lifestyle", "travel", "fashion", "health", "wellness" },
        ["Science"] = new[] { "science", "research", "study", "discovery", "experiment", "biology", "physics", "chemistry" },
        ["Business"] = new[] { "business", "entrepreneur", "marketing", "finance", "economy", "investment", "startup" },
        ["Gaming"] = new[] { "gaming", "game", "esports", "streamer", "twitch", "xbox", "playstation", "nintendo" }
    };

    public TopicService(
        YapplrDbContext context,
        ITagAnalyticsService tagAnalyticsService,
        ITrendingService trendingService,
        ILogger<TopicService> logger)
    {
        _context = context;
        _tagAnalyticsService = tagAnalyticsService;
        _trendingService = trendingService;
        _logger = logger;
    }

    #region Topic Management

    public async Task<IEnumerable<TopicDto>> GetTopicsAsync(string? category = null, bool? featured = null, int? userId = null)
    {
        var query = _context.Topics.Where(t => t.IsActive);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category == category);
        }

        if (featured.HasValue)
        {
            query = query.Where(t => t.IsFeatured == featured.Value);
        }

        var topics = await query
            .OrderByDescending(t => t.IsFeatured)
            .ThenByDescending(t => t.FollowerCount)
            .ThenBy(t => t.Name)
            .ToListAsync();

        var result = new List<TopicDto>();
        foreach (var topic in topics)
        {
            var isFollowed = userId.HasValue && await IsFollowingTopicAsync(userId.Value, topic.Name);
            result.Add(MapToTopicDto(topic, isFollowed));
        }

        return result;
    }

    public async Task<TopicDto?> GetTopicAsync(string identifier, int? userId = null)
    {
        Topic? topic = null;

        // Try to parse as ID first, then fall back to slug
        if (int.TryParse(identifier, out var topicId))
        {
            topic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == topicId && t.IsActive);
        }

        if (topic == null)
        {
            topic = await _context.Topics.FirstOrDefaultAsync(t => t.Slug == identifier && t.IsActive);
        }

        if (topic == null)
        {
            return null;
        }

        var isFollowed = userId.HasValue && await IsFollowingTopicAsync(userId.Value, topic.Name);
        return MapToTopicDto(topic, isFollowed);
    }

    public async Task<TopicSearchResultDto> SearchTopicsAsync(string query, int? userId = null, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new TopicSearchResultDto(
                ExactMatches: Enumerable.Empty<TopicDto>(),
                PartialMatches: Enumerable.Empty<TopicDto>(),
                Recommendations: Enumerable.Empty<TopicRecommendationDto>(),
                SuggestedHashtags: Enumerable.Empty<string>(),
                TotalResults: 0
            );
        }

        var normalizedQuery = query.ToLowerInvariant().Trim();

        // Exact matches
        var exactMatches = await _context.Topics
            .Where(t => t.IsActive && t.Name.ToLower() == normalizedQuery)
            .Take(5)
            .ToListAsync();

        // Partial matches
        var partialMatches = await _context.Topics
            .Where(t => t.IsActive && 
                       t.Name.ToLower() != normalizedQuery &&
                       (t.Name.ToLower().Contains(normalizedQuery) || 
                        t.Description.ToLower().Contains(normalizedQuery) ||
                        t.RelatedHashtags.ToLower().Contains(normalizedQuery)))
            .Take(limit - exactMatches.Count)
            .ToListAsync();

        // Get recommendations if user is provided
        var recommendations = userId.HasValue 
            ? await GetTopicRecommendationsAsync(userId.Value, 5)
            : Enumerable.Empty<TopicRecommendationDto>();

        // Suggest related hashtags
        var suggestedHashtags = await GetSuggestedHashtagsAsync(normalizedQuery, 10);

        var exactDtos = new List<TopicDto>();
        var partialDtos = new List<TopicDto>();

        foreach (var topic in exactMatches)
        {
            var isFollowed = userId.HasValue && await IsFollowingTopicAsync(userId.Value, topic.Name);
            exactDtos.Add(MapToTopicDto(topic, isFollowed));
        }

        foreach (var topic in partialMatches)
        {
            var isFollowed = userId.HasValue && await IsFollowingTopicAsync(userId.Value, topic.Name);
            partialDtos.Add(MapToTopicDto(topic, isFollowed));
        }

        return new TopicSearchResultDto(
            ExactMatches: exactDtos,
            PartialMatches: partialDtos,
            Recommendations: recommendations,
            SuggestedHashtags: suggestedHashtags,
            TotalResults: exactMatches.Count + partialMatches.Count
        );
    }

    public async Task<IEnumerable<TopicRecommendationDto>> GetTopicRecommendationsAsync(int userId, int limit = 10)
    {
        try
        {
            // Get user's current interests from hashtag usage
            var userInterests = await GetUserHashtagInterestsAsync(userId);
            var followedTopics = await GetUserTopicsAsync(userId);
            var followedTopicNames = followedTopics.Select(tf => tf.TopicName.ToLowerInvariant()).ToHashSet();

            // Get all available topics not already followed
            var availableTopics = await _context.Topics
                .Where(t => t.IsActive && !followedTopicNames.Contains(t.Name.ToLower()))
                .ToListAsync();

            var recommendations = new List<TopicRecommendationDto>();

            foreach (var topic in availableTopics)
            {
                var score = CalculateTopicRecommendationScore(topic, userInterests);
                if (score > 0.1) // Only recommend topics with meaningful scores
                {
                    var matchingInterests = GetMatchingInterests(topic, userInterests);
                    var samplePosts = await GetTopicSamplePostsAsync(topic.Name, 3);
                    var reason = GenerateRecommendationReason(score, matchingInterests);

                    recommendations.Add(new TopicRecommendationDto(
                        Topic: MapToTopicDto(topic, false),
                        RecommendationScore: score,
                        RecommendationReason: reason,
                        MatchingInterests: matchingInterests,
                        SamplePosts: samplePosts,
                        IsPersonalized: true
                    ));
                }
            }

            return recommendations
                .OrderByDescending(r => r.RecommendationScore)
                .Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting topic recommendations for user {UserId}", userId);
            return Enumerable.Empty<TopicRecommendationDto>();
        }
    }

    #endregion

    #region Topic Following

    public async Task<TopicFollowDto> FollowTopicAsync(int userId, CreateTopicFollowDto createDto)
    {
        // Check if already following
        var existingFollow = await _context.TopicFollows
            .FirstOrDefaultAsync(tf => tf.UserId == userId && tf.TopicName.ToLower() == createDto.TopicName.ToLower());

        if (existingFollow != null)
        {
            throw new InvalidOperationException($"User is already following topic '{createDto.TopicName}'");
        }

        var topicFollow = new TopicFollow
        {
            UserId = userId,
            TopicName = createDto.TopicName,
            TopicDescription = createDto.TopicDescription,
            Category = createDto.Category,
            RelatedHashtags = string.Join(",", createDto.RelatedHashtags),
            InterestLevel = createDto.InterestLevel,
            IncludeInMainFeed = createDto.IncludeInMainFeed,
            EnableNotifications = createDto.EnableNotifications,
            NotificationThreshold = createDto.NotificationThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TopicFollows.Add(topicFollow);

        // Update topic follower count if it's a predefined topic
        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name.ToLower() == createDto.TopicName.ToLower());
        if (topic != null)
        {
            topic.FollowerCount++;
            topic.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} followed topic '{TopicName}'", userId, createDto.TopicName);

        return MapToTopicFollowDto(topicFollow);
    }

    public async Task<bool> UnfollowTopicAsync(int userId, string topicName)
    {
        var topicFollow = await _context.TopicFollows
            .FirstOrDefaultAsync(tf => tf.UserId == userId && tf.TopicName.ToLower() == topicName.ToLower());

        if (topicFollow == null)
        {
            return false;
        }

        _context.TopicFollows.Remove(topicFollow);

        // Update topic follower count if it's a predefined topic
        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name.ToLower() == topicName.ToLower());
        if (topic != null && topic.FollowerCount > 0)
        {
            topic.FollowerCount--;
            topic.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} unfollowed topic '{TopicName}'", userId, topicName);

        return true;
    }

    public async Task<TopicFollowDto?> UpdateTopicFollowAsync(int userId, string topicName, UpdateTopicFollowDto updateDto)
    {
        var topicFollow = await _context.TopicFollows
            .FirstOrDefaultAsync(tf => tf.UserId == userId && tf.TopicName.ToLower() == topicName.ToLower());

        if (topicFollow == null)
        {
            return null;
        }

        if (updateDto.InterestLevel.HasValue)
            topicFollow.InterestLevel = updateDto.InterestLevel.Value;

        if (updateDto.IncludeInMainFeed.HasValue)
            topicFollow.IncludeInMainFeed = updateDto.IncludeInMainFeed.Value;

        if (updateDto.EnableNotifications.HasValue)
            topicFollow.EnableNotifications = updateDto.EnableNotifications.Value;

        if (updateDto.NotificationThreshold.HasValue)
            topicFollow.NotificationThreshold = updateDto.NotificationThreshold.Value;

        topicFollow.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToTopicFollowDto(topicFollow);
    }

    public async Task<IEnumerable<TopicFollowDto>> GetUserTopicsAsync(int userId, bool? includeInMainFeed = null)
    {
        var query = _context.TopicFollows.Where(tf => tf.UserId == userId);

        if (includeInMainFeed.HasValue)
        {
            query = query.Where(tf => tf.IncludeInMainFeed == includeInMainFeed.Value);
        }

        var topicFollows = await query
            .OrderByDescending(tf => tf.InterestLevel)
            .ThenBy(tf => tf.TopicName)
            .ToListAsync();

        return topicFollows.Select(MapToTopicFollowDto);
    }

    public async Task<bool> IsFollowingTopicAsync(int userId, string topicName)
    {
        return await _context.TopicFollows
            .AnyAsync(tf => tf.UserId == userId && tf.TopicName.ToLower() == topicName.ToLower());
    }

    #endregion

    #region Topic Feeds

    public async Task<TopicFeedDto> GetTopicFeedAsync(string topicName, int? userId = null, TopicFeedConfigDto? config = null)
    {
        var startTime = DateTime.UtcNow;
        config ??= GetDefaultTopicFeedConfig();

        try
        {
            // Get topic hashtags
            var topicHashtags = await GetTopicHashtagsAsync(topicName);

            // Get posts for this topic
            var posts = await GetPostsByTopicAsync(topicName, userId, config);

            // Get trending hashtags for this topic
            var trendingHashtags = await _tagAnalyticsService.GetTrendingHashtagsWithVelocityAsync(
                config.TimeWindowHours, 10, DetermineTopicCategory(topicName));

            // Get top contributors
            var topContributors = await GetTopicContributorsAsync(topicName, 5);

            // Calculate metrics
            var metrics = new TopicFeedMetricsDto(
                TotalPosts: posts.Count(),
                TotalEngagement: posts.Sum(p => p.LikeCount + p.CommentCount + p.RepostCount),
                UniqueContributors: posts.Select(p => p.User.Id).Distinct().Count(),
                AvgEngagementRate: CalculateAverageEngagementRate(posts),
                TrendingScore: await CalculateTopicTrendingScoreAsync(topicName),
                GrowthRate: await CalculateTopicGrowthRateAsync(topicName),
                GenerationTime: DateTime.UtcNow - startTime
            );

            return new TopicFeedDto(
                TopicName: topicName,
                Category: DetermineTopicCategory(topicName),
                Posts: posts,
                TrendingHashtags: trendingHashtags.Where(h => topicHashtags.Contains(h.Name.ToLowerInvariant())),
                TopContributors: topContributors,
                Metrics: metrics,
                GeneratedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating topic feed for '{TopicName}'", topicName);
            throw;
        }
    }

    public async Task<PersonalizedTopicFeedDto> GetPersonalizedTopicFeedAsync(int userId, TopicFeedConfigDto? config = null)
    {
        var startTime = DateTime.UtcNow;
        config ??= GetDefaultTopicFeedConfig();

        try
        {
            // Get user's followed topics
            var followedTopics = await GetUserTopicsAsync(userId, includeInMainFeed: true);

            // Generate feeds for each topic
            var topicFeeds = new List<TopicFeedDto>();
            foreach (var topicFollow in followedTopics.Take(config.MaxTopics))
            {
                var topicFeed = await GetTopicFeedAsync(topicFollow.TopicName, userId, config);
                topicFeeds.Add(topicFeed);
            }

            // Generate mixed feed
            var mixedFeed = await GetMixedTopicFeedAsync(userId, config);

            // Calculate metrics
            var metrics = new PersonalizedFeedMetricsDto(
                TotalTopicsFollowed: followedTopics.Count(),
                TotalPosts: mixedFeed.Count(),
                PersonalizationScore: CalculatePersonalizationScore(followedTopics, mixedFeed),
                ActiveTopics: followedTopics.Select(tf => tf.TopicName),
                GenerationTime: DateTime.UtcNow - startTime
            );

            return new PersonalizedTopicFeedDto(
                UserId: userId,
                TopicFeeds: topicFeeds,
                MixedFeed: mixedFeed,
                Metrics: metrics,
                GeneratedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating personalized topic feed for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<PostDto>> GetMixedTopicFeedAsync(int userId, TopicFeedConfigDto? config = null)
    {
        config ??= GetDefaultTopicFeedConfig();

        try
        {
            // Get user's followed topics with their interest levels
            var followedTopics = await GetUserTopicsAsync(userId, includeInMainFeed: true);

            if (!followedTopics.Any())
            {
                return Enumerable.Empty<PostDto>();
            }

            var allPosts = new List<(PostDto Post, float Weight)>();

            // Get posts from each followed topic, weighted by interest level
            foreach (var topicFollow in followedTopics)
            {
                var topicPosts = await GetPostsByTopicAsync(topicFollow.TopicName, userId, config);

                foreach (var post in topicPosts)
                {
                    var weight = topicFollow.InterestLevel;

                    // Boost recent posts
                    if (post.CreatedAt > DateTime.UtcNow.AddHours(-6))
                    {
                        weight *= 1.2f;
                    }

                    // Boost high-engagement posts
                    var engagementScore = (post.LikeCount + post.CommentCount + post.RepostCount) / 100.0f;
                    weight *= (1 + Math.Min(engagementScore, 1.0f));

                    allPosts.Add((post, weight));
                }
            }

            // Sort by weighted score and remove duplicates
            var sortedPosts = allPosts
                .GroupBy(p => p.Post.Id)
                .Select(g => g.OrderByDescending(p => p.Weight).First())
                .OrderByDescending(p => p.Weight)
                .Take(config.PostsPerTopic * config.MaxTopics)
                .Select(p => p.Post);

            return sortedPosts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mixed topic feed for user {UserId}", userId);
            return Enumerable.Empty<PostDto>();
        }
    }

    #endregion

    #region Topic Analytics

    public async Task<TopicAnalyticsDto?> GetTopicAnalyticsAsync(string topicName, int days = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var topicHashtags = await GetTopicHashtagsAsync(topicName);

            // Get posts in this topic
            var posts = await _context.GetPostsForFeed()
                .Where(p => p.PostTags.Any(pt => topicHashtags.Contains(pt.Tag.Name.ToLowerInvariant())) &&
                           p.CreatedAt >= cutoffDate &&
                           !p.IsHidden &&
                           !p.IsDeletedByUser &&
                           p.Privacy == PostPrivacy.Public &&
                           p.User.Status == UserStatus.Active)
                .ToListAsync();

            if (!posts.Any())
            {
                return null;
            }

            var totalEngagement = posts.Sum(p => p.Reactions.Count + p.Children.Count + p.Reposts.Count);
            var uniquePosters = posts.Select(p => p.UserId).Distinct().Count();
            var avgEngagementRate = posts.Any() ? (float)totalEngagement / posts.Count : 0;

            // Get trending hashtags for this topic
            var trendingHashtags = await _tagAnalyticsService.GetTrendingHashtagsWithVelocityAsync(
                days * 24, 10, DetermineTopicCategory(topicName));

            return new TopicAnalyticsDto(
                TopicName: topicName,
                Category: DetermineTopicCategory(topicName),
                AnalyticsDate: DateTime.UtcNow.Date,
                PostCount: posts.Count,
                TotalEngagement: totalEngagement,
                UniquePosters: uniquePosters,
                AvgEngagementRate: avgEngagementRate,
                TrendingScore: await CalculateTopicTrendingScoreAsync(topicName),
                GrowthRate: await CalculateTopicGrowthRateAsync(topicName),
                TopHashtags: trendingHashtags.Where(h => topicHashtags.Contains(h.Name.ToLowerInvariant()))
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for topic '{TopicName}'", topicName);
            return null;
        }
    }

    public async Task<IEnumerable<TopicTrendingDto>> GetTrendingTopicsAsync(int timeWindow = 24, int limit = 10, string? category = null)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-timeWindow);
            var previousPeriodCutoff = cutoffTime.AddHours(-timeWindow);

            // Get all topics or filter by category
            var topicsQuery = _context.Topics.Where(t => t.IsActive);
            if (!string.IsNullOrEmpty(category))
            {
                topicsQuery = topicsQuery.Where(t => t.Category == category);
            }

            var topics = await topicsQuery.ToListAsync();
            var trendingTopics = new List<TopicTrendingDto>();

            foreach (var topic in topics)
            {
                var topicHashtags = topic.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim().ToLowerInvariant()).ToList();

                // Get current and previous period post counts
                var currentPosts = await _context.GetPostsForFeed()
                    .Where(p => p.PostTags.Any(pt => topicHashtags.Contains(pt.Tag.Name.ToLowerInvariant())) &&
                               p.CreatedAt >= cutoffTime &&
                               !p.IsHidden &&
                               p.Privacy == PostPrivacy.Public)
                    .CountAsync();

                var previousPosts = await _context.GetPostsForFeed()
                    .Where(p => p.PostTags.Any(pt => topicHashtags.Contains(pt.Tag.Name.ToLowerInvariant())) &&
                               p.CreatedAt >= previousPeriodCutoff &&
                               p.CreatedAt < cutoffTime &&
                               !p.IsHidden &&
                               p.Privacy == PostPrivacy.Public)
                    .CountAsync();

                var currentScore = await CalculateTopicTrendingScoreAsync(topic.Name);
                var velocityScore = previousPosts > 0 ? (double)(currentPosts - previousPosts) / previousPosts : 1.0;

                // Get driving hashtags
                var drivingHashtags = await _tagAnalyticsService.GetTrendingHashtagsWithVelocityAsync(
                    timeWindow, 5, topic.Category);

                trendingTopics.Add(new TopicTrendingDto(
                    TopicName: topic.Name,
                    Category: topic.Category,
                    CurrentTrendingScore: currentScore,
                    PreviousTrendingScore: 0, // Would need historical data
                    VelocityScore: (float)velocityScore,
                    CurrentPosts: currentPosts,
                    PreviousPosts: previousPosts,
                    DrivingHashtags: drivingHashtags.Where(h => topicHashtags.Contains(h.Name.ToLowerInvariant())),
                    AnalyzedAt: DateTime.UtcNow
                ));
            }

            return trendingTopics
                .OrderByDescending(t => t.CurrentTrendingScore)
                .Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending topics for timeWindow={TimeWindow}h, category={Category}", timeWindow, category);
            return Enumerable.Empty<TopicTrendingDto>();
        }
    }

    public async Task<TopicStatsDto?> GetTopicStatsAsync(string topicName)
    {
        try
        {
            var topicHashtags = await GetTopicHashtagsAsync(topicName);
            var now = DateTime.UtcNow;

            // Get follower count
            var followerCount = await _context.TopicFollows
                .CountAsync(tf => tf.TopicName.ToLower() == topicName.ToLower());

            // Get post counts for different periods
            var postsToday = await GetTopicPostCountAsync(topicHashtags, now.Date, now);
            var postsThisWeek = await GetTopicPostCountAsync(topicHashtags, now.AddDays(-7), now);
            var postsThisMonth = await GetTopicPostCountAsync(topicHashtags, now.AddDays(-30), now);

            var avgDailyPosts = postsThisMonth / 30.0f;

            // Get engagement rate
            var engagementRate = await CalculateTopicEngagementRateAsync(topicHashtags);

            // Get top contributors
            var topContributors = await GetTopicContributorsAsync(topicName, 5);

            return new TopicStatsDto(
                TopicName: topicName,
                TotalFollowers: followerCount,
                PostsToday: postsToday,
                PostsThisWeek: postsThisWeek,
                PostsThisMonth: postsThisMonth,
                AvgDailyPosts: avgDailyPosts,
                EngagementRate: engagementRate,
                TopContributors: topContributors,
                LastUpdated: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats for topic '{TopicName}'", topicName);
            return null;
        }
    }

    #endregion

    #region Topic Clustering

    public async Task<IEnumerable<TopicClusterDto>> GetTopicClustersAsync(int limit = 10)
    {
        try
        {
            var topics = await _context.Topics.Where(t => t.IsActive).ToListAsync();
            var clusters = new List<TopicClusterDto>();

            // Group topics by category first
            var categoryGroups = topics.GroupBy(t => t.Category);

            foreach (var categoryGroup in categoryGroups)
            {
                var categoryTopics = categoryGroup.ToList();
                if (categoryTopics.Count < 2) continue;

                // Find similar topics within the category
                var relatedTopics = new List<string>();
                var commonHashtags = new HashSet<string>();

                foreach (var topic in categoryTopics)
                {
                    relatedTopics.Add(topic.Name);
                    var hashtags = topic.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(h => h.Trim().ToLowerInvariant());

                    foreach (var hashtag in hashtags)
                    {
                        commonHashtags.Add(hashtag);
                    }
                }

                var totalPosts = await GetCategoryPostCountAsync(categoryGroup.Key);
                var similarityScore = CalculateClusterSimilarityScore(categoryTopics);

                clusters.Add(new TopicClusterDto(
                    ClusterName: categoryGroup.Key,
                    RelatedTopics: relatedTopics,
                    CommonHashtags: commonHashtags,
                    SimilarityScore: similarityScore,
                    TotalPosts: totalPosts,
                    Category: categoryGroup.Key
                ));
            }

            return clusters
                .OrderByDescending(c => c.SimilarityScore)
                .Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting topic clusters");
            return Enumerable.Empty<TopicClusterDto>();
        }
    }

    public async Task<IEnumerable<TopicDto>> GetRelatedTopicsAsync(string topicName, int limit = 5)
    {
        try
        {
            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name.ToLower() == topicName.ToLower());
            if (topic == null) return Enumerable.Empty<TopicDto>();

            var topicHashtags = topic.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim().ToLowerInvariant()).ToHashSet();

            // Find topics with overlapping hashtags
            var relatedTopics = await _context.Topics
                .Where(t => t.IsActive && t.Id != topic.Id)
                .ToListAsync();

            var scoredTopics = relatedTopics
                .Select(t => new
                {
                    Topic = t,
                    SimilarityScore = CalculateHashtagSimilarity(topicHashtags,
                        t.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(h => h.Trim().ToLowerInvariant()).ToHashSet())
                })
                .Where(x => x.SimilarityScore > 0.1)
                .OrderByDescending(x => x.SimilarityScore)
                .Take(limit);

            return scoredTopics.Select(x => MapToTopicDto(x.Topic, false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related topics for '{TopicName}'", topicName);
            return Enumerable.Empty<TopicDto>();
        }
    }

    #endregion

    #region Bulk Operations

    public async Task<IEnumerable<TopicFollowDto>> BulkTopicOperationAsync(int userId, BulkTopicOperationDto operation)
    {
        var results = new List<TopicFollowDto>();

        try
        {
            foreach (var topicName in operation.TopicNames)
            {
                switch (operation.Operation.ToLowerInvariant())
                {
                    case "follow":
                        if (operation.OperationData is CreateTopicFollowDto createDto)
                        {
                            try
                            {
                                var result = await FollowTopicAsync(userId, createDto with { TopicName = topicName });
                                results.Add(result);
                            }
                            catch (InvalidOperationException)
                            {
                                // Already following - skip
                            }
                        }
                        break;

                    case "unfollow":
                        await UnfollowTopicAsync(userId, topicName);
                        break;

                    case "update_preferences":
                        if (operation.OperationData is UpdateTopicFollowDto updateDto)
                        {
                            var result = await UpdateTopicFollowAsync(userId, topicName, updateDto);
                            if (result != null)
                            {
                                results.Add(result);
                            }
                        }
                        break;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk topic operation '{Operation}' for user {UserId}", operation.Operation, userId);
            return results;
        }
    }

    public async Task<IEnumerable<TopicFollowDto>> ImportTopicsFromHashtagUsageAsync(int userId, int minUsageCount = 3)
    {
        try
        {
            // Get user's most used hashtags
            var userHashtags = await _context.PostTags
                .Where(pt => pt.Post.UserId == userId &&
                            pt.CreatedAt >= DateTime.UtcNow.AddDays(-90)) // Last 90 days
                .GroupBy(pt => pt.Tag.Name)
                .Where(g => g.Count() >= minUsageCount)
                .OrderByDescending(g => g.Count())
                .Take(20)
                .Select(g => g.Key.ToLowerInvariant())
                .ToListAsync();

            var importedTopics = new List<TopicFollowDto>();

            // Map hashtags to topic categories
            foreach (var categoryKvp in _topicCategories)
            {
                var category = categoryKvp.Key;
                var categoryHashtags = categoryKvp.Value;

                var matchingHashtags = userHashtags.Where(uh =>
                    categoryHashtags.Any(ch => uh.Contains(ch) || ch.Contains(uh))).ToList();

                if (matchingHashtags.Any())
                {
                    var interestLevel = Math.Min(1.0f, matchingHashtags.Count / 5.0f); // Max 5 hashtags = 100% interest

                    var createDto = new CreateTopicFollowDto(
                        TopicName: category,
                        TopicDescription: $"Auto-imported based on your usage of: {string.Join(", ", matchingHashtags.Take(3))}",
                        Category: category,
                        RelatedHashtags: matchingHashtags,
                        InterestLevel: interestLevel,
                        IncludeInMainFeed: interestLevel > 0.3f,
                        EnableNotifications: false
                    );

                    try
                    {
                        var result = await FollowTopicAsync(userId, createDto);
                        importedTopics.Add(result);
                    }
                    catch (InvalidOperationException)
                    {
                        // Already following - skip
                    }
                }
            }

            _logger.LogInformation("Imported {Count} topics for user {UserId} from hashtag usage", importedTopics.Count, userId);
            return importedTopics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing topics from hashtag usage for user {UserId}", userId);
            return Enumerable.Empty<TopicFollowDto>();
        }
    }

    #endregion

    #region Helper Methods

    public async Task<double> CalculateTopicSimilarityAsync(IEnumerable<string> topic1Hashtags, IEnumerable<string> topic2Hashtags)
    {
        var hashtags1 = topic1Hashtags.Select(h => h.ToLowerInvariant()).ToHashSet();
        var hashtags2 = topic2Hashtags.Select(h => h.ToLowerInvariant()).ToHashSet();

        return CalculateHashtagSimilarity(hashtags1, hashtags2);
    }

    private TopicFeedConfigDto GetDefaultTopicFeedConfig()
    {
        return new TopicFeedConfigDto(
            PostsPerTopic: 10,
            MaxTopics: 5,
            IncludeTrendingContent: true,
            IncludePersonalizedContent: true,
            MinInterestLevel: 0.1f,
            TimeWindowHours: 24,
            SortBy: "personalized"
        );
    }

    private async Task<IEnumerable<string>> GetTopicHashtagsAsync(string topicName)
    {
        // First try to get from predefined topics
        var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Name.ToLower() == topicName.ToLower());
        if (topic != null)
        {
            return topic.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim().ToLowerInvariant());
        }

        // Fall back to user-defined topic
        var userTopic = await _context.TopicFollows.FirstOrDefaultAsync(tf => tf.TopicName.ToLower() == topicName.ToLower());
        if (userTopic != null)
        {
            return userTopic.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim().ToLowerInvariant());
        }

        // Fall back to category-based hashtags
        if (_topicCategories.ContainsKey(topicName))
        {
            return _topicCategories[topicName].Select(h => h.ToLowerInvariant());
        }

        return new[] { topicName.ToLowerInvariant() };
    }

    private async Task<IEnumerable<PostDto>> GetPostsByTopicAsync(string topicName, int? userId, TopicFeedConfigDto config)
    {
        var topicHashtags = await GetTopicHashtagsAsync(topicName);
        var blockedUserIds = userId.HasValue
            ? await _context.GetBlockedUserIdsAsync(userId.Value)
            : new HashSet<int>();

        var posts = await _context.GetPostsForFeed()
            .Where(p => p.PostTags.Any(pt => topicHashtags.Contains(pt.Tag.Name.ToLowerInvariant())) &&
                       !p.IsHidden &&
                       !p.IsDeletedByUser &&
                       p.Privacy == PostPrivacy.Public &&
                       p.User.Status == UserStatus.Active &&
                       !blockedUserIds.Contains(p.UserId))
            .OrderByDescending(p => p.CreatedAt)
            .Take(config.PostsPerTopic)
            .ToListAsync();

        return posts.Select(p => p.MapToPostDto(userId));
    }

    private async Task<IEnumerable<UserDto>> GetTopicContributorsAsync(string topicName, int limit)
    {
        var topicHashtags = await GetTopicHashtagsAsync(topicName);

        var topContributors = await _context.PostTags
            .Where(pt => topicHashtags.Contains(pt.Tag.Name.ToLowerInvariant()) &&
                        !pt.Post.IsHidden &&
                        pt.Post.User.Status == UserStatus.Active &&
                        pt.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(pt => pt.Post.UserId)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.First().Post.User)
            .ToListAsync();

        return topContributors.Select(u => u.MapToUserDto());
    }

    private string DetermineTopicCategory(string topicName)
    {
        foreach (var categoryKvp in _topicCategories)
        {
            if (categoryKvp.Value.Any(h => topicName.ToLowerInvariant().Contains(h)))
            {
                return categoryKvp.Key;
            }
        }
        return "General";
    }

    private async Task<float> CalculateTopicTrendingScoreAsync(string topicName)
    {
        // Simplified trending score calculation
        var topicHashtags = await GetTopicHashtagsAsync(topicName);
        var recentPosts = await _context.GetPostsForFeed()
            .Where(p => p.PostTags.Any(pt => topicHashtags.Contains(pt.Tag.Name.ToLowerInvariant())) &&
                       p.CreatedAt >= DateTime.UtcNow.AddHours(-24) &&
                       !p.IsHidden &&
                       p.Privacy == PostPrivacy.Public)
            .CountAsync();

        return Math.Min(1.0f, recentPosts / 100.0f); // Normalize to 0-1 scale
    }

    private async Task<float> CalculateTopicGrowthRateAsync(string topicName)
    {
        var topicHashtags = await GetTopicHashtagsAsync(topicName);
        var now = DateTime.UtcNow;
        var yesterday = now.AddDays(-1);
        var dayBefore = now.AddDays(-2);

        var todayPosts = await GetTopicPostCountAsync(topicHashtags, yesterday, now);
        var yesterdayPosts = await GetTopicPostCountAsync(topicHashtags, dayBefore, yesterday);

        if (yesterdayPosts == 0) return todayPosts > 0 ? 1.0f : 0.0f;
        return (float)(todayPosts - yesterdayPosts) / yesterdayPosts;
    }

    private async Task<int> GetTopicPostCountAsync(IEnumerable<string> topicHashtags, DateTime startTime, DateTime endTime)
    {
        return await _context.GetPostsForFeed()
            .Where(p => p.PostTags.Any(pt => topicHashtags.Contains(pt.Tag.Name.ToLowerInvariant())) &&
                       p.CreatedAt >= startTime &&
                       p.CreatedAt < endTime &&
                       !p.IsHidden &&
                       p.Privacy == PostPrivacy.Public)
            .CountAsync();
    }

    private async Task<int> GetCategoryPostCountAsync(string category)
    {
        if (!_topicCategories.ContainsKey(category)) return 0;

        var categoryHashtags = _topicCategories[category];
        return await _context.GetPostsForFeed()
            .Where(p => p.PostTags.Any(pt => categoryHashtags.Contains(pt.Tag.Name.ToLowerInvariant())) &&
                       p.CreatedAt >= DateTime.UtcNow.AddDays(-7) &&
                       !p.IsHidden &&
                       p.Privacy == PostPrivacy.Public)
            .CountAsync();
    }

    private async Task<float> CalculateTopicEngagementRateAsync(IEnumerable<string> topicHashtags)
    {
        var posts = await _context.GetPostsForFeed()
            .Where(p => p.PostTags.Any(pt => topicHashtags.Contains(pt.Tag.Name.ToLowerInvariant())) &&
                       p.CreatedAt >= DateTime.UtcNow.AddDays(-7) &&
                       !p.IsHidden &&
                       p.Privacy == PostPrivacy.Public)
            .ToListAsync();

        if (!posts.Any()) return 0;

        var totalEngagement = posts.Sum(p => p.Reactions.Count + p.Children.Count + p.Reposts.Count);
        return (float)totalEngagement / posts.Count;
    }

    private float CalculateAverageEngagementRate(IEnumerable<PostDto> posts)
    {
        if (!posts.Any()) return 0;

        var totalEngagement = posts.Sum(p => p.LikeCount + p.CommentCount + p.RepostCount);
        return (float)totalEngagement / posts.Count();
    }

    private float CalculatePersonalizationScore(IEnumerable<TopicFollowDto> followedTopics, IEnumerable<PostDto> mixedFeed)
    {
        if (!followedTopics.Any() || !mixedFeed.Any()) return 0;

        var avgInterestLevel = followedTopics.Average(tf => tf.InterestLevel);
        var feedDiversity = mixedFeed.Select(p => p.User.Id).Distinct().Count() / (float)mixedFeed.Count();

        return (avgInterestLevel + feedDiversity) / 2;
    }

    private double CalculateTopicRecommendationScore(Topic topic, HashSet<string> userInterests)
    {
        var topicHashtags = topic.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(h => h.Trim().ToLowerInvariant()).ToHashSet();

        var matchingInterests = userInterests.Intersect(topicHashtags).Count();
        var totalInterests = userInterests.Union(topicHashtags).Count();

        if (totalInterests == 0) return 0;

        var baseScore = (double)matchingInterests / totalInterests;

        // Boost popular topics slightly
        var popularityBoost = Math.Min(0.2, topic.FollowerCount / 1000.0);

        // Boost featured topics
        var featuredBoost = topic.IsFeatured ? 0.1 : 0;

        return Math.Min(1.0, baseScore + popularityBoost + featuredBoost);
    }

    private IEnumerable<string> GetMatchingInterests(Topic topic, HashSet<string> userInterests)
    {
        var topicHashtags = topic.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(h => h.Trim().ToLowerInvariant()).ToHashSet();

        return userInterests.Intersect(topicHashtags);
    }

    private async Task<IEnumerable<PostDto>> GetTopicSamplePostsAsync(string topicName, int limit)
    {
        var topicHashtags = await GetTopicHashtagsAsync(topicName);

        var posts = await _context.GetPostsForFeed()
            .Where(p => p.PostTags.Any(pt => topicHashtags.Contains(pt.Tag.Name.ToLowerInvariant())) &&
                       !p.IsHidden &&
                       p.Privacy == PostPrivacy.Public &&
                       p.User.Status == UserStatus.Active)
            .OrderByDescending(p => p.Reactions.Count + p.Children.Count + p.Reposts.Count)
            .Take(limit)
            .ToListAsync();

        return posts.Select(p => p.MapToPostDto(null));
    }

    private string GenerateRecommendationReason(double score, IEnumerable<string> matchingInterests)
    {
        var interests = matchingInterests.ToList();

        if (interests.Count == 0)
        {
            return "Popular topic that might interest you";
        }

        if (interests.Count == 1)
        {
            return $"Based on your interest in #{interests[0]}";
        }

        return $"Based on your interests in #{string.Join(", #", interests.Take(2))}";
    }

    private async Task<HashSet<string>> GetUserHashtagInterestsAsync(int userId)
    {
        var recentDate = DateTime.UtcNow.AddDays(-60);

        var interests = await _context.PostTags
            .Where(pt => pt.Post.UserId == userId && pt.CreatedAt >= recentDate)
            .GroupBy(pt => pt.Tag.Name)
            .OrderByDescending(g => g.Count())
            .Take(30)
            .Select(g => g.Key.ToLowerInvariant())
            .ToListAsync();

        return interests.ToHashSet();
    }

    private async Task<IEnumerable<string>> GetSuggestedHashtagsAsync(string query, int limit)
    {
        var suggestions = await _context.Tags
            .Where(t => t.Name.ToLower().Contains(query) && t.PostCount > 5)
            .OrderByDescending(t => t.PostCount)
            .Take(limit)
            .Select(t => t.Name)
            .ToListAsync();

        return suggestions;
    }

    private float CalculateClusterSimilarityScore(List<Topic> topics)
    {
        if (topics.Count < 2) return 0;

        var allHashtags = new HashSet<string>();
        var topicHashtagSets = new List<HashSet<string>>();

        foreach (var topic in topics)
        {
            var hashtags = topic.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim().ToLowerInvariant()).ToHashSet();

            topicHashtagSets.Add(hashtags);
            foreach (var hashtag in hashtags)
            {
                allHashtags.Add(hashtag);
            }
        }

        // Calculate average pairwise similarity
        var similarities = new List<double>();
        for (int i = 0; i < topicHashtagSets.Count; i++)
        {
            for (int j = i + 1; j < topicHashtagSets.Count; j++)
            {
                var similarity = CalculateHashtagSimilarity(topicHashtagSets[i], topicHashtagSets[j]);
                similarities.Add(similarity);
            }
        }

        return similarities.Any() ? (float)similarities.Average() : 0;
    }

    private double CalculateHashtagSimilarity(HashSet<string> hashtags1, HashSet<string> hashtags2)
    {
        if (!hashtags1.Any() || !hashtags2.Any()) return 0;

        var intersection = hashtags1.Intersect(hashtags2).Count();
        var union = hashtags1.Union(hashtags2).Count();

        return union > 0 ? (double)intersection / union : 0;
    }

    private TopicDto MapToTopicDto(Topic topic, bool isFollowed)
    {
        var relatedHashtags = topic.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(h => h.Trim());

        return new TopicDto(
            Id: topic.Id,
            Name: topic.Name,
            Description: topic.Description,
            Category: topic.Category,
            RelatedHashtags: relatedHashtags,
            Slug: topic.Slug,
            Icon: topic.Icon,
            Color: topic.Color,
            IsFeatured: topic.IsFeatured,
            FollowerCount: topic.FollowerCount,
            IsFollowedByCurrentUser: isFollowed,
            CreatedAt: topic.CreatedAt
        );
    }

    private TopicFollowDto MapToTopicFollowDto(TopicFollow topicFollow)
    {
        var relatedHashtags = topicFollow.RelatedHashtags.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(h => h.Trim());

        return new TopicFollowDto(
            Id: topicFollow.Id,
            UserId: topicFollow.UserId,
            TopicName: topicFollow.TopicName,
            TopicDescription: topicFollow.TopicDescription,
            Category: topicFollow.Category,
            RelatedHashtags: relatedHashtags,
            InterestLevel: topicFollow.InterestLevel,
            IncludeInMainFeed: topicFollow.IncludeInMainFeed,
            EnableNotifications: topicFollow.EnableNotifications,
            NotificationThreshold: topicFollow.NotificationThreshold,
            CreatedAt: topicFollow.CreatedAt
        );
    }

    #endregion
}
