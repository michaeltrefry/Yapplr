using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Unified;
using Xunit;

namespace Yapplr.Api.Tests;

public class TrendingFilteringTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly TagService _tagService;
    private readonly TagAnalyticsService _tagAnalyticsService;
    private readonly AdminService _adminService;

    public TrendingFilteringTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        
        // Create mock services
        var mockHttpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
        var mockBlockService = new Mock<IBlockService>();
        var mockCountCache = new Mock<ICountCacheService>();
        var mockAnalyticsService = new Mock<IAnalyticsService>();
        var mockLogger = new Mock<ILogger<TagAnalyticsService>>();
        var mockAuditService = new Mock<IAuditService>();
        var mockNotificationService = new Mock<IUnifiedNotificationService>();
        var mockModerationMessageService = new Mock<IModerationMessageService>();
        var mockTrustScoreService = new Mock<ITrustScoreService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockAdminLogger = new Mock<ILogger<AdminService>>();

        _tagService = new TagService(_context, mockHttpContextAccessor, mockBlockService.Object, mockCountCache.Object);
        _tagAnalyticsService = new TagAnalyticsService(_context, mockAnalyticsService.Object, mockLogger.Object);
        _adminService = new AdminService(_context, mockAuditService.Object, mockNotificationService.Object, mockTrustScoreService.Object, mockServiceProvider.Object, mockAdminLogger.Object);
    }

    [Fact]
    public async Task GetTrendingTagsAsync_ShouldFilterOutDeletedPosts()
    {
        // Arrange
        var activeUser = new User
        {
            Id = 1,
            Username = "activeuser",
            Email = "active@test.com",
            Status = UserStatus.Active,
            TrustScore = 1.0f
        };

        var suspendedUser = new User
        {
            Id = 2,
            Username = "suspendeduser",
            Email = "suspended@test.com",
            Status = UserStatus.Suspended,
            TrustScore = 1.0f
        };

        var tag = new Tag
        {
            Id = 1,
            Name = "testtag",
            PostCount = 3 // This will include deleted posts initially
        };

        var visiblePost = new Post
        {
            Id = 1,
            Content = "Visible post #testtag",
            UserId = activeUser.Id,
            User = activeUser,
            IsHidden = false,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var deletedPost = new Post
        {
            Id = 2,
            Content = "Deleted post #testtag",
            UserId = activeUser.Id,
            User = activeUser,
            IsHidden = true,
            HiddenReasonType = PostHiddenReasonType.DeletedByUser,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var suspendedUserPost = new Post
        {
            Id = 3,
            Content = "Post from suspended user #testtag",
            UserId = suspendedUser.Id,
            User = suspendedUser,
            IsHidden = false,
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };

        _context.Users.AddRange(activeUser, suspendedUser);
        _context.Tags.Add(tag);
        _context.Posts.AddRange(visiblePost, deletedPost, suspendedUserPost);

        _context.PostTags.AddRange(
            new PostTag { PostId = visiblePost.Id, TagId = tag.Id, Post = visiblePost, Tag = tag, CreatedAt = visiblePost.CreatedAt },
            new PostTag { PostId = deletedPost.Id, TagId = tag.Id, Post = deletedPost, Tag = tag, CreatedAt = deletedPost.CreatedAt },
            new PostTag { PostId = suspendedUserPost.Id, TagId = tag.Id, Post = suspendedUserPost, Tag = tag, CreatedAt = suspendedUserPost.CreatedAt }
        );

        await _context.SaveChangesAsync();

        // Act
        var trendingTags = await _tagService.GetTrendingTagsAsync(10);
        var trendingTagsAnalytics = await _tagAnalyticsService.GetTrendingTagsAsync(7, 10);
        var contentTrends = await _adminService.GetContentTrendsAsync(30);

        // Assert
        var tagServiceResult = trendingTags.FirstOrDefault(t => t.Name == "testtag");
        Assert.NotNull(tagServiceResult);
        Assert.Equal(1, tagServiceResult.PostCount); // Should only count the visible post

        var analyticsResult = trendingTagsAnalytics.FirstOrDefault(t => t.Name == "testtag");
        Assert.NotNull(analyticsResult);

        var adminResult = contentTrends.TrendingHashtags.FirstOrDefault(h => h.Hashtag == "testtag");
        Assert.NotNull(adminResult);
        Assert.Equal(1, adminResult.Count); // Should only count the visible post
        Assert.Equal(1, adminResult.UniqueUsers); // Should only count the active user
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
