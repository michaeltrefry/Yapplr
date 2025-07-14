using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class ContentPage
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Slug { get; set; } = string.Empty;
    
    public ContentPageType Type { get; set; }
    
    public int? PublishedVersionId { get; set; }
    public ContentPageVersion? PublishedVersion { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public List<ContentPageVersion> Versions { get; set; } = new();
}
