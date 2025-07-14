namespace Yapplr.Api.DTOs;

public class FollowRequestDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto Requester { get; set; } = null!;
    public UserDto Requested { get; set; } = null!;
}
