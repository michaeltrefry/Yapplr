using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public interface IPostService
{
    Task<PostDto?> CreatePostAsync(int userId, CreatePostDto createDto);
    Task<PostDto?> GetPostByIdAsync(int postId, int? currentUserId = null);
    Task<PostDto?> UpdatePostAsync(int postId, int userId, UpdatePostDto updateDto);
    Task<IEnumerable<PostDto>> GetTimelineAsync(int userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<TimelineItemDto>> GetTimelineWithRepostsAsync(int userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<TimelineItemDto>> GetPublicTimelineAsync(int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<IEnumerable<PostDto>> GetUserPostsAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<IEnumerable<TimelineItemDto>> GetUserTimelineAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<bool> DeletePostAsync(int postId, int userId);

    // Social features
    Task<bool> LikePostAsync(int postId, int userId);
    Task<bool> UnlikePostAsync(int postId, int userId);
    Task<bool> RepostAsync(int postId, int userId);
    Task<bool> UnrepostAsync(int postId, int userId);

    // Comments
    Task<CommentDto?> AddCommentAsync(int postId, int userId, CreateCommentDto createDto);
    Task<CommentDto?> UpdateCommentAsync(int commentId, int userId, UpdateCommentDto updateDto);
    Task<IEnumerable<CommentDto>> GetPostCommentsAsync(int postId);
    Task<IEnumerable<CommentDto>> GetPostCommentsAsync(int postId, int currentUserId);
    Task<bool> DeleteCommentAsync(int commentId, int userId);
}
