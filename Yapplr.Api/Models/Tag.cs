using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class Tag
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty; // The hashtag without the # symbol
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int PostCount { get; set; } = 0; // Denormalized count for performance
    
    // Navigation properties
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}
