namespace Postr.Api.Models;

public class Follow
{
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int FollowerId { get; set; }  // User who is following
    public int FollowingId { get; set; } // User being followed
    
    // Navigation properties
    public User Follower { get; set; } = null!;
    public User Following { get; set; } = null!;
}
