using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class AuditLog
{
    public int Id { get; set; }
    
    public AuditAction Action { get; set; }
    
    public int PerformedByUserId { get; set; }
    public User PerformedByUser { get; set; } = null!;
    
    public int? TargetUserId { get; set; }
    public User? TargetUser { get; set; }
    
    public int? TargetPostId { get; set; }
    public Post? TargetPost { get; set; }
    
    public int? TargetCommentId { get; set; } // Now references Posts table with PostType.Comment
    public Post? TargetComment { get; set; } // Now references Posts table with PostType.Comment
    
    [StringLength(1000)]
    public string? Reason { get; set; }
    
    [StringLength(2000)]
    public string? Details { get; set; } // JSON serialized additional details
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
