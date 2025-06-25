using Postr.Api.DTOs;

namespace Postr.Api.Services;

public interface IPostService
{
    Task<PostDto?> CreatePostAsync(int userId, CreatePostDto createDto);
    Task<PostDto?> GetPostByIdAsync(int postId, int? currentUserId = null);
    Task<IEnumerable<PostDto>> GetTimelineAsync(int userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<TimelineItemDto>> GetTimelineWithRepostsAsync(int userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<PostDto>> GetUserPostsAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<bool> DeletePostAsync(int postId, int userId);

    // Social features
    Task<bool> LikePostAsync(int postId, int userId);
    Task<bool> UnlikePostAsync(int postId, int userId);
    Task<bool> RepostAsync(int postId, int userId);
    Task<bool> UnrepostAsync(int postId, int userId);

    // Comments
    Task<CommentDto?> AddCommentAsync(int postId, int userId, CreateCommentDto createDto);
    Task<IEnumerable<CommentDto>> GetPostCommentsAsync(int postId);
    Task<bool> DeleteCommentAsync(int commentId, int userId);
}
