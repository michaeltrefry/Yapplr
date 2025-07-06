using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class Post
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(256)]
    public string Content { get; set; } = string.Empty;
    
    public string? ImageFileName { get; set; }

    // Video support
    public string? VideoFileName { get; set; }
    public string? VideoThumbnailFileName { get; set; }
    public int? VideoDurationSeconds { get; set; }
    public long? VideoSizeBytes { get; set; }
    public string? VideoFormat { get; set; }
    public VideoProcessingStatus VideoProcessingStatus { get; set; } = VideoProcessingStatus.None;

    public PostPrivacy Privacy { get; set; } = PostPrivacy.Public;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Moderation fields
    public bool IsHidden { get; set; } = false;
    public int? HiddenByUserId { get; set; }
    public User? HiddenByUser { get; set; }
    public DateTime? HiddenAt { get; set; }
    public string? HiddenReason { get; set; }
    
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
    public bool HasVideo => !string.IsNullOrEmpty(VideoFileName);
    public bool IsVideoReady => HasVideo && VideoProcessingStatus == VideoProcessingStatus.Completed;
}

public enum VideoProcessingStatus
{
    None = 0,
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}
