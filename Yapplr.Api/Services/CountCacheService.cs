using Yapplr.Api.Common;
using Yapplr.Api.Data;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class CountCacheService : ICountCacheService
{
    private readonly ICachingService _cache;
    private readonly YapplrDbContext _context;
    private readonly ILogger<CountCacheService> _logger;
    
    // Cache expiration times
    private static readonly TimeSpan UserCountExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PostCountExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NotificationCountExpiration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan TagCountExpiration = TimeSpan.FromMinutes(15);

    public CountCacheService(ICachingService cache, YapplrDbContext context, ILogger<CountCacheService> logger)
    {
        _cache = cache;
        _context = context;
        _logger = logger;
    }

    // User counts
    public async Task<int> GetFollowerCountAsync(int userId)
    {
        var key = $"count:followers:{userId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.Follows.CountAsync(f => f.FollowingId == userId);
            _logger.LogDebug("Calculated follower count for user {UserId}: {Count}", userId, count);
            return count;
        }, UserCountExpiration);
    }

    public async Task<int> GetFollowingCountAsync(int userId)
    {
        var key = $"count:following:{userId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.Follows.CountAsync(f => f.FollowerId == userId);
            _logger.LogDebug("Calculated following count for user {UserId}: {Count}", userId, count);
            return count;
        }, UserCountExpiration);
    }

    public async Task<int> GetPostCountAsync(int userId)
    {
        var key = $"count:posts:{userId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.Posts
                .CountAsync(p => p.UserId == userId && !p.IsHidden);
            _logger.LogDebug("Calculated post count for user {UserId}: {Count}", userId, count);
            return count;
        }, UserCountExpiration);
    }

    // Post counts
    public async Task<int> GetLikeCountAsync(int postId)
    {
        var key = $"count:likes:{postId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.Likes.CountAsync(l => l.PostId == postId);
            _logger.LogDebug("Calculated like count for post {PostId}: {Count}", postId, count);
            return count;
        }, PostCountExpiration);
    }

    public async Task<int> GetCommentCountAsync(int postId)
    {
        var key = $"count:comments:{postId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.Posts
                .CountAsync(c => c.ParentId == postId && c.PostType == PostType.Comment && !c.IsDeletedByUser && !c.IsHidden);
            _logger.LogDebug("Calculated comment count for post {PostId}: {Count}", postId, count);
            return count;
        }, PostCountExpiration);
    }

    public async Task<int> GetRepostCountAsync(int postId)
    {
        var key = $"count:reposts:{postId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.Reposts.CountAsync(r => r.PostId == postId);
            _logger.LogDebug("Calculated repost count for post {PostId}: {Count}", postId, count);
            return count;
        }, PostCountExpiration);
    }

    // Comment counts
    public async Task<int> GetCommentLikeCountAsync(int commentId)
    {
        var key = $"count:comment:likes:{commentId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            // Legacy - now using PostReactions with Heart reaction type
            var count = await _context.PostReactions.CountAsync(pr => pr.PostId == commentId && pr.ReactionType == ReactionType.Heart);
            _logger.LogDebug("Calculated like count for comment {CommentId}: {Count}", commentId, count);
            return count;
        }, PostCountExpiration);
    }

    public async Task<bool> HasUserLikedCommentAsync(int commentId, int userId)
    {
        var key = $"user:liked:comment:{userId}:{commentId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            // Legacy - now using PostReactions with Heart reaction type
            var hasLiked = await _context.PostReactions.AnyAsync(pr => pr.PostId == commentId && pr.UserId == userId && pr.ReactionType == ReactionType.Heart);
            _logger.LogDebug("Checked if user {UserId} liked comment {CommentId}: {HasLiked}", userId, commentId, hasLiked);
            return hasLiked;
        }, PostCountExpiration);
    }

    // Post reaction counts
    public async Task<int> GetPostReactionCountAsync(int postId, ReactionType reactionType)
    {
        var key = $"count:post:reactions:{postId}:{(int)reactionType}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.PostReactions.CountAsync(r => r.PostId == postId && r.ReactionType == reactionType);
            _logger.LogDebug("Calculated {ReactionType} count for post {PostId}: {Count}", reactionType, postId, count);
            return count;
        }, PostCountExpiration);
    }

    public async Task<Dictionary<ReactionType, int>> GetPostReactionCountsAsync(int postId)
    {
        // Since caching service doesn't support Dictionary, we'll calculate this directly
        var reactions = await _context.PostReactions
            .Where(r => r.PostId == postId)
            .GroupBy(r => r.ReactionType)
            .Select(g => new { ReactionType = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = reactions.ToDictionary(r => r.ReactionType, r => r.Count);
        _logger.LogDebug("Calculated all reaction counts for post {PostId}: {Counts}", postId, result);
        return result;
    }

    public async Task<int> GetTotalPostReactionCountAsync(int postId)
    {
        var key = $"count:post:reactions:total:{postId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.PostReactions.CountAsync(r => r.PostId == postId);
            _logger.LogDebug("Calculated total reaction count for post {PostId}: {Count}", postId, count);
            return count;
        }, PostCountExpiration);
    }

    public async Task<ReactionType?> GetUserPostReactionAsync(int postId, int userId)
    {
        // Since caching service doesn't support nullable types, we'll calculate this directly
        var reaction = await _context.PostReactions
            .Where(r => r.PostId == postId && r.UserId == userId)
            .Select(r => (ReactionType?)r.ReactionType)
            .FirstOrDefaultAsync();
        _logger.LogDebug("Checked user {UserId} reaction to post {PostId}: {Reaction}", userId, postId, reaction);
        return reaction;
    }

    // Comment reaction counts
    public async Task<int> GetCommentReactionCountAsync(int commentId, ReactionType reactionType)
    {
        var key = $"count:comment:reactions:{commentId}:{(int)reactionType}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.PostReactions.CountAsync(r => r.PostId == commentId && r.ReactionType == reactionType);
            _logger.LogDebug("Calculated {ReactionType} count for comment {CommentId}: {Count}", reactionType, commentId, count);
            return count;
        }, PostCountExpiration);
    }

    public async Task<Dictionary<ReactionType, int>> GetCommentReactionCountsAsync(int commentId)
    {
        // Since caching service doesn't support Dictionary, we'll calculate this directly
        var reactions = await _context.PostReactions
            .Where(r => r.PostId == commentId)
            .GroupBy(r => r.ReactionType)
            .Select(g => new { ReactionType = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = reactions.ToDictionary(r => r.ReactionType, r => r.Count);
        _logger.LogDebug("Calculated all reaction counts for comment {CommentId}: {Counts}", commentId, result);
        return result;
    }

    public async Task<int> GetTotalCommentReactionCountAsync(int commentId)
    {
        var key = $"count:comment:reactions:total:{commentId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.PostReactions.CountAsync(r => r.PostId == commentId);
            _logger.LogDebug("Calculated total reaction count for comment {CommentId}: {Count}", commentId, count);
            return count;
        }, PostCountExpiration);
    }

    public async Task<ReactionType?> GetUserCommentReactionAsync(int commentId, int userId)
    {
        // Since caching service doesn't support nullable types, we'll calculate this directly
        var reaction = await _context.PostReactions
            .Where(r => r.PostId == commentId && r.UserId == userId)
            .Select(r => (ReactionType?)r.ReactionType)
            .FirstOrDefaultAsync();
        _logger.LogDebug("Checked user {UserId} reaction to comment {CommentId}: {Reaction}", userId, commentId, reaction);
        return reaction;
    }

    // Notification counts
    public async Task<int> GetUnreadNotificationCountAsync(int userId)
    {
        var key = $"count:notifications:unseen:{userId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsSeen);
            _logger.LogDebug("Calculated unseen notification count for user {UserId}: {Count}", userId, count);
            return count;
        }, NotificationCountExpiration);
    }

    public async Task<int> GetUnreadMessageCountAsync(int userId)
    {
        var key = $"count:messages:unread:{userId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            // This is a complex query, so we'll implement it similar to the existing MessageService
            var conversations = await _context.ConversationParticipants
                .Where(cp => cp.UserId == userId)
                .Include(cp => cp.Conversation)
                .ToListAsync();

            var totalUnread = 0;
            foreach (var participant in conversations)
            {
                var unreadCount = participant.LastReadAt == null
                    ? await _context.Messages.CountAsync(m => 
                        m.ConversationId == participant.ConversationId && 
                        m.SenderId != userId && 
                        !m.IsDeleted)
                    : await _context.Messages.CountAsync(m => 
                        m.ConversationId == participant.ConversationId &&
                        m.SenderId != userId &&
                        m.CreatedAt > participant.LastReadAt &&
                        !m.IsDeleted);
                
                totalUnread += unreadCount;
            }

            _logger.LogDebug("Calculated unread message count for user {UserId}: {Count}", userId, totalUnread);
            return totalUnread;
        }, NotificationCountExpiration);
    }

    // Tag counts
    public async Task<int> GetTagPostCountAsync(int tagId)
    {
        var key = $"count:tag:posts:{tagId}";
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var count = await _context.PostTags
                .CountAsync(pt => pt.TagId == tagId &&
                    pt.Post.Privacy == PostPrivacy.Public &&
                    !pt.Post.IsHidden);
            _logger.LogDebug("Calculated post count for tag {TagId}: {Count}", tagId, count);
            return count;
        }, TagCountExpiration);
    }

    public async Task<int> GetTagPostCountAsync(string tagName, int? currentUserId = null)
    {
        var key = currentUserId.HasValue 
            ? $"count:tag:posts:name:{tagName}:user:{currentUserId}"
            : $"count:tag:posts:name:{tagName}:public";
            
        return await _cache.GetOrSetValueAsync(key, async () =>
        {
            var normalizedTagName = tagName.ToLowerInvariant().TrimStart('#');

            // Get blocked user IDs if user is specified
            var blockedUserIds = new List<int>();
            if (currentUserId.HasValue)
            {
                blockedUserIds = await _context.Blocks
                    .Where(b => b.BlockerId == currentUserId.Value)
                    .Select(b => b.BlockedId)
                    .ToListAsync();
            }

            var count = await _context.PostTags
                .Where(pt => pt.Tag.Name == normalizedTagName &&
                    !blockedUserIds.Contains(pt.Post.UserId) &&
                    (!pt.Post.IsHidden ||
                     (pt.Post.HiddenReasonType == PostHiddenReasonType.VideoProcessing &&
                      currentUserId.HasValue && pt.Post.UserId == currentUserId.Value)) &&
                    pt.Post.User.Status == UserStatus.Active &&
                    (pt.Post.Privacy == PostPrivacy.Public ||
                     (currentUserId.HasValue && pt.Post.UserId == currentUserId.Value)))
                .CountAsync();

            _logger.LogDebug("Calculated post count for tag {TagName} (user {UserId}): {Count}",
                tagName, currentUserId, count);
            return count;
        }, TagCountExpiration);
    }

    // Cache invalidation methods
    public async Task InvalidateUserCountsAsync(int userId)
    {
        await _cache.RemoveAsync($"count:followers:{userId}");
        await _cache.RemoveAsync($"count:following:{userId}");
        await _cache.RemoveAsync($"count:posts:{userId}");
        _logger.LogDebug("Invalidated user counts for user {UserId}", userId);
    }

    public async Task InvalidatePostCountsAsync(int postId)
    {
        await _cache.RemoveAsync($"count:likes:{postId}");
        await _cache.RemoveAsync($"count:comments:{postId}");
        await _cache.RemoveAsync($"count:reposts:{postId}");
        _logger.LogDebug("Invalidated post counts for post {PostId}", postId);
    }

    public async Task InvalidateCommentCountsAsync(int commentId)
    {
        await _cache.RemoveAsync($"count:comment:likes:{commentId}");
        await _cache.RemoveByPatternAsync($"user:liked:comment:*:{commentId}");
        _logger.LogDebug("Invalidated comment counts for comment {CommentId}", commentId);
    }

    public async Task InvalidateFollowCountsAsync(int followerId, int followingId)
    {
        await _cache.RemoveAsync($"count:followers:{followingId}");
        await _cache.RemoveAsync($"count:following:{followerId}");
        _logger.LogDebug("Invalidated follow counts for follower {FollowerId} and following {FollowingId}", 
            followerId, followingId);
    }

    public async Task InvalidateNotificationCountsAsync(int userId)
    {
        await _cache.RemoveAsync($"count:notifications:unseen:{userId}");
        await _cache.RemoveAsync($"count:messages:unread:{userId}");
        _logger.LogDebug("Invalidated notification counts for user {UserId}", userId);
    }

    public async Task InvalidateTagCountsAsync(int tagId)
    {
        await _cache.RemoveByPatternAsync($"count:tag:posts:{tagId}");
        _logger.LogDebug("Invalidated tag counts for tag {TagId}", tagId);
    }

    public async Task InvalidateTagCountsAsync(string tagName)
    {
        var normalizedTagName = tagName.ToLowerInvariant().TrimStart('#');
        await _cache.RemoveByPatternAsync($"count:tag:posts:name:{normalizedTagName}");
        _logger.LogDebug("Invalidated tag counts for tag {TagName}", tagName);
    }
}
