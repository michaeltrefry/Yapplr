using Microsoft.EntityFrameworkCore;
using Xunit;
using Yapplr.Api.Models;
using Yapplr.Api.Common;

namespace Yapplr.Api.Tests;

public class RepostDeletionFilteringTests : IDisposable
{
    private readonly TestYapplrDbContext _context;

    public RepostDeletionFilteringTests()
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
            Username = "user1",
            Email = "user1@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            Status = UserStatus.Active,
            TrustScore = 0.8f,
            Role = UserRole.User
        };

        var user2 = new User
        {
            Id = 2,
            Username = "user2",
            Email = "user2@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            Status = UserStatus.Active,
            TrustScore = 0.9f,
            Role = UserRole.User
        };

        var suspendedUser = new User
        {
            Id = 3,
            Username = "suspendeduser",
            Email = "suspended@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            Status = UserStatus.Suspended,
            TrustScore = 0.5f,
            Role = UserRole.User
        };

        var lowTrustUser = new User
        {
            Id = 4,
            Username = "lowtrustuser",
            Email = "lowtrust@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            Status = UserStatus.Active,
            TrustScore = 0.05f, // Below 0.1 threshold
            Role = UserRole.User
        };

        _context.Users.AddRange(user1, user2, suspendedUser, lowTrustUser);

        // Create test posts
        var normalPost = new Post
        {
            Id = 1,
            Content = "Normal post",
            UserId = 1,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            User = user1,
            IsHidden = false,
            HiddenReasonType = PostHiddenReasonType.None
        };

        var deletedPost = new Post
        {
            Id = 2,
            Content = "Deleted post",
            UserId = 1,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            User = user1,
            IsHidden = true,
            HiddenReasonType = PostHiddenReasonType.DeletedByUser,
            HiddenAt = DateTime.UtcNow.AddHours(-1),
            HiddenByUserId = 1,
            HiddenReason = "Post deleted by user"
        };

        var moderatorHiddenPost = new Post
        {
            Id = 3,
            Content = "Moderator hidden post",
            UserId = 1,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            User = user1,
            IsHidden = true,
            HiddenReasonType = PostHiddenReasonType.ModeratorHidden,
            HiddenAt = DateTime.UtcNow.AddMinutes(-30),
            HiddenByUserId = 1,
            HiddenReason = "Inappropriate content"
        };

        var videoProcessingPost = new Post
        {
            Id = 4,
            Content = "Video processing post",
            UserId = 1,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow,
            User = user1,
            IsHidden = true,
            HiddenReasonType = PostHiddenReasonType.VideoProcessing,
            HiddenAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var suspendedUserPost = new Post
        {
            Id = 5,
            Content = "Post from suspended user",
            UserId = 3,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            User = suspendedUser,
            IsHidden = false,
            HiddenReasonType = PostHiddenReasonType.None
        };

        var lowTrustUserPost = new Post
        {
            Id = 6,
            Content = "Post from low trust user",
            UserId = 4,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            User = lowTrustUser,
            IsHidden = false,
            HiddenReasonType = PostHiddenReasonType.None
        };

        _context.Posts.AddRange(
            normalPost, 
            deletedPost, 
            moderatorHiddenPost, 
            videoProcessingPost, 
            suspendedUserPost, 
            lowTrustUserPost
        );

        // Create reposts
        var repostOfNormalPost = new Repost
        {
            Id = 1,
            UserId = 2,
            PostId = 1,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            User = user2,
            Post = normalPost
        };

        var repostOfDeletedPost = new Repost
        {
            Id = 2,
            UserId = 2,
            PostId = 2,
            CreatedAt = DateTime.UtcNow.AddMinutes(-25),
            User = user2,
            Post = deletedPost
        };

        var repostOfModeratorHiddenPost = new Repost
        {
            Id = 3,
            UserId = 2,
            PostId = 3,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            User = user2,
            Post = moderatorHiddenPost
        };

        var repostOfVideoProcessingPost = new Repost
        {
            Id = 4,
            UserId = 2,
            PostId = 4,
            CreatedAt = DateTime.UtcNow.AddMinutes(-15),
            User = user2,
            Post = videoProcessingPost
        };

        var repostOfSuspendedUserPost = new Repost
        {
            Id = 5,
            UserId = 2,
            PostId = 5,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            User = user2,
            Post = suspendedUserPost
        };

        var repostOfLowTrustUserPost = new Repost
        {
            Id = 6,
            UserId = 2,
            PostId = 6,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            User = user2,
            Post = lowTrustUserPost
        };

        _context.Reposts.AddRange(
            repostOfNormalPost,
            repostOfDeletedPost,
            repostOfModeratorHiddenPost,
            repostOfVideoProcessingPost,
            repostOfSuspendedUserPost,
            repostOfLowTrustUserPost
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task ApplyRepostVisibilityFilters_ExcludesRepostsOfDeletedPosts()
    {
        // Arrange
        var blockedUserIds = new HashSet<int>();

        // Act
        var visibleReposts = await _context.GetRepostsWithIncludes()
            .ApplyRepostVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visibleReposts, r => r.Post.HiddenReasonType == PostHiddenReasonType.DeletedByUser);
        Assert.Contains(visibleReposts, r => r.PostId == 1); // Repost of normal post should be visible
    }

    [Fact]
    public async Task ApplyRepostVisibilityFilters_ExcludesRepostsOfModeratorHiddenPosts()
    {
        // Arrange
        var blockedUserIds = new HashSet<int>();

        // Act
        var visibleReposts = await _context.GetRepostsWithIncludes()
            .ApplyRepostVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visibleReposts, r => r.Post.HiddenReasonType == PostHiddenReasonType.ModeratorHidden);
    }

    [Fact]
    public async Task ApplyRepostVisibilityFilters_ExcludesRepostsOfVideoProcessingPosts()
    {
        // Arrange
        var blockedUserIds = new HashSet<int>();

        // Act
        var visibleReposts = await _context.GetRepostsWithIncludes()
            .ApplyRepostVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visibleReposts, r => r.Post.HiddenReasonType == PostHiddenReasonType.VideoProcessing);
    }

    [Fact]
    public async Task ApplyRepostVisibilityFilters_ExcludesRepostsOfPostsFromSuspendedUsers()
    {
        // Arrange
        var blockedUserIds = new HashSet<int>();

        // Act
        var visibleReposts = await _context.GetRepostsWithIncludes()
            .ApplyRepostVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visibleReposts, r => r.Post.User.Status == UserStatus.Suspended);
    }

    [Fact]
    public async Task ApplyRepostVisibilityFilters_ExcludesRepostsOfLowTrustUserPosts()
    {
        // Arrange
        var blockedUserIds = new HashSet<int>();

        // Act
        var visibleReposts = await _context.GetRepostsWithIncludes()
            .ApplyRepostVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visibleReposts, r => r.Post.User.TrustScore < 0.1f);
    }

    [Fact]
    public async Task ApplyRepostVisibilityFilters_ExcludesRepostsFromBlockedUsers()
    {
        // Arrange
        var blockedUserIds = new HashSet<int> { 2 }; // Block the user who made reposts

        // Act
        var visibleReposts = await _context.GetRepostsWithIncludes()
            .ApplyRepostVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visibleReposts, r => r.UserId == 2);
    }

    [Fact]
    public async Task ApplyRepostVisibilityFilters_ExcludesRepostsOfPostsFromBlockedUsers()
    {
        // Arrange
        var blockedUserIds = new HashSet<int> { 1 }; // Block the original post author

        // Act
        var visibleReposts = await _context.GetRepostsWithIncludes()
            .ApplyRepostVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visibleReposts, r => r.Post.UserId == 1);
    }

    [Fact]
    public async Task ApplyRepostVisibilityFilters_OnlyShowsPublicPosts()
    {
        // Arrange
        var blockedUserIds = new HashSet<int>();

        // Make one post followers-only
        var post = await _context.Posts.FindAsync(1);
        post!.Privacy = PostPrivacy.Followers;
        await _context.SaveChangesAsync();

        // Act
        var visibleReposts = await _context.GetRepostsWithIncludes()
            .ApplyRepostVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visibleReposts, r => r.Post.Privacy != PostPrivacy.Public);
    }

    [Fact]
    public async Task ApplyRepostVisibilityFilters_CombinedFiltering()
    {
        // Arrange
        var blockedUserIds = new HashSet<int>();

        // Act
        var visibleReposts = await _context.GetRepostsWithIncludes()
            .ApplyRepostVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        // Should only show repost of the normal post (post ID 1)
        Assert.Single(visibleReposts);
        Assert.Equal(1, visibleReposts.First().PostId);
        Assert.Equal(PostHiddenReasonType.None, visibleReposts.First().Post.HiddenReasonType);
        Assert.Equal(UserStatus.Active, visibleReposts.First().Post.User.Status);
        Assert.True(visibleReposts.First().Post.User.TrustScore >= 0.1f);
        Assert.Equal(PostPrivacy.Public, visibleReposts.First().Post.Privacy);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
