using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Yapplr.Api.Common;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;

namespace Yapplr.Api.Tests;

public class CountCacheServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<ICachingService> _mockCache;
    private readonly Mock<ILogger<CountCacheService>> _mockLogger;
    private readonly CountCacheService _service;

    public CountCacheServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockCache = new Mock<ICachingService>();
        _mockLogger = new Mock<ILogger<CountCacheService>>();
        _service = new CountCacheService(_mockCache.Object, _context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetFollowerCountAsync_WhenCacheHit_ReturnsFromCache()
    {
        // Arrange
        var userId = 1;
        var expectedCount = 5;
        var cacheKey = $"count:followers:{userId}";
        var wrapper = new ValueWrapper<int>(expectedCount);

        _mockCache.Setup(x => x.GetAsync<ValueWrapper<int>>(cacheKey))
            .ReturnsAsync(wrapper);

        // Act
        var result = await _service.GetFollowerCountAsync(userId);

        // Assert
        Assert.Equal(expectedCount, result);
        _mockCache.Verify(x => x.GetAsync<ValueWrapper<int>>(cacheKey), Times.Once);
    }

    [Fact]
    public async Task GetFollowerCountAsync_WhenCacheMiss_QueriesDatabase()
    {
        // Arrange
        var userId = 1;
        var followerId = 2;

        // Add test data
        _context.Follows.Add(new Follow { FollowerId = followerId, FollowingId = userId });
        await _context.SaveChangesAsync();

        var cacheKey = $"count:followers:{userId}";

        // Setup cache miss
        _mockCache.Setup(x => x.GetAsync<ValueWrapper<int>>(cacheKey))
            .ReturnsAsync((ValueWrapper<int>?)null);

        _mockCache.Setup(x => x.SetAsync(cacheKey, It.IsAny<ValueWrapper<int>>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetFollowerCountAsync(userId);

        // Assert
        Assert.Equal(1, result);
        _mockCache.Verify(x => x.GetAsync<ValueWrapper<int>>(cacheKey), Times.Once);
        _mockCache.Verify(x => x.SetAsync(cacheKey, It.IsAny<ValueWrapper<int>>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetPostCountAsync_ExcludesHiddenPosts()
    {
        // Arrange
        var userId = 1;

        // Add test data - one visible, one hidden
        _context.Posts.AddRange(
            new Post { UserId = userId, Content = "Visible post", IsHidden = false },
            new Post { UserId = userId, Content = "Hidden post", IsHidden = true }
        );
        await _context.SaveChangesAsync();

        var cacheKey = $"count:posts:{userId}";

        // Setup cache miss
        _mockCache.Setup(x => x.GetAsync<ValueWrapper<int>>(cacheKey))
            .ReturnsAsync((ValueWrapper<int>?)null);

        _mockCache.Setup(x => x.SetAsync(cacheKey, It.IsAny<ValueWrapper<int>>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetPostCountAsync(userId);

        // Assert
        Assert.Equal(1, result); // Only the visible post should be counted
    }

    [Fact]
    public async Task GetLikeCountAsync_CountsCorrectly()
    {
        // Arrange
        var userId1 = 1;
        var userId2 = 2;

        // Add test data - let EF generate the ID
        var post = new Post { UserId = userId1, Content = "Test post" };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync(); // Save to get the generated ID

        var postId = post.Id;

        _context.Likes.AddRange(
            new Like { PostId = postId, UserId = userId1 },
            new Like { PostId = postId, UserId = userId2 }
        );
        await _context.SaveChangesAsync();

        var cacheKey = $"count:likes:{postId}";

        // Setup cache miss
        _mockCache.Setup(x => x.GetAsync<ValueWrapper<int>>(cacheKey))
            .ReturnsAsync((ValueWrapper<int>?)null);

        _mockCache.Setup(x => x.SetAsync(cacheKey, It.IsAny<ValueWrapper<int>>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetLikeCountAsync(postId);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetCommentCountAsync_ExcludesDeletedAndHiddenComments()
    {
        // Arrange
        var userId = 1;

        // Add test data - let EF generate the ID
        var post = new Post { UserId = userId, Content = "Test post", PostType = PostType.Post };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync(); // Save to get the generated ID

        var postId = post.Id;

        _context.Posts.AddRange(
            new Post { ParentId = postId, UserId = userId, Content = "Visible comment", IsDeletedByUser = false, IsHidden = false, PostType = PostType.Comment },
            new Post { ParentId = postId, UserId = userId, Content = "Deleted comment", IsDeletedByUser = true, IsHidden = false, PostType = PostType.Comment },
            new Post { ParentId = postId, UserId = userId, Content = "Hidden comment", IsDeletedByUser = false, IsHidden = true, PostType = PostType.Comment }
        );
        await _context.SaveChangesAsync();

        var cacheKey = $"count:comments:{postId}";

        // Setup cache miss
        _mockCache.Setup(x => x.GetAsync<ValueWrapper<int>>(cacheKey))
            .ReturnsAsync((ValueWrapper<int>?)null);

        _mockCache.Setup(x => x.SetAsync(cacheKey, It.IsAny<ValueWrapper<int>>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetCommentCountAsync(postId);

        // Assert
        Assert.Equal(1, result); // Only the visible comment should be counted
    }

    [Fact]
    public async Task InvalidateUserCountsAsync_CallsCorrectCacheRemoves()
    {
        // Arrange
        var userId = 1;

        // Act
        await _service.InvalidateUserCountsAsync(userId);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync($"count:followers:{userId}"), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync($"count:following:{userId}"), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync($"count:posts:{userId}"), Times.Once);
    }

    [Fact]
    public async Task InvalidatePostCountsAsync_CallsCorrectCacheRemoves()
    {
        // Arrange
        var postId = 1;

        // Act
        await _service.InvalidatePostCountsAsync(postId);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync($"count:likes:{postId}"), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync($"count:comments:{postId}"), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync($"count:reposts:{postId}"), Times.Once);
    }

    [Fact]
    public async Task InvalidateFollowCountsAsync_CallsCorrectCacheRemoves()
    {
        // Arrange
        var followerId = 1;
        var followingId = 2;

        // Act
        await _service.InvalidateFollowCountsAsync(followerId, followingId);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync($"count:followers:{followingId}"), Times.Once);
        _mockCache.Verify(x => x.RemoveAsync($"count:following:{followerId}"), Times.Once);
    }

    [Fact]
    public async Task InvalidateTagCountsAsync_WithTagName_CallsPatternRemove()
    {
        // Arrange
        var tagName = "test";

        // Act
        await _service.InvalidateTagCountsAsync(tagName);

        // Assert
        _mockCache.Verify(x => x.RemoveByPatternAsync($"count:tag:posts:name:{tagName}"), Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
