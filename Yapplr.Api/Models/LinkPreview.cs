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
    
    public LinkPreviewStatus Status { get; set; } = LinkPreviewStatus.Pending;
    
    [StringLength(500)]
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<PostLinkPreview> PostLinkPreviews { get; set; } = new List<PostLinkPreview>();
}

public enum LinkPreviewStatus
{
    Pending = 0,
    Success = 1,
    NotFound = 2,        // 404
    Unauthorized = 3,    // 401
    Forbidden = 4,       // 403
    Timeout = 5,
    NetworkError = 6,
    InvalidUrl = 7,
    TooLarge = 8,        // Response too large
    UnsupportedContent = 9, // Not HTML content
    Error = 10           // General error
}
