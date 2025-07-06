using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public enum AuditAction
{
    // User actions
    UserSuspended = 100,
    UserBanned = 101,
    UserShadowBanned = 102,
    UserUnsuspended = 103,
    UserUnbanned = 104,
    UserRoleChanged = 105,
    UserForcePasswordReset = 106,
    UserEmailVerificationToggled = 107,
    
    // Content actions
    PostHidden = 200,
    PostDeleted = 201,
    PostRestored = 202,
    PostSystemTagAdded = 203,
    PostSystemTagRemoved = 204,
    
    CommentHidden = 210,
    CommentDeleted = 211,
    CommentRestored = 212,
    CommentSystemTagAdded = 213,
    CommentSystemTagRemoved = 214,
    
    // System actions
    SystemTagCreated = 300,
    SystemTagUpdated = 301,
    SystemTagDeleted = 302,
    
    // Security actions
    IpBlocked = 400,
    IpUnblocked = 401,
    SecurityIncidentReported = 402,
    
    // Bulk actions
    BulkContentDeleted = 500,
    BulkContentHidden = 501,
    BulkUsersActioned = 502
}

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
    
    public int? TargetCommentId { get; set; }
    public Comment? TargetComment { get; set; }
    
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
