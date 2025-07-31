using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IPostService
{
    Task<PostDto?> CreatePostAsync(int userId, CreatePostDto createDto);
    Task<PostDto?> CreatePostWithMediaAsync(int userId, CreatePostWithMediaDto createDto);
    Task<PostDto?> GetPostByIdAsync(int postId, int? currentUserId = null);
    Task<PostDto?> UpdatePostAsync(int postId, int userId, UpdatePostDto updateDto);
    Task<IEnumerable<TimelineItemDto>> GetTimelineAsync(int userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<TimelineItemDto>> GetPublicTimelineAsync(int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<IEnumerable<PostDto>> GetUserPostsAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<IEnumerable<PostDto>> GetUserPhotosAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<IEnumerable<PostDto>> GetUserVideosAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<IEnumerable<TimelineItemDto>> GetUserTimelineAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20);
    Task<bool> DeletePostAsync(int postId, int userId);

    // Social features
    Task<bool> ReactToPostAsync(int postId, int userId, ReactionType reactionType);
    Task<bool> RemovePostReactionAsync(int postId, int userId);

    // Enhanced Repost functionality (replaces simple repost and quote tweet)
    Task<PostDto?> CreateRepostAsync(int userId, CreateRepostDto createDto);
    Task<PostDto?> CreateRepostWithMediaAsync(int userId, CreateRepostWithMediaDto createDto);
    Task<IEnumerable<PostDto>> GetRepostsAsync(int postId, int? currentUserId = null, int page = 1, int pageSize = 20);





    // Comments
    Task<CommentDto?> AddCommentAsync(int postId, int userId, CreateCommentDto createDto);
    Task<CommentDto?> UpdateCommentAsync(int commentId, int userId, UpdateCommentDto updateDto);
    Task<IEnumerable<CommentDto>> GetPostCommentsAsync(int postId);
    Task<IEnumerable<CommentDto>> GetPostCommentsAsync(int postId, int currentUserId);
    Task<bool> DeleteCommentAsync(int commentId, int userId);

    // Comment reactions
    Task<bool> ReactToCommentAsync(int commentId, int userId, ReactionType reactionType);
    Task<bool> RemoveCommentReactionAsync(int commentId, int userId);
}
