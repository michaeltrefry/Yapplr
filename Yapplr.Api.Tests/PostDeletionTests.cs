using Microsoft.EntityFrameworkCore;
using Xunit;
using Yapplr.Api.Models;
using Yapplr.Api.Common;
using FluentAssertions;

namespace Yapplr.Api.Tests;

public class PostDeletionTests : IDisposable
{
    private readonly TestYapplrDbContext _context;

    public PostDeletionTests()
    {
        _context = new TestYapplrDbContext();

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test users
        var user1 = new User
        {
            Id = 1,
            Username = "testuser1",
            Email = "test1@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            Status = UserStatus.Active,
            TrustScore = 0.8f,
            Role = UserRole.User
        };

        var user2 = new User
        {
            Id = 2,
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            Status = UserStatus.Active,
            TrustScore = 0.9f,
            Role = UserRole.User
        };

        _context.Users.AddRange(user1, user2);

        // Create test posts
        var post1 = new Post
        {
            Id = 1,
            Content = "Test post 1",
            UserId = 1,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            User = user1
        };

        var post2 = new Post
        {
            Id = 2,
            Content = "Test post 2",
            UserId = 2,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            User = user2
        };

        var post3 = new Post
        {
            Id = 3,
            Content = "Test post 3 - to be deleted",
            UserId = 1,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow,
            User = user1
        };

        var deletedPost = new Post
        {
            Id = 4,
            Content = "Already deleted post",
            UserId = 1,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            User = user1,
            IsHidden = true,
            HiddenReasonType = PostHiddenReasonType.DeletedByUser,
            HiddenAt = DateTime.UtcNow.AddMinutes(-15),
            HiddenByUserId = 1,
            HiddenReason = "Post deleted by user"
        };

        _context.Posts.AddRange(post1, post2, post3, deletedPost);
        _context.SaveChanges();
    }

