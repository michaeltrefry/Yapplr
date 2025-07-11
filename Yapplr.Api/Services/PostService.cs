using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;
using Yapplr.Api.Utils;
using Yapplr.Api.Common;

namespace Yapplr.Api.Services;

public class PostService : BaseService, IPostService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IBlockService _blockService;
    private readonly INotificationService _notificationService;
    private readonly ILinkPreviewService _linkPreviewService;
    private readonly IContentModerationService _contentModerationService;
    private readonly IConfiguration _configuration;

    public PostService(
        YapplrDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IBlockService blockService,
        INotificationService notificationService,
        ILinkPreviewService linkPreviewService,
        IContentModerationService contentModerationService,
        IConfiguration configuration,
        ILogger<PostService> logger) : base(context, logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _blockService = blockService;
        _notificationService = notificationService;
        _linkPreviewService = linkPreviewService;
        _contentModerationService = contentModerationService;
        _configuration = configuration;
    }

    public async Task<PostDto?> CreatePostAsync(int userId, CreatePostDto createDto)
    {
        var post = new Post
        {
            Content = createDto.Content,
            ImageFileName = createDto.ImageFileName,
            Privacy = createDto.Privacy,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Process hashtags in the post content
        await ProcessPostTagsAsync(post.Id, createDto.Content);

        // Process link previews in the post content
        await ProcessPostLinkPreviewsAsync(post.Id, createDto.Content);

        // Create mention notifications for users mentioned in the post
        await _notificationService.CreateMentionNotificationsAsync(createDto.Content, userId, post.Id);

        // Perform content moderation analysis
        await ProcessContentModerationAsync(post.Id, createDto.Content, userId);

        // Load the post with user data
        var createdPost = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .AsSplitQuery()
            .FirstAsync(p => p.Id == post.Id);

        return createdPost.MapToPostDto(userId, _httpContextAccessor.HttpContext);
    }

    public async Task<PostDto?> GetPostByIdAsync(int postId, int? currentUserId = null)
    {
        var post = await _context.GetPostsWithIncludes()
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeletedByUser);

        if (post == null) return null;

        // Check if user can view this hidden post
        if (post.IsHidden && !await _context.CanViewHiddenContentAsync(currentUserId, post.UserId))
        {
            return null; // Hide the post from unauthorized users
        }

        return post.MapToPostDto(currentUserId, _httpContextAccessor.HttpContext, includeModeration: true);
    }

    public async Task<IEnumerable<PostDto>> GetTimelineAsync(int userId, int page = 1, int pageSize = 20)
    {
        // Get user's following list for privacy filtering
        var followingIds = await _context.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync();

        // Get blocked user IDs to filter them out
        var blockedUserIds = await GetBlockedUserIdsAsync(userId);

        var posts = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeletedByUser && !c.IsHidden)) // Filter out user-deleted and moderator-hidden comments
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .Where(p =>
                !p.IsDeletedByUser && // Filter out user-deleted posts
                !p.IsHidden && // Filter out moderator-hidden posts
                !blockedUserIds.Contains(p.UserId) && // Filter out blocked users
                (p.Privacy == PostPrivacy.Public || // Public posts are visible to everyone
                p.UserId == userId || // User's own posts are always visible
                (p.Privacy == PostPrivacy.Followers && followingIds.Contains(p.UserId)))) // Followers-only posts visible if following the author
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return posts.Select(p => p.MapToPostDto(userId, _httpContextAccessor.HttpContext));
    }

    public async Task<IEnumerable<TimelineItemDto>> GetTimelineWithRepostsAsync(int userId, int page = 1, int pageSize = 20)
    {
        LogOperation(nameof(GetTimelineWithRepostsAsync), new { userId, page, pageSize });

        // Get user's following list and blocked users for filtering
        var followingIds = await GetFollowingUserIdsAsync(userId);
        var blockedUserIds = await GetBlockedUserIdsAsync(userId);

        // Create a larger page size for fetching since we'll filter and sort in memory
        var fetchSize = pageSize * 3; // Fetch more to account for mixed posts and reposts

        // Get original posts with filtering
        var posts = await _context.GetPostsWithIncludes()
            .FilterForVisibility(userId, blockedUserIds, followingIds)
            .OrderByNewest()
            .Take(fetchSize)
            .ToListAsync();

        // Get reposts from followed users and self
        var reposts = await _context.GetRepostsWithIncludes()
            .Where(r =>
                !r.Post.IsDeletedByUser && // Filter out reposts of user-deleted posts
                !blockedUserIds.Contains(r.UserId) && // Filter out reposts from blocked users
                !blockedUserIds.Contains(r.Post.UserId) && // Filter out reposts of posts from blocked users
                (r.UserId == userId || followingIds.Contains(r.UserId))) // Reposts from self or followed users
            .Where(r =>
                r.Post.Privacy == PostPrivacy.Public || // Public posts can be reposted
                r.Post.UserId == userId || // User's own posts
                (r.Post.Privacy == PostPrivacy.Followers && followingIds.Contains(r.Post.UserId))) // Followers-only posts if following original author
            .OrderByDescending(r => r.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Convert to timeline items in memory (no EF translation issues)
        var timelineItems = new List<TimelineItemDto>();

        // Add original posts
        foreach (var post in posts)
        {
            timelineItems.Add(new TimelineItemDto("post", post.CreatedAt,
                post.MapToPostDto(userId, _httpContextAccessor.HttpContext)));
        }

        // Add reposts
        foreach (var repost in reposts)
        {
            var repostedByUser = repost.User.MapToUserDto();

            timelineItems.Add(new TimelineItemDto("repost", repost.CreatedAt,
                repost.Post.MapToPostDto(userId, _httpContextAccessor.HttpContext), repostedByUser));
        }

        // Sort by creation date and apply pagination
        var result = timelineItems
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return result;
    }

    public async Task<IEnumerable<TimelineItemDto>> GetPublicTimelineAsync(int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        // For public timeline, we only show public posts and reposts of public posts
        var fetchSize = pageSize * 2; // Fetch more to account for mixed posts and reposts

        // Get blocked user IDs if user is authenticated
        var blockedUserIds = currentUserId.HasValue
            ? await _context.GetBlockedUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();

        // Get public posts only
        var posts = await _context.GetPostsForFeed()
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .OrderByDescending(p => p.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Get reposts of public posts only
        var reposts = await _context.GetRepostsWithIncludes()
            .ApplyRepostVisibilityFilters(blockedUserIds)
            .OrderByDescending(r => r.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Combine posts and reposts into timeline items
        var timelineItems = new List<TimelineItemDto>();

        // Add original posts
        timelineItems.AddRange(posts.Select(p => new TimelineItemDto(
            "post",
            p.CreatedAt,
            p.MapToPostDto(currentUserId, _httpContextAccessor.HttpContext)
        )));

        // Add reposts
        timelineItems.AddRange(reposts.Select(r => new TimelineItemDto(
            "repost",
            r.CreatedAt,
            r.Post.MapToPostDto(currentUserId, _httpContextAccessor.HttpContext),
            r.User.ToDto()
        )));

        // Sort by creation date and take the requested page
        return timelineItems
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<IEnumerable<PostDto>> GetUserPostsAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        // Check if current user is blocked by the profile owner or has blocked them
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            var isBlocked = await _blockService.IsUserBlockedAsync(currentUserId.Value, userId) ||
                           await _blockService.IsBlockedByUserAsync(currentUserId.Value, userId);
            if (isBlocked)
            {
                return new List<PostDto>(); // Return empty list if blocked
            }
        }

        // Check if current user is following the target user
        var isFollowing = false;
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            isFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == userId);
        }

        var posts = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeletedByUser && !c.IsHidden)) // Filter out user-deleted and moderator-hidden comments
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .AsSplitQuery()
            .Where(p => p.UserId == userId &&
                !p.IsDeletedByUser && // Filter out user-deleted posts
                !p.IsHidden && // Filter out moderator-hidden posts
                (p.Privacy == PostPrivacy.Public || // Public posts are visible to everyone
                 currentUserId == userId || // User's own posts are always visible
                 (p.Privacy == PostPrivacy.Followers && isFollowing))) // Followers-only posts visible if following
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return posts.Select(p => p.MapToPostDto(currentUserId, _httpContextAccessor.HttpContext));
    }

    public async Task<IEnumerable<TimelineItemDto>> GetUserTimelineAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        // Check if current user is blocked by the profile owner or has blocked them
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            var isBlocked = await _blockService.IsUserBlockedAsync(currentUserId.Value, userId) ||
                           await _blockService.IsBlockedByUserAsync(currentUserId.Value, userId);
            if (isBlocked)
            {
                return new List<TimelineItemDto>(); // Return empty list if blocked
            }
        }

        // Check if current user is following the target user (for privacy filtering)
        var isFollowing = false;
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            isFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == userId);
        }

        var fetchSize = pageSize * 2; // Fetch more to account for mixed posts and reposts

        // Get user's original posts
        var posts = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeletedByUser && !c.IsHidden)) // Filter out user-deleted and moderator-hidden comments
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .AsSplitQuery()
            .Where(p => p.UserId == userId &&
                !p.IsDeletedByUser && // Filter out user-deleted posts
                !p.IsHidden && // Filter out moderator-hidden posts
                (p.Privacy == PostPrivacy.Public || // Public posts are visible to everyone
                 currentUserId == userId || // User's own posts are always visible
                 (p.Privacy == PostPrivacy.Followers && isFollowing))) // Followers-only posts visible if following
            .OrderByDescending(p => p.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Get user's reposts
        var reposts = await _context.Reposts
            .Include(r => r.User)
            .Include(r => r.Post)
            .ThenInclude(p => p.User)
            .Include(r => r.Post)
            .ThenInclude(p => p.Likes)
            .Include(r => r.Post)
            .ThenInclude(p => p.Comments.Where(c => !c.IsDeletedByUser && !c.IsHidden)) // Filter out user-deleted and moderator-hidden comments
            .Include(r => r.Post)
            .ThenInclude(p => p.Reposts)
            .AsSplitQuery()
            .Where(r => r.UserId == userId && !r.Post.IsDeletedByUser && !r.Post.IsHidden) // Reposts by this user, filter out deleted and hidden posts
            .Where(r =>
                r.Post.Privacy == PostPrivacy.Public || // Public posts can be seen
                r.Post.UserId == currentUserId || // Current user's own posts
                (r.Post.Privacy == PostPrivacy.Followers && currentUserId == userId)) // User viewing their own reposts
            .OrderByDescending(r => r.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Filter reposts based on whether current user follows the original authors (for followers-only posts)
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            var followingIds = await _context.Follows
                .Where(f => f.FollowerId == currentUserId.Value)
                .Select(f => f.FollowingId)
                .ToListAsync();

            reposts = reposts.Where(r =>
                r.Post.Privacy == PostPrivacy.Public ||
                r.Post.UserId == currentUserId ||
                (r.Post.Privacy == PostPrivacy.Followers && followingIds.Contains(r.Post.UserId)))
                .ToList();
        }

        // Convert to timeline items in memory
        var timelineItems = new List<TimelineItemDto>();

        // Add original posts
        foreach (var post in posts)
        {
            timelineItems.Add(new TimelineItemDto("post", post.CreatedAt, post.MapToPostDto(currentUserId, _httpContextAccessor.HttpContext)));
        }

        // Add reposts
        foreach (var repost in reposts)
        {
            var repostedByUser = repost.User.ToDto();

            timelineItems.Add(new TimelineItemDto("repost", repost.CreatedAt, repost.Post.MapToPostDto(currentUserId, _httpContextAccessor.HttpContext), repostedByUser));
        }

        // Sort by creation date and apply pagination
        var result = timelineItems
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return result;
    }

    public async Task<bool> DeletePostAsync(int postId, int userId)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

        if (post == null)
            return false;

        // Soft delete: mark as deleted by user instead of removing from database
        post.IsDeletedByUser = true;
        post.DeletedByUserAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LikePostAsync(int postId, int userId)
    {
        // Check if already liked
        if (await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == userId))
            return false;

        var like = new Like
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Likes.Add(like);
        await _context.SaveChangesAsync();

        // Get the post owner to create notification
        var post = await _context.Posts.FindAsync(postId);
        if (post != null)
        {
            await _notificationService.CreateLikeNotificationAsync(post.UserId, userId, postId);
        }

        return true;
    }

    public async Task<bool> UnlikePostAsync(int postId, int userId)
    {
        var like = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        
        if (like == null)
            return false;

        _context.Likes.Remove(like);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RepostAsync(int postId, int userId)
    {
        // Check if already reposted
        if (await _context.Reposts.AnyAsync(r => r.PostId == postId && r.UserId == userId))
            return false;

        var repost = new Repost
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reposts.Add(repost);
        await _context.SaveChangesAsync();

        // Get the post owner to create notification
        var post = await _context.Posts.FindAsync(postId);
        if (post != null)
        {
            await _notificationService.CreateRepostNotificationAsync(post.UserId, userId, postId);
        }

        return true;
    }

    public async Task<bool> UnrepostAsync(int postId, int userId)
    {
        var repost = await _context.Reposts.FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);
        
        if (repost == null)
            return false;

        _context.Reposts.Remove(repost);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CommentDto?> AddCommentAsync(int postId, int userId, CreateCommentDto createDto)
    {
        var comment = new Comment
        {
            Content = createDto.Content,
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Get the post owner to create comment notification
        var post = await _context.Posts.FindAsync(postId);
        if (post != null)
        {
            await _notificationService.CreateCommentNotificationAsync(post.UserId, userId, postId, comment.Id, createDto.Content);
        }

        // Create mention notifications for users mentioned in the comment
        await _notificationService.CreateMentionNotificationsAsync(createDto.Content, userId, postId, comment.Id);

        // Perform content moderation analysis on comment
        await ProcessCommentModerationAsync(comment.Id, createDto.Content, userId);

        // Load the comment with user data
        var createdComment = await _context.Comments
            .Include(c => c.User)
            .FirstAsync(c => c.Id == comment.Id);

        return createdComment.MapToCommentDto(_httpContextAccessor.HttpContext);
    }

    public async Task<IEnumerable<CommentDto>> GetPostCommentsAsync(int postId)
    {
        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId && !c.IsDeletedByUser && !c.IsHidden) // Filter out user-deleted and moderator-hidden comments
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(c => c.MapToCommentDto(_httpContextAccessor.HttpContext));
    }

    public async Task<IEnumerable<CommentDto>> GetPostCommentsAsync(int postId, int currentUserId)
    {
        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId && !c.IsDeletedByUser && !c.IsHidden) // Filter out user-deleted and moderator-hidden comments
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        // Filter out comments from blocked users if user is authenticated
        var blockedUserIds = await GetBlockedUserIdsAsync(currentUserId);
        comments = comments.Where(c => !blockedUserIds.Contains(c.UserId)).ToList();
    

        return comments.Select(c => c.MapToCommentDto(_httpContextAccessor.HttpContext));
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);

        if (comment == null)
            return false;

        // Soft delete: mark as deleted by user instead of removing from database
        comment.IsDeletedByUser = true;
        comment.DeletedByUserAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    // Removed MapToPostDto method - now using extension method from MappingUtilities

    // Removed MapToCommentDto method - now using extension method from MappingUtilities

    public async Task<PostDto?> UpdatePostAsync(int postId, int userId, UpdatePostDto updateDto)
    {
        LogOperation(nameof(UpdatePostAsync), new { postId, userId });

        var post = await _context.GetPostsWithIncludes()
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
            return null;

        ValidateUserAuthorization(userId, post.UserId, "update this post");

        post.Content = updateDto.Content;
        post.Privacy = updateDto.Privacy;
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Update hashtags for the post
        await UpdatePostTagsAsync(post.Id, updateDto.Content);

        // Update link previews for the post
        await UpdatePostLinkPreviewsAsync(post.Id, updateDto.Content);

        // Create mention notifications for new mentions in the updated post
        await _notificationService.CreateMentionNotificationsAsync(updateDto.Content, userId, post.Id);

        // Perform content moderation analysis on updated content
        await ProcessContentModerationAsync(post.Id, updateDto.Content, userId);

        return post.MapToPostDto(userId, _httpContextAccessor.HttpContext);
    }

    public async Task<CommentDto?> UpdateCommentAsync(int commentId, int userId, UpdateCommentDto updateDto)
    {
        var comment = await _context.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null || comment.UserId != userId)
            return null;

        comment.Content = updateDto.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Create mention notifications for new mentions in the updated comment
        await _notificationService.CreateMentionNotificationsAsync(updateDto.Content, userId, comment.PostId, comment.Id);

        // Perform content moderation analysis on updated comment
        await ProcessCommentModerationAsync(comment.Id, updateDto.Content, userId);

        return comment.MapToCommentDto(_httpContextAccessor.HttpContext);
    }

    private new async Task<List<int>> GetBlockedUserIdsAsync(int userId)
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

    private async Task ProcessPostTagsAsync(int postId, string content)
    {
        // Extract hashtags from the content
        var tagNames = TagParser.ExtractTags(content);

        if (!tagNames.Any())
            return;

        // Get or create tags
        var existingTags = await _context.Tags
            .Where(t => tagNames.Contains(t.Name))
            .ToListAsync();

        var existingTagNames = existingTags.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newTagNames = tagNames.Where(name => !existingTagNames.Contains(name)).ToList();

        // Create new tags
        var newTags = newTagNames.Select(name => new Tag
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            PostCount = 1
        }).ToList();

        if (newTags.Any())
        {
            _context.Tags.AddRange(newTags);
            await _context.SaveChangesAsync();
        }

        // Update post count for existing tags
        foreach (var existingTag in existingTags)
        {
            existingTag.PostCount++;
        }

        // Combine all tags (existing + new)
        var allTags = existingTags.Concat(newTags).ToList();

        // Create PostTag relationships
        var postTags = allTags.Select(tag => new PostTag
        {
            PostId = postId,
            TagId = tag.Id,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.PostTags.AddRange(postTags);
        await _context.SaveChangesAsync();
    }

    private async Task ProcessPostLinkPreviewsAsync(int postId, string content)
    {
        try
        {
            // Extract and process link previews
            var linkPreviews = (await _linkPreviewService.ProcessPostLinksAsync(content)).ToList();

            if (!linkPreviews.Any())
                return;

            // Create PostLinkPreview relationships
            var postLinkPreviews = linkPreviews.Select(lp => new PostLinkPreview
            {
                PostId = postId,
                LinkPreviewId = lp.Id,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.PostLinkPreviews.AddRange(postLinkPreviews);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't fail post creation if link preview processing fails
            // This could be logged to a proper logging system
            Console.WriteLine($"Failed to process link previews for post {postId}: {ex.Message}");
        }
    }

    private async Task UpdatePostTagsAsync(int postId, string content)
    {
        // Get current tags for the post
        var currentPostTags = await _context.PostTags
            .Include(pt => pt.Tag)
            .Where(pt => pt.PostId == postId)
            .ToListAsync();

        var currentTagNames = currentPostTags.Select(pt => pt.Tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Extract new tags from content
        var newTagNames = TagParser.ExtractTags(content).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Find tags to remove and add
        var tagsToRemove = currentPostTags.Where(pt => !newTagNames.Contains(pt.Tag.Name)).ToList();
        var tagNamesToAdd = newTagNames.Where(name => !currentTagNames.Contains(name)).ToList();

        // Remove old tags
        if (tagsToRemove.Any())
        {
            _context.PostTags.RemoveRange(tagsToRemove);

            // Decrease post count for removed tags
            foreach (var removedPostTag in tagsToRemove)
            {
                removedPostTag.Tag.PostCount = Math.Max(0, removedPostTag.Tag.PostCount - 1);
            }
        }

        // Add new tags
        if (tagNamesToAdd.Any())
        {
            // Get or create new tags
            var existingTags = await _context.Tags
                .Where(t => tagNamesToAdd.Contains(t.Name))
                .ToListAsync();

            var existingTagNames = existingTags.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newTagNamesToCreate = tagNamesToAdd.Where(name => !existingTagNames.Contains(name)).ToList();

            // Create new tags
            var newTags = newTagNamesToCreate.Select(name => new Tag
            {
                Name = name,
                CreatedAt = DateTime.UtcNow,
                PostCount = 1
            }).ToList();

            if (newTags.Any())
            {
                _context.Tags.AddRange(newTags);
                await _context.SaveChangesAsync();
            }

            // Update post count for existing tags
            foreach (var existingTag in existingTags)
            {
                existingTag.PostCount++;
            }

            // Combine all tags (existing + new)
            var allTags = existingTags.Concat(newTags).ToList();

            // Create PostTag relationships
            var newPostTags = allTags.Select(tag => new PostTag
            {
                PostId = postId,
                TagId = tag.Id,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.PostTags.AddRange(newPostTags);
        }

        await _context.SaveChangesAsync();
    }

    private async Task UpdatePostLinkPreviewsAsync(int postId, string content)
    {
        try
        {
            // Get current link previews for the post
            var currentPostLinkPreviews = await _context.PostLinkPreviews
                .Include(plp => plp.LinkPreview)
                .Where(plp => plp.PostId == postId)
                .ToListAsync();

            // Remove all existing link preview relationships for this post
            if (currentPostLinkPreviews.Any())
            {
                _context.PostLinkPreviews.RemoveRange(currentPostLinkPreviews);
                await _context.SaveChangesAsync();
            }

            // Process new link previews
            var linkPreviews = (await _linkPreviewService.ProcessPostLinksAsync(content)).ToList();

            if (linkPreviews.Any())
            {
                // Create new PostLinkPreview relationships
                var newPostLinkPreviews = linkPreviews.Select(lp => new PostLinkPreview
                {
                    PostId = postId,
                    LinkPreviewId = lp.Id,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.PostLinkPreviews.AddRange(newPostLinkPreviews);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail post update if link preview processing fails
            Console.WriteLine($"Failed to update link previews for post {postId}: {ex.Message}");
        }
    }

    private async Task ProcessContentModerationAsync(int postId, string content, int userId)
    {
        try
        {
            var moderationEnabled = _configuration.GetValue("ContentModeration:Enabled", true);
            if (!moderationEnabled)
                return;

            // Check if the content moderation service is available
            var isServiceAvailable = await _contentModerationService.IsServiceAvailableAsync();
            if (!isServiceAvailable)
            {
                _logger.LogWarning("Content moderation service is not available for post {PostId}", postId);
                return;
            }

            // Analyze the content
            var moderationResult = await _contentModerationService.AnalyzeContentAsync(content);

            // Log the analysis result
            _logger.LogInformation("Content moderation analysis for post {PostId}: Risk Level {RiskLevel}, Requires Review: {RequiresReview}",
                postId, moderationResult.RiskAssessment.Level, moderationResult.RequiresReview);

            // Apply suggested tags if any were found
            if (moderationResult.SuggestedTags.Any())
            {
                await _contentModerationService.ApplySuggestedTagsToPostAsync(postId, moderationResult, userId);
            }

            // Auto-hide content if risk is very high
            var autoHideThreshold = _configuration.GetValue("ContentModeration:AutoHideThreshold", 0.8);
            if (moderationResult.RiskAssessment.Score >= autoHideThreshold)
            {
                var post = await _context.Posts.FindAsync(postId);
                if (post != null)
                {
                    post.IsHidden = true;
                    post.HiddenByUserId = userId; // System user ID would be better
                    post.HiddenAt = DateTime.UtcNow;
                    post.HiddenReason = $"Auto-hidden due to high risk content (Score: {moderationResult.RiskAssessment.Score:F2})";
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Auto-hidden post {PostId} due to high risk score {RiskScore}",
                        postId, moderationResult.RiskAssessment.Score);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing content moderation for post {PostId}", postId);
            // Don't fail post creation/update if moderation fails
        }
    }

    private async Task ProcessCommentModerationAsync(int commentId, string content, int userId)
    {
        try
        {
            var moderationEnabled = _configuration.GetValue("ContentModeration:Enabled", true);
            if (!moderationEnabled)
                return;

            // Check if the content moderation service is available
            var isServiceAvailable = await _contentModerationService.IsServiceAvailableAsync();
            if (!isServiceAvailable)
            {
                _logger.LogWarning("Content moderation service is not available for comment {CommentId}", commentId);
                return;
            }

            // Analyze the content
            var moderationResult = await _contentModerationService.AnalyzeContentAsync(content);

            // Log the analysis result
            _logger.LogInformation("Content moderation analysis for comment {CommentId}: Risk Level {RiskLevel}, Requires Review: {RequiresReview}",
                commentId, moderationResult.RiskAssessment.Level, moderationResult.RequiresReview);

            // Apply suggested tags if any were found
            if (moderationResult.SuggestedTags.Any())
            {
                await _contentModerationService.ApplySuggestedTagsToCommentAsync(commentId, moderationResult, userId);
            }

            // Auto-hide content if risk is very high
            var autoHideThreshold = _configuration.GetValue("ContentModeration:AutoHideThreshold", 0.8);
            if (moderationResult.RiskAssessment.Score >= autoHideThreshold)
            {
                var comment = await _context.Comments.FindAsync(commentId);
                if (comment != null)
                {
                    comment.IsHidden = true;
                    comment.HiddenByUserId = userId; // System user ID would be better
                    comment.HiddenAt = DateTime.UtcNow;
                    comment.HiddenReason = $"Auto-hidden due to high risk content (Score: {moderationResult.RiskAssessment.Score:F2})";
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Auto-hidden comment {CommentId} due to high risk score {RiskScore}",
                        commentId, moderationResult.RiskAssessment.Score);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing content moderation for comment {CommentId}", commentId);
            // Don't fail comment creation/update if moderation fails
        }
    }
}
