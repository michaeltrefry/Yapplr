using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models.Analytics;

public enum EngagementType
{
    Like = 0,
    Unlike = 1,
    Comment = 2,
    Repost = 3,
    Share = 4,
    View = 5,
    Click = 6,
    Save = 7,
    Report = 8,
    Hide = 9,
    Block = 10
}

public enum ContentType
{
    Post = 0,
    Comment = 1,
    User = 2,
    Tag = 3,
    Link = 4
}

/// <summary>
/// Tracks content engagement metrics for analytics
/// </summary>
public class ContentEngagement : IEntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// User who performed the engagement
    /// </summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Type of content being engaged with
    /// </summary>
    public ContentType ContentType { get; set; }
    
    /// <summary>
    /// ID of the content being engaged with
    /// </summary>
    public int ContentId { get; set; }
    
    /// <summary>
    /// Owner of the content (for attribution analytics)
    /// </summary>
    public int? ContentOwnerId { get; set; }
    public User? ContentOwner { get; set; }
    
    /// <summary>
    /// Type of engagement
    /// </summary>
    public EngagementType EngagementType { get; set; }
    
    /// <summary>
    /// Additional context or metadata as JSON
    /// </summary>
    [StringLength(1000)]
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Source of the engagement (e.g., "feed", "profile", "search", "notification")
    /// </summary>
    [StringLength(50)]
    public string? Source { get; set; }
    
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
    /// Duration of engagement in milliseconds (for views, video watches, etc.)
    /// </summary>
    public int? DurationMs { get; set; }
    
    /// <summary>
    /// Position in feed or list when engagement occurred
    /// </summary>
    public int? Position { get; set; }
    
    /// <summary>
    /// Session identifier for grouping engagements
    /// </summary>
    [StringLength(100)]
    public string? SessionId { get; set; }
}
