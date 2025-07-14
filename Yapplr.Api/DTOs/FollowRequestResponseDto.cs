namespace Yapplr.Api.DTOs;

public class FollowRequestResponseDto
{
    public bool IsFollowing { get; set; }
    public bool HasPendingRequest { get; set; }
    public int FollowerCount { get; set; }
}
