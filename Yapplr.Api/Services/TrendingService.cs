using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Common;
using Yapplr.Api.Services.Analytics;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for calculating and retrieving trending content based on engagement velocity and quality metrics
/// </summary>
public class TrendingService : ITrendingService
{
    private readonly YapplrDbContext _context;
    private readonly IAnalyticsService _analyticsService;
    private readonly ITagAnalyticsService _tagAnalyticsService;
    private readonly ILogger<TrendingService> _logger;

    // Trending algorithm weights
    private readonly Dictionary<string, double> _weights = new()
    {
        ["engagementVelocity"] = 0.4,    // How fast engagement is growing
        ["recency"] = 0.25,              // How recent the post is
        ["quality"] = 0.2,               // Author trust score and content quality
        ["diversity"] = 0.1,             // Content type diversity bonus
        ["personalization"] = 0.05       // User-specific boost
    };

    // Engagement type weights for velocity calculation
    private readonly Dictionary<EngagementType, double> _engagementWeights = new()
    {
        [EngagementType.Like] = 1.0,
        [EngagementType.Comment] = 3.0,     // Comments are more valuable
        [EngagementType.Repost] = 4.0,      // Reposts are highly valuable
        [EngagementType.Share] = 3.5,       // Shares indicate quality
        [EngagementType.View] = 0.1,        // Views are less valuable but still count
        [EngagementType.Save] = 2.0         // Saves indicate quality content
    };

    public TrendingService(
        YapplrDbContext context,
        IAnalyticsService analyticsService,
        ITagAnalyticsService tagAnalyticsService,
        ILogger<TrendingService> logger)
    {
        _context = context;
        _analyticsService = analyticsService;
        _tagAnalyticsService = tagAnalyticsService;
        _logger = logger;
    }

    public async Task<IEnumerable<PostDto>> GetTrendingPostsAsync(int timeWindow = 24, int limit = 20, int? currentUserId = null)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var trendingPosts = await GetTrendingPostsWithScoresAsync(timeWindow, limit * 2, currentUserId); // Get more for filtering
            var result = trendingPosts.Take(limit).Select(tp => tp.Post);
            
