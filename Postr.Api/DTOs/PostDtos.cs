using System.ComponentModel.DataAnnotations;

namespace Postr.Api.DTOs;

public record CreatePostDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content,
    string? ImageFileName = null
);

public record PostDto(
    int Id,
    string Content,
    string? ImageUrl,
    DateTime CreatedAt,
    UserDto User,
    int LikeCount,
    int CommentCount,
    int RepostCount,
    bool IsLikedByCurrentUser = false,
    bool IsRepostedByCurrentUser = false
);

public record CreateCommentDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content
);

public record CommentDto(
    int Id,
    string Content,
    DateTime CreatedAt,
    UserDto User
);
