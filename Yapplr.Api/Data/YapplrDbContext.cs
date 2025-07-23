using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Data;

public class YapplrDbContext : DbContext
{
    public YapplrDbContext(DbContextOptions<YapplrDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostMedia> PostMedia { get; set; }
    public DbSet<Like> Likes { get; set; } // Legacy - will be removed
    public DbSet<PostReaction> PostReactions { get; set; }
    // Comment-related DbSets removed - now using unified Post model
    public DbSet<Repost> Reposts { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<FollowRequest> FollowRequests { get; set; }
    public DbSet<Block> Blocks { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<EmailVerification> EmailVerifications { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageStatus> MessageStatuses { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<QueuedNotification> QueuedNotifications { get; set; }
    public DbSet<Mention> Mentions { get; set; }
    public DbSet<NotificationPreferences> NotificationPreferences { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<PostTag> PostTags { get; set; }
    public DbSet<LinkPreview> LinkPreviews { get; set; }
    public DbSet<PostLinkPreview> PostLinkPreviews { get; set; }
    public DbSet<NotificationDeliveryConfirmation> NotificationDeliveryConfirmations { get; set; }
    public DbSet<NotificationHistory> NotificationHistory { get; set; }
    public DbSet<NotificationAuditLog> NotificationAuditLogs { get; set; }

    // Admin/Moderation entities
    public DbSet<SystemTag> SystemTags { get; set; }
    public DbSet<PostSystemTag> PostSystemTags { get; set; }
    // CommentSystemTag removed - now using PostSystemTag for unified model
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<UserAppeal> UserAppeals { get; set; }
    public DbSet<AiSuggestedTag> AiSuggestedTags { get; set; }
    public DbSet<UserReport> UserReports { get; set; }
    public DbSet<UserReportSystemTag> UserReportSystemTags { get; set; }

    // Content Management entities
    public DbSet<ContentPage> ContentPages { get; set; }
    public DbSet<ContentPageVersion> ContentPageVersions { get; set; }

    // System Configuration entities
    public DbSet<UploadSettings> UploadSettings { get; set; }
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

    // Analytics entities
    public DbSet<UserActivity> UserActivities { get; set; }
    public DbSet<ContentEngagement> ContentEngagements { get; set; }
    public DbSet<UserTrustScoreHistory> UserTrustScoreHistories { get; set; }
    public DbSet<TagAnalytics> TagAnalytics { get; set; }
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }

    // Subscription entities
    public DbSet<SubscriptionTier> SubscriptionTiers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.Username).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();

            // Performance indexes for user queries
            entity.HasIndex(e => e.LastSeenAt); // For online status queries

            // Subscription relationship
            entity.HasOne(e => e.SubscriptionTier)
                  .WithMany(e => e.Users)
                  .HasForeignKey(e => e.SubscriptionTierId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        
        // Post configuration
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(1024);
            entity.Property(e => e.Privacy)
                  .HasConversion<int>()
                  .HasDefaultValue(PostPrivacy.Public);

            // Configure PostType enum
            entity.Property(e => e.PostType)
                  .HasConversion<int>()
                  .HasDefaultValue(PostType.Post);

            // Configure consolidated hiding system
            entity.Property(e => e.HiddenReasonType)
                  .HasConversion<int>()
                  .HasDefaultValue(PostHiddenReasonType.None);
            entity.Property(e => e.HiddenReason).HasMaxLength(500);

            entity.HasOne(e => e.User)
                  .WithMany(e => e.Posts)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Group)
                  .WithMany(e => e.Posts)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure self-referencing relationship for Parent/Children
            entity.HasOne(e => e.Parent)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Performance indexes for unified post model
            entity.HasIndex(e => new { e.PostType, e.CreatedAt }); // For filtering posts vs comments by date
            entity.HasIndex(e => new { e.ParentId, e.PostType, e.CreatedAt }); // For getting comments for a post
            entity.HasIndex(e => new { e.UserId, e.PostType, e.CreatedAt }); // For user's posts vs comments

            // Performance indexes for common query patterns
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }); // User profile posts ordered by date
            entity.HasIndex(e => new { e.Privacy, e.CreatedAt }); // Public timeline queries
            entity.HasIndex(e => new { e.GroupId, e.CreatedAt }); // Group posts ordered by date
            entity.HasIndex(e => e.CreatedAt); // General timeline ordering

            // Optimized index for hybrid hiding system
            entity.HasIndex(e => new { e.IsHidden, HiddenReasonType = e.HiddenReasonType, e.UserId, e.Privacy, e.CreatedAt })
                  .HasDatabaseName("IX_Posts_HybridVisibility")
                  .HasFilter("\"IsHidden\" = false OR \"HiddenReasonType\" = 3"); // Include video processing for author visibility
        });

