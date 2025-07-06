namespace Yapplr.Api.Models;

public class PostLinkPreview
{
    public int Id { get; set; }
    
    // Foreign keys
    public int PostId { get; set; }
    public int LinkPreviewId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Post Post { get; set; } = null!;
    public LinkPreview LinkPreview { get; set; } = null!;
}
