using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Common;

namespace Yapplr.Api.Services;

public class TagService : ITagService
{
    private readonly YapplrDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IBlockService _blockService;
    private readonly ICountCacheService _countCache;

    public TagService(YapplrDbContext context, IHttpContextAccessor httpContextAccessor, IBlockService blockService, ICountCacheService countCache)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _blockService = blockService;
        _countCache = countCache;
    }

    public async Task<IEnumerable<TagDto>> SearchTagsAsync(string query, int? currentUserId = null, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<TagDto>();

        var normalizedQuery = query.ToLowerInvariant().TrimStart('#');

        // Get blocked user IDs to filter them out
        var blockedUserIds = new List<int>();
        if (currentUserId.HasValue)
        {
            blockedUserIds = await GetBlockedUserIdsAsync(currentUserId.Value);
        }

        var tags = await _context.Tags
            .Where(t => EF.Functions.ILike(t.Name, $"%{normalizedQuery}%"))
            .OrderByDescending(t => t.PostCount)
            .ThenBy(t => t.Name)
            .Take(limit)
            .ToListAsync();

        // Calculate actual visible post counts for each tag using cache
        var result = new List<TagDto>();
        foreach (var tag in tags)
        {
            var actualPostCount = await _countCache.GetTagPostCountAsync(tag.Name, currentUserId);
            result.Add(new TagDto(tag.Id, tag.Name, actualPostCount));
        }

        return result.OrderByDescending(t => t.PostCount).ThenBy(t => t.Name);
    }

    public async Task<IEnumerable<TagDto>> GetTrendingTagsAsync(int limit = 10)
    {
        // Calculate actual visible post counts for trending tags
        var tags = await _context.Tags
            .Where(t => _context.PostTags
                .Any(pt => pt.TagId == t.Id &&
                          !pt.Post.IsHidden &&
                          pt.Post.User.Status == UserStatus.Active))
            .Select(t => new
            {
                Tag = t,
                VisiblePostCount = _context.PostTags
                    .Count(pt => pt.TagId == t.Id &&
                               !pt.Post.IsHidden &&
                               pt.Post.User.Status == UserStatus.Active)
            })
            .Where(x => x.VisiblePostCount > 0)
            .OrderByDescending(x => x.VisiblePostCount)
            .ThenByDescending(x => x.Tag.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return tags.Select(x => new TagDto(x.Tag.Id, x.Tag.Name, x.VisiblePostCount));
    }

    public async Task<IEnumerable<PostDto>> GetPostsByTagAsync(string tagName, int? currentUserId = null, int page = 1, int pageSize = 25)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return new List<PostDto>();

        var normalizedTagName = tagName.ToLowerInvariant().TrimStart('#');

        // Get blocked user IDs and following IDs for filtering
        var blockedUserIds = currentUserId.HasValue
            ? await _context.GetBlockedUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();
        var followingIds = currentUserId.HasValue
            ? await _context.GetFollowingUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();

        var posts = await _context.GetPostsForFeed()
            .Where(p => p.PostTags.Any(pt => pt.Tag.Name == normalizedTagName))
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return posts.Select(p => p.MapToPostDto(currentUserId));
    }

    public async Task<TagDto?> GetTagByNameAsync(string tagName, int? currentUserId = null)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return null;

        var normalizedTagName = tagName.ToLowerInvariant().TrimStart('#');

        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == normalizedTagName);

        if (tag == null)
            return null;

        // Get blocked user IDs to filter them out
        var blockedUserIds = new List<int>();
        if (currentUserId.HasValue)
        {
            blockedUserIds = await GetBlockedUserIdsAsync(currentUserId.Value);
        }

        // Get the actual visible post count using cache
        var actualPostCount = await _countCache.GetTagPostCountAsync(normalizedTagName, currentUserId);

        // Create a new TagDto with the actual visible count
        return new TagDto(tag.Id, tag.Name, actualPostCount);
    }

    private async Task<List<int>> GetBlockedUserIdsAsync(int userId)
    {
        // Get users that the current user has blocked
        var blockedByUser = await _context.Blocks
            .Where(b => b.BlockerId == userId)
            .Select(b => b.BlockedId)
            .ToListAsync();

        // Get users that have blocked the current user
        var blockedByOthers = await _context.Blocks
            .Where(b => b.BlockedId == userId)
            .Select(b => b.BlockerId)
            .ToListAsync();

        // Combine both lists to filter out all blocked relationships
        return blockedByUser.Concat(blockedByOthers).Distinct().ToList();
    }
}
