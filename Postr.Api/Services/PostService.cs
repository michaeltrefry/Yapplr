using Microsoft.EntityFrameworkCore;
using Postr.Api.Data;
using Postr.Api.DTOs;
using Postr.Api.Models;

namespace Postr.Api.Services;

public class PostService : IPostService
{
    private readonly PostrDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostService(PostrDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PostDto?> CreatePostAsync(int userId, CreatePostDto createDto)
    {
        var post = new Post
        {
            Content = createDto.Content,
            ImageFileName = createDto.ImageFileName,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Load the post with user data
        var createdPost = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.Reposts)
            .FirstAsync(p => p.Id == post.Id);

        return MapToPostDto(createdPost, userId);
    }

    public async Task<PostDto?> GetPostByIdAsync(int postId, int? currentUserId = null)
    {
        var post = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.Reposts)
            .FirstOrDefaultAsync(p => p.Id == postId);

        return post == null ? null : MapToPostDto(post, currentUserId);
    }

    public async Task<IEnumerable<PostDto>> GetTimelineAsync(int userId, int page = 1, int pageSize = 20)
    {
        // For now, just return all posts ordered by creation date
        // In a real app, you'd implement following logic here
        var posts = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.Reposts)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return posts.Select(p => MapToPostDto(p, userId));
    }

    public async Task<IEnumerable<PostDto>> GetUserPostsAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        var posts = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.Reposts)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return posts.Select(p => MapToPostDto(p, currentUserId));
    }

    public async Task<bool> DeletePostAsync(int postId, int userId)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);
        
        if (post == null)
            return false;

        _context.Posts.Remove(post);
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

        // Load the comment with user data
        var createdComment = await _context.Comments
            .Include(c => c.User)
            .FirstAsync(c => c.Id == comment.Id);

        return MapToCommentDto(createdComment);
    }

    public async Task<IEnumerable<CommentDto>> GetPostCommentsAsync(int postId)
    {
        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(MapToCommentDto);
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);
        
        if (comment == null)
            return false;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        return true;
    }

    private PostDto MapToPostDto(Post post, int? currentUserId)
    {
        var userDto = new UserDto(post.User.Id, post.User.Email, post.User.Username,
                                 post.User.Bio, post.User.Birthday, post.User.Pronouns,
                                 post.User.Tagline, post.User.ProfileImageFileName, post.User.CreatedAt);

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

        return new PostDto(post.Id, post.Content, imageUrl, post.CreatedAt, userDto,
                          post.Likes.Count, post.Comments.Count, post.Reposts.Count, isLiked, isReposted);
    }

    private CommentDto MapToCommentDto(Comment comment)
    {
        var userDto = new UserDto(comment.User.Id, comment.User.Email, comment.User.Username,
                                 comment.User.Bio, comment.User.Birthday, comment.User.Pronouns,
                                 comment.User.Tagline, comment.User.ProfileImageFileName, comment.User.CreatedAt);

        return new CommentDto(comment.Id, comment.Content, comment.CreatedAt, userDto);
    }
}
