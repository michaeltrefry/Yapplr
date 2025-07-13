using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Services;

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

        // Get tags with recent activity
        var trendingTags = await _context.Tags
            .Where(t => _context.PostTags
                .Any(pt => pt.TagId == t.Id && pt.CreatedAt >= cutoffDate))
            .Select(t => new
            {
                Tag = t,
                RecentPostCount = _context.PostTags
                    .Count(pt => pt.TagId == t.Id && pt.CreatedAt >= cutoffDate)
            })
            .OrderByDescending(x => x.RecentPostCount)
            .ThenByDescending(x => x.Tag.PostCount)
            .Take(limit)
            .ToListAsync();

        return trendingTags.Select(x => x.Tag.ToDto());
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

        // Get post counts for different time periods
        var postsThisWeek = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id && pt.CreatedAt >= oneWeekAgo)
            .CountAsync();

        var postsThisMonth = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id && pt.CreatedAt >= oneMonthAgo)
            .CountAsync();

        // Get first and last usage dates
        var firstUsage = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id)
            .OrderBy(pt => pt.CreatedAt)
            .Select(pt => pt.CreatedAt)
            .FirstOrDefaultAsync();

        var lastUsage = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id)
            .OrderByDescending(pt => pt.CreatedAt)
            .Select(pt => pt.CreatedAt)
            .FirstOrDefaultAsync();

        // Get unique users count
        var uniqueUsers = await _context.PostTags
            .Where(pt => pt.TagId == tag.Id)
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
