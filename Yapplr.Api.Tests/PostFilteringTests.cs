using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Common;

namespace Yapplr.Api.Tests;

/// <summary>
/// Comprehensive tests for the hybrid post filtering system
/// Tests both permanent post-level hiding and real-time user status checks
/// </summary>
public class PostFilteringTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<ILogger<PostFilteringTests>> _mockLogger;

    public PostFilteringTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestYapplrDbContext(options);
        _mockLogger = new Mock<ILogger<PostFilteringTests>>();
    }

    #region Test Data Setup

    private async Task<User> CreateTestUserAsync(
        string username = "testuser",
        UserStatus status = UserStatus.Active,
        float trustScore = 1.0f,
        bool emailVerified = true)
    {
        var user = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            EmailVerified = emailVerified,
            Status = status,
            TrustScore = trustScore,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastSeenAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Post> CreateTestPostAsync(
        User user,
        string content = "Test post content",
        PostPrivacy privacy = PostPrivacy.Public,
        bool isHidden = false,
        PostHiddenReasonType hiddenReasonType = PostHiddenReasonType.None,
        string? hiddenReason = null,
        bool isDeletedByUser = false)
    {
        var post = new Post
        {
            Content = content,
            UserId = user.Id,
            User = user,
            Privacy = privacy,
            IsHidden = isHidden,
            HiddenReasonType = hiddenReasonType,
            HiddenReason = hiddenReason,
            HiddenAt = isHidden ? DateTime.UtcNow : null,
            IsDeletedByUser = isDeletedByUser,
            DeletedByUserAt = isDeletedByUser ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    #endregion

    #region Permanent Post-Level Hiding Tests

    [Fact]
    public async Task ApplyVisibilityFilters_HiddenPost_ShouldBeFiltered()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var hiddenPost = await CreateTestPostAsync(user, isHidden: true, hiddenReasonType: PostHiddenReasonType.ModeratorHidden);
        var visiblePost = await CreateTestPostAsync(user);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(null, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(p => p.Id == hiddenPost.Id);
        result.Should().Contain(p => p.Id == visiblePost.Id);
    }

    [Fact]
    public async Task ApplyVisibilityFilters_VideoProcessingPost_ShouldBeVisibleToAuthor()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var videoPost = await CreateTestPostAsync(user, 
            isHidden: true, 
            hiddenReasonType: PostHiddenReasonType.VideoProcessing);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act - Author viewing
        var resultForAuthor = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(user.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Act - Other user viewing
        var resultForOther = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(999, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        resultForAuthor.Should().HaveCount(1);
        resultForAuthor.Should().Contain(p => p.Id == videoPost.Id);
        
        resultForOther.Should().BeEmpty();
    }

    [Theory]
    [InlineData(PostHiddenReasonType.DeletedByUser)]
    [InlineData(PostHiddenReasonType.ModeratorHidden)]
    [InlineData(PostHiddenReasonType.ContentModerationHidden)]
    [InlineData(PostHiddenReasonType.SpamDetection)]
    [InlineData(PostHiddenReasonType.MaliciousContent)]
    public async Task ApplyVisibilityFilters_PermanentlyHiddenPosts_ShouldBeFiltered(PostHiddenReasonType reasonType)
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var hiddenPost = await CreateTestPostAsync(user, isHidden: true, hiddenReasonType: reasonType);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(user.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Real-Time User Status Tests

    [Theory]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Banned)]
    [InlineData(UserStatus.ShadowBanned)]
    public async Task ApplyVisibilityFilters_InactiveUserPosts_ShouldBeFiltered(UserStatus userStatus)
    {
        // Arrange
        var inactiveUser = await CreateTestUserAsync("inactiveuser", status: userStatus);
        var activeUser = await CreateTestUserAsync("activeuser", status: UserStatus.Active);
        
        var inactiveUserPost = await CreateTestPostAsync(inactiveUser);
        var activeUserPost = await CreateTestPostAsync(activeUser);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(null, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(p => p.Id == inactiveUserPost.Id);
        result.Should().Contain(p => p.Id == activeUserPost.Id);
    }

    [Fact]
    public async Task ApplyVisibilityFilters_LowTrustScorePosts_ShouldBeFilteredExceptForAuthor()
    {
        // Arrange
        var lowTrustUser = await CreateTestUserAsync("lowtrustuser", trustScore: 0.05f);
        var highTrustUser = await CreateTestUserAsync("hightrustuser", trustScore: 0.8f);
        
        var lowTrustPost = await CreateTestPostAsync(lowTrustUser);
        var highTrustPost = await CreateTestPostAsync(highTrustUser);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act - Other user viewing
        var resultForOther = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(999, blockedUserIds, followingIds)
            .ToListAsync();

        // Act - Low trust user viewing their own posts
        var resultForAuthor = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(lowTrustUser.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        resultForOther.Should().HaveCount(1);
        resultForOther.Should().NotContain(p => p.Id == lowTrustPost.Id);
        resultForOther.Should().Contain(p => p.Id == highTrustPost.Id);

        resultForAuthor.Should().HaveCount(2);
        resultForAuthor.Should().Contain(p => p.Id == lowTrustPost.Id);
        resultForAuthor.Should().Contain(p => p.Id == highTrustPost.Id);
    }

    #endregion

    #region User-Specific Filtering Tests

    [Fact]
    public async Task ApplyVisibilityFilters_BlockedUserPosts_ShouldBeFiltered()
    {
        // Arrange
        var blockedUser = await CreateTestUserAsync("blockeduser");
        var normalUser = await CreateTestUserAsync("normaluser");
        var currentUser = await CreateTestUserAsync("currentuser");
        
        var blockedUserPost = await CreateTestPostAsync(blockedUser);
        var normalUserPost = await CreateTestPostAsync(normalUser);

        var blockedUserIds = new HashSet<int> { blockedUser.Id };
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(currentUser.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(p => p.Id == blockedUserPost.Id);
        result.Should().Contain(p => p.Id == normalUserPost.Id);
    }

    [Theory]
    [InlineData(PostPrivacy.Public, true)]
    [InlineData(PostPrivacy.Followers, false)]
    public async Task ApplyVisibilityFilters_PrivacySettings_ShouldRespectFollowingStatus(PostPrivacy privacy, bool shouldBeVisible)
    {
        // Arrange
        var postAuthor = await CreateTestUserAsync("author");
        var viewer = await CreateTestUserAsync("viewer");
        
        var post = await CreateTestPostAsync(postAuthor, privacy: privacy);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>(); // Viewer is not following author

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(viewer.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        if (shouldBeVisible)
        {
            result.Should().HaveCount(1);
            result.Should().Contain(p => p.Id == post.Id);
        }
        else
        {
            result.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task ApplyVisibilityFilters_FollowersOnlyPost_ShouldBeVisibleToFollowers()
    {
        // Arrange
        var postAuthor = await CreateTestUserAsync("author");
        var follower = await CreateTestUserAsync("follower");
        var nonFollower = await CreateTestUserAsync("nonfollower");
        
        var followersOnlyPost = await CreateTestPostAsync(postAuthor, privacy: PostPrivacy.Followers);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int> { postAuthor.Id }; // Follower is following author

        // Act - Follower viewing
        var resultForFollower = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(follower.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Act - Non-follower viewing
        var resultForNonFollower = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(nonFollower.Id, blockedUserIds, new HashSet<int>())
            .ToListAsync();

        // Assert
        resultForFollower.Should().HaveCount(1);
        resultForFollower.Should().Contain(p => p.Id == followersOnlyPost.Id);

        resultForNonFollower.Should().BeEmpty();
    }

    #endregion

    #region QueryFilters Extension Method Tests

    [Fact]
    public async Task QueryFilters_ForUser_ShouldApplyAllFiltersCorrectly()
    {
        // Arrange
        var activeUser = await CreateTestUserAsync("active", UserStatus.Active, 0.8f);
        var suspendedUser = await CreateTestUserAsync("suspended", UserStatus.Suspended, 0.9f);
        var lowTrustUser = await CreateTestUserAsync("lowtrust", UserStatus.Active, 0.05f);
        var blockedUser = await CreateTestUserAsync("blocked", UserStatus.Active, 0.7f);
        var currentUser = await CreateTestUserAsync("current", UserStatus.Active, 0.6f);

        var visiblePost = await CreateTestPostAsync(activeUser);
        var suspendedPost = await CreateTestPostAsync(suspendedUser);
        var lowTrustPost = await CreateTestPostAsync(lowTrustUser);
        var blockedPost = await CreateTestPostAsync(blockedUser);
        var hiddenPost = await CreateTestPostAsync(activeUser, isHidden: true, hiddenReasonType: PostHiddenReasonType.ModeratorHidden);

        var blockedUserIds = new List<int> { blockedUser.Id };
        var followingUserIds = new List<int> { activeUser.Id };

        // Act
        var queryBuilder = new QueryBuilder<Post>(_context.Posts.Include(p => p.User), _context);
        var result = await queryBuilder
            .ForUser(currentUser.Id, blockedUserIds, followingUserIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == visiblePost.Id);
        result.Should().NotContain(p => p.Id == suspendedPost.Id);
        result.Should().NotContain(p => p.Id == lowTrustPost.Id);
        result.Should().NotContain(p => p.Id == blockedPost.Id);
        result.Should().NotContain(p => p.Id == hiddenPost.Id);
    }

    [Fact]
    public async Task QueryFilters_ForUser_VideoProcessingPost_ShouldBeVisibleToAuthor()
    {
        // Arrange
        var author = await CreateTestUserAsync("author", UserStatus.Active, 0.8f);
        var viewer = await CreateTestUserAsync("viewer", UserStatus.Active, 0.8f);

        var videoPost = await CreateTestPostAsync(author, isHidden: true, hiddenReasonType: PostHiddenReasonType.VideoProcessing);

        // Act - Author viewing
        var authorQueryBuilder = new QueryBuilder<Post>(_context.Posts.Include(p => p.User), _context);
        var authorResult = await authorQueryBuilder
            .ForUser(author.Id)
            .ToListAsync();

        // Act - Viewer viewing
        var viewerQueryBuilder = new QueryBuilder<Post>(_context.Posts.Include(p => p.User), _context);
        var viewerResult = await viewerQueryBuilder
            .ForUser(viewer.Id)
            .ToListAsync();

        // Assert
        authorResult.Should().HaveCount(1);
        authorResult.Should().Contain(p => p.Id == videoPost.Id);

        viewerResult.Should().BeEmpty();
    }

    #endregion

    [Fact]
    public async Task ApplyVisibilityFilters_WithoutFeedFilters_ShouldIncludeReposts()
    {
        // Arrange
        var user = await CreateTestUserAsync("testuser");

        // Create original post
        var originalPost = await CreateTestPostAsync(user, content: "Original post content");

        // Create repost
        var repost = new Post
        {
            Content = "This is my repost comment",
            UserId = user.Id,
            PostType = PostType.Repost,
            RepostedPostId = originalPost.Id,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(repost);
        await _context.SaveChangesAsync();

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act - Test with feed filters (should exclude reposts)
        var feedResults = await _context.GetPostsWithIncludes()
            .Where(p => p.Id == repost.Id)
            .ApplyVisibilityFilters(user.Id, blockedUserIds, followingIds, applyFeedFilters: true)
            .ToListAsync();

        // Act - Test without feed filters (should include reposts)
        var individualResults = await _context.GetPostsWithIncludes()
            .Where(p => p.Id == repost.Id)
            .ApplyVisibilityFilters(user.Id, blockedUserIds, followingIds, applyFeedFilters: false)
            .ToListAsync();

        // Assert
        Assert.Empty(feedResults); // Feed filters should exclude reposts
        Assert.Single(individualResults); // Individual access should include reposts

        var result = individualResults.First();
        Assert.Equal(repost.Id, result.Id);
        Assert.Equal(PostType.Repost, result.PostType);
        Assert.Equal("This is my repost comment", result.Content);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
