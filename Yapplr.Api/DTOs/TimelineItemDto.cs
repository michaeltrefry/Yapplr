namespace Yapplr.Api.DTOs;

public record TimelineItemDto(
    string Type, // "post" or "repost"
    DateTime CreatedAt,
    PostDto Post,
    UserDto? RepostedBy = null // Only set for reposts
);