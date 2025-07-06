using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class Tag
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty; // Hashtag name without the # symbol
    
    [StringLength(100)]
    public string NormalizedName { get; set; } = string.Empty; // Lowercase version for case-insensitive searches
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Metrics
    public int UsageCount { get; set; } = 0; // Total number of times this tag has been used
    
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow; // When this tag was last used
    
    // Navigation properties
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}