        // PostMedia configuration
        modelBuilder.Entity<PostMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PostId); // For efficient post-media queries
            entity.HasIndex(e => new { e.PostId, e.MediaType }); // For specific media type queries
            entity.HasIndex(e => e.MediaType); // For media type filtering
            entity.HasIndex(e => e.VideoProcessingStatus); // For video processing queries
            entity.HasIndex(e => e.CreatedAt); // For chronological queries
            entity.HasOne(e => e.Post)
                  .WithMany(e => e.PostMedia)
                  .HasForeignKey(e => e.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Like configuration
        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.PostId }).IsUnique(); // Prevent duplicate likes
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Likes)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Post)
                  .WithMany(e => e.Likes)
                  .HasForeignKey(e => e.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        


        // PostReaction configuration
        modelBuilder.Entity<PostReaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.PostId }).IsUnique(); // Prevent duplicate reactions per user per post
            entity.HasIndex(e => new { e.PostId, e.ReactionType }); // For efficient reaction count queries
            entity.Property(e => e.ReactionType)
                  .HasConversion<int>(); // Store as integer in database
            entity.HasOne(e => e.User)
                  .WithMany(e => e.PostReactions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Post)
                  .WithMany(e => e.Reactions)
                  .HasForeignKey(e => e.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
        });



        // Repost configuration
        modelBuilder.Entity<Repost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.PostId }).IsUnique(); // Prevent duplicate reposts
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Reposts)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Post)
                  .WithMany(e => e.Reposts)
                  .HasForeignKey(e => e.PostId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Performance indexes for repost timeline queries
            entity.HasIndex(e => e.CreatedAt); // Timeline ordering for reposts
        });

        // Follow configuration
        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FollowerId, e.FollowingId }).IsUnique(); // Prevent duplicate follows
            entity.HasOne(e => e.Follower)
                  .WithMany(e => e.Following)
                  .HasForeignKey(e => e.FollowerId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Following)
                  .WithMany(e => e.Followers)
                  .HasForeignKey(e => e.FollowingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FollowRequest configuration
        modelBuilder.Entity<FollowRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Removed unique constraint to allow multiple requests over time
            entity.HasIndex(e => new { e.RequesterId, e.RequestedId, e.Status }); // Index for performance
            entity.HasOne(e => e.Requester)
                  .WithMany(e => e.FollowRequestsSent)
                  .HasForeignKey(e => e.RequesterId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Requested)
                  .WithMany(e => e.FollowRequestsReceived)
                  .HasForeignKey(e => e.RequestedId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Block configuration
        modelBuilder.Entity<Block>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BlockerId, e.BlockedId }).IsUnique(); // Prevent duplicate blocks
            entity.HasOne(e => e.Blocker)
                  .WithMany(e => e.BlockedUsers)
                  .HasForeignKey(e => e.BlockerId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Blocked)
                  .WithMany(e => e.BlockedByUsers)
                  .HasForeignKey(e => e.BlockedId)
                  .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete when blocked user is deleted
        });

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique(); // Group names must be unique
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageFileName).HasMaxLength(255);

            entity.HasOne(e => e.User)
                  .WithMany(e => e.CreatedGroups)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Performance indexes
            entity.HasIndex(e => e.CreatedAt); // For ordering groups by creation date
            entity.HasIndex(e => e.UserId); // For finding groups created by a user
        });

        // GroupMember configuration
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique(); // Prevent duplicate memberships
            entity.Property(e => e.Role)
                  .HasConversion<int>()
                  .HasDefaultValue(GroupMemberRole.Member);

            entity.HasOne(e => e.Group)
                  .WithMany(e => e.Members)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.GroupMemberships)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Performance indexes
            entity.HasIndex(e => e.GroupId); // For finding members of a group
            entity.HasIndex(e => e.UserId); // For finding groups a user is member of
            entity.HasIndex(e => e.JoinedAt); // For ordering members by join date
        });

        // PasswordReset configuration
        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // EmailVerification configuration
        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Conversation configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Performance indexes for conversation queries
            entity.HasIndex(e => e.UpdatedAt); // For ordering conversations by last activity
        });

        // ConversationParticipant configuration
        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ConversationId, e.UserId }).IsUnique(); // Prevent duplicate participants
            entity.HasOne(e => e.Conversation)
                  .WithMany(e => e.Participants)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.ConversationParticipants)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.HasOne(e => e.Conversation)
                  .WithMany(e => e.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Sender)
                  .WithMany(e => e.SentMessages)
                  .HasForeignKey(e => e.SenderId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Performance indexes for message queries
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt }); // Messages in conversation ordered by date
            entity.HasIndex(e => new { e.ConversationId, e.IsDeleted, e.CreatedAt }); // Non-deleted messages in conversation
        });

        // MessageStatus configuration
        modelBuilder.Entity<MessageStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MessageId, e.UserId }).IsUnique(); // Prevent duplicate status per user per message
            entity.Property(e => e.Status)
                  .HasConversion<int>()
                  .HasDefaultValue(MessageStatusType.Sent);
            entity.HasOne(e => e.Message)
                  .WithMany(e => e.MessageStatuses)
                  .HasForeignKey(e => e.MessageId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.MessageStatuses)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPreferences configuration
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique(); // One preference record per user
            entity.HasOne(e => e.User)
                  .WithOne()
                  .HasForeignKey<UserPreferences>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type)
                  .HasConversion<int>();
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.HasIndex(e => new { e.UserId, e.IsRead }); // For efficient querying of unread notifications
            entity.HasIndex(e => new { e.UserId, e.IsSeen }); // For efficient querying of unseen notifications
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }); // For notification timeline ordering
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ActorUser)
                  .WithMany()
                  .HasForeignKey(e => e.ActorUserId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Post)
                  .WithMany()
                  .HasForeignKey(e => e.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Comment)
                  .WithMany()
                  .HasForeignKey(e => e.CommentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Mention configuration
        modelBuilder.Entity<Mention>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.MentionedUserId, e.PostId }); // For efficient querying
            entity.HasIndex(e => new { e.MentionedUserId, e.CommentId }); // For efficient querying
            entity.HasOne(e => e.MentionedUser)
                  .WithMany(u => u.MentionsReceived)
                  .HasForeignKey(e => e.MentionedUserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.MentioningUser)
                  .WithMany(u => u.MentionsMade)
                  .HasForeignKey(e => e.MentioningUserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Post)
                  .WithMany()
                  .HasForeignKey(e => e.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Comment)
                  .WithMany()
                  .HasForeignKey(e => e.CommentId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Notification)
                  .WithOne()
                  .HasForeignKey<Mention>(e => e.NotificationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique(); // Ensure unique tag names
            entity.HasIndex(e => e.PostCount); // For trending tags queries
            entity.HasIndex(e => e.CreatedAt); // For chronological queries
        });

        // PostTag configuration
        modelBuilder.Entity<PostTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PostId, e.TagId }).IsUnique(); // Prevent duplicate post-tag relationships
            entity.HasIndex(e => e.TagId); // For efficient tag-based queries
            entity.HasIndex(e => e.PostId); // For efficient post-based queries
            entity.HasOne(e => e.Post)
                  .WithMany(e => e.PostTags)
                  .HasForeignKey(e => e.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Tag)
                  .WithMany(e => e.PostTags)
                  .HasForeignKey(e => e.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // LinkPreview configuration
        modelBuilder.Entity<LinkPreview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(2048);
            entity.Property(e => e.SiteName).HasMaxLength(100);
            entity.Property(e => e.YouTubeVideoId).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.Status)
                  .HasConversion<int>()
                  .HasDefaultValue(LinkPreviewStatus.Pending);

            // Index for URL lookups to avoid duplicate previews
            entity.HasIndex(e => e.Url).IsUnique();
            entity.HasIndex(e => e.Status); // For querying by status
            entity.HasIndex(e => e.CreatedAt); // For chronological queries
        });

        // PostLinkPreview configuration
        modelBuilder.Entity<PostLinkPreview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PostId, e.LinkPreviewId }).IsUnique(); // Prevent duplicate post-linkpreview relationships
            entity.HasIndex(e => e.LinkPreviewId); // For efficient linkpreview-based queries
            entity.HasIndex(e => e.PostId); // For efficient post-based queries
            entity.HasOne(e => e.Post)
                  .WithMany(e => e.PostLinkPreviews)
                  .HasForeignKey(e => e.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LinkPreview)
                  .WithMany(e => e.PostLinkPreviews)
                  .HasForeignKey(e => e.LinkPreviewId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // User admin relationships configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(e => e.SuspendedByUser)
                  .WithMany(e => e.SuspendedUsers)
                  .HasForeignKey(e => e.SuspendedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // SystemTag configuration
        modelBuilder.Entity<SystemTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
        });

        // PostSystemTag configuration
        modelBuilder.Entity<PostSystemTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PostId, e.SystemTagId }).IsUnique();
            entity.HasOne(e => e.Post)
                  .WithMany(e => e.PostSystemTags)
                  .HasForeignKey(e => e.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SystemTag)
                  .WithMany(e => e.PostSystemTags)
                  .HasForeignKey(e => e.SystemTagId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AppliedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.AppliedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });



        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.PerformedByUserId);
            entity.HasIndex(e => e.TargetUserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.PerformedByUser)
                  .WithMany(e => e.PerformedAuditLogs)
                  .HasForeignKey(e => e.PerformedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TargetUser)
                  .WithMany(e => e.AuditLogs)
                  .HasForeignKey(e => e.TargetUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TargetPost)
                  .WithMany(e => e.AuditLogs)
                  .HasForeignKey(e => e.TargetPostId)
                  .OnDelete(DeleteBehavior.Restrict);
            // TargetComment now points to Post entity (unified model)
            // We need to configure this without conflicting with TargetPost
            entity.HasOne(e => e.TargetComment)
                  .WithMany() // Don't use navigation property to avoid conflict
                  .HasForeignKey(e => e.TargetCommentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // UserAppeal configuration
        modelBuilder.Entity<UserAppeal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ReviewedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ReviewedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AuditLog)
                  .WithMany()
                  .HasForeignKey(e => e.AuditLogId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TargetPost)
                  .WithMany()
                  .HasForeignKey(e => e.TargetPostId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TargetComment)
                  .WithMany()
                  .HasForeignKey(e => e.TargetCommentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure AiSuggestedTag
        modelBuilder.Entity<AiSuggestedTag>(entity =>
        {
            entity.HasOne(ast => ast.Post)
                .WithMany()
                .HasForeignKey(ast => ast.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ast => ast.Comment)
                .WithMany()
                .HasForeignKey(ast => ast.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ast => ast.ApprovedByUser)
                .WithMany()
                .HasForeignKey(ast => ast.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(ast => new { ast.PostId, ast.TagName })
                .HasDatabaseName("IX_AiSuggestedTags_PostId_TagName");

            entity.HasIndex(ast => new { ast.CommentId, ast.TagName })
                .HasDatabaseName("IX_AiSuggestedTags_CommentId_TagName");

            entity.HasIndex(ast => ast.SuggestedAt)
                .HasDatabaseName("IX_AiSuggestedTags_SuggestedAt");

            entity.HasIndex(ast => new { ast.IsApproved, ast.IsRejected })
                .HasDatabaseName("IX_AiSuggestedTags_ApprovalStatus");
        });

        // UserReport configuration
        modelBuilder.Entity<UserReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ReportedByUserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.PostId);
            entity.HasIndex(e => e.CommentId);

            entity.HasOne(e => e.ReportedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ReportedByUserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ReviewedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ReviewedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Post)
                  .WithMany()
                  .HasForeignKey(e => e.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Comment)
                  .WithMany()
                  .HasForeignKey(e => e.CommentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserReportSystemTag configuration
        modelBuilder.Entity<UserReportSystemTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserReportId, e.SystemTagId }).IsUnique();

            entity.HasOne(e => e.UserReport)
                  .WithMany(e => e.UserReportSystemTags)
                  .HasForeignKey(e => e.UserReportId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SystemTag)
                  .WithMany()
                  .HasForeignKey(e => e.SystemTagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentPage configuration
        modelBuilder.Entity<ContentPage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Type).IsUnique();
            entity.Property(e => e.Type)
                  .HasConversion<int>();
            entity.HasOne(e => e.PublishedVersion)
                  .WithOne()
                  .HasForeignKey<ContentPage>(e => e.PublishedVersionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ContentPageVersion configuration
        modelBuilder.Entity<ContentPageVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ContentPageId, e.VersionNumber }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsPublished);
            entity.HasOne(e => e.ContentPage)
                  .WithMany(e => e.Versions)
                  .HasForeignKey(e => e.ContentPageId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PublishedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.PublishedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Analytics entity configurations

        // UserActivity configuration
        modelBuilder.Entity<UserActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }); // User activity timeline
            entity.HasIndex(e => new { e.ActivityType, e.CreatedAt }); // Activity type analysis
            entity.HasIndex(e => new { e.TargetEntityType, e.TargetEntityId }); // Target entity lookups
            entity.HasIndex(e => e.SessionId); // Session-based analytics
            entity.HasIndex(e => e.CreatedAt); // Time-based queries
            entity.HasOne(e => e.User)
                  .WithMany(e => e.UserActivities)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentEngagement configuration
        modelBuilder.Entity<ContentEngagement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ContentType, e.ContentId, e.CreatedAt }); // Content engagement timeline
            entity.HasIndex(e => new { e.UserId, e.EngagementType, e.CreatedAt }); // User engagement patterns
            entity.HasIndex(e => new { e.EngagementType, e.CreatedAt }); // Engagement type analysis
            entity.HasIndex(e => new { e.ContentOwnerId, e.CreatedAt }); // Content owner analytics
            entity.HasIndex(e => e.SessionId); // Session-based analytics
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ContentOwner)
                  .WithMany()
                  .HasForeignKey(e => e.ContentOwnerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // UserTrustScoreHistory configuration
        modelBuilder.Entity<UserTrustScoreHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }); // User trust score timeline
            entity.HasIndex(e => new { e.Reason, e.CreatedAt }); // Reason-based analysis
            entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId }); // Related entity lookups
            entity.HasIndex(e => e.TriggeredByUserId); // Admin action tracking
            entity.HasOne(e => e.User)
                  .WithMany(e => e.TrustScoreHistory)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.TriggeredByUser)
                  .WithMany()
                  .HasForeignKey(e => e.TriggeredByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // TagAnalytics configuration
        modelBuilder.Entity<TagAnalytics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TagId, e.CreatedAt }); // Tag usage timeline
            entity.HasIndex(e => new { e.Action, e.CreatedAt }); // Action-based analysis
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }); // User tag behavior
            entity.HasIndex(e => e.SessionId); // Session-based analytics
            entity.HasOne(e => e.Tag)
                  .WithMany()
                  .HasForeignKey(e => e.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // PerformanceMetric configuration
        modelBuilder.Entity<PerformanceMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MetricType, e.CreatedAt }); // Metric type timeline
            entity.HasIndex(e => new { e.Source, e.CreatedAt }); // Source-based analysis
            entity.HasIndex(e => new { e.Environment, e.CreatedAt }); // Environment-based analysis
            entity.HasIndex(e => e.CreatedAt); // Time-based queries
            entity.HasIndex(e => e.SessionId); // Session-based analytics
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // QueuedNotification configuration
        modelBuilder.Entity<QueuedNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.DeliveredAt }); // Primary query pattern: pending notifications for user
            entity.HasIndex(e => e.DeliveredAt); // All pending notifications
            entity.HasIndex(e => e.NextRetryAt); // Retry processing
            entity.HasIndex(e => e.CreatedAt); // Cleanup old notifications
            entity.HasIndex(e => new { e.Type, e.CreatedAt }); // Analytics by notification type
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UploadSettings configuration
        modelBuilder.Entity<UploadSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UpdatedAt);

            entity.HasOne(e => e.UpdatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.UpdatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // SubscriptionTier configuration
        modelBuilder.Entity<SubscriptionTier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.SortOrder);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Features).HasMaxLength(2000);
            entity.Property(e => e.Price).HasPrecision(10, 2); // Allows for prices up to 99,999,999.99
        });
    }
}
