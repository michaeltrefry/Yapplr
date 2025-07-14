using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;
using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Models;

public class User : IEntity
{
    public int Id { get; set; }
    
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Bio { get; set; } = string.Empty;
    
    public DateTime? Birthday { get; set; }
    
    [StringLength(100)]
    public string Pronouns { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string Tagline { get; set; } = string.Empty;

    [StringLength(255)]
    public string ProfileImageFileName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? FcmToken { get; set; }

    [StringLength(500)]
    public string? ExpoPushToken { get; set; }

    public bool EmailVerified { get; set; } = false;

    public DateTime? TermsAcceptedAt { get; set; }

    // Admin/Moderation fields
    public UserRole Role { get; set; } = UserRole.User;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime? SuspendedUntil { get; set; }
    [StringLength(500)]
    public string? SuspensionReason { get; set; }
    public int? SuspendedByUserId { get; set; }
    public User? SuspendedByUser { get; set; }
    public DateTime? LastLoginAt { get; set; }
    [StringLength(64)]
    public string? LastLoginIp { get; set; }
    public float? TrustScore { get; set; } = 1.0f;

    // Rate limiting settings
    public bool? RateLimitingEnabled { get; set; } // null = use system default, true/false = override
    public bool? TrustBasedRateLimitingEnabled { get; set; } // null = use system default, true/false = override

    // Analytics relationships
    public ICollection<UserActivity> UserActivities { get; set; } = new List<UserActivity>();
    public ICollection<UserTrustScoreHistory> TrustScoreHistory { get; set; } = new List<UserTrustScoreHistory>();

    // Navigation properties
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Repost> Reposts { get; set; } = new List<Repost>();

    // Follow relationships
    public ICollection<Follow> Followers { get; set; } = new List<Follow>(); // Users following this user
    public ICollection<Follow> Following { get; set; } = new List<Follow>(); // Users this user is following

    // Follow request relationships
    public ICollection<FollowRequest> FollowRequestsSent { get; set; } = new List<FollowRequest>(); // Follow requests sent by this user
    public ICollection<FollowRequest> FollowRequestsReceived { get; set; } = new List<FollowRequest>(); // Follow requests received by this user

    // Block relationships
    public ICollection<Block> BlockedUsers { get; set; } = new List<Block>(); // Users this user has blocked
    public ICollection<Block> BlockedByUsers { get; set; } = new List<Block>(); // Users who have blocked this user

    // Messaging relationships
    public ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<MessageStatus> MessageStatuses { get; set; } = new List<MessageStatus>();

    // Notification relationships
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>(); // Notifications received by this user
    public ICollection<Mention> MentionsReceived { get; set; } = new List<Mention>(); // Mentions of this user
    public ICollection<Mention> MentionsMade { get; set; } = new List<Mention>(); // Mentions made by this user

    // Admin relationships
    public ICollection<User> SuspendedUsers { get; set; } = new List<User>(); // Users suspended by this admin/moderator
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>(); // Audit logs for actions on this user
    public ICollection<AuditLog> PerformedAuditLogs { get; set; } = new List<AuditLog>(); // Audit logs for actions performed by this user
}
