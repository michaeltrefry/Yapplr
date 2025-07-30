using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Yapplr.Api.Common;
using Yapplr.Shared.Models;

namespace Yapplr.Api.Models;

public class Post : IUserOwnedEntity
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(1024)]
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
    
    // Foreign keys
    public int UserId { get; set; }
    public int? GroupId { get; set; } // Optional - null for regular posts, set for group posts
    public int? ParentId { get; set; } // Optional - null for top-level posts, set for comments/replies
    public int? RepostedPostId { get; set; } // Optional - null for regular posts, set for reposts

    // Post type to distinguish between posts, comments, and reposts
    public PostType PostType { get; set; } = PostType.Post;

    // Navigation properties
    public User User { get; set; } = null!;
    public Group? Group { get; set; } // Optional - only set for group posts
    public Post? Parent { get; set; } // Optional - only set for comments/replies
    public Post? RepostedPost { get; set; } // Optional - only set for reposts
    public ICollection<Post> Children { get; set; } = new List<Post>(); // Comments/replies to this post
    public ICollection<Post> Reposts { get; set; } = new List<Post>(); // Reposts of this post


    public ICollection<Like> Likes { get; set; } = new List<Like>(); // Legacy - will be removed
    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    // Comments are now handled through Children collection (Posts with PostType.Comment)

    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public ICollection<PostLinkPreview> PostLinkPreviews { get; set; } = new List<PostLinkPreview>();
    public ICollection<PostSystemTag> PostSystemTags { get; set; } = new List<PostSystemTag>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<PostMedia> PostMedia { get; set; } = new List<PostMedia>();

    // Computed properties
    public int LikeCount => Likes.Count; // Legacy - will be replaced with reaction counts
    public int ReactionCount => Reactions.Count;
    public int CommentCount => PostType == PostType.Post ? Children.Count(c => c.PostType == PostType.Comment) : 0;
    public int RepostCount => Reposts.Count; // Count from new unified system



    // Unified model helper properties
    [NotMapped]
    public bool IsComment => PostType == PostType.Comment;

    [NotMapped]
    public bool IsRepost => PostType == PostType.Repost;



    [NotMapped]
    public bool IsTopLevelPost => PostType == PostType.Post && ParentId == null;

    [NotMapped]
    public IEnumerable<Post> ChildComments => Children.Where(c => c.PostType == PostType.Comment);

    [NotMapped]
    public IEnumerable<Post> ChildReposts => Children.Where(c => c.PostType == PostType.Repost);

    // Reaction count helpers
    public int GetReactionCount(ReactionType reactionType) => Reactions.Count(r => r.ReactionType == reactionType);
    public Dictionary<ReactionType, int> GetReactionCounts() =>
        Reactions.GroupBy(r => r.ReactionType).ToDictionary(g => g.Key, g => g.Count());

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
