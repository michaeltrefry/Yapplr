using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record CreatePostDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content,
    string? ImageFileName = null,
    PostPrivacy Privacy = PostPrivacy.Public
);

public record UpdatePostDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content,
    PostPrivacy Privacy = PostPrivacy.Public
);

public record PostDto(
    int Id,
    string Content,
    string? ImageUrl,
    PostPrivacy Privacy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    UserDto User,
    int LikeCount,
    int CommentCount,
    int RepostCount,
    IEnumerable<TagDto> Tags,
    IEnumerable<LinkPreviewDto> LinkPreviews,
    bool IsLikedByCurrentUser = false,
    bool IsRepostedByCurrentUser = false,
    bool IsEdited = false
);

public record CreateCommentDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content
);

public record UpdateCommentDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content
);

public record CommentDto(
    int Id,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    UserDto User,
    bool IsEdited = false
);

public record TagDto(
    int Id,
    string Name,
    int PostCount
);

public record TimelineItemDto(
    string Type, // "post" or "repost"
    DateTime CreatedAt,
    PostDto Post,
    UserDto? RepostedBy = null // Only set for reposts
);
