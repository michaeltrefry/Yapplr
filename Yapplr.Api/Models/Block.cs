namespace Yapplr.Api.Models;

public class Block
{
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int BlockerId { get; set; }  // User who is doing the blocking
    public int BlockedId { get; set; }  // User who is being blocked
    
    // Navigation properties
    public User Blocker { get; set; } = null!;
    public User Blocked { get; set; } = null!;
}