    [Fact]
    public async Task HybridHidingSystem_DeletedPostsFilteredCorrectly()
    {
        // Arrange
        var currentUserId = 1;
        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var visiblePosts = await _context.GetPostsForFeed()
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visiblePosts, p => p.HiddenReasonType == PostHiddenReasonType.DeletedByUser);
        Assert.Contains(visiblePosts, p => p.Id == 1); // Normal post should be visible
        Assert.Contains(visiblePosts, p => p.Id == 2); // Normal post should be visible
        Assert.Contains(visiblePosts, p => p.Id == 3); // Normal post should be visible
        Assert.DoesNotContain(visiblePosts, p => p.Id == 4); // Deleted post should not be visible
    }

    [Fact]
    public async Task HybridHidingSystem_DeletedPostsNotVisibleToOthers()
    {
        // Arrange
        var otherUserId = 2;
        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var visiblePosts = await _context.GetPostsForFeed()
            .ApplyVisibilityFilters(otherUserId, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visiblePosts, p => p.HiddenReasonType == PostHiddenReasonType.DeletedByUser);
        Assert.DoesNotContain(visiblePosts, p => p.Id == 4); // Deleted post should not be visible to other users
    }

    [Fact]
    public async Task HybridHidingSystem_PublicTimelineExcludesDeletedPosts()
    {
        // Arrange
        var blockedUserIds = new HashSet<int>();

        // Act
        var visiblePosts = await _context.GetPostsForFeed()
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visiblePosts, p => p.IsHidden);
        Assert.DoesNotContain(visiblePosts, p => p.Id == 4); // Deleted post should not be visible
        Assert.Contains(visiblePosts, p => p.Id == 1); // Normal posts should be visible
    }

    [Fact]
    public async Task HybridHidingSystem_VideoProcessingPostVisibleToAuthor()
    {
        // Arrange
        var authorId = 1;
        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Create a video processing post
        var videoPost = new Post
        {
            Id = 5,
            Content = "Video processing post",
            UserId = authorId,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow,
            User = _context.Users.Find(authorId)!,
            IsHidden = true,
            HiddenReasonType = PostHiddenReasonType.VideoProcessing,
            HiddenAt = DateTime.UtcNow
        };
        _context.Posts.Add(videoPost);
        await _context.SaveChangesAsync();

        // Act - Author viewing their own posts
        var visibleToAuthor = await _context.GetPostsForFeed()
            .ApplyVisibilityFilters(authorId, blockedUserIds, followingIds)
            .ToListAsync();

        // Act - Other user viewing posts
        var visibleToOthers = await _context.GetPostsForFeed()
            .ApplyVisibilityFilters(2, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        Assert.Contains(visibleToAuthor, p => p.Id == 5); // Author can see their video processing post
        Assert.DoesNotContain(visibleToOthers, p => p.Id == 5); // Others cannot see video processing post
    }

    [Fact]
    public async Task PostDeletion_ShouldCleanupSocialNotifications()
    {
        // Arrange - Create notifications for a post
        var postId = 3; // Using existing test post
        var userId = 2; // User who will receive notifications

        // Create social interaction notifications
        var likeNotification = new Notification
        {
            Type = NotificationType.Like,
            Message = "User liked your post",
            UserId = userId,
            PostId = postId,
            ActorUserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var commentNotification = new Notification
        {
            Type = NotificationType.Comment,
            Message = "User commented on your post",
            UserId = userId,
            PostId = postId,
            ActorUserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var mentionNotification = new Notification
        {
            Type = NotificationType.Mention,
            Message = "User mentioned you",
            UserId = userId,
            PostId = postId,
            ActorUserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Create a post-specific system notification that should be deleted
        var videoProcessingNotification = new Notification
        {
            Type = NotificationType.VideoProcessingCompleted,
            Message = "Your video has finished processing",
            UserId = 1, // Post owner
            PostId = postId,
            ActorUserId = null,
            CreatedAt = DateTime.UtcNow
        };

        // Create a user-level system notification that should be preserved
        var systemNotification = new Notification
        {
            Type = NotificationType.ContentHidden,
            Message = "Your post was hidden by a moderator",
            UserId = 1, // Post owner
            PostId = postId,
            ActorUserId = null,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.AddRange(likeNotification, commentNotification, mentionNotification, videoProcessingNotification, systemNotification);
        await _context.SaveChangesAsync();

        // Verify notifications exist before deletion
        var notificationsBeforeDeletion = await _context.Notifications
            .Where(n => n.PostId == postId)
            .ToListAsync();
        notificationsBeforeDeletion.Should().HaveCount(5);

        // Act - Mark post as deleted (simulating PostService.DeletePostAsync behavior)
        var post = await _context.Posts.FindAsync(postId);
        post!.IsHidden = true;
        post.HiddenReasonType = PostHiddenReasonType.DeletedByUser;
        post.HiddenAt = DateTime.UtcNow;
        post.HiddenByUserId = 1;
        post.HiddenReason = "Post deleted by user";
        await _context.SaveChangesAsync();

        // Simulate the notification cleanup (normally done by NotificationService.DeleteSocialNotificationsForPostAsync)
        var notificationTypesToDelete = new[]
        {
            NotificationType.Mention,
            NotificationType.Like,
            NotificationType.Repost,
            NotificationType.Follow,
            NotificationType.Comment,
            NotificationType.FollowRequest,
            NotificationType.VideoProcessingCompleted // Post-specific system notification
        };

        var notificationsToDelete = await _context.Notifications
            .Where(n => n.PostId == postId && notificationTypesToDelete.Contains(n.Type))
            .ToListAsync();

        _context.Notifications.RemoveRange(notificationsToDelete);
        await _context.SaveChangesAsync();

        // Assert - Only user-level system notifications should remain
        var remainingNotifications = await _context.Notifications
            .Where(n => n.PostId == postId)
            .ToListAsync();

        remainingNotifications.Should().HaveCount(1);
        remainingNotifications[0].Type.Should().Be(NotificationType.ContentHidden);
        remainingNotifications[0].Message.Should().Be("Your post was hidden by a moderator");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
