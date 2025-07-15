using Yapplr.Api.Models;
using Yapplr.Shared.Models;

namespace Yapplr.Api.DTOs;

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
    VideoMetadata? VideoMetadata = null,
    IEnumerable<PostMediaDto>? MediaItems = null
);