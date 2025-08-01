using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Services.Analytics;

public class TagAnalyticsService : ITagAnalyticsService
{
    private readonly YapplrDbContext _context;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<TagAnalyticsService> _logger;

    public TagAnalyticsService(YapplrDbContext context, IAnalyticsService analyticsService, ILogger<TagAnalyticsService> logger)
    {
        _context = context;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<IEnumerable<TagDto>> GetTrendingTagsAsync(int days = 7, int limit = 10)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        // Get tags with recent activity, filtering out hidden/deleted posts
        var trendingTags = await _context.Tags
            .Where(t => _context.PostTags
                .Any(pt => pt.TagId == t.Id &&
                          pt.CreatedAt >= cutoffDate &&
                          !pt.Post.IsHidden && // Filter out hidden posts
                          pt.Post.User.Status == UserStatus.Active)) // Filter out posts from suspended users
            .Select(t => new
            {
                Tag = t,
                RecentPostCount = _context.PostTags
                    .Count(pt => pt.TagId == t.Id &&
                               pt.CreatedAt >= cutoffDate &&
                               !pt.Post.IsHidden && // Filter out hidden posts
                               pt.Post.User.Status == UserStatus.Active) // Filter out posts from suspended users
            })
            .OrderByDescending(x => x.RecentPostCount)
            .ThenByDescending(x => x.Tag.PostCount)
            .Take(limit)
            .ToListAsync();

        return trendingTags.Select(x => x.Tag.ToDto());
    }

