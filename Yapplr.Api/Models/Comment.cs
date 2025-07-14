using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models;

public class Comment : IUserOwnedEntity
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(256)]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Moderation fields
    public bool IsHidden { get; set; } = false;
    public int? HiddenByUserId { get; set; }
    public User? HiddenByUser { get; set; }
    public DateTime? HiddenAt { get; set; }
    [StringLength(256)]
    public string? HiddenReason { get; set; }

    public bool IsFlagged { get; set; } = false;
    [StringLength(256)]
    public string? FlaggedReason { get; set; }
    public DateTime? FlaggedAt { get; set; }

    // User deletion fields (soft delete)
    public bool IsDeletedByUser { get; set; } = false;
    public DateTime? DeletedByUserAt { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public int PostId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Post Post { get; set; } = null!;
    public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
    public ICollection<CommentSystemTag> CommentSystemTags { get; set; } = new List<CommentSystemTag>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
