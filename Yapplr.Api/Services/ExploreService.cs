using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Common;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Services.Analytics;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for comprehensive content discovery and exploration features
/// </summary>
public class ExploreService : IExploreService
{
    private readonly YapplrDbContext _context;
    private readonly ITrendingService _trendingService;
    private readonly ITagAnalyticsService _tagAnalyticsService;
    private readonly IUserService _userService;
    private readonly ILogger<ExploreService> _logger;

    // Algorithm weights for user similarity calculation
    private readonly Dictionary<string, double> _similarityWeights = new()
    {
        ["sharedInterests"] = 0.3,      // 30% weight on shared hashtag interests
        ["mutualFollows"] = 0.25,       // 25% weight on mutual follows
        ["interactionHistory"] = 0.2,   // 20% weight on interaction history
        ["contentSimilarity"] = 0.15,   // 15% weight on content similarity
        ["activityPattern"] = 0.1       // 10% weight on activity patterns
    };

    public ExploreService(
        YapplrDbContext context,
        ITrendingService trendingService,
        ITagAnalyticsService tagAnalyticsService,
        IUserService userService,
        ILogger<ExploreService> logger)
    {
        _context = context;
        _trendingService = trendingService;
        _tagAnalyticsService = tagAnalyticsService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<ExplorePageDto> GetExplorePageAsync(int? userId = null, ExploreConfigDto? config = null)
    {
        var startTime = DateTime.UtcNow;
        config ??= GetDefaultExploreConfig();

        try
        {
            // Fetch components sequentially to avoid DbContext concurrency issues
            var trendingPosts = await _trendingService.GetTrendingPostsAsync(
                config.TimeWindowHours, config.TrendingPostsLimit, userId);

            var trendingHashtags = await GetTrendingHashtagsSafeAsync(
                config.TimeWindowHours, config.TrendingHashtagsLimit);

            var trendingCategories = await _tagAnalyticsService.GetTrendingHashtagsByCategoryAsync(
                config.TimeWindowHours, 5);

            var recommendedUsers = userId.HasValue && config.IncludeUserRecommendations
                ? await GetUserRecommendationsAsync(userId.Value, config.RecommendedUsersLimit, config.MinSimilarityScore)
                : Enumerable.Empty<UserRecommendationDto>();

            var personalizedPosts = userId.HasValue && config.IncludePersonalizedContent
                ? await _trendingService.GetPersonalizedTrendingPostsAsync(userId.Value, config.TimeWindowHours, 10)
                : Enumerable.Empty<PostDto>();

            var generationTime = DateTime.UtcNow - startTime;
            var metrics = new ExploreMetricsDto(
                TotalTrendingPosts: trendingPosts.Count(),
                TotalTrendingHashtags: trendingHashtags.Count(),
                TotalRecommendedUsers: recommendedUsers.Count(),
                AverageEngagementRate: CalculateAverageEngagementRate(trendingPosts),
                PersonalizationScore: userId.HasValue ? 0.8 : 0.0, // Placeholder calculation
                GenerationTime: generationTime,
                AlgorithmVersion: "v2.0"
            );

            return new ExplorePageDto(
                TrendingPosts: trendingPosts,
                TrendingHashtags: trendingHashtags,
                TrendingCategories: trendingCategories,
                RecommendedUsers: recommendedUsers,
                PersonalizedPosts: personalizedPosts,
                Metrics: metrics,
                GeneratedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating explore page for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<UserRecommendationDto>> GetUserRecommendationsAsync(int userId, int limit = 10, double minSimilarityScore = 0.1)
    {
        try
        {
            // Get user's current following to exclude from recommendations
            var currentFollowing = await _context.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToHashSetAsync();

            // Get blocked users to exclude
            var blockedUsers = await _context.Blocks
                .Where(b => b.BlockerId == userId || b.BlockedId == userId)
                .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId)
                .ToHashSetAsync();

            // Get candidate users (active users with good trust scores)
            var candidateUsers = await _context.Users
                .Where(u => u.Id != userId &&
                           u.Status == UserStatus.Active &&
                           u.TrustScore >= 0.3f &&
                           !currentFollowing.Contains(u.Id) &&
                           !blockedUsers.Contains(u.Id))
                .Take(limit * 10) // Get more candidates for better filtering
                .ToListAsync();

            var recommendations = new List<UserRecommendationDto>();

            foreach (var candidate in candidateUsers)
            {
                var similarityScore = await CalculateUserSimilarityAsync(userId, candidate.Id);
                
                if (similarityScore >= minSimilarityScore)
                {
                    var commonInterests = await GetCommonInterestsAsync(userId, candidate.Id);
                    var mutualFollows = await GetMutualFollowsAsync(userId, candidate.Id);
                    var reason = GenerateRecommendationReason(similarityScore, commonInterests, mutualFollows);
                    
                    recommendations.Add(new UserRecommendationDto(
                        User: candidate.MapToUserDto(),
                        SimilarityScore: similarityScore,
                        RecommendationReason: reason,
                        CommonInterests: commonInterests,
                        MutualFollows: mutualFollows,
                        IsNewUser: candidate.CreatedAt > DateTime.UtcNow.AddDays(-30),
                        ActivityScore: await CalculateActivityScoreAsync(candidate.Id)
                    ));
                }
            }

            return recommendations
                .OrderByDescending(r => r.SimilarityScore)
                .Take(limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user recommendations for user {UserId}", userId);
            return Enumerable.Empty<UserRecommendationDto>();
        }
    }

    public async Task<double> CalculateUserSimilarityAsync(int userId1, int userId2)
    {
        try
        {
            var totalScore = 0.0;

            // Calculate shared interests score
            var sharedInterestsScore = await CalculateSharedInterestsScoreAsync(userId1, userId2);
            totalScore += sharedInterestsScore * _similarityWeights["sharedInterests"];

            // Calculate mutual follows score
            var mutualFollowsScore = await CalculateMutualFollowsScoreAsync(userId1, userId2);
            totalScore += mutualFollowsScore * _similarityWeights["mutualFollows"];

            // Calculate interaction history score
            var interactionScore = await CalculateInteractionHistoryScoreAsync(userId1, userId2);
            totalScore += interactionScore * _similarityWeights["interactionHistory"];

            // Calculate content similarity score
            var contentSimilarityScore = await CalculateContentSimilarityScoreAsync(userId1, userId2);
            totalScore += contentSimilarityScore * _similarityWeights["contentSimilarity"];

            // Calculate activity pattern score
            var activityPatternScore = await CalculateActivityPatternScoreAsync(userId1, userId2);
            totalScore += activityPatternScore * _similarityWeights["activityPattern"];

            return Math.Min(1.0, totalScore);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating similarity between users {UserId1} and {UserId2}", userId1, userId2);
            return 0.0;
        }
    }

    private async Task<IEnumerable<TrendingHashtagDto>> GetTrendingHashtagsSafeAsync(int timeWindow, int limit)
    {
        try
        {
            _logger.LogInformation("Attempting to get trending hashtags with timeWindow={TimeWindow}, limit={Limit}", timeWindow, limit);
            var result = await _tagAnalyticsService.GetTrendingHashtagsWithVelocityAsync(timeWindow, limit);
            _logger.LogInformation("Successfully retrieved {Count} trending hashtags", result.Count());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get trending hashtags, returning empty list. TimeWindow={TimeWindow}, Limit={Limit}", timeWindow, limit);
            return Enumerable.Empty<TrendingHashtagDto>();
        }
    }

    private ExploreConfigDto GetDefaultExploreConfig()
    {
        return new ExploreConfigDto(
            TrendingPostsLimit: 20,
            TrendingHashtagsLimit: 15,
            RecommendedUsersLimit: 10,
            TimeWindowHours: 24,
            IncludePersonalizedContent: true,
            IncludeUserRecommendations: true,
            PreferredCategories: new[] { "Technology", "Sports", "Arts & Entertainment" },
            MinSimilarityScore: 0.1
        );
    }

    private double CalculateAverageEngagementRate(IEnumerable<PostDto> posts)
    {
        if (!posts.Any()) return 0.0;
        
        return posts.Average(p =>
        {
            var totalEngagements = p.LikeCount + p.CommentCount + p.RepostCount;
            return totalEngagements > 0 ? (double)totalEngagements / Math.Max(1, 100) : 0.0; // Placeholder for views
        });
    }

    private async Task<IEnumerable<string>> GetCommonInterestsAsync(int userId1, int userId2)
    {
        var user1Interests = await GetUserInterestsAsync(userId1);
        var user2Interests = await GetUserInterestsAsync(userId2);
        
        return user1Interests.Intersect(user2Interests).Take(5);
    }

    private async Task<IEnumerable<UserDto>> GetMutualFollowsAsync(int userId1, int userId2)
    {
        var mutualFollowIds = await _context.Follows
            .Where(f1 => f1.FollowerId == userId1)
            .Join(_context.Follows.Where(f2 => f2.FollowerId == userId2),
                  f1 => f1.FollowingId,
                  f2 => f2.FollowingId,
                  (f1, f2) => f1.FollowingId)
            .Take(3)
            .ToListAsync();

        var mutualUsers = await _context.Users
            .Where(u => mutualFollowIds.Contains(u.Id))
            .ToListAsync();

        return mutualUsers.Select(u => u.MapToUserDto());
    }

    private async Task<HashSet<string>> GetUserInterestsAsync(int userId)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        var interests = await _context.TagAnalytics
            .Where(ta => ta.UserId == userId &&
                        ta.CreatedAt >= cutoffDate &&
                        (ta.Action == TagAction.Clicked || ta.Action == TagAction.Used))
            .GroupBy(ta => ta.Tag.Name)
            .OrderByDescending(g => g.Count())
            .Take(20)
            .Select(g => g.Key.ToLowerInvariant())
            .ToListAsync();

        return interests.ToHashSet();
    }

    private string GenerateRecommendationReason(double similarityScore, IEnumerable<string> commonInterests, IEnumerable<UserDto> mutualFollows)
    {
        var reasons = new List<string>();

        if (commonInterests.Any())
        {
            reasons.Add($"Shares {commonInterests.Count()} interests with you");
        }

        if (mutualFollows.Any())
        {
            reasons.Add($"Followed by {mutualFollows.Count()} people you follow");
        }

        if (similarityScore > 0.7)
        {
            reasons.Add("High similarity in content preferences");
        }

        return reasons.Any() ? string.Join(" • ", reasons) : "Recommended based on your activity";
    }

    private async Task<double> CalculateActivityScoreAsync(int userId)
    {
        var recentActivity = DateTime.UtcNow.AddDays(-7);
        
        var recentPosts = await _context.Posts
            .Where(p => p.UserId == userId && p.CreatedAt >= recentActivity)
            .CountAsync();

        var recentEngagements = await _context.PostReactions
            .Where(r => r.UserId == userId && r.CreatedAt >= recentActivity)
            .CountAsync();

        return Math.Min(1.0, (recentPosts * 0.3 + recentEngagements * 0.1) / 10.0);
    }

    public async Task<IEnumerable<SimilarUserDto>> GetSimilarUsersAsync(int userId, int limit = 10)
    {
        var recommendations = await GetUserRecommendationsAsync(userId, limit * 2, 0.3);

        return recommendations.Select(r => new SimilarUserDto(
            User: r.User,
            SimilarityScore: r.SimilarityScore,
            SharedInterests: r.CommonInterests,
            MutualConnections: r.MutualFollows,
            SimilarityReason: r.RecommendationReason
        )).Take(limit);
    }

    public async Task<IEnumerable<ContentClusterDto>> GetContentClustersAsync(int? userId = null, int limit = 5)
    {
        var trendingCategories = await _tagAnalyticsService.GetTrendingHashtagsByCategoryAsync(24, limit);
        var clusters = new List<ContentClusterDto>();

        foreach (var category in trendingCategories)
        {
            var categoryPosts = await GetPostsByCategory(category.Category, userId, 10);
            var topContributors = await GetTopContributorsByCategory(category.Category, 5);

            clusters.Add(new ContentClusterDto(
                Topic: category.Category,
                Description: category.Description ?? $"Trending content in {category.Category}",
                Posts: categoryPosts,
                RelatedHashtags: category.TrendingHashtags,
                TopContributors: topContributors,
                ClusterScore: category.CategoryGrowthRate,
                TotalPosts: category.TotalPosts
            ));
        }

        return clusters;
    }

    public async Task<IEnumerable<InterestBasedContentDto>> GetInterestBasedContentAsync(int userId, int limit = 5)
    {
        var userInterests = await GetUserInterestsAsync(userId);
        var interestContent = new List<InterestBasedContentDto>();

        foreach (var interest in userInterests.Take(limit))
        {
            var posts = await GetPostsByHashtag(interest, userId, 5);
            var topCreators = await GetTopCreatorsByHashtag(interest, 3);
            var isGrowing = await IsInterestGrowing(interest);

            interestContent.Add(new InterestBasedContentDto(
                Interest: interest,
                RecommendedPosts: posts,
                TopCreators: topCreators,
                InterestStrength: await CalculateInterestStrength(userId, interest),
                IsGrowing: isGrowing
            ));
        }

        return interestContent;
    }

    public async Task<IEnumerable<TrendingTopicDto>> GetTrendingTopicsAsync(int timeWindow = 24, int limit = 10, int? userId = null)
    {
        var trendingHashtags = await _tagAnalyticsService.GetTrendingHashtagsWithVelocityAsync(timeWindow, limit * 2);
        var topics = new List<TrendingTopicDto>();

        var groupedByCategory = trendingHashtags.GroupBy(h => h.Category);

        foreach (var categoryGroup in groupedByCategory.Take(limit))
        {
            var topHashtags = categoryGroup.OrderByDescending(h => h.TrendingScore).Take(3);
            var categoryPosts = await GetPostsByCategory(categoryGroup.Key, userId, 5);
            var topContributors = await GetTopContributorsByCategory(categoryGroup.Key, 3);

            topics.Add(new TrendingTopicDto(
                Topic: categoryGroup.Key,
                TrendingPosts: categoryPosts,
                RelatedHashtags: topHashtags,
                TopContributors: topContributors,
                TopicScore: categoryGroup.Average(h => h.TrendingScore),
                GrowthRate: categoryGroup.Average(h => h.GrowthRate),
                Category: categoryGroup.Key
            ));
        }

        return topics.OrderByDescending(t => t.TopicScore);
    }

    public async Task<IEnumerable<NetworkBasedUserDto>> GetNetworkBasedUsersAsync(int userId, int maxDegrees = 3, int limit = 10)
    {
        var networkUsers = new List<NetworkBasedUserDto>();

        // Get friends of friends (2nd degree connections)
        var secondDegreeUsers = await GetSecondDegreeConnections(userId, limit);

        foreach (var user in secondDegreeUsers)
        {
            var connectionPath = await GetConnectionPath(userId, user.Id);

            networkUsers.Add(new NetworkBasedUserDto(
                User: user.MapToUserDto(),
                NetworkScore: await CalculateNetworkScore(userId, user.Id),
                ConnectionPath: connectionPath,
                DiscoveryMethod: "Friends of friends",
                DegreesOfSeparation: 2
            ));
        }

        return networkUsers.OrderByDescending(u => u.NetworkScore).Take(limit);
    }

    public async Task<IEnumerable<ExplainedContentDto>> GetExplainedContentRecommendationsAsync(int userId, int limit = 20)
    {
        var personalizedPosts = await _trendingService.GetPersonalizedTrendingPostsAsync(userId, 24, limit);
        var explainedContent = new List<ExplainedContentDto>();

        foreach (var post in personalizedPosts)
        {
            var explanation = await GenerateContentExplanation(userId, post);
            var reasonTags = await GenerateReasonTags(userId, post);

            explainedContent.Add(new ExplainedContentDto(
                Post: post,
                RecommendationScore: 0.8, // Placeholder - would calculate based on personalization
                Explanation: explanation,
                ReasonTags: reasonTags,
                IsPersonalized: true
            ));
        }

        return explainedContent;
    }

    public async Task<IEnumerable<ExploreSectionDto>> GetExploreSectionsAsync(int? userId = null, IEnumerable<string>? sectionTypes = null)
    {
        var sections = new List<ExploreSectionDto>();
        var defaultSections = new[] { "trending_posts", "trending_hashtags", "recommended_users", "trending_topics" };
        var targetSections = sectionTypes ?? defaultSections;

        foreach (var sectionType in targetSections)
        {
            var section = await GenerateExploreSection(sectionType, userId);
            if (section != null)
            {
                sections.Add(section);
            }
        }

        return sections.OrderBy(s => s.Priority);
    }

    #region Helper Methods

    private async Task<double> CalculateSharedInterestsScoreAsync(int userId1, int userId2)
    {
        var user1Interests = await GetUserInterestsAsync(userId1);
        var user2Interests = await GetUserInterestsAsync(userId2);

        var sharedCount = user1Interests.Intersect(user2Interests).Count();
        var totalUniqueCount = user1Interests.Union(user2Interests).Count();

        return totalUniqueCount > 0 ? (double)sharedCount / totalUniqueCount : 0.0;
    }

    private async Task<double> CalculateMutualFollowsScoreAsync(int userId1, int userId2)
    {
        var user1Following = await _context.Follows
            .Where(f => f.FollowerId == userId1)
            .Select(f => f.FollowingId)
            .ToHashSetAsync();

        var user2Following = await _context.Follows
            .Where(f => f.FollowerId == userId2)
            .Select(f => f.FollowingId)
            .ToHashSetAsync();

        var mutualCount = user1Following.Intersect(user2Following).Count();
        var totalUniqueCount = user1Following.Union(user2Following).Count();

        return totalUniqueCount > 0 ? (double)mutualCount / totalUniqueCount : 0.0;
    }

    private async Task<double> CalculateInteractionHistoryScoreAsync(int userId1, int userId2)
    {
        var recentDate = DateTime.UtcNow.AddDays(-30);

        // Check for likes, comments, reposts between users
        var interactions = await _context.PostReactions
            .Where(r => ((r.UserId == userId1 && r.Post.UserId == userId2) ||
                        (r.UserId == userId2 && r.Post.UserId == userId1)) &&
                       r.CreatedAt >= recentDate)
            .CountAsync();

        return Math.Min(1.0, interactions / 10.0); // Normalize to 0-1 scale
    }

    private async Task<double> CalculateContentSimilarityScoreAsync(int userId1, int userId2)
    {
        // Simplified content similarity based on hashtag usage patterns
        var user1Tags = await GetUserHashtagUsage(userId1);
        var user2Tags = await GetUserHashtagUsage(userId2);

        var sharedTags = user1Tags.Keys.Intersect(user2Tags.Keys).Count();
        var totalUniqueTags = user1Tags.Keys.Union(user2Tags.Keys).Count();

        return totalUniqueTags > 0 ? (double)sharedTags / totalUniqueTags : 0.0;
    }

    private async Task<double> CalculateActivityPatternScoreAsync(int userId1, int userId2)
    {
        // Simplified activity pattern comparison
        var user1Activity = await GetUserActivityPattern(userId1);
        var user2Activity = await GetUserActivityPattern(userId2);

        // Compare posting frequency and engagement patterns
        var frequencyDiff = Math.Abs(user1Activity.PostingFrequency - user2Activity.PostingFrequency);
        var engagementDiff = Math.Abs(user1Activity.EngagementRate - user2Activity.EngagementRate);

        return 1.0 - Math.Min(1.0, (frequencyDiff + engagementDiff) / 2.0);
    }

    private async Task<IEnumerable<PostDto>> GetPostsByCategory(string category, int? userId, int limit)
    {
        // Get all tags and filter by category in memory to avoid EF translation issues
        var allTags = await _context.Tags
            .Select(t => t.Name)
            .ToListAsync();

        var categoryHashtags = allTags
            .Where(tagName => DetermineHashtagCategory(tagName) == category)
            .ToList();

        var posts = await _context.GetPostsForFeed()
            .Where(p => p.PostTags.Any(pt => categoryHashtags.Contains(pt.Tag.Name)) &&
                       p.PostType == PostType.Post &&
                       !p.IsHidden &&
                       !p.IsDeletedByUser &&
                       p.Privacy == PostPrivacy.Public &&
                       p.User.Status == UserStatus.Active)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return posts.Select(p => p.MapToPostDto(userId));
    }

    private async Task<IEnumerable<UserDto>> GetTopContributorsByCategory(string category, int limit)
    {
        // Get all tags and filter by category in memory to avoid EF translation issues
        var allTags = await _context.Tags
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();

        var categoryHashtagIds = allTags
            .Where(tag => DetermineHashtagCategory(tag.Name) == category)
            .Select(tag => tag.Id)
            .ToList();

        var topContributors = await _context.PostTags
            .Where(pt => categoryHashtagIds.Contains(pt.TagId) &&
                        !pt.Post.IsHidden &&
                        pt.Post.User.Status == UserStatus.Active)
            .GroupBy(pt => pt.Post.UserId)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.First().Post.User)
            .ToListAsync();

        return topContributors.Select(u => u.MapToUserDto());
    }

    private async Task<IEnumerable<PostDto>> GetPostsByHashtag(string hashtag, int? userId, int limit)
    {
        var posts = await _context.GetPostsForFeed()
            .Where(p => p.PostTags.Any(pt => pt.Tag.Name.ToLowerInvariant() == hashtag) &&
                       p.PostType == PostType.Post &&
                       !p.IsHidden &&
                       !p.IsDeletedByUser &&
                       p.Privacy == PostPrivacy.Public &&
                       p.User.Status == UserStatus.Active)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return posts.Select(p => p.MapToPostDto(userId));
    }

    private async Task<IEnumerable<UserDto>> GetTopCreatorsByHashtag(string hashtag, int limit)
    {
        var topCreators = await _context.PostTags
            .Where(pt => pt.Tag.Name.ToLowerInvariant() == hashtag &&
                        !pt.Post.IsHidden &&
                        pt.Post.User.Status == UserStatus.Active)
            .GroupBy(pt => pt.Post.UserId)
            .OrderByDescending(g => g.Count())
            .Take(limit)
            .Select(g => g.First().Post.User)
            .ToListAsync();

        return topCreators.Select(u => u.MapToUserDto());
    }

    private async Task<bool> IsInterestGrowing(string interest)
    {
        var now = DateTime.UtcNow;
        var thisWeek = now.AddDays(-7);
        var lastWeek = now.AddDays(-14);

        var thisWeekCount = await _context.PostTags
            .Where(pt => pt.Tag.Name.ToLowerInvariant() == interest &&
                        pt.CreatedAt >= thisWeek)
            .CountAsync();

        var lastWeekCount = await _context.PostTags
            .Where(pt => pt.Tag.Name.ToLowerInvariant() == interest &&
                        pt.CreatedAt >= lastWeek &&
                        pt.CreatedAt < thisWeek)
            .CountAsync();

        return thisWeekCount > lastWeekCount;
    }

    private async Task<double> CalculateInterestStrength(int userId, string interest)
    {
        var recentDate = DateTime.UtcNow.AddDays(-30);

        var interactionCount = await _context.TagAnalytics
            .Where(ta => ta.UserId == userId &&
                        ta.Tag.Name.ToLowerInvariant() == interest &&
                        ta.CreatedAt >= recentDate)
            .CountAsync();

        return Math.Min(1.0, interactionCount / 10.0);
    }

    private async Task<IEnumerable<User>> GetSecondDegreeConnections(int userId, int limit)
    {
        var firstDegreeIds = await _context.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync();

        var secondDegreeIds = await _context.Follows
            .Where(f => firstDegreeIds.Contains(f.FollowerId) && f.FollowingId != userId)
            .Select(f => f.FollowingId)
            .Distinct()
            .Take(limit)
            .ToListAsync();

        return await _context.Users
            .Where(u => secondDegreeIds.Contains(u.Id) &&
                       u.Status == UserStatus.Active)
            .ToListAsync();
    }

    private async Task<IEnumerable<UserDto>> GetConnectionPath(int fromUserId, int toUserId)
    {
        // Simplified - find one mutual connection
        var mutualConnection = await _context.Follows
            .Where(f1 => f1.FollowerId == fromUserId)
            .Join(_context.Follows.Where(f2 => f2.FollowingId == toUserId),
                  f1 => f1.FollowingId,
                  f2 => f2.FollowerId,
                  (f1, f2) => f1.Following)
            .FirstOrDefaultAsync();

        return mutualConnection != null
            ? new[] { mutualConnection.MapToUserDto() }
            : Enumerable.Empty<UserDto>();
    }

    private async Task<double> CalculateNetworkScore(int userId, int targetUserId)
    {
        var mutualConnections = await _context.Follows
            .Where(f1 => f1.FollowerId == userId)
            .Join(_context.Follows.Where(f2 => f2.FollowingId == targetUserId),
                  f1 => f1.FollowingId,
                  f2 => f2.FollowerId,
                  (f1, f2) => f1.FollowingId)
            .CountAsync();

        return Math.Min(1.0, mutualConnections / 5.0);
    }

    private async Task<string> GenerateContentExplanation(int userId, PostDto post)
    {
        var userInterests = await GetUserInterestsAsync(userId);
        var postTags = post.Tags.Select(t => t.Name.ToLowerInvariant());
        var matchingTags = userInterests.Intersect(postTags).ToList();

        if (matchingTags.Any())
        {
            return $"Recommended because you're interested in {string.Join(", ", matchingTags.Take(2))}";
        }

        return "Recommended based on trending content and your activity";
    }

    private async Task<IEnumerable<string>> GenerateReasonTags(int userId, PostDto post)
    {
        var reasons = new List<string>();

        var userInterests = await GetUserInterestsAsync(userId);
        var postTags = post.Tags.Select(t => t.Name.ToLowerInvariant());

        if (userInterests.Intersect(postTags).Any())
        {
            reasons.Add("Matches interests");
        }

        if (post.LikeCount > 100)
        {
            reasons.Add("Popular content");
        }

        if (post.CreatedAt > DateTime.UtcNow.AddHours(-6))
        {
            reasons.Add("Fresh content");
        }

        return reasons;
    }

    private async Task<ExploreSectionDto?> GenerateExploreSection(string sectionType, int? userId)
    {
        return sectionType switch
        {
            "trending_posts" => new ExploreSectionDto(
                SectionType: "trending_posts",
                Title: "Trending Now",
                Description: "Posts gaining momentum across the platform",
                Content: await _trendingService.GetTrendingPostsAsync(24, 10, userId),
                Priority: 1,
                IsPersonalized: userId.HasValue
            ),
            "trending_hashtags" => new ExploreSectionDto(
                SectionType: "trending_hashtags",
                Title: "Trending Topics",
                Description: "Hashtags with growing engagement",
                Content: await _tagAnalyticsService.GetTrendingHashtagsWithVelocityAsync(24, 10),
                Priority: 2,
                IsPersonalized: false
            ),
            "recommended_users" => userId.HasValue ? new ExploreSectionDto(
                SectionType: "recommended_users",
                Title: "People You Might Know",
                Description: "Users with similar interests and connections",
                Content: await GetUserRecommendationsAsync(userId.Value, 5),
                Priority: 3,
                IsPersonalized: true
            ) : null,
            _ => null
        };
    }

    private async Task<Dictionary<string, int>> GetUserHashtagUsage(int userId)
    {
        var recentDate = DateTime.UtcNow.AddDays(-30);

        return await _context.PostTags
            .Where(pt => pt.Post.UserId == userId && pt.CreatedAt >= recentDate)
            .GroupBy(pt => pt.Tag.Name)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    private async Task<(double PostingFrequency, double EngagementRate)> GetUserActivityPattern(int userId)
    {
        var recentDate = DateTime.UtcNow.AddDays(-30);

        var postCount = await _context.Posts
            .Where(p => p.UserId == userId && p.CreatedAt >= recentDate)
            .CountAsync();

        var engagementCount = await _context.PostReactions
            .Where(r => r.UserId == userId && r.CreatedAt >= recentDate)
            .CountAsync();

        return (
            PostingFrequency: postCount / 30.0,
            EngagementRate: engagementCount / 30.0
        );
    }

    private string DetermineHashtagCategory(string tagName)
    {
        var lowerTag = tagName.ToLowerInvariant();

        if (lowerTag.Contains("tech") || lowerTag.Contains("ai") || lowerTag.Contains("code") || lowerTag.Contains("programming"))
            return "Technology";
        if (lowerTag.Contains("sport") || lowerTag.Contains("game") || lowerTag.Contains("football") || lowerTag.Contains("basketball"))
            return "Sports";
        if (lowerTag.Contains("music") || lowerTag.Contains("art") || lowerTag.Contains("photo") || lowerTag.Contains("creative"))
            return "Arts & Entertainment";
        if (lowerTag.Contains("news") || lowerTag.Contains("politics") || lowerTag.Contains("breaking"))
            return "News & Politics";
        if (lowerTag.Contains("food") || lowerTag.Contains("recipe") || lowerTag.Contains("cooking"))
            return "Food & Lifestyle";

        return "General";
    }

    #endregion
}
