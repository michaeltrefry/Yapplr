using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Common;

namespace Yapplr.Api.Tests;

/// <summary>
/// Integration tests for the complete post filtering workflow
/// Tests complex scenarios with multiple filtering conditions
/// </summary>
public class PostFilteringIntegrationTests : IDisposable
{
    private readonly YapplrDbContext _context;

    public PostFilteringIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestYapplrDbContext(options);
    }

    #region Complex Scenario Tests

    [Fact]
    public async Task ComplexFilteringScenario_ShouldApplyAllFiltersCorrectly()
    {
        // Arrange - Create a complex scenario with multiple users and posts
        var activeUser = await CreateUserAsync("active", UserStatus.Active, 0.8f);
        var suspendedUser = await CreateUserAsync("suspended", UserStatus.Suspended, 0.9f);
        var lowTrustUser = await CreateUserAsync("lowtrust", UserStatus.Active, 0.05f);
        var blockedUser = await CreateUserAsync("blocked", UserStatus.Active, 0.7f);
        var currentUser = await CreateUserAsync("current", UserStatus.Active, 0.6f);

        // Create various posts
        var visiblePost = await CreatePostAsync(activeUser, "Visible post", PostPrivacy.Public);
        var suspendedUserPost = await CreatePostAsync(suspendedUser, "Suspended user post", PostPrivacy.Public);
        var lowTrustPost = await CreatePostAsync(lowTrustUser, "Low trust post", PostPrivacy.Public);
        var blockedUserPost = await CreatePostAsync(blockedUser, "Blocked user post", PostPrivacy.Public);
        var hiddenPost = await CreatePostAsync(activeUser, "Hidden post", PostPrivacy.Public, true, PostHiddenReasonType.ModeratorHidden);
        var videoProcessingPost = await CreatePostAsync(activeUser, "Video processing", PostPrivacy.Public, true, PostHiddenReasonType.VideoProcessing);
        var followersOnlyPost = await CreatePostAsync(activeUser, "Followers only", PostPrivacy.Followers);

        // Set up relationships
        var blockedUserIds = new HashSet<int> { blockedUser.Id };
        var followingIds = new HashSet<int> { activeUser.Id }; // Current user follows active user

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(currentUser.Id, blockedUserIds, followingIds)
            .OrderBy(p => p.Id)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(2); // Only visiblePost and followersOnlyPost should be visible
        result.Should().Contain(p => p.Id == visiblePost.Id);
        result.Should().Contain(p => p.Id == followersOnlyPost.Id);
        result.Should().NotContain(p => p.Id == suspendedUserPost.Id); // Filtered by user status
        result.Should().NotContain(p => p.Id == lowTrustPost.Id); // Filtered by trust score
        result.Should().NotContain(p => p.Id == blockedUserPost.Id); // Filtered by block list
        result.Should().NotContain(p => p.Id == hiddenPost.Id); // Filtered by hidden status
        result.Should().NotContain(p => p.Id == videoProcessingPost.Id); // Filtered (not author)
    }

    [Fact]
    public async Task AuthorViewingOwnContent_ShouldSeeAllOwnPosts()
    {
        // Arrange
        var author = await CreateUserAsync("author", UserStatus.Active, 0.05f); // Low trust score
        
        var publicPost = await CreatePostAsync(author, "Public post", PostPrivacy.Public);
        var followersPost = await CreatePostAsync(author, "Followers post", PostPrivacy.Followers);
        var hiddenPost = await CreatePostAsync(author, "Hidden post", PostPrivacy.Public, true, PostHiddenReasonType.ModeratorHidden);
        var videoProcessingPost = await CreatePostAsync(author, "Video processing", PostPrivacy.Public, true, PostHiddenReasonType.VideoProcessing);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(author.Id, blockedUserIds, followingIds)
            .OrderBy(p => p.Id)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(3); // All except permanently hidden post
        result.Should().Contain(p => p.Id == publicPost.Id);
        result.Should().Contain(p => p.Id == followersPost.Id);
        result.Should().Contain(p => p.Id == videoProcessingPost.Id); // Visible to author
        result.Should().NotContain(p => p.Id == hiddenPost.Id); // Permanently hidden
    }

    [Fact]
    public async Task PublicTimelineFiltering_ShouldOnlyShowAppropriateContent()
    {
        // Arrange
        var activeUser = await CreateUserAsync("active", UserStatus.Active, 0.8f);
        var suspendedUser = await CreateUserAsync("suspended", UserStatus.Suspended, 0.9f);
        var lowTrustUser = await CreateUserAsync("lowtrust", UserStatus.Active, 0.05f);

        var goodPost = await CreatePostAsync(activeUser, "Good post", PostPrivacy.Public);
        var suspendedPost = await CreatePostAsync(suspendedUser, "Suspended post", PostPrivacy.Public);
        var lowTrustPost = await CreatePostAsync(lowTrustUser, "Low trust post", PostPrivacy.Public);
        var followersOnlyPost = await CreatePostAsync(activeUser, "Followers only", PostPrivacy.Followers);
        var hiddenPost = await CreatePostAsync(activeUser, "Hidden post", PostPrivacy.Public, true, PostHiddenReasonType.ContentModerationHidden);

        var blockedUserIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .OrderBy(p => p.Id)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1); // Only the good public post
        result.Should().Contain(p => p.Id == goodPost.Id);
        result.Should().NotContain(p => p.Id == suspendedPost.Id);
        result.Should().NotContain(p => p.Id == lowTrustPost.Id);
        result.Should().NotContain(p => p.Id == followersOnlyPost.Id);
        result.Should().NotContain(p => p.Id == hiddenPost.Id);
    }

    [Fact]
    public async Task VideoProcessingSpecialCase_ShouldWorkCorrectly()
    {
        // Arrange
        var author = await CreateUserAsync("author", UserStatus.Active, 0.8f);
        var viewer = await CreateUserAsync("viewer", UserStatus.Active, 0.8f);

        var videoPost = await CreatePostAsync(author, "Video post", PostPrivacy.Public, true, PostHiddenReasonType.VideoProcessing);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act - Author viewing
        var authorResult = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(author.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Act - Other user viewing
        var viewerResult = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(viewer.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Act - Public timeline
        var publicResult = await _context.Posts
            .Include(p => p.User)
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        authorResult.Should().HaveCount(1); // Author can see their video processing post
        authorResult.Should().Contain(p => p.Id == videoPost.Id);

        viewerResult.Should().BeEmpty(); // Other users cannot see video processing posts

        publicResult.Should().BeEmpty(); // Video processing posts not in public timeline
    }

    [Fact]
    public async Task FollowersOnlyPostVisibility_ShouldRespectFollowingRelationships()
    {
        // Arrange
        var author = await CreateUserAsync("author", UserStatus.Active, 0.8f);
        var follower = await CreateUserAsync("follower", UserStatus.Active, 0.8f);
        var nonFollower = await CreateUserAsync("nonfollower", UserStatus.Active, 0.8f);

        var followersPost = await CreatePostAsync(author, "Followers only", PostPrivacy.Followers);

        var blockedUserIds = new HashSet<int>();
        var followerFollowingIds = new HashSet<int> { author.Id };
        var nonFollowerFollowingIds = new HashSet<int>();

        // Act - Follower viewing
        var followerResult = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(follower.Id, blockedUserIds, followerFollowingIds)
            .ToListAsync();

        // Act - Non-follower viewing
        var nonFollowerResult = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(nonFollower.Id, blockedUserIds, nonFollowerFollowingIds)
            .ToListAsync();

        // Act - Author viewing
        var authorResult = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(author.Id, blockedUserIds, new HashSet<int>())
            .ToListAsync();

        // Assert
        followerResult.Should().HaveCount(1); // Follower can see the post
        followerResult.Should().Contain(p => p.Id == followersPost.Id);

        nonFollowerResult.Should().BeEmpty(); // Non-follower cannot see the post

        authorResult.Should().HaveCount(1); // Author can always see their own posts
        authorResult.Should().Contain(p => p.Id == followersPost.Id);
    }

    #endregion

    #region Performance and Edge Case Tests

    [Fact]
    public async Task LargeDatasetFiltering_ShouldPerformEfficiently()
    {
        // Arrange - Create a larger dataset
        var users = new List<User>();
        var posts = new List<Post>();

        for (int i = 0; i < 50; i++)
        {
            var user = await CreateUserAsync($"user{i}", UserStatus.Active, 0.5f + (i % 5) * 0.1f);
            users.Add(user);

            for (int j = 0; j < 5; j++)
            {
                var post = await CreatePostAsync(user, $"Post {j} by user {i}", PostPrivacy.Public);
                posts.Add(post);
            }
        }

        var currentUser = users[0];
        var blockedUserIds = new HashSet<int> { users[1].Id, users[2].Id };
        var followingIds = new HashSet<int> { users[3].Id, users[4].Id };

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(currentUser.Id, blockedUserIds, followingIds)
            .ToListAsync();
        var endTime = DateTime.UtcNow;

        // Assert
        result.Should().NotBeEmpty();
        result.Should().NotContain(p => blockedUserIds.Contains(p.UserId));
        
        // Performance assertion - should complete reasonably quickly
        var duration = endTime - startTime;
        duration.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EmptyDatabase_ShouldReturnEmptyResults()
    {
        // Arrange - Empty database
        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(1, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NullCurrentUser_ShouldOnlyShowPublicPosts()
    {
        // Arrange
        var user = await CreateUserAsync("user", UserStatus.Active, 0.8f);
        var publicPost = await CreatePostAsync(user, "Public post", PostPrivacy.Public);
        var followersPost = await CreatePostAsync(user, "Followers post", PostPrivacy.Followers);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(null, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == publicPost.Id);
        result.Should().NotContain(p => p.Id == followersPost.Id);
    }

    #endregion

    #region Helper Methods

    private async Task<User> CreateUserAsync(
        string username,
        UserStatus status = UserStatus.Active,
        float trustScore = 1.0f)
    {
        var user = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            EmailVerified = true,
            Status = status,
            TrustScore = trustScore,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastSeenAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Post> CreatePostAsync(
        User user,
        string content = "Test post content",
        PostPrivacy privacy = PostPrivacy.Public,
        bool isHidden = false,
        PostHiddenReasonType hiddenReasonType = PostHiddenReasonType.None)
    {
        var post = new Post
        {
            Content = content,
            UserId = user.Id,
            User = user,
            Privacy = privacy,
            IsHidden = isHidden,
            HiddenReasonType = hiddenReasonType,
            HiddenAt = isHidden ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
