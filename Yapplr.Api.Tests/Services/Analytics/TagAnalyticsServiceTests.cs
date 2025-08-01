using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Services.Analytics;
using Xunit;

namespace Yapplr.Api.Tests.Services.Analytics;

public class TagAnalyticsServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly Mock<IAnalyticsService> _mockAnalyticsService;
    private readonly Mock<ILogger<TagAnalyticsService>> _mockLogger;
    private readonly TagAnalyticsService _service;

    public TagAnalyticsServiceTests()
    {
        _context = new TestYapplrDbContext();
        _mockAnalyticsService = new Mock<IAnalyticsService>();
        _mockLogger = new Mock<ILogger<TagAnalyticsService>>();
        _service = new TagAnalyticsService(_context, _mockAnalyticsService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetTrendingHashtagsWithVelocityAsync_ShouldReturnTrendingHashtags()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetTrendingHashtagsWithVelocityAsync(timeWindow: 24, limit: 10);

        // Assert
        Assert.NotNull(result);
        var trendingHashtags = result.ToList();
        Assert.NotEmpty(trendingHashtags);
        
        // Verify trending hashtags are ordered by trending score
        var scores = trendingHashtags.Select(h => h.TrendingScore).ToList();
        Assert.True(scores.SequenceEqual(scores.OrderByDescending(s => s)));
        
        // Verify velocity calculations
        foreach (var hashtag in trendingHashtags)
        {
            Assert.True(hashtag.TrendingScore >= 0);
            Assert.True(hashtag.PostCount >= 0);
            Assert.NotNull(hashtag.Category);
        }
    }

    [Fact]
    public async Task GetTrendingHashtagsByCategoryAsync_ShouldGroupByCategory()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetTrendingHashtagsByCategoryAsync(timeWindow: 24, limit: 5);

        // Assert
        Assert.NotNull(result);
        var categories = result.ToList();
        Assert.NotEmpty(categories);
        
        // Verify each category has hashtags
        foreach (var category in categories)
        {
            Assert.NotNull(category.Category);
            Assert.NotNull(category.TrendingHashtags);
            Assert.True(category.TotalPosts >= 0);
        }
    }

    [Fact]
    public async Task GetPersonalizedTrendingHashtagsAsync_ShouldReturnPersonalizedResults()
    {
        // Arrange
        await SeedTestDataAsync();
        var userId = 1;

        // Act
        var result = await _service.GetPersonalizedTrendingHashtagsAsync(userId, timeWindow: 24, limit: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.NotNull(result.RecommendedHashtags);
        Assert.NotNull(result.UserInterests);
        Assert.True(result.PersonalizationScore >= 0);
    }

    private async Task SeedTestDataAsync()
    {
        // Create test users
        var users = new[]
        {
            new User { Id = 1, Username = "user1", Email = "user1@test.com", TrustScore = 0.8f, Status = UserStatus.Active },
            new User { Id = 2, Username = "user2", Email = "user2@test.com", TrustScore = 0.7f, Status = UserStatus.Active },
            new User { Id = 3, Username = "user3", Email = "user3@test.com", TrustScore = 0.9f, Status = UserStatus.Active }
        };
        _context.Users.AddRange(users);

        // Create test tags
        var tags = new[]
        {
            new Tag { Id = 1, Name = "technology", PostCount = 10, CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new Tag { Id = 2, Name = "sports", PostCount = 8, CreatedAt = DateTime.UtcNow.AddDays(-25) },
            new Tag { Id = 3, Name = "music", PostCount = 12, CreatedAt = DateTime.UtcNow.AddDays(-20) },
            new Tag { Id = 4, Name = "programming", PostCount = 15, CreatedAt = DateTime.UtcNow.AddDays(-15) }
        };
        _context.Tags.AddRange(tags);

        // Create test posts
        var posts = new[]
        {
            new Post { Id = 1, UserId = 1, Content = "Tech post", PostType = PostType.Post, CreatedAt = DateTime.UtcNow.AddHours(-12), Privacy = PostPrivacy.Public },
            new Post { Id = 2, UserId = 2, Content = "Sports post", PostType = PostType.Post, CreatedAt = DateTime.UtcNow.AddHours(-6), Privacy = PostPrivacy.Public },
            new Post { Id = 3, UserId = 3, Content = "Music post", PostType = PostType.Post, CreatedAt = DateTime.UtcNow.AddHours(-3), Privacy = PostPrivacy.Public },
            new Post { Id = 4, UserId = 1, Content = "Programming post", PostType = PostType.Post, CreatedAt = DateTime.UtcNow.AddHours(-1), Privacy = PostPrivacy.Public }
        };
        _context.Posts.AddRange(posts);

        // Create post-tag relationships
        var postTags = new[]
        {
            new PostTag { PostId = 1, TagId = 1, CreatedAt = DateTime.UtcNow.AddHours(-12) },
            new PostTag { PostId = 2, TagId = 2, CreatedAt = DateTime.UtcNow.AddHours(-6) },
            new PostTag { PostId = 3, TagId = 3, CreatedAt = DateTime.UtcNow.AddHours(-3) },
            new PostTag { PostId = 4, TagId = 4, CreatedAt = DateTime.UtcNow.AddHours(-1) }
        };
        _context.PostTags.AddRange(postTags);

        // Create some tag analytics for user interests
        var tagAnalytics = new[]
        {
            new TagAnalytics { Id = 1, TagId = 1, UserId = 1, Action = TagAction.Used, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new TagAnalytics { Id = 2, TagId = 4, UserId = 1, Action = TagAction.Clicked, CreatedAt = DateTime.UtcNow.AddDays(-3) }
        };
        _context.TagAnalytics.AddRange(tagAnalytics);

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
