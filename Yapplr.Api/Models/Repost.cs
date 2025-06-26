namespace Yapplr.Api.Models;

public class Repost
{
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int UserId { get; set; }
    public int PostId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
