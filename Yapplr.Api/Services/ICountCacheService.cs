using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for caching count operations (followers, posts, likes, etc.)
/// </summary>
public interface ICountCacheService
{
    // User counts
    Task<int> GetFollowerCountAsync(int userId);
    Task<int> GetFollowingCountAsync(int userId);
    Task<int> GetPostCountAsync(int userId);
    
    // Post counts
    Task<int> GetPostReactionCountAsync(int postId, ReactionType reactionType);
    Task<Dictionary<ReactionType, int>> GetPostReactionCountsAsync(int postId);
    Task<int> GetTotalPostReactionCountAsync(int postId);
    Task<ReactionType?> GetUserPostReactionAsync(int postId, int userId);
    Task<int> GetCommentCountAsync(int postId);
    Task<int> GetRepostCountAsync(int postId);

    // Comment counts
    Task<int> GetCommentReactionCountAsync(int commentId, ReactionType reactionType);
    Task<Dictionary<ReactionType, int>> GetCommentReactionCountsAsync(int commentId);
    Task<int> GetTotalCommentReactionCountAsync(int commentId);
    Task<ReactionType?> GetUserCommentReactionAsync(int commentId, int userId);

    // Notification counts
    Task<int> GetUnreadNotificationCountAsync(int userId);
    Task<int> GetUnreadMessageCountAsync(int userId);
    
    // Tag counts
    Task<int> GetTagPostCountAsync(int tagId);
    Task<int> GetTagPostCountAsync(string tagName, int? currentUserId = null);
    
    // Cache invalidation
    Task InvalidateUserCountsAsync(int userId);
    Task InvalidatePostCountsAsync(int postId);
    Task InvalidateCommentCountsAsync(int commentId);
    Task InvalidateFollowCountsAsync(int followerId, int followingId);
    Task InvalidateNotificationCountsAsync(int userId);
    Task InvalidateTagCountsAsync(int tagId);
    Task InvalidateTagCountsAsync(string tagName);
}