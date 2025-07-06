using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Services;

public class TagAnalyticsService : ITagAnalyticsService
{
    private readonly YapplrDbContext _context;

    public TagAnalyticsService(YapplrDbContext context)
    {
        _context = context;
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
}
