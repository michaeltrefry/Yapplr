using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

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
}
