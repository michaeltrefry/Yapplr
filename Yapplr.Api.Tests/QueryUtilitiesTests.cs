using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Common;
using System.Security.Claims;

namespace Yapplr.Api.Tests;

/// <summary>
/// Tests for QueryUtilities extension methods used in post filtering
/// </summary>
public class QueryUtilitiesTests : IDisposable
{
    private readonly YapplrDbContext _context;

    public QueryUtilitiesTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestYapplrDbContext(options);
    }

    #region Test Data Setup

    private async Task<User> CreateTestUserAsync(
        string username = "testuser",
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

    private ClaimsPrincipal CreateTestClaimsPrincipal(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    private async Task<Post> CreateTestPostAsync(
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

    #region GetPostsWithIncludes Tests

    [Fact]
    public async Task GetPostsWithIncludes_ShouldIncludeAllRequiredNavigationProperties()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post = await CreateTestPostAsync(user);

        // Act
        var result = await _context.GetPostsWithIncludes()
            .FirstOrDefaultAsync(p => p.Id == post.Id);

        // Assert
        result.Should().NotBeNull();
        result!.User.Should().NotBeNull();
        result.User.Username.Should().Be(user.Username);
        result.Likes.Should().NotBeNull();
        result.Children.Should().NotBeNull();
        result.Reposts.Should().NotBeNull();
        result.PostTags.Should().NotBeNull();
        result.PostLinkPreviews.Should().NotBeNull();
        result.PostMedia.Should().NotBeNull();
        result.PostSystemTags.Should().NotBeNull();
    }

    #endregion

    #region ApplyPublicVisibilityFilters Tests

    [Fact]
    public async Task ApplyPublicVisibilityFilters_ShouldOnlyReturnPublicPosts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var publicPost = await CreateTestPostAsync(user, privacy: PostPrivacy.Public);
        var followersPost = await CreateTestPostAsync(user, privacy: PostPrivacy.Followers);

        var blockedUserIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == publicPost.Id);
        result.Should().NotContain(p => p.Id == followersPost.Id);
    }

    [Fact]
    public async Task ApplyPublicVisibilityFilters_ShouldFilterHiddenPosts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var visiblePost = await CreateTestPostAsync(user);
        var hiddenPost = await CreateTestPostAsync(user, isHidden: true, hiddenReasonType: PostHiddenReasonType.ModeratorHidden);

        var blockedUserIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == visiblePost.Id);
        result.Should().NotContain(p => p.Id == hiddenPost.Id);
    }

    [Fact]
    public async Task ApplyPublicVisibilityFilters_ShouldFilterLowTrustScorePosts()
    {
        // Arrange
        var lowTrustUser = await CreateTestUserAsync("lowtrustuser", trustScore: 0.05f);
        var highTrustUser = await CreateTestUserAsync("hightrustuser", trustScore: 0.8f);
        
        var lowTrustPost = await CreateTestPostAsync(lowTrustUser);
        var highTrustPost = await CreateTestPostAsync(highTrustUser);

        var blockedUserIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(p => p.Id == lowTrustPost.Id);
        result.Should().Contain(p => p.Id == highTrustPost.Id);
    }

    [Fact]
    public async Task ApplyPublicVisibilityFilters_ShouldFilterInactiveUsers()
    {
        // Arrange
        var activeUser = await CreateTestUserAsync("activeuser", status: UserStatus.Active);
        var suspendedUser = await CreateTestUserAsync("suspendeduser", status: UserStatus.Suspended);
        
        var activeUserPost = await CreateTestPostAsync(activeUser);
        var suspendedUserPost = await CreateTestPostAsync(suspendedUser);

        var blockedUserIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == activeUserPost.Id);
        result.Should().NotContain(p => p.Id == suspendedUserPost.Id);
    }

    [Fact]
    public async Task ApplyPublicVisibilityFilters_ShouldFilterBlockedUsers()
    {
        // Arrange
        var normalUser = await CreateTestUserAsync("normaluser");
        var blockedUser = await CreateTestUserAsync("blockeduser");
        
        var normalPost = await CreateTestPostAsync(normalUser);
        var blockedPost = await CreateTestPostAsync(blockedUser);

        var blockedUserIds = new HashSet<int> { blockedUser.Id };

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == normalPost.Id);
        result.Should().NotContain(p => p.Id == blockedPost.Id);
    }

    #endregion

    #region GetBlockedUserIdsAsync Tests

    [Fact]
    public async Task GetBlockedUserIdsAsync_ShouldReturnCorrectBlockedUsers()
    {
        // Arrange
        var blocker = await CreateTestUserAsync("blocker");
        var blocked1 = await CreateTestUserAsync("blocked1");
        var blocked2 = await CreateTestUserAsync("blocked2");
        var notBlocked = await CreateTestUserAsync("notblocked");

        _context.Blocks.AddRange(
            new Block { BlockerId = blocker.Id, BlockedId = blocked1.Id, CreatedAt = DateTime.UtcNow },
            new Block { BlockerId = blocker.Id, BlockedId = blocked2.Id, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _context.GetBlockedUserIdsAsync(blocker.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(blocked1.Id);
        result.Should().Contain(blocked2.Id);
        result.Should().NotContain(notBlocked.Id);
    }

    [Fact]
    public async Task GetBlockedUserIdsAsync_NoBlocks_ShouldReturnEmptySet()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        var result = await _context.GetBlockedUserIdsAsync(user.Id);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetFollowingUserIdsAsync Tests

    [Fact]
    public async Task GetFollowingUserIdsAsync_ShouldReturnCorrectFollowedUsers()
    {
        // Arrange
        var follower = await CreateTestUserAsync("follower");
        var followed1 = await CreateTestUserAsync("followed1");
        var followed2 = await CreateTestUserAsync("followed2");
        var notFollowed = await CreateTestUserAsync("notfollowed");

        _context.Follows.AddRange(
            new Follow { FollowerId = follower.Id, FollowingId = followed1.Id, CreatedAt = DateTime.UtcNow },
            new Follow { FollowerId = follower.Id, FollowingId = followed2.Id, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _context.GetFollowingUserIdsAsync(follower.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(followed1.Id);
        result.Should().Contain(followed2.Id);
        result.Should().NotContain(notFollowed.Id);
    }

    [Fact]
    public async Task GetFollowingUserIdsAsync_NoFollows_ShouldReturnEmptySet()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        var result = await _context.GetFollowingUserIdsAsync(user.Id);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CanViewHiddenContent Tests

    [Fact]
    public async Task CanViewHiddenContent_ContentOwner_ShouldReturnTrue()
    {
        // Arrange
        var owner = await CreateTestUserAsync("owner");
        var user = CreateTestClaimsPrincipal(owner);

        // Act
        var result = QueryUtilities.CanViewHiddenContent(user, owner.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanViewHiddenContent_Admin_ShouldReturnTrue()
    {
        // Arrange
        var admin = await CreateTestUserAsync("admin");
        admin.Role = UserRole.Admin;
        _context.Users.Update(admin);
        await _context.SaveChangesAsync();

        var contentOwner = await CreateTestUserAsync("owner");
        var user = CreateTestClaimsPrincipal(admin);

        // Act
        var result = QueryUtilities.CanViewHiddenContent(user, contentOwner.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanViewHiddenContent_Moderator_ShouldReturnTrue()
    {
        // Arrange
        var moderator = await CreateTestUserAsync("moderator");
        moderator.Role = UserRole.Moderator;
        _context.Users.Update(moderator);
        await _context.SaveChangesAsync();

        var contentOwner = await CreateTestUserAsync("owner");
        var user = CreateTestClaimsPrincipal(moderator);

        // Act
        var result = QueryUtilities.CanViewHiddenContent(user, contentOwner.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanViewHiddenContent_RegularUser_ShouldReturnFalse()
    {
        // Arrange
        var regularUser = await CreateTestUserAsync("regular");
        var contentOwner = await CreateTestUserAsync("owner");
        var user = CreateTestClaimsPrincipal(regularUser);

        // Act
        var result = QueryUtilities.CanViewHiddenContent(user, contentOwner.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanViewHiddenContent_NoCurrentUser_ShouldReturnFalse()
    {
        // Arrange
        var user = new ClaimsPrincipal(); // Empty claims principal
        var contentOwnerId = 1;

        // Act
        var result = QueryUtilities.CanViewHiddenContent(user, contentOwnerId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
