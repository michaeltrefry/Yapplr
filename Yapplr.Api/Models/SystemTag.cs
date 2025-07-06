using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public enum SystemTagCategory
{
    ContentWarning = 0,     // Visible to users - content warnings like NSFW, violence, etc.
    Violation = 1,          // Hidden from users - policy violations
    ModerationStatus = 2,   // Hidden from users - under review, approved, etc.
    Quality = 3,            // Hidden from users - spam, low quality, etc.
    Legal = 4,              // Hidden from users - copyright, legal issues
    Safety = 5              // Hidden from users - safety concerns, harassment
}

public class SystemTag
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public SystemTagCategory Category { get; set; }
    
    public bool IsVisibleToUsers { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
    
    [StringLength(20)]
    public string Color { get; set; } = "#6B7280"; // Default gray color
    
    [StringLength(50)]
    public string? Icon { get; set; }
    
    public int SortOrder { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<PostSystemTag> PostSystemTags { get; set; } = new List<PostSystemTag>();
    public ICollection<CommentSystemTag> CommentSystemTags { get; set; } = new List<CommentSystemTag>();
}

public class PostSystemTag
{
    public int Id { get; set; }
    
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    
    public int SystemTagId { get; set; }
    public SystemTag SystemTag { get; set; } = null!;
    
    public int AppliedByUserId { get; set; }
    public User AppliedByUser { get; set; } = null!;
    
    [StringLength(500)]
    public string? Reason { get; set; }
    
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

public class CommentSystemTag
{
    public int Id { get; set; }
    
    public int CommentId { get; set; }
    public Comment Comment { get; set; } = null!;
    
    public int SystemTagId { get; set; }
    public SystemTag SystemTag { get; set; } = null!;
    
    public int AppliedByUserId { get; set; }
    public User AppliedByUser { get; set; } = null!;
    
    [StringLength(500)]
    public string? Reason { get; set; }
    
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}
