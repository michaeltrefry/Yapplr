namespace Yapplr.Api.Models;

public enum FollowRequestStatus
{
    Pending = 0,
    Approved = 1,
    Denied = 2
}

public class FollowRequest
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public FollowRequestStatus Status { get; set; } = FollowRequestStatus.Pending;
    public DateTime? ProcessedAt { get; set; }

    // Foreign keys
    public int RequesterId { get; set; }  // User who is requesting to follow
    public int RequestedId { get; set; }  // User being requested to follow

    // Navigation properties
    public User Requester { get; set; } = null!;
    public User Requested { get; set; } = null!;
}
