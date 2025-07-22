using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

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
    
    public int? TargetCommentId { get; set; } // Now references Posts table with PostType.Comment
    public Post? TargetComment { get; set; } // Now references Posts table with PostType.Comment
    
    // Review information
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    
    [StringLength(2000)]
    public string? ReviewNotes { get; set; }
    
    public DateTime? ReviewedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
