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
    // Video processing visibility - when true, post is hidden from public feeds until video processing completes
    public bool IsHiddenDuringVideoProcessing { get; set; } = false;

    public PostPrivacy Privacy { get; set; } = PostPrivacy.Public;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // CONSOLIDATED PERMANENT HIDING SYSTEM
    /// <summary>
    /// Whether this post is permanently hidden (requires manual action to change)
    /// </summary>
    public bool IsHidden { get; set; } = false;

    /// <summary>
    /// Reason type why the post is permanently hidden (enum)
    /// </summary>
    public PostHiddenReasonType HiddenReasonType { get; set; } = PostHiddenReasonType.None;

    /// <summary>
    /// When the post was hidden
    /// </summary>
    public DateTime? HiddenAt { get; set; }

    /// <summary>
    /// User who hid the post (for moderator actions)
    /// </summary>
    public int? HiddenByUserId { get; set; }
    public User? HiddenByUser { get; set; }

    /// <summary>
    /// Additional details about why the post was hidden
    /// </summary>
    [StringLength(500)]
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
    public ICollection<PostMedia> PostMedia { get; set; } = new List<PostMedia>();

    // Computed properties
    public int LikeCount => Likes.Count;
    public int CommentCount => Comments.Count;
    public int RepostCount => Reposts.Count;

    // Media convenience properties for backward compatibility
    public PostMedia? ImageMedia => PostMedia.FirstOrDefault(m => m.MediaType == MediaType.Image);
    public PostMedia? VideoMedia => PostMedia.FirstOrDefault(m => m.MediaType == MediaType.Video);

    // Legacy properties for backward compatibility
    public string? ImageFileName => ImageMedia?.ImageFileName;
    public string? VideoFileName => VideoMedia?.VideoFileName;
    public string? ProcessedVideoFileName => VideoMedia?.ProcessedVideoFileName;
    public string? VideoThumbnailFileName => VideoMedia?.VideoThumbnailFileName;
    public VideoProcessingStatus VideoProcessingStatus => VideoMedia?.VideoProcessingStatus ?? VideoProcessingStatus.Pending;
    public DateTime? VideoProcessingStartedAt => VideoMedia?.VideoProcessingStartedAt;
    public DateTime? VideoProcessingCompletedAt => VideoMedia?.VideoProcessingCompletedAt;
    public string? VideoProcessingError => VideoMedia?.VideoProcessingError;
}