            var calculationTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("Calculated trending posts in {CalculationTime}ms for timeWindow={TimeWindow}h, limit={Limit}", 
                calculationTime, timeWindow, limit);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating trending posts for timeWindow={TimeWindow}h, limit={Limit}", timeWindow, limit);
            throw;
        }
    }

    public async Task<IEnumerable<TrendingPostDto>> GetTrendingPostsWithScoresAsync(int timeWindow = 24, int limit = 20, int? currentUserId = null)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-timeWindow);
        var calculatedAt = DateTime.UtcNow;

        // Get blocked user IDs for filtering
        var blockedUserIds = currentUserId.HasValue 
            ? await _context.GetBlockedUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();

        // Get posts from the time window that are eligible for trending
        // GetPostsForFeed() already includes all necessary navigation properties including Children
        var candidatePosts = await _context.GetPostsForFeed()
            .Where(p => p.CreatedAt >= cutoffTime &&
                       p.PostType == PostType.Post && // Only top-level posts
                       !p.IsHidden &&
                       !p.IsDeletedByUser &&
                       p.Privacy == PostPrivacy.Public && // Only public posts can trend
                       p.User.Status == UserStatus.Active &&
                       p.User.TrustScore >= 0.1f && // Filter out very low trust users
                       !blockedUserIds.Contains(p.UserId))
            .ToListAsync();

        _logger.LogDebug("Found {CandidateCount} candidate posts for trending calculation", candidatePosts.Count);

        // Calculate trending scores for each post
        var trendingPosts = new List<TrendingPostDto>();
        
        foreach (var post in candidatePosts)
        {
            try
            {
                var score = await CalculatePostTrendingScoreInternalAsync(post, timeWindow, currentUserId);
                
                // Only include posts with a meaningful trending score
                if (score.TotalScore > 0.1)
                {
                    var postDto = post.MapToPostDto(currentUserId);
                    trendingPosts.Add(new TrendingPostDto(postDto, score, calculatedAt));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating trending score for post {PostId}", post.Id);
            }
        }

        // Sort by trending score and return top results
        var result = trendingPosts
            .OrderByDescending(tp => tp.Score.TotalScore)
            .Take(limit)
            .ToList();

        _logger.LogDebug("Calculated trending scores for {PostCount} posts, returning top {Limit}", 
            trendingPosts.Count, Math.Min(limit, result.Count));

        return result;
    }

    public async Task<TrendingScoreDto> CalculatePostTrendingScoreAsync(int postId, int timeWindow = 24)
    {
        var post = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Reactions)
            .Include(p => p.Children.Where(c => c.PostType == PostType.Comment))
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostMedia)
            .Include(p => p.PostLinkPreviews)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            throw new ArgumentException($"Post with ID {postId} not found");
        }

        return await CalculatePostTrendingScoreInternalAsync(post, timeWindow);
    }

    private async Task<TrendingScoreDto> CalculatePostTrendingScoreInternalAsync(Post post, int timeWindow, int? currentUserId = null)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-timeWindow);
        var now = DateTime.UtcNow;

        // Get engagement data from analytics
        var engagements = await _context.ContentEngagements
            .Where(ce => ce.ContentType == ContentType.Post &&
                        ce.ContentId == post.Id &&
                        ce.CreatedAt >= cutoffTime)
            .ToListAsync();

        // Calculate engagement metrics
        var totalEngagements = engagements.Count;
        var likesCount = post.Reactions.Count(r => r.ReactionType == ReactionType.Heart || r.ReactionType == ReactionType.ThumbsUp);
        var commentsCount = post.Children.Count(c => c.PostType == PostType.Comment);
        var repostsCount = post.Reposts.Count;
        var viewsCount = engagements.Count(e => e.EngagementType == EngagementType.View);

        // Calculate engagement velocity (weighted engagements per hour)
        var hoursSinceCreation = Math.Max(1, (now - post.CreatedAt).TotalHours);
        var weightedEngagements = CalculateWeightedEngagements(engagements);
        var engagementVelocity = weightedEngagements / hoursSinceCreation;

        // Calculate engagement rate (engagements per view, with minimum views)
        var engagementRate = viewsCount > 0 ? (double)totalEngagements / viewsCount : 0;

        // Calculate component scores
        var engagementVelocityScore = CalculateEngagementVelocityScore(engagementVelocity);
        var recencyScore = CalculateRecencyScore(hoursSinceCreation);
        var qualityScore = CalculateQualityScore(post, engagementRate);
        var trustScore = (double)post.User.TrustScore;
        var diversityScore = CalculateDiversityScore(post);

        // Calculate personalization boost if user provided
        var personalizationBoost = currentUserId.HasValue 
            ? await CalculatePersonalizationBoostAsync(post, currentUserId.Value)
            : 0;

        // Calculate total weighted score
        var totalScore = 
            (engagementVelocityScore * _weights["engagementVelocity"]) +
            (recencyScore * _weights["recency"]) +
            (qualityScore * _weights["quality"]) +
            (trustScore * _weights["quality"]) + // Trust score contributes to quality
            (diversityScore * _weights["diversity"]) +
            (personalizationBoost * _weights["personalization"]);

        // Create breakdown
        var breakdown = new TrendingScoreBreakdownDto(
            TotalEngagements: totalEngagements,
            LikesCount: likesCount,
            CommentsCount: commentsCount,
            RepostsCount: repostsCount,
            ViewsCount: viewsCount,
            EngagementRate: engagementRate,
            EngagementVelocity: engagementVelocity,
            AuthorTrustScore: (double)post.User.TrustScore,
            ContentQualityScore: qualityScore,
            SpamProbability: 0, // Could be enhanced with spam detection
            PostCreatedAt: post.CreatedAt,
            HoursSinceCreation: hoursSinceCreation,
            RecencyMultiplier: recencyScore,
            HasMedia: post.PostMedia.Any(),
            HasHashtags: post.PostTags.Any(),
            HasLinks: post.PostLinkPreviews.Any(),
            HashtagCount: post.PostTags.Count,
            IsFromFollowedUser: false, // Would need following data
            MatchesUserInterests: false, // Would need interest data
            PersonalizationBoost: personalizationBoost
        );

        return new TrendingScoreDto(
            TotalScore: totalScore,
            EngagementVelocityScore: engagementVelocityScore,
            RecencyScore: recencyScore,
            QualityScore: qualityScore,
            TrustScore: (double)trustScore,
            DiversityScore: diversityScore,
            Breakdown: breakdown
        );
    }

    public async Task<IEnumerable<PostDto>> GetTrendingPostsByHashtagAsync(string? hashtag = null, int timeWindow = 24, int limit = 20, int? currentUserId = null)
    {
        var trendingPosts = await GetTrendingPostsWithScoresAsync(timeWindow, limit * 3, currentUserId);

        if (!string.IsNullOrEmpty(hashtag))
        {
            var normalizedHashtag = hashtag.ToLowerInvariant().TrimStart('#');
            trendingPosts = trendingPosts.Where(tp =>
                tp.Post.Tags.Any(t => t.Name.ToLowerInvariant() == normalizedHashtag));
        }

        return trendingPosts.Take(limit).Select(tp => tp.Post);
    }

    public async Task<IEnumerable<PostDto>> GetPersonalizedTrendingPostsAsync(int userId, int timeWindow = 24, int limit = 20)
    {
        // Get user's interests based on their hashtag interactions
        var userInterests = await GetUserInterestsAsync(userId);

        // Get user's following list for personalization
        var followingIds = await _context.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToHashSetAsync();

        var trendingPosts = await GetTrendingPostsWithScoresAsync(timeWindow, limit * 2, userId);

        // Boost posts from followed users and matching interests
        var personalizedPosts = trendingPosts.Select(tp =>
        {
            var personalizedScore = tp.Score.TotalScore;

            // Boost posts from followed users
            if (followingIds.Contains(tp.Post.User.Id))
            {
                personalizedScore *= 1.3; // 30% boost for followed users
            }

            // Boost posts with user's interested hashtags
            var matchingInterests = tp.Post.Tags.Count(t => userInterests.Contains(t.Name.ToLowerInvariant()));
            if (matchingInterests > 0)
            {
                personalizedScore *= (1 + (matchingInterests * 0.2)); // 20% boost per matching hashtag
            }

            return new { Post = tp.Post, Score = personalizedScore };
        })
        .OrderByDescending(p => p.Score)
        .Take(limit)
        .Select(p => p.Post);

        return personalizedPosts;
    }

    public async Task<TrendingAnalyticsDto> GetTrendingAnalyticsAsync(int timeWindow = 24)
    {
        var analyzedAt = DateTime.UtcNow;
        var cutoffTime = analyzedAt.AddHours(-timeWindow);

        // Get trending posts for analysis
        var trendingPosts = await GetTrendingPostsWithScoresAsync(timeWindow, 100);

        // Calculate overall stats
        var stats = new TrendingStatsDto(
            TotalTrendingPosts: trendingPosts.Count(),
            TotalEngagements: trendingPosts.Sum(tp => tp.Score.Breakdown.TotalEngagements),
            AverageEngagementRate: trendingPosts.Any() ? trendingPosts.Average(tp => tp.Score.Breakdown.EngagementRate) : 0,
            AverageTrendingScore: trendingPosts.Any() ? trendingPosts.Average(tp => tp.Score.TotalScore) : 0,
            UniqueAuthors: trendingPosts.Select(tp => tp.Post.User.Id).Distinct().Count(),
            UniqueHashtags: trendingPosts.SelectMany(tp => tp.Post.Tags).Select(t => t.Name).Distinct().Count()
        );

        // Get top categories (hashtags)
        var topCategories = trendingPosts
            .SelectMany(tp => tp.Post.Tags.Select(t => new { Hashtag = t.Name, Post = tp }))
            .GroupBy(x => x.Hashtag)
            .Select(g => new TrendingCategoryDto(
                Hashtag: g.Key,
                PostCount: g.Count(),
                TotalEngagements: g.Sum(x => x.Post.Score.Breakdown.TotalEngagements),
                AverageScore: g.Average(x => x.Post.Score.TotalScore),
                GrowthRate: 0 // Could be calculated by comparing to previous period
            ))
            .OrderByDescending(c => c.PostCount)
            .Take(10);

        // Get top authors
        var topAuthors = trendingPosts
            .GroupBy(tp => tp.Post.User)
            .Select(g => new TrendingAuthorDto(
                Author: new UserDto(
                    Id: g.Key.Id,
                    Email: g.Key.Email,
                    Username: g.Key.Username,
                    Bio: g.Key.Bio,
                    Birthday: g.Key.Birthday,
                    Pronouns: g.Key.Pronouns,
                    Tagline: g.Key.Tagline,
                    ProfileImageUrl: g.Key.ProfileImageUrl,
                    CreatedAt: g.Key.CreatedAt,
                    FcmToken: g.Key.FcmToken,
                    ExpoPushToken: g.Key.ExpoPushToken,
                    EmailVerified: g.Key.EmailVerified,
                    Role: g.Key.Role,
                    Status: g.Key.Status,
                    SuspendedUntil: g.Key.SuspendedUntil,
                    SuspensionReason: g.Key.SuspensionReason,
                    SubscriptionTier: null // Would need to be loaded separately
                ),
                TrendingPostsCount: g.Count(),
                AverageTrendingScore: g.Average(tp => tp.Score.TotalScore),
                TotalEngagements: g.Sum(tp => tp.Score.Breakdown.TotalEngagements)
            ))
            .OrderByDescending(a => a.TrendingPostsCount)
            .Take(10);

        var metrics = new TrendingMetricsDto(
            CalculationTimeMs: 0, // Would be measured in actual implementation
            PostsAnalyzed: trendingPosts.Count(),
            PostsFiltered: 0, // Would track filtered posts
            FilterRate: 0,
            LastCalculation: analyzedAt
        );

        return new TrendingAnalyticsDto(
            AnalyzedAt: analyzedAt,
            TimeWindowHours: timeWindow,
            Stats: stats,
            TopCategories: topCategories,
            TopAuthors: topAuthors,
            Metrics: metrics
        );
    }

    #region Helper Methods

    private double CalculateWeightedEngagements(IEnumerable<ContentEngagement> engagements)
    {
        return engagements.Sum(e => _engagementWeights.GetValueOrDefault(e.EngagementType, 1.0));
    }

    private double CalculateEngagementVelocityScore(double engagementVelocity)
    {
        // Logarithmic scaling to prevent extremely viral content from dominating
        // Score ranges from 0 to 1, with diminishing returns for very high velocity
        return Math.Min(1.0, Math.Log10(engagementVelocity + 1) / 3.0);
    }

    private double CalculateRecencyScore(double hoursSinceCreation)
    {
        // Exponential decay favoring newer content
        // Posts lose 50% of recency score every 12 hours
        var halfLife = 12.0; // hours
        return Math.Pow(0.5, hoursSinceCreation / halfLife);
    }

    private double CalculateQualityScore(Post post, double engagementRate)
    {
        var qualityScore = 0.0;

        // Base quality from engagement rate
        qualityScore += Math.Min(0.5, engagementRate * 10); // Cap at 0.5

        // Content length bonus (not too short, not too long)
        var contentLength = post.Content.Length;
        if (contentLength >= 50 && contentLength <= 500)
        {
            qualityScore += 0.2;
        }

        // Media content bonus
        if (post.PostMedia.Any())
        {
            qualityScore += 0.15;
        }

        // Hashtag usage bonus (but not too many)
        var hashtagCount = post.PostTags.Count;
        if (hashtagCount >= 1 && hashtagCount <= 5)
        {
            qualityScore += 0.1;
        }

        // Link preview bonus
        if (post.PostLinkPreviews.Any())
        {
            qualityScore += 0.05;
        }

        return Math.Min(1.0, qualityScore);
    }

    private double CalculateDiversityScore(Post post)
    {
        var diversityScore = 0.0;

        // Bonus for different content types
        if (post.PostMedia.Any()) diversityScore += 0.3;
        if (post.PostTags.Any()) diversityScore += 0.3;
        if (post.PostLinkPreviews.Any()) diversityScore += 0.2;
        if (post.Content.Length > 100) diversityScore += 0.2; // Substantial text content

        return Math.Min(1.0, diversityScore);
    }

    private async Task<double> CalculatePersonalizationBoostAsync(Post post, int userId)
    {
        var boost = 0.0;

        // Check if user follows the author
        var isFollowing = await _context.Follows
            .AnyAsync(f => f.FollowerId == userId && f.FollowingId == post.UserId);

        if (isFollowing)
        {
            boost += 0.5; // 50% boost for followed users
        }

        // Check if post matches user's interests (simplified)
        var userInterests = await GetUserInterestsAsync(userId);
        var matchingHashtags = post.PostTags.Count(pt =>
            userInterests.Contains(pt.Tag.Name.ToLowerInvariant()));

        boost += matchingHashtags * 0.2; // 20% boost per matching hashtag

        return Math.Min(1.0, boost);
    }

    private async Task<HashSet<string>> GetUserInterestsAsync(int userId)
    {
        // Get user's most interacted-with hashtags from the last 30 days
        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        var interests = await _context.TagAnalytics
            .Where(ta => ta.UserId == userId &&
                        ta.CreatedAt >= cutoffDate &&
                        (ta.Action == TagAction.Clicked || ta.Action == TagAction.Used))
            .GroupBy(ta => ta.Tag.Name)
            .OrderByDescending(g => g.Count())
            .Take(20) // Top 20 interests
            .Select(g => g.Key.ToLowerInvariant())
            .ToListAsync();

        return interests.ToHashSet();
    }

    public async Task<IEnumerable<CategoryTrendingDto>> GetEnhancedTrendingCategoriesAsync(int timeWindow = 24, int limit = 10)
    {
        try
        {
            // Use the enhanced hashtag trending service for better velocity-based calculations
            var enhancedCategories = await _tagAnalyticsService.GetTrendingHashtagsByCategoryAsync(timeWindow, limit);

            _logger.LogInformation("Retrieved {CategoryCount} enhanced trending categories for timeWindow={TimeWindow}h",
                enhancedCategories.Count(), timeWindow);

            return enhancedCategories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enhanced trending categories for timeWindow={TimeWindow}h", timeWindow);

            // Fallback to basic trending categories from existing analytics
            return await GetBasicTrendingCategoriesAsync(timeWindow, limit);
        }
    }

    private async Task<IEnumerable<CategoryTrendingDto>> GetBasicTrendingCategoriesAsync(int timeWindow, int limit)
    {
        // Fallback implementation using existing trending analytics
        var analytics = await GetTrendingAnalyticsAsync(timeWindow);

        // Convert existing TrendingCategoryDto to new CategoryTrendingDto format
        var basicCategories = analytics.TopCategories.Take(limit).Select(tc =>
            new CategoryTrendingDto(
                Category: tc.Hashtag,
                TrendingHashtags: new List<TrendingHashtagDto>(), // Empty for basic fallback
                TotalPosts: tc.PostCount,
                CategoryGrowthRate: tc.GrowthRate,
                Description: $"Trending topic: {tc.Hashtag}"
            ));

        return basicCategories;
    }

    #endregion
}
