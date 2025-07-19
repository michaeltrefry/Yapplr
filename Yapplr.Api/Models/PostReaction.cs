namespace Yapplr.Api.Models;

public class PostReaction
{
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The type of reaction (heart, thumbs up, laugh, etc.)
    /// </summary>
    public ReactionType ReactionType { get; set; }
    
    // Foreign keys
    public int UserId { get; set; }
    public int PostId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
