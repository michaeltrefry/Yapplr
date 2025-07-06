using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public enum AppealType
{
    Suspension = 0,
    Ban = 1,
    ContentRemoval = 2,
    SystemTag = 3,
    Other = 4
}

public enum AppealStatus
{
    Pending = 0,
    UnderReview = 1,
    Approved = 2,
    Denied = 3,
    Escalated = 4
}

public class UserAppeal
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public AppealType Type { get; set; }
    
    public AppealStatus Status { get; set; } = AppealStatus.Pending;
    
    [Required]
    [StringLength(2000)]
    public string Reason { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? AdditionalInfo { get; set; }
    
    // Reference to the original action being appealed
    public int? AuditLogId { get; set; }
    public AuditLog? AuditLog { get; set; }
    
    public int? TargetPostId { get; set; }
    public Post? TargetPost { get; set; }
    
    public int? TargetCommentId { get; set; }
    public Comment? TargetComment { get; set; }
    
    // Review information
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    
    [StringLength(2000)]
    public string? ReviewNotes { get; set; }
    
    public DateTime? ReviewedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