    public async Task<IEnumerable<TrendingHashtagDto>> GetTrendingHashtagsWithVelocityAsync(int timeWindow = 24, int limit = 20, string? category = null, string? location = null)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-timeWindow);
        var previousPeriodCutoff = cutoffTime.AddHours(-timeWindow); // Previous period for velocity calculation

        // Simplify the query to avoid EF Core translation issues
        // First get the basic PostTags data
        var postTagData = await _context.PostTags
            .Include(pt => pt.Tag)
            .Include(pt => pt.Post)
                .ThenInclude(p => p.User)
            .Where(pt => pt.CreatedAt >= previousPeriodCutoff)
            .ToListAsync();

        // Apply filters in memory to avoid EF Core translation issues
        // Add null checks to prevent issues
        postTagData = postTagData
            .Where(pt => pt.Post != null &&
                        pt.Tag != null &&
                        pt.Post.User != null &&
                        !pt.Post.IsHidden &&
                        !pt.Post.IsDeletedByUser &&
                        pt.Post.User.Status == UserStatus.Active &&
                        pt.Post.User.TrustScore >= 0.1f)
            .ToList();

        var hashtagData = postTagData
            .Where(pt => pt.Tag != null && !string.IsNullOrEmpty(pt.Tag.Name))
            .GroupBy(pt => pt.Tag.Name)
            .Select(g => new
            {
                TagName = g.Key,
                Tag = g.First().Tag,
                CurrentPeriodCount = g.Count(pt => pt.CreatedAt >= cutoffTime),
                PreviousPeriodCount = g.Count(pt => pt.CreatedAt < cutoffTime),
                TotalEngagements = g.Sum(pt =>
                    pt.Post.Reactions.Count +
                    pt.Post.Children.Count(c => c.PostType == PostType.Comment) +
                    pt.Post.Reposts.Count),
                UniqueUsers = g.Select(pt => pt.Post.UserId).Distinct().Count(),
                AverageUserTrustScore = g.Average(pt => pt.Post.User.TrustScore),
                FirstUsageInPeriod = g.Min(pt => pt.CreatedAt),
                LastUsageInPeriod = g.Max(pt => pt.CreatedAt)
            })
            .Where(x => x.CurrentPeriodCount > 0) // Only hashtags used in current period
            .ToList();

        // Calculate trending scores with velocity
        var trendingHashtags = hashtagData
            .Select(data => CalculateHashtagTrendingScore(data, timeWindow))
            .Where(trending => trending.TrendingScore > 0.1) // Filter out low-scoring hashtags
            .OrderByDescending(trending => trending.TrendingScore)
            .Take(limit);

        return trendingHashtags;
    }

    private TrendingHashtagDto CalculateHashtagTrendingScore(dynamic data, int timeWindow)
    {
        var currentCount = (int)data.CurrentPeriodCount;
        var previousCount = (int)data.PreviousPeriodCount;
        var totalEngagements = (int)data.TotalEngagements;
        var uniqueUsers = (int)data.UniqueUsers;
        var avgTrustScore = (float)data.AverageUserTrustScore;
        var tagName = (string)data.TagName;

        // Calculate velocity (growth rate)
        var velocity = previousCount > 0
            ? (double)(currentCount - previousCount) / previousCount
            : currentCount > 0 ? 1.0 : 0.0; // 100% growth if no previous usage

        // Calculate engagement rate
        var engagementRate = currentCount > 0 ? (double)totalEngagements / currentCount : 0;

        // Calculate diversity score (unique users vs total posts)
        var diversityScore = currentCount > 0 ? (double)uniqueUsers / currentCount : 0;

        // Calculate trending score with weighted components
        var velocityScore = Math.Min(1.0, Math.Log10(Math.Abs(velocity) + 1) / 2.0) * (velocity >= 0 ? 1 : 0.5);
        var volumeScore = Math.Min(1.0, Math.Log10(currentCount + 1) / 3.0);
        var qualityScore = Math.Min(1.0, avgTrustScore * engagementRate * diversityScore);
        var recencyScore = CalculateRecencyScore(timeWindow);

        var trendingScore =
            (velocityScore * 0.4) +      // 40% weight on velocity
            (volumeScore * 0.3) +        // 30% weight on volume
            (qualityScore * 0.2) +       // 20% weight on quality
            (recencyScore * 0.1);        // 10% weight on recency

        return new TrendingHashtagDto(
            Name: tagName,
            PostCount: currentCount,
            PreviousPeriodCount: previousCount,
            Velocity: velocity,
            TrendingScore: trendingScore,
            TotalEngagements: totalEngagements,
            UniqueUsers: uniqueUsers,
            AverageUserTrustScore: avgTrustScore,
            Category: DetermineHashtagCategory(tagName), // Simple categorization
            GrowthRate: velocity,
            EngagementRate: engagementRate,
            DiversityScore: diversityScore
        );
    }

    private double CalculateRecencyScore(int timeWindow)
    {
        // Simple recency score - could be enhanced with actual timing data
        return timeWindow <= 6 ? 1.0 : timeWindow <= 24 ? 0.8 : 0.6;
    }

    private string DetermineHashtagCategory(string tagName)
    {
        // Simple category determination - could be enhanced with ML or predefined categories
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

    public async Task<IEnumerable<CategoryTrendingDto>> GetTrendingHashtagsByCategoryAsync(int timeWindow = 24, int limit = 10)
    {
        var trendingHashtags = await GetTrendingHashtagsWithVelocityAsync(timeWindow, limit * 5); // Get more to group by category

        var categoryGroups = trendingHashtags
            .GroupBy(h => h.Category)
            .Select(g => new CategoryTrendingDto(
                Category: g.Key,
                TrendingHashtags: g.Take(limit),
                TotalPosts: g.Sum(h => h.PostCount),
                CategoryGrowthRate: g.Average(h => h.GrowthRate),
                Description: GetCategoryDescription(g.Key)
            ))
            .OrderByDescending(c => c.CategoryGrowthRate)
            .Take(10); // Top 10 categories

        return categoryGroups;
    }

    public async Task<PersonalizedTrendingDto> GetPersonalizedTrendingHashtagsAsync(int userId, int timeWindow = 24, int limit = 20)
    {
        // Get user's interests from their interaction history
        var userInterests = await GetUserInterestsAsync(userId);
        var allTrending = await GetTrendingHashtagsWithVelocityAsync(timeWindow, limit * 3);

        // Score hashtags based on user interests and interaction patterns
        var personalizedHashtags = allTrending
            .Select(hashtag => new
            {
                Hashtag = hashtag,
                PersonalizationScore = CalculatePersonalizationScore(hashtag, userInterests.ToList(), userId)
            })
            .Where(x => x.PersonalizationScore > 0.1)
            .OrderByDescending(x => x.PersonalizationScore)
            .Take(limit)
            .Select(x => x.Hashtag);

        var overallPersonalizationScore = personalizedHashtags.Any()
            ? personalizedHashtags.Average(h => h.TrendingScore)
            : 0.0;

        return new PersonalizedTrendingDto(
            UserId: userId,
            RecommendedHashtags: personalizedHashtags,
            UserInterests: userInterests,
            PersonalizationScore: overallPersonalizationScore,
            GeneratedAt: DateTime.UtcNow
        );
    }

    public async Task<TrendingHashtagAnalyticsDto> GetTrendingHashtagAnalyticsAsync(int timeWindow = 24)
    {
        var globalTrending = await GetTrendingHashtagsWithVelocityAsync(timeWindow, 50);
        var categoryTrending = await GetTrendingHashtagsByCategoryAsync(timeWindow, 10);

        // For geographic trending, we'd need user location data - placeholder for now
        var geographicTrending = new List<GeographicTrendingDto>();

        var stats = new TrendingHashtagStatsDto(
            TotalTrendingHashtags: globalTrending.Count(),
            TotalPosts: globalTrending.Sum(h => h.PostCount),
            AverageVelocity: globalTrending.Any() ? globalTrending.Average(h => h.Velocity) : 0,
            AverageEngagementRate: globalTrending.Any() ? globalTrending.Average(h => h.EngagementRate) : 0,
            UniqueCategories: globalTrending.Select(h => h.Category).Distinct().Count(),
            TopCategory: categoryTrending.FirstOrDefault()?.Category ?? "General",
            OverallGrowthRate: globalTrending.Any() ? globalTrending.Average(h => h.GrowthRate) : 0
        );

        return new TrendingHashtagAnalyticsDto(
            AnalyzedAt: DateTime.UtcNow,
            TimeWindowHours: timeWindow,
            GlobalTrending: globalTrending,
            CategoryTrending: categoryTrending,
            GeographicTrending: geographicTrending,
            Stats: stats
        );
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

    private double CalculatePersonalizationScore(TrendingHashtagDto hashtag, List<string> userInterests, int userId)
    {
        var baseScore = hashtag.TrendingScore;
        var interestBoost = 0.0;

        // Boost if hashtag matches user interests
        if (userInterests.Contains(hashtag.Name.ToLowerInvariant()))
        {
            interestBoost += 0.5; // 50% boost for direct interest match
        }

        // Boost if hashtag category matches user's preferred categories
        var userCategories = userInterests.Select(i => DetermineHashtagCategory(i)).Distinct();
        if (userCategories.Contains(hashtag.Category))
        {
            interestBoost += 0.3; // 30% boost for category match
        }

        // Apply diminishing returns to prevent over-boosting
        var personalizedScore = baseScore * (1 + Math.Min(interestBoost, 0.8));

        return Math.Min(1.0, personalizedScore);
    }

    private string GetCategoryDescription(string category)
    {
        return category switch
        {
            "Technology" => "Latest in tech, AI, programming, and digital innovation",
            "Sports" => "Sports news, games, and athletic achievements",
            "Arts & Entertainment" => "Music, art, photography, and creative content",
            "News & Politics" => "Breaking news, political discussions, and current events",
            "Food & Lifestyle" => "Recipes, cooking tips, and lifestyle content",
            _ => "General trending topics and discussions"
        };
    }

    public async Task<IEnumerable<TagDto>> GetTopTagsAsync(int limit = 20)
    {
        var topTags = await _context.Tags
            .Where(t => t.PostCount > 0)
            .OrderByDescending(t => t.PostCount)
            .ThenBy(t => t.Name)
            .Take(limit)
            .ToListAsync();

        return topTags.Select(t => t.ToDto());
    }

    public async Task<TagAnalyticsDto?> GetTagAnalyticsAsync(string tagName)
    {
        var normalizedTagName = tagName.ToLowerInvariant().TrimStart('#');

        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == normalizedTagName);

        if (tag == null)
            return null;

        var now = DateTime.UtcNow;
        var oneWeekAgo = now.AddDays(-7);
        var oneMonthAgo = now.AddDays(-30);

        // Get post counts for different time periods (only from visible posts)
        var postsThisWeek = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id &&
                        pt.CreatedAt >= oneWeekAgo &&
                        !pt.Post.IsHidden &&
                        pt.Post.User.Status == UserStatus.Active)
            .CountAsync();

        var postsThisMonth = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id &&
                        pt.CreatedAt >= oneMonthAgo &&
                        !pt.Post.IsHidden &&
                        pt.Post.User.Status == UserStatus.Active)
            .CountAsync();

        // Get first and last usage dates (only from visible posts)
        var firstUsage = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id &&
                        !pt.Post.IsHidden &&
                        pt.Post.User.Status == UserStatus.Active)
            .OrderBy(pt => pt.CreatedAt)
            .Select(pt => pt.CreatedAt)
            .FirstOrDefaultAsync();

        var lastUsage = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id &&
                        !pt.Post.IsHidden &&
                        pt.Post.User.Status == UserStatus.Active)
            .OrderByDescending(pt => pt.CreatedAt)
            .Select(pt => pt.CreatedAt)
            .FirstOrDefaultAsync();

        // Get unique users count (only from visible posts)
        var uniqueUsers = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id &&
                        !pt.Post.IsHidden &&
                        pt.Post.User.Status == UserStatus.Active)
            .Select(pt => pt.Post.UserId)
            .Distinct()
            .CountAsync();

        return new TagAnalyticsDto(
            tag.Name,
            tag.PostCount,
            postsThisWeek,
            postsThisMonth,
            firstUsage,
            lastUsage,
            uniqueUsers
        );
    }

    public async Task<IEnumerable<TagUsageDto>> GetTagUsageOverTimeAsync(string tagName, int days = 30)
    {
        var normalizedTagName = tagName.ToLowerInvariant().TrimStart('#');
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == normalizedTagName);

        if (tag == null)
            return new List<TagUsageDto>();

        // Get daily usage counts
        var dailyUsage = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id && pt.CreatedAt >= cutoffDate)
            .GroupBy(pt => pt.CreatedAt.Date)
            .Select(g => new TagUsageDto(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill in missing dates with zero counts
        var result = new List<TagUsageDto>();
        var currentDate = cutoffDate.Date;
        var endDate = DateTime.UtcNow.Date;

        while (currentDate <= endDate)
        {
            var usage = dailyUsage.FirstOrDefault(x => x.Date == currentDate);
            result.Add(usage ?? new TagUsageDto(currentDate, 0));
            currentDate = currentDate.AddDays(1);
        }

        return result;
    }

    public async Task UpdateTagMetricsAsync(int tagId, string action, DateTime timestamp)
    {
        try
        {
            // Convert string action to enum
            if (!Enum.TryParse<TagAction>(action, true, out var tagAction))
            {
                _logger.LogWarning("Unknown tag action: {Action}", action);
                return;
            }

            // Track the tag action using the analytics service
            await _analyticsService.TrackTagActionAsync(
                tagId: tagId,
                action: tagAction,
                userId: null, // No specific user for this action
                relatedContentType: null,
                relatedContentId: null,
                source: "system",
                metadata: $"{{\"timestamp\":\"{timestamp:O}\"}}");

            _logger.LogInformation("Updated tag metrics for tag {TagId}: {Action} at {Timestamp}",
                tagId, action, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tag metrics for tag {TagId}: {Error}",
                tagId, ex.Message);
            // Don't throw - analytics failures shouldn't break the main flow
        }
    }
}
