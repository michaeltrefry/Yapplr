namespace Yapplr.Api.Models;

public class PostTag
{
    public int Id { get; set; }
    
    // Foreign keys
    public int PostId { get; set; }
    public int TagId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Post Post { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
