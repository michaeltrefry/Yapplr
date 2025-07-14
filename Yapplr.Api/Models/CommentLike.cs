namespace Yapplr.Api.Models;

public class CommentLike
{
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int UserId { get; set; }
    public int CommentId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Comment Comment { get; set; } = null!;
}
