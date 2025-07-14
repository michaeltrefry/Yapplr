namespace Yapplr.Api.DTOs;

public record FollowResponseDto(
    bool IsFollowing,
    int FollowerCount,
    bool HasPendingRequest = false
);