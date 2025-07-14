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
    Task<int> GetLikeCountAsync(int postId);
    Task<int> GetCommentCountAsync(int postId);
    Task<int> GetRepostCountAsync(int postId);

    // Comment counts
    Task<int> GetCommentLikeCountAsync(int commentId);
    Task<bool> HasUserLikedCommentAsync(int commentId, int userId);

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