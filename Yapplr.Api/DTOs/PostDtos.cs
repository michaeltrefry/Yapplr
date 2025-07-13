using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;
using Yapplr.Shared.Models;

namespace Yapplr.Api.DTOs;

public record CreatePostDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content,
    string? ImageFileName = null,
    string? VideoFileName = null,
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
    string? VideoUrl,
    string? VideoThumbnailUrl,
    VideoProcessingStatus? VideoProcessingStatus,
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
    bool IsEdited = false,
    PostModerationInfoDto? ModerationInfo = null,
    VideoMetadata? VideoMetadata = null
);

public record PostModerationInfoDto(
    bool IsHidden,
    string? HiddenReason,
    DateTime? HiddenAt,
    UserDto? HiddenByUser,
    IEnumerable<PostSystemTagDto> SystemTags,
    double? RiskScore,
    string? RiskLevel,
    PostAppealInfoDto? AppealInfo
);

public record PostSystemTagDto(
    int Id,
    string Name,
    string Description,
    string Category,
    bool IsVisibleToUsers,
    string Color,
    string? Icon,
    string? Reason,
    DateTime AppliedAt,
    UserDto AppliedByUser
);

public record PostAppealInfoDto(
    int Id,
    AppealStatus Status,
    string Reason,
    string? AdditionalInfo,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? ReviewedByUsername,
    string? ReviewNotes
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
