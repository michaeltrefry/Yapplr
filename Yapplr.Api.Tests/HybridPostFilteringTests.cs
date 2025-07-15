using Microsoft.EntityFrameworkCore;
using Xunit;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Common;
using Yapplr.Api.Tests.Common;

namespace Yapplr.Api.Tests;

public class HybridPostFilteringTests : IDisposable
{
    private readonly TestYapplrDbContext _context;

    public HybridPostFilteringTests()
    {
        _context = new TestYapplrDbContext();
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test users with different statuses and trust scores
        var activeUser = new User
        {
            Id = 1,
            Username = "activeuser",
            Email = "active@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            Status = UserStatus.Active,
            TrustScore = 0.8f,
            Role = UserRole.User
        };

        var suspendedUser = new User
        {
            Id = 2,
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
            Id = 3,
            Username = "lowtrustuser",
            Email = "lowtrust@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            Status = UserStatus.Active,
            TrustScore = 0.05f, // Below 0.1 threshold
            Role = UserRole.User
        };

        _context.Users.AddRange(activeUser, suspendedUser, lowTrustUser);

        // Create test posts with different hiding states
        var normalPost = new Post
        {
            Id = 1,
            Content = "Normal post",
            UserId = 1,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            User = activeUser,
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
            User = activeUser,
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
            User = activeUser,
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
            User = activeUser,
            IsHidden = true,
            HiddenReasonType = PostHiddenReasonType.VideoProcessing,
            HiddenAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var suspendedUserPost = new Post
        {
            Id = 5,
            Content = "Post from suspended user",
            UserId = 2,
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
            UserId = 3,
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

        _context.SaveChanges();
    }

    [Fact]
    public async Task ApplyVisibilityFilters_ExcludesDeletedPosts()
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
    }

    [Fact]
    public async Task ApplyVisibilityFilters_ExcludesModeratorHiddenPosts()
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
        Assert.DoesNotContain(visiblePosts, p => p.HiddenReasonType == PostHiddenReasonType.ModeratorHidden);
    }

    [Fact]
    public async Task ApplyVisibilityFilters_VideoProcessingPostVisibleToAuthor()
    {
        // Arrange
        var currentUserId = 1; // Author of the video processing post
        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var visiblePosts = await _context.GetPostsForFeed()
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        Assert.Contains(visiblePosts, p => p.Id == 4 && p.HiddenReasonType == PostHiddenReasonType.VideoProcessing);
    }

    [Fact]
    public async Task ApplyVisibilityFilters_VideoProcessingPostNotVisibleToOthers()
    {
        // Arrange
        var currentUserId = 2; // Different user
        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var visiblePosts = await _context.GetPostsForFeed()
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visiblePosts, p => p.Id == 4 && p.HiddenReasonType == PostHiddenReasonType.VideoProcessing);
    }

    [Fact]
    public async Task ApplyVisibilityFilters_ExcludesPostsFromSuspendedUsers()
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
        Assert.DoesNotContain(visiblePosts, p => p.UserId == 2); // Suspended user's posts
    }

    [Fact]
    public async Task ApplyVisibilityFilters_ExcludesLowTrustPostsExceptFromAuthor()
    {
        // Arrange - Viewing as different user
        var currentUserId = 1;
        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var visiblePosts = await _context.GetPostsForFeed()
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visiblePosts, p => p.UserId == 3); // Low trust user's posts not visible to others
    }

    [Fact]
    public async Task ApplyVisibilityFilters_LowTrustPostsVisibleToAuthor()
    {
        // Arrange - Viewing as the low trust user themselves
        var currentUserId = 3; // Low trust user
        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var visiblePosts = await _context.GetPostsForFeed()
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        Assert.Contains(visiblePosts, p => p.UserId == 3); // Low trust user can see their own posts
    }

    [Fact]
    public async Task ApplyVisibilityFilters_ExcludesBlockedUsers()
    {
        // Arrange
        var currentUserId = 1;
        var blockedUserIds = new HashSet<int> { 3 }; // Block user 3
        var followingIds = new HashSet<int>();

        // Act
        var visiblePosts = await _context.GetPostsForFeed()
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visiblePosts, p => p.UserId == 3); // Blocked user's posts
    }

    [Fact]
    public async Task ApplyPublicVisibilityFilters_ExcludesAllHiddenPosts()
    {
        // Arrange
        var blockedUserIds = new HashSet<int>();

        // Act
        var visiblePosts = await _context.GetPostsForFeed()
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visiblePosts, p => p.IsHidden);
        Assert.Contains(visiblePosts, p => p.Id == 1); // Normal post should be visible
    }

    [Fact]
    public async Task ApplyPublicVisibilityFilters_ExcludesLowTrustAndSuspendedUsers()
    {
        // Arrange
        var blockedUserIds = new HashSet<int>();

        // Act
        var visiblePosts = await _context.GetPostsForFeed()
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(visiblePosts, p => p.UserId == 2); // Suspended user
        Assert.DoesNotContain(visiblePosts, p => p.UserId == 3); // Low trust user
        Assert.Contains(visiblePosts, p => p.UserId == 1); // Active user with good trust score
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
