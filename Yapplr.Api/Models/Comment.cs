using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class Comment
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(256)]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int UserId { get; set; }
    public int PostId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
