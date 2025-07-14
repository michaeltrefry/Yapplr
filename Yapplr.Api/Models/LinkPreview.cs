using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class LinkPreview
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Title { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [StringLength(2048)]
    public string? ImageUrl { get; set; }
    
    [StringLength(500)]
    public string? SiteName { get; set; }

    [StringLength(50)]
    public string? YouTubeVideoId { get; set; }

    public LinkPreviewStatus Status { get; set; } = LinkPreviewStatus.Pending;
    
    [StringLength(500)]
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<PostLinkPreview> PostLinkPreviews { get; set; } = new List<PostLinkPreview>();
}