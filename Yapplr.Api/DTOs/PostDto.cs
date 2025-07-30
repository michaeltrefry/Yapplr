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
    GroupDto? Group, // Optional - only set for group posts
    int LikeCount, // Legacy - will be replaced by ReactionCounts
    int CommentCount,
    int RepostCount,

    IEnumerable<TagDto> Tags,
    IEnumerable<LinkPreviewDto> LinkPreviews,
    bool IsLikedByCurrentUser = false, // Legacy - will be replaced by CurrentUserReaction
    bool IsRepostedByCurrentUser = false,
    bool IsEdited = false,
    PostModerationInfoDto? ModerationInfo = null,
    VideoMetadata? VideoMetadata = null,
    IEnumerable<PostMediaDto>? MediaItems = null,
    IEnumerable<ReactionCountDto>? ReactionCounts = null,
    ReactionType? CurrentUserReaction = null,
    int TotalReactionCount = 0,
    PostType PostType = PostType.Post,
    PostDto? RepostedPost = null // Only set for reposts
);