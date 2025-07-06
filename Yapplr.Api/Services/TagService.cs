using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Services;

public class TagService : ITagService
{
    private readonly YapplrDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IBlockService _blockService;

    public TagService(YapplrDbContext context, IHttpContextAccessor httpContextAccessor, IBlockService blockService)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _blockService = blockService;
    }

    public async Task<IEnumerable<TagDto>> SearchTagsAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<TagDto>();

        var normalizedQuery = query.ToLowerInvariant().TrimStart('#');

        var tags = await _context.Tags
            .Where(t => t.Name.Contains(normalizedQuery))
            .OrderByDescending(t => t.PostCount)
            .ThenBy(t => t.Name)
            .Take(limit)
            .ToListAsync();

        return tags.Select(t => t.ToDto());
    }

    public async Task<IEnumerable<TagDto>> GetTrendingTagsAsync(int limit = 10)
    {
        var tags = await _context.Tags
            .Where(t => t.PostCount > 0)
            .OrderByDescending(t => t.PostCount)
            .ThenByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return tags.Select(t => t.ToDto());
    }

    public async Task<IEnumerable<PostDto>> GetPostsByTagAsync(string tagName, int? currentUserId = null, int page = 1, int pageSize = 25)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return new List<PostDto>();

        var normalizedTagName = tagName.ToLowerInvariant().TrimStart('#');

        // Get blocked user IDs to filter them out
        var blockedUserIds = new List<int>();
        if (currentUserId.HasValue)
        {
            blockedUserIds = await GetBlockedUserIdsAsync(currentUserId.Value);
        }

        var posts = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .AsSplitQuery()
            .Where(p => p.PostTags.Any(pt => pt.Tag.Name == normalizedTagName) &&
                       !blockedUserIds.Contains(p.UserId) &&
                       (p.Privacy == PostPrivacy.Public || 
                        (currentUserId.HasValue && p.UserId == currentUserId.Value)))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return posts.Select(p => MapToPostDto(p, currentUserId));
    }

    public async Task<TagDto?> GetTagByNameAsync(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return null;

        var normalizedTagName = tagName.ToLowerInvariant().TrimStart('#');

        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == normalizedTagName);

        return tag?.ToDto();
    }

    private PostDto MapToPostDto(Post post, int? currentUserId)
    {
        var userDto = post.User.ToDto();

        var isLiked = currentUserId.HasValue && post.Likes.Any(l => l.UserId == currentUserId.Value);
        var isReposted = currentUserId.HasValue && post.Reposts.Any(r => r.UserId == currentUserId.Value);

        // Generate image URL from filename
        string? imageUrl = null;
        if (!string.IsNullOrEmpty(post.ImageFileName))
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                imageUrl = $"{request.Scheme}://{request.Host}/api/images/{post.ImageFileName}";
            }
        }

        var isEdited = post.UpdatedAt > post.CreatedAt.AddMinutes(1);

        // Map tags to DTOs
        var tags = post.PostTags.Select(pt => pt.Tag.ToDto()).ToList();

        return new PostDto(post.Id, post.Content, imageUrl, post.Privacy, post.CreatedAt, post.UpdatedAt, userDto,
                          post.Likes.Count, post.Comments.Count, post.Reposts.Count, tags, isLiked, isReposted, isEdited);
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
