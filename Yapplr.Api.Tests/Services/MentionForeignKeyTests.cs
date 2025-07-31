using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Notifications;
using Yapplr.Shared.Models;

namespace Yapplr.Api.Tests.Services;

/// <summary>
/// Tests to verify that mention creation properly handles foreign key relationships
/// </summary>
public class MentionForeignKeyTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly NotificationService _notificationService;

    public MentionForeignKeyTests()
    {
        _context = new TestYapplrDbContext();
        
        // Create mock dependencies for NotificationService
        var mockPreferencesService = new Mock<INotificationPreferencesService>();
        var mockConnectionPool = new Mock<ISignalRConnectionPool>();
        var mockCountCache = new Mock<ICountCacheService>();
        var mockConversationTracker = new Mock<IActiveConversationTracker>();
        var mockLogger = new Mock<ILogger<NotificationService>>();

        _notificationService = new NotificationService(
            _context,
            mockPreferencesService.Object,
            mockConnectionPool.Object,
            mockCountCache.Object,
            mockConversationTracker.Object,
            mockLogger.Object
        );

        // Setup test users
        var mentioningUser = new User
        {
            Id = 1,
            Username = "mentioner",
            Email = "mentioner@test.com",
            Role = UserRole.User,
            Status = UserStatus.Active
        };

        var mentionedUser = new User
        {
            Id = 2,
            Username = "mentioned",
            Email = "mentioned@test.com",
            Role = UserRole.User,
            Status = UserStatus.Active
        };

        _context.Users.AddRange(mentioningUser, mentionedUser);

        // Setup test post
        var post = new Post
        {
            Id = 1,
            Content = "Test post content",
            UserId = 1,
            PostType = PostType.Post,
            CreatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateMentionNotificationsAsync_ShouldCreateMentionWithValidNotificationId()
    {
        // Arrange
        var content = "Hello @mentioned, this is a test!";
        var mentioningUserId = 1;
        var postId = 1;

        // Act
        await _notificationService.CreateMentionNotificationsAsync(content, mentioningUserId, postId);

        // Assert
        // Verify notification was created
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Type == NotificationType.Mention);
        
        Assert.NotNull(notification);
        Assert.Equal(2, notification.UserId); // mentioned user
        Assert.Equal(1, notification.ActorUserId); // mentioning user
        Assert.Equal(1, notification.PostId);

        // Verify mention was created with proper foreign key
        var mention = await _context.Mentions
            .FirstOrDefaultAsync(m => m.MentionedUserId == 2);
        
        Assert.NotNull(mention);
        Assert.Equal("mentioned", mention.Username);
        Assert.Equal(1, mention.MentioningUserId);
        Assert.Equal(2, mention.MentionedUserId);
        Assert.Equal(1, mention.PostId);
        Assert.Equal(notification.Id, mention.NotificationId); // This is the key fix!

        // Verify foreign key relationship works
        var mentionWithNotification = await _context.Mentions
            .Include(m => m.Notification)
            .FirstOrDefaultAsync(m => m.Id == mention.Id);
        
        Assert.NotNull(mentionWithNotification);
        Assert.NotNull(mentionWithNotification.Notification);
        Assert.Equal(notification.Id, mentionWithNotification.Notification.Id);
    }

    [Fact]
    public async Task CreateMentionNotificationsAsync_WithComment_ShouldCreateMentionWithValidNotificationId()
    {
        // Arrange
        var content = "Replying to @mentioned in this comment!";
        var mentioningUserId = 1;
        var postId = 1;
        var commentId = 2;

        // Create a comment post
        var comment = new Post
        {
            Id = 2,
            Content = "This is a comment",
            UserId = 1,
            PostType = PostType.Comment,
            ParentId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(comment);
        await _context.SaveChangesAsync();

        // Act
        await _notificationService.CreateMentionNotificationsAsync(content, mentioningUserId, commentId);

        // Assert
        // Verify notification was created
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Type == NotificationType.Mention && n.CommentId == commentId);
        
        Assert.NotNull(notification);
        Assert.Equal(2, notification.UserId); // mentioned user
        Assert.Equal(1, notification.ActorUserId); // mentioning user
        Assert.Equal(1, notification.PostId); // parent post
        Assert.Equal(2, notification.CommentId); // comment ID

        // Verify mention was created with proper foreign key
        var mention = await _context.Mentions
            .FirstOrDefaultAsync(m => m.MentionedUserId == 2 && m.PostId == commentId);
        
        Assert.NotNull(mention);
        Assert.Equal("mentioned", mention.Username);
        Assert.Equal(1, mention.MentioningUserId);
        Assert.Equal(2, mention.MentionedUserId);
        Assert.Equal(2, mention.PostId); // This should be the comment's own ID
        Assert.Equal(notification.Id, mention.NotificationId); // This is the key fix!
    }

    [Fact]
    public async Task CreateMentionNotificationsAsync_WithMultipleMentions_ShouldCreateMultipleMentionsWithValidNotificationIds()
    {
        // Arrange
        // Add another user to mention
        var anotherUser = new User
        {
            Id = 3,
            Username = "another",
            Email = "another@test.com",
            Role = UserRole.User,
            Status = UserStatus.Active
        };
        _context.Users.Add(anotherUser);
        await _context.SaveChangesAsync();

        var content = "Hello @mentioned and @another, this is a test!";
        var mentioningUserId = 1;
        var postId = 1;

        // Act
        await _notificationService.CreateMentionNotificationsAsync(content, mentioningUserId, postId);

        // Assert
        // Verify two notifications were created
        var notifications = await _context.Notifications
            .Where(n => n.Type == NotificationType.Mention)
            .ToListAsync();

        Assert.Equal(2, notifications.Count);

        // Verify two mentions were created with proper foreign keys
        var mentions = await _context.Mentions
            .Include(m => m.Notification)
            .ToListAsync();

        Assert.Equal(2, mentions.Count);

        foreach (var mention in mentions)
        {
            Assert.NotNull(mention.Notification);
            Assert.True(mention.NotificationId > 0);
            Assert.Contains(notifications, n => n.Id == mention.NotificationId);
        }
    }

    [Fact]
    public async Task CreateReactionNotificationAsync_ShouldCreateOnlyOneDatabaseNotification()
    {
        // Arrange
        var reactedUserId = 2;
        var reactingUserId = 1;
        var postId = 1;
        var reactionType = ReactionType.Heart;

        // Act
        await _notificationService.CreateReactionNotificationAsync(reactedUserId, reactingUserId, postId, reactionType);

        // Assert
        // Verify only ONE database notification was created
        var notifications = await _context.Notifications
            .Where(n => n.Type == NotificationType.React && n.UserId == reactedUserId)
            .ToListAsync();

        Assert.Single(notifications); // Should be exactly 1, not 2

        var notification = notifications.First();
        Assert.Equal(reactedUserId, notification.UserId);
        Assert.Equal(reactingUserId, notification.ActorUserId);
        Assert.Equal(postId, notification.PostId);
        Assert.Contains("❤️", notification.Message); // Should contain the heart emoji
        Assert.Contains("@mentioner reacted", notification.Message); // Should contain the reaction message
    }

    [Fact]
    public async Task CreateCommentNotificationAsync_ShouldCreateOnlyOneDatabaseNotification()
    {
        // Arrange
        var postOwnerId = 2;
        var commentingUserId = 1;
        var postId = 1;
        var commentId = 2;

        // Create a comment post
        var comment = new Post
        {
            Id = 2,
            Content = "This is a comment",
            UserId = 1,
            PostType = PostType.Comment,
            ParentId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(comment);
        await _context.SaveChangesAsync();

        // Act
        await _notificationService.CreateCommentNotificationAsync(postOwnerId, commentingUserId, postId, commentId, "Test comment");

        // Assert
        // Verify only ONE database notification was created
        var notifications = await _context.Notifications
            .Where(n => n.Type == NotificationType.Comment && n.UserId == postOwnerId)
            .ToListAsync();

        Assert.Single(notifications); // Should be exactly 1, not 2

        var notification = notifications.First();
        Assert.Equal(postOwnerId, notification.UserId);
        Assert.Equal(commentingUserId, notification.ActorUserId);
        Assert.Equal(postId, notification.PostId);
        Assert.Equal(commentId, notification.CommentId);
        Assert.Contains("@mentioner commented", notification.Message);
    }

    [Fact]
    public async Task CreateMentionNotificationsAsync_ShouldCreateOnlyOneDatabaseNotificationPerMention()
    {
        // Arrange
        var content = "Hello @mentioned, this is a test!";
        var mentioningUserId = 1;
        var postId = 1;

        // Act
        await _notificationService.CreateMentionNotificationsAsync(content, mentioningUserId, postId);

        // Assert
        // Verify only ONE database notification was created for the mention
        var notifications = await _context.Notifications
            .Where(n => n.Type == NotificationType.Mention && n.UserId == 2)
            .ToListAsync();

        Assert.Single(notifications); // Should be exactly 1, not 2

        var notification = notifications.First();
        Assert.Equal(2, notification.UserId); // mentioned user
        Assert.Equal(1, notification.ActorUserId); // mentioning user
        Assert.Equal(1, notification.PostId);
        Assert.Contains("@mentioner mentioned you", notification.Message);

        // Also verify the mention record was created
        var mention = await _context.Mentions
            .FirstOrDefaultAsync(m => m.MentionedUserId == 2);

        Assert.NotNull(mention);
        Assert.Equal(notification.Id, mention.NotificationId);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldIncludeMentionDataForMentionNotifications()
    {
        // Arrange
        var content = "Hello @mentioned, this is a comment mention!";
        var mentioningUserId = 1;
        var postId = 1;
        var commentId = 2;

        // Create a comment post
        var comment = new Post
        {
            Id = 2,
            Content = "This is a comment",
            UserId = 1,
            PostType = PostType.Comment,
            ParentId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(comment);
        await _context.SaveChangesAsync();

        // Create mention notification
        await _notificationService.CreateMentionNotificationsAsync(content, mentioningUserId, commentId);

        // Act - Get notifications as the mentioned user would
        var result = await _notificationService.GetUserNotificationsAsync(2); // mentioned user

        // Assert
        Assert.Single(result.Notifications);
        var notificationDto = result.Notifications.First();

        // Verify basic notification properties
        Assert.Equal(NotificationType.Mention, notificationDto.Type);
        Assert.Contains("@mentioner mentioned you in a comment", notificationDto.Message);

        // Verify mention data is included
        Assert.NotNull(notificationDto.Mention);
        Assert.Equal("mentioned", notificationDto.Mention.Username);
        Assert.Equal(2, notificationDto.Mention.MentionedUserId);
        Assert.Equal(1, notificationDto.Mention.MentioningUserId);
        Assert.Equal(2, notificationDto.Mention.PostId); // The comment's own ID
        Assert.True(notificationDto.Mention.IsCommentMention); // Should be true for comment mentions
        Assert.Equal(1, notificationDto.Mention.ParentPostId); // The parent post ID

        // Verify post data is included (for navigation)
        Assert.NotNull(notificationDto.Post);
        Assert.Equal(1, notificationDto.Post.Id); // This should be the parent post
    }

    [Fact]
    public async Task CreateCommentNotificationAsync_WhenPostOwnerIsMentioned_ShouldSkipCommentNotification()
    {
        // Arrange
        var postOwnerId = 2; // mentioned user
        var commentingUserId = 1; // mentioner user
        var postId = 1;
        var commentId = 2;
        var commentContent = "Hey @mentioned, this is a comment on your post!";

        // Create a comment post
        var comment = new Post
        {
            Id = 2,
            Content = commentContent,
            UserId = 1,
            PostType = PostType.Comment,
            ParentId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(comment);
        await _context.SaveChangesAsync();

        // Act
        await _notificationService.CreateCommentNotificationAsync(postOwnerId, commentingUserId, postId, commentId, commentContent);

        // Assert
        // Should NOT create a comment notification since the post owner is mentioned
        var commentNotifications = await _context.Notifications
            .Where(n => n.Type == NotificationType.Comment && n.UserId == postOwnerId)
            .ToListAsync();

        Assert.Empty(commentNotifications); // Should be empty - no comment notification created
    }

    [Fact]
    public async Task CreateCommentNotificationAsync_WhenPostOwnerIsNotMentioned_ShouldCreateCommentNotification()
    {
        // Arrange
        var postOwnerId = 2; // mentioned user
        var commentingUserId = 1; // mentioner user
        var postId = 1;
        var commentId = 2;
        var commentContent = "This is a comment without mentioning the post owner";

        // Create a comment post
        var comment = new Post
        {
            Id = 2,
            Content = commentContent,
            UserId = 1,
            PostType = PostType.Comment,
            ParentId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(comment);
        await _context.SaveChangesAsync();

        // Act
        await _notificationService.CreateCommentNotificationAsync(postOwnerId, commentingUserId, postId, commentId, commentContent);

        // Assert
        // Should create a comment notification since the post owner is NOT mentioned
        var commentNotifications = await _context.Notifications
            .Where(n => n.Type == NotificationType.Comment && n.UserId == postOwnerId)
            .ToListAsync();

        Assert.Single(commentNotifications); // Should have exactly 1 comment notification

        var notification = commentNotifications.First();
        Assert.Equal(postOwnerId, notification.UserId);
        Assert.Equal(commentingUserId, notification.ActorUserId);
        Assert.Equal(postId, notification.PostId);
        Assert.Equal(commentId, notification.CommentId);
        Assert.Contains("@mentioner commented", notification.Message);
    }

    [Fact]
    public async Task CommentWithMention_ShouldOnlyCreateMentionNotificationForPostOwner()
    {
        // Arrange
        var postOwnerId = 2; // mentioned user (also post owner)
        var commentingUserId = 1; // mentioner user
        var postId = 1;
        var commentContent = "Hey @mentioned, this is a comment on your post!";

        // Create a comment post
        var comment = new Post
        {
            Content = commentContent,
            UserId = commentingUserId,
            PostType = PostType.Comment,
            ParentId = postId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(comment);
        await _context.SaveChangesAsync();

        // Act - Simulate the full flow from PostService.AddCommentAsync
        // 1. Create comment notification (should be skipped due to mention)
        await _notificationService.CreateCommentNotificationAsync(postOwnerId, commentingUserId, postId, comment.Id, commentContent);

        // 2. Create mention notifications
        await _notificationService.CreateMentionNotificationsAsync(commentContent, commentingUserId, comment.Id);

        // Assert
        var allNotifications = await _context.Notifications
            .Where(n => n.UserId == postOwnerId)
            .ToListAsync();

        // Should only have 1 notification (mention), not 2 (mention + comment)
        Assert.Single(allNotifications);

        var notification = allNotifications.First();
        Assert.Equal(NotificationType.Mention, notification.Type);
        Assert.Contains("mentioned you in a comment", notification.Message);
    }

    [Fact]
    public async Task CommentWithMultipleMentions_IncludingPostOwner_ShouldSkipCommentNotificationButCreateAllMentionNotifications()
    {
        // Arrange
        var postOwnerId = 2; // mentioned user (also post owner)
        var commentingUserId = 1; // mentioner user
        var otherUserId = 3; // another mentioned user
        var postId = 1;
        var commentContent = "Hey @mentioned and @otheruser, this is a comment!";

        // Create another user to mention
        var otherUser = new User
        {
            Id = 3,
            Username = "otheruser",
            Email = "other@example.com",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            TrustScore = 1.0f,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(otherUser);
        await _context.SaveChangesAsync();

        // Create a comment post
        var comment = new Post
        {
            Content = commentContent,
            UserId = commentingUserId,
            PostType = PostType.Comment,
            ParentId = postId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(comment);
        await _context.SaveChangesAsync();

        // Act - Simulate the full flow from PostService.AddCommentAsync
        // 1. Create comment notification (should be skipped due to post owner being mentioned)
        await _notificationService.CreateCommentNotificationAsync(postOwnerId, commentingUserId, postId, comment.Id, commentContent);

        // 2. Create mention notifications
        await _notificationService.CreateMentionNotificationsAsync(commentContent, commentingUserId, comment.Id);

        // Assert
        // Post owner should only get mention notification, not comment notification
        var postOwnerNotifications = await _context.Notifications
            .Where(n => n.UserId == postOwnerId)
            .ToListAsync();

        Assert.Single(postOwnerNotifications);
        Assert.Equal(NotificationType.Mention, postOwnerNotifications.First().Type);
        Assert.Contains("mentioned you in a comment", postOwnerNotifications.First().Message);

        // Other user should get mention notification
        var otherUserNotifications = await _context.Notifications
            .Where(n => n.UserId == otherUserId)
            .ToListAsync();

        Assert.Single(otherUserNotifications);
        Assert.Equal(NotificationType.Mention, otherUserNotifications.First().Type);
        Assert.Contains("mentioned you in a comment", otherUserNotifications.First().Message);

        // Total notifications should be 2 (one mention for each mentioned user)
        var allNotifications = await _context.Notifications.ToListAsync();
        Assert.Equal(2, allNotifications.Count);
        Assert.All(allNotifications, n => Assert.Equal(NotificationType.Mention, n.Type));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
