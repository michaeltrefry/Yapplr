using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record CommentDto(
    int Id,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    UserDto User,
    bool IsEdited = false,
    int LikeCount = 0, // Legacy - will be replaced by ReactionCounts
    bool IsLikedByCurrentUser = false, // Legacy - will be replaced by CurrentUserReaction
    IEnumerable<ReactionCountDto>? ReactionCounts = null,
    ReactionType? CurrentUserReaction = null,
    int TotalReactionCount = 0
);