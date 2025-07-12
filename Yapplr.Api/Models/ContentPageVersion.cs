using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class ContentPageVersion
{
    public int Id { get; set; }
    
    public int ContentPageId { get; set; }
    public ContentPage ContentPage { get; set; } = null!;
    
    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? ChangeNotes { get; set; }
    
    public int VersionNumber { get; set; }
    
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }
    public int? PublishedByUserId { get; set; }
    public User? PublishedByUser { get; set; }
    
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
