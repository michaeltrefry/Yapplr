using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;
using Yapplr.Shared.Models;

namespace Yapplr.Api.Models;

public class Post : IUserOwnedEntity
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(256)]
    public string Content { get; set; } = string.Empty;
    [StringLength(256)]
    public string? ImageFileName { get; set; }

    // Video support
    [StringLength(256)]
    public string? VideoFileName { get; set; }
    [StringLength(256)]
    public string? ProcessedVideoFileName { get; set; }
    [StringLength(256)]
    public string? VideoThumbnailFileName { get; set; }
    public VideoProcessingStatus VideoProcessingStatus { get; set; } = VideoProcessingStatus.Pending;
    public DateTime? VideoProcessingStartedAt { get; set; }
    public DateTime? VideoProcessingCompletedAt { get; set; }
    [StringLength(500)]
    public string? VideoProcessingError { get; set; }

    public PostPrivacy Privacy { get; set; } = PostPrivacy.Public;

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
    
    // Foreign key
    public int UserId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Repost> Reposts { get; set; } = new List<Repost>();
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public ICollection<PostLinkPreview> PostLinkPreviews { get; set; } = new List<PostLinkPreview>();
    public ICollection<PostSystemTag> PostSystemTags { get; set; } = new List<PostSystemTag>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // Computed properties
    public int LikeCount => Likes.Count;
    public int CommentCount => Comments.Count;
    public int RepostCount => Reposts.Count;
}
