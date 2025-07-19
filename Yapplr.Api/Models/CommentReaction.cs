namespace Yapplr.Api.Models;

public class CommentReaction
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
    public int CommentId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Comment Comment { get; set; } = null!;
}
