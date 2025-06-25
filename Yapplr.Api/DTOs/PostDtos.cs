using System.ComponentModel.DataAnnotations;
using Postr.Api.Models;

namespace Postr.Api.DTOs;

public record CreatePostDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content,
    string? ImageFileName = null,
    PostPrivacy Privacy = PostPrivacy.Public
);

public record PostDto(
    int Id,
    string Content,
    string? ImageUrl,
    PostPrivacy Privacy,
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

public record TimelineItemDto(
    string Type, // "post" or "repost"
    DateTime CreatedAt,
    PostDto Post,
    UserDto? RepostedBy = null // Only set for reposts
);
