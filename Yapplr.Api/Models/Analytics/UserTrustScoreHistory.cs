using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models.Analytics;

/// <summary>
/// Tracks the history of user trust score changes for audit and analytics
/// </summary>
public class UserTrustScoreHistory : IEntity
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Trust score before the change
    /// </summary>
    public float PreviousScore { get; set; }
    
    /// <summary>
    /// Trust score after the change
    /// </summary>
    public float NewScore { get; set; }
    
    /// <summary>
    /// The amount of change (can be positive or negative)
    /// </summary>
    public float ScoreChange { get; set; }
    
    /// <summary>
    /// Reason for the trust score change
    /// </summary>
    public TrustScoreChangeReason Reason { get; set; }
    
    /// <summary>
    /// Additional details about the change
    /// </summary>
    [StringLength(500)]
    public string? Details { get; set; }
    
    /// <summary>
    /// Related entity type (e.g., "post", "comment", "report")
    /// </summary>
    [StringLength(50)]
    public string? RelatedEntityType { get; set; }
    
    /// <summary>
    /// Related entity ID
    /// </summary>
    public int? RelatedEntityId { get; set; }
    
    /// <summary>
    /// User who triggered the change (for admin adjustments)
    /// </summary>
    public int? TriggeredByUserId { get; set; }
    public User? TriggeredByUser { get; set; }
    
    /// <summary>
    /// System or algorithm that calculated the change
    /// </summary>
    [StringLength(100)]
    public string? CalculatedBy { get; set; }
    
    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    [StringLength(1000)]
    public string? Metadata { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this change was automatically calculated or manually applied
    /// </summary>
    public bool IsAutomatic { get; set; } = true;
    
    /// <summary>
    /// Confidence level of the score change (0.0 to 1.0)
    /// </summary>
    public float? Confidence { get; set; }
}
