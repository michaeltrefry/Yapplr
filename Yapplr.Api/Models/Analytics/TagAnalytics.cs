using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models.Analytics;

public enum TagAction
{
    Created = 0,
    Used = 1,
    Clicked = 2,
    Searched = 3,
    Trending = 4,
    Removed = 5,
    Reported = 6,
    Blocked = 7
}

/// <summary>
/// Tracks tag usage and analytics for trending and recommendation algorithms
/// </summary>
public class TagAnalytics : IEntity
{
    public int Id { get; set; }
    
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
    
    /// <summary>
    /// User who performed the action (nullable for system actions)
    /// </summary>
    public int? UserId { get; set; }
    public User? User { get; set; }
    
    /// <summary>
    /// Type of action performed with the tag
    /// </summary>
    public TagAction Action { get; set; }
    
    /// <summary>
    /// Related content type (e.g., "post", "search")
    /// </summary>
    [StringLength(50)]
    public string? RelatedContentType { get; set; }
    
    /// <summary>
    /// Related content ID
    /// </summary>
    public int? RelatedContentId { get; set; }
    
    /// <summary>
    /// Source of the action (e.g., "compose", "search", "trending")
    /// </summary>
    [StringLength(50)]
    public string? Source { get; set; }
    
    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    [StringLength(1000)]
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Session identifier for grouping actions
    /// </summary>
    [StringLength(100)]
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Device type (e.g., "mobile", "desktop", "tablet")
    /// </summary>
    [StringLength(20)]
    public string? DeviceType { get; set; }
    
    /// <summary>
    /// Platform (e.g., "web", "ios", "android")
    /// </summary>
    [StringLength(20)]
    public string? Platform { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Position of tag in list/suggestions when action occurred
    /// </summary>
    public int? Position { get; set; }
    
    /// <summary>
    /// Whether this was a suggested tag or user-entered
    /// </summary>
    public bool? WasSuggested { get; set; }
}
