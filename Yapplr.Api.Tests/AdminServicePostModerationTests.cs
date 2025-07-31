using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Unified;

namespace Yapplr.Api.Tests;

/// <summary>
/// Tests for AdminService post moderation functionality with the new hiding system
/// </summary>
public class AdminServicePostModerationTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IModerationMessageService> _mockModerationMessageService;
    private readonly Mock<ITrustScoreService> _mockTrustScoreService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<AdminService>> _mockLogger;
    private readonly AdminService _adminService;

    public AdminServicePostModerationTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestYapplrDbContext(options);
        _mockNotificationService = new Mock<INotificationService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockModerationMessageService = new Mock<IModerationMessageService>();
        _mockTrustScoreService = new Mock<ITrustScoreService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<AdminService>>();

        _adminService = new AdminService(
            _context,
            _mockAuditService.Object,
            _mockNotificationService.Object,
            _mockTrustScoreService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);
    }

    #region Test Data Setup

    private async Task<User> CreateTestUserAsync(string username = "testuser", UserRole role = UserRole.User)
    {
        var user = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            EmailVerified = true,
            Status = UserStatus.Active,
            Role = role,
            TrustScore = 1.0f,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastSeenAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Post> CreateTestPostAsync(User user, string content = "Test post content")
    {
        var post = new Post
        {
            Content = content,
            UserId = user.Id,
            User = user,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    #endregion

    #region Hide Post Tests

    [Fact]
    public async Task HidePostAsync_ValidPost_ShouldHidePostCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var moderator = await CreateTestUserAsync("moderator", UserRole.Moderator);
        var post = await CreateTestPostAsync(user);
        var reason = "Inappropriate content";

        // Act
        var result = await _adminService.HidePostAsync(post.Id, moderator.Id, reason);

        // Assert
        result.Should().BeTrue();

        var updatedPost = await _context.Posts.FindAsync(post.Id);
        updatedPost.Should().NotBeNull();
        updatedPost!.IsHidden.Should().BeTrue();
        updatedPost.HiddenReasonType.Should().Be(PostHiddenReasonType.ModeratorHidden);
        updatedPost.HiddenReason.Should().Be(reason);
        updatedPost.HiddenByUserId.Should().Be(moderator.Id);
        updatedPost.HiddenAt.Should().NotBeNull();
        updatedPost.HiddenAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Note: Audit service verification would be done here in a real test
    }

    [Fact]
    public async Task HidePostAsync_NonExistentPost_ShouldReturnFalse()
    {
        // Arrange
        var moderator = await CreateTestUserAsync("moderator", UserRole.Moderator);
        var nonExistentPostId = 999;
        var reason = "Test reason";

        // Act
        var result = await _adminService.HidePostAsync(nonExistentPostId, moderator.Id, reason);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HidePostAsync_NonExistentModerator_ShouldReturnFalse()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post = await CreateTestPostAsync(user);
        var nonExistentModeratorId = 999;
        var reason = "Test reason";

        // Act
        var result = await _adminService.HidePostAsync(post.Id, nonExistentModeratorId, reason);

        // Assert
        result.Should().BeFalse();

        var updatedPost = await _context.Posts.FindAsync(post.Id);
        updatedPost!.IsHidden.Should().BeFalse();
    }

    #endregion

    #region Unhide Post Tests

    [Fact]
    public async Task UnhidePostAsync_HiddenPost_ShouldUnhidePostCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var moderator = await CreateTestUserAsync("moderator", UserRole.Moderator);
        var post = await CreateTestPostAsync(user);

        // First hide the post
        post.IsHidden = true;
        post.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
        post.HiddenReason = "Test reason";
        post.HiddenByUserId = moderator.Id;
        post.HiddenAt = DateTime.UtcNow;
        _context.Posts.Update(post);
        await _context.SaveChangesAsync();

        // Act
        var result = await _adminService.UnhidePostAsync(post.Id);

        // Assert
        result.Should().BeTrue();

        var updatedPost = await _context.Posts.FindAsync(post.Id);
        updatedPost.Should().NotBeNull();
        updatedPost!.IsHidden.Should().BeFalse();
        updatedPost.HiddenReasonType.Should().Be(PostHiddenReasonType.None);
        updatedPost.HiddenReason.Should().BeNull();
        updatedPost.HiddenByUserId.Should().BeNull();
        updatedPost.HiddenAt.Should().BeNull();
    }

    [Fact]
    public async Task UnhidePostAsync_NonExistentPost_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentPostId = 999;

        // Act
        var result = await _adminService.UnhidePostAsync(nonExistentPostId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Bulk Hide Posts Tests

    [Fact]
    public async Task BulkHidePostsAsync_ValidPosts_ShouldHideAllPosts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var moderator = await CreateTestUserAsync("moderator", UserRole.Moderator);
        var post1 = await CreateTestPostAsync(user, "Post 1");
        var post2 = await CreateTestPostAsync(user, "Post 2");
        var post3 = await CreateTestPostAsync(user, "Post 3");

        var postIds = new[] { post1.Id, post2.Id, post3.Id };
        var reason = "Bulk moderation action";

        // Act
        var result = await _adminService.BulkHidePostsAsync(postIds, moderator.Id, reason);

        // Assert
        result.Should().Be(3);

        var updatedPosts = await _context.Posts
            .Where(p => postIds.Contains(p.Id))
            .ToListAsync();

        updatedPosts.Should().HaveCount(3);
        updatedPosts.Should().OnlyContain(p => p.IsHidden);
        updatedPosts.Should().OnlyContain(p => p.HiddenReasonType == PostHiddenReasonType.ModeratorHidden);
        updatedPosts.Should().OnlyContain(p => p.HiddenReason == reason);
        updatedPosts.Should().OnlyContain(p => p.HiddenByUserId == moderator.Id);
        updatedPosts.Should().OnlyContain(p => p.HiddenAt.HasValue);
    }

    [Fact]
    public async Task BulkHidePostsAsync_SomeAlreadyHidden_ShouldOnlyHideVisiblePosts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var moderator = await CreateTestUserAsync("moderator", UserRole.Moderator);
        var visiblePost = await CreateTestPostAsync(user, "Visible post");
        var hiddenPost = await CreateTestPostAsync(user, "Hidden post");

        // Hide one post first
        hiddenPost.IsHidden = true;
        hiddenPost.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
        hiddenPost.HiddenReason = "Already hidden";
        hiddenPost.HiddenByUserId = moderator.Id;
        hiddenPost.HiddenAt = DateTime.UtcNow;
        _context.Posts.Update(hiddenPost);
        await _context.SaveChangesAsync();

        var postIds = new[] { visiblePost.Id, hiddenPost.Id };
        var reason = "Bulk moderation action";

        // Act
        var result = await _adminService.BulkHidePostsAsync(postIds, moderator.Id, reason);

        // Assert
        result.Should().Be(1); // Only one post was actually hidden

        var updatedVisiblePost = await _context.Posts.FindAsync(visiblePost.Id);
        updatedVisiblePost!.IsHidden.Should().BeTrue();
        updatedVisiblePost.HiddenReason.Should().Be(reason);

        var updatedHiddenPost = await _context.Posts.FindAsync(hiddenPost.Id);
        updatedHiddenPost!.HiddenReason.Should().Be("Already hidden"); // Should remain unchanged
    }

    [Fact]
    public async Task BulkHidePostsAsync_EmptyList_ShouldReturnZero()
    {
        // Arrange
        var moderator = await CreateTestUserAsync("moderator", UserRole.Moderator);
        var emptyPostIds = Array.Empty<int>();
        var reason = "Test reason";

        // Act
        var result = await _adminService.BulkHidePostsAsync(emptyPostIds, moderator.Id, reason);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Get Posts for Moderation Tests

    [Fact]
    public async Task GetPostsForModerationAsync_WithHiddenFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var visiblePost = await CreateTestPostAsync(user, "Visible post");
        var hiddenPost = await CreateTestPostAsync(user, "Hidden post");

        hiddenPost.IsHidden = true;
        hiddenPost.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
        hiddenPost.HiddenReason = "Test reason";
        hiddenPost.HiddenAt = DateTime.UtcNow;
        _context.Posts.Update(hiddenPost);
        await _context.SaveChangesAsync();

        // Act - Get only hidden posts
        var hiddenResults = await _adminService.GetPostsForModerationAsync(1, 25, true);

        // Act - Get only visible posts
        var visibleResults = await _adminService.GetPostsForModerationAsync(1, 25, false);

        // Act - Get all posts
        var allResults = await _adminService.GetPostsForModerationAsync(1, 25, null);

        // Assert
        hiddenResults.Should().HaveCount(1);
        hiddenResults.First().Id.Should().Be(hiddenPost.Id);
        hiddenResults.First().IsHidden.Should().BeTrue();

        visibleResults.Should().HaveCount(1);
        visibleResults.First().Id.Should().Be(visiblePost.Id);
        visibleResults.First().IsHidden.Should().BeFalse();

        allResults.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPostsForModerationAsync_ShouldIncludeHiddenReasonInformation()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var moderator = await CreateTestUserAsync("moderator", UserRole.Moderator);
        var post = await CreateTestPostAsync(user);

        post.IsHidden = true;
        post.HiddenReasonType = PostHiddenReasonType.ContentModerationHidden;
        post.HiddenReason = "AI detected inappropriate content";
        post.HiddenByUserId = moderator.Id;
        post.HiddenAt = DateTime.UtcNow;
        _context.Posts.Update(post);
        await _context.SaveChangesAsync();

        // Act
        var results = await _adminService.GetPostsForModerationAsync(1, 25, true);

        // Assert
        results.Should().HaveCount(1);
        var result = results.First();
        result.IsHidden.Should().BeTrue();
        result.HiddenReason.Should().Be("AI detected inappropriate content");
        result.HiddenAt.Should().NotBeNull();
        result.HiddenByUsername.Should().Be(moderator.Username);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetModerationStatsAsync_ShouldIncludeHiddenPostCounts()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var visiblePost1 = await CreateTestPostAsync(user, "Visible 1");
        var visiblePost2 = await CreateTestPostAsync(user, "Visible 2");
        var hiddenPost1 = await CreateTestPostAsync(user, "Hidden 1");
        var hiddenPost2 = await CreateTestPostAsync(user, "Hidden 2");

        // Hide some posts
        hiddenPost1.IsHidden = true;
        hiddenPost1.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
        hiddenPost2.IsHidden = true;
        hiddenPost2.HiddenReasonType = PostHiddenReasonType.ContentModerationHidden;
        
        _context.Posts.UpdateRange(hiddenPost1, hiddenPost2);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _adminService.GetModerationStatsAsync();

        // Assert
        stats.TotalPosts.Should().Be(4);
        stats.HiddenPosts.Should().Be(2);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
