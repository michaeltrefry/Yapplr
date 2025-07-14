using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models.Analytics;

/// <summary>
/// Tracks detailed user activities for analytics and behavior analysis
/// </summary>
public class UserActivity : IEntity
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public ActivityType ActivityType { get; set; }
    
    /// <summary>
    /// Optional target entity type (e.g., "post", "user", "comment")
    /// </summary>
    [StringLength(50)]
    public string? TargetEntityType { get; set; }
    
    /// <summary>
    /// Optional target entity ID
    /// </summary>
    public int? TargetEntityId { get; set; }
    
    /// <summary>
    /// Additional metadata as JSON (e.g., device info, location, etc.)
    /// </summary>
    [StringLength(1000)]
    public string? Metadata { get; set; }
    
    /// <summary>
    /// IP address for security and analytics
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent for device/browser analytics
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// Session identifier for grouping activities
    /// </summary>
    [StringLength(100)]
    public string? SessionId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Duration in milliseconds for activities that have duration (e.g., video watched)
    /// </summary>
    public int? DurationMs { get; set; }
    
    /// <summary>
    /// Success indicator for activities that can fail
    /// </summary>
    public bool? Success { get; set; }
    
    /// <summary>
    /// Error message if the activity failed
    /// </summary>
    [StringLength(500)]
    public string? ErrorMessage { get; set; }
}
