using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Common;

namespace Yapplr.Api.Tests;

/// <summary>
/// Tests for the QueryBuilder class and its fluent interface
/// </summary>
public class QueryBuilderTests : IDisposable
{
    private readonly YapplrDbContext _context;

    public QueryBuilderTests()
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

    private async Task<Post> CreateTestPostAsync(
        User user,
        string content = "Test post content",
        PostPrivacy privacy = PostPrivacy.Public,
        DateTime? createdAt = null)
    {
        var post = new Post
        {
            Content = content,
            UserId = user.Id,
            User = user,
            Privacy = privacy,
            CreatedAt = createdAt ?? DateTime.UtcNow.AddHours(-1)
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    #endregion

    #region Basic QueryBuilder Tests

    [Fact]
    public async Task QueryBuilder_BasicConstruction_ShouldWork()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post = await CreateTestPostAsync(user);

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var result = await builder.ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == post.Id);
    }

    [Fact]
    public async Task QueryBuilder_WithQueryableConstruction_ShouldWork()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post = await CreateTestPostAsync(user);

        // Act
        var builder = new QueryBuilder<Post>(_context.Posts, _context);
        var result = await builder.ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == post.Id);
    }

    #endregion

    #region Where Clause Tests

    [Fact]
    public async Task QueryBuilder_Where_ShouldFilterCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post1 = await CreateTestPostAsync(user, "First post");
        var post2 = await CreateTestPostAsync(user, "Second post");

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var result = await builder
            .Where(p => p.Content.Contains("First"))
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == post1.Id);
        result.Should().NotContain(p => p.Id == post2.Id);
    }

    [Fact]
    public async Task QueryBuilder_MultipleWhere_ShouldChainCorrectly()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1");
        var user2 = await CreateTestUserAsync("user2");
        var post1 = await CreateTestPostAsync(user1, "Test content");
        var post2 = await CreateTestPostAsync(user2, "Test content");
        var post3 = await CreateTestPostAsync(user1, "Other content");

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var result = await builder
            .Where(p => p.Content.Contains("Test"))
            .Where(p => p.UserId == user1.Id)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == post1.Id);
    }

    #endregion

    #region Include Tests

    [Fact]
    public async Task QueryBuilder_Include_ShouldLoadNavigationProperties()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post = await CreateTestPostAsync(user);

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var result = await builder
            .Include(p => p.User)
            .FirstOrDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.User.Should().NotBeNull();
        result.User.Username.Should().Be(user.Username);
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public async Task QueryBuilder_OrderBy_ShouldOrderCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post1 = await CreateTestPostAsync(user, "A post", PostPrivacy.Public, DateTime.UtcNow.AddHours(-2));
        var post2 = await CreateTestPostAsync(user, "B post", PostPrivacy.Public, DateTime.UtcNow.AddHours(-1));

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var result = await builder
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(post1.Id);
        result[1].Id.Should().Be(post2.Id);
    }

    [Fact]
    public async Task QueryBuilder_OrderByDescending_ShouldOrderCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post1 = await CreateTestPostAsync(user, "A post", PostPrivacy.Public, DateTime.UtcNow.AddHours(-2));
        var post2 = await CreateTestPostAsync(user, "B post", PostPrivacy.Public, DateTime.UtcNow.AddHours(-1));

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var result = await builder
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(post2.Id);
        result[1].Id.Should().Be(post1.Id);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task QueryBuilder_Paginate_ShouldPaginateCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var posts = new List<Post>();
        for (int i = 0; i < 5; i++)
        {
            var post = await CreateTestPostAsync(user, $"Post {i}", PostPrivacy.Public, DateTime.UtcNow.AddHours(-i));
            posts.Add(post);
        }

        // Act - Get page 1 (first 2 items)
        var builder1 = new QueryBuilder<Post>(_context);
        var page1 = await builder1
            .OrderByDescending(p => p.CreatedAt)
            .Paginate(1, 2)
            .ToListAsync();

        // Act - Get page 2 (next 2 items)
        var builder2 = new QueryBuilder<Post>(_context);
        var page2 = await builder2
            .OrderByDescending(p => p.CreatedAt)
            .Paginate(2, 2)
            .ToListAsync();

        // Assert
        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
        page1[0].Id.Should().Be(posts[0].Id); // Most recent
        page1[1].Id.Should().Be(posts[1].Id);
        page2[0].Id.Should().Be(posts[2].Id);
        page2[1].Id.Should().Be(posts[3].Id);
    }

    [Fact]
    public async Task QueryBuilder_Paginate_ShouldValidateParameters()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await CreateTestPostAsync(user);

        // Act & Assert - Invalid page number should be corrected to 1
        var builder1 = new QueryBuilder<Post>(_context);
        var result1 = await builder1
            .OrderBy(p => p.Id)
            .Paginate(0, 10)
            .ToListAsync();

        result1.Should().HaveCount(1);

        // Act & Assert - Invalid page size should be clamped
        var builder2 = new QueryBuilder<Post>(_context);
        var result2 = await builder2
            .OrderBy(p => p.Id)
            .Paginate(1, 0)
            .ToListAsync();

        result2.Should().HaveCount(1); // Clamped to minimum of 1

        var builder3 = new QueryBuilder<Post>(_context);
        var result3 = await builder3
            .OrderBy(p => p.Id)
            .Paginate(1, 200)
            .ToListAsync();

        result3.Should().HaveCount(1); // Clamped to maximum of 100
    }

    #endregion

    #region Execution Method Tests

    [Fact]
    public async Task QueryBuilder_FirstOrDefaultAsync_ShouldReturnFirstItem()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post1 = await CreateTestPostAsync(user, "First");
        var post2 = await CreateTestPostAsync(user, "Second");

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var result = await builder
            .OrderBy(p => p.Content)
            .FirstOrDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(post1.Id);
    }

    [Fact]
    public async Task QueryBuilder_SingleOrDefaultAsync_ShouldReturnSingleItem()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post = await CreateTestPostAsync(user, "Unique content");

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var result = await builder
            .Where(p => p.Content == "Unique content")
            .SingleOrDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(post.Id);
    }

    [Fact]
    public async Task QueryBuilder_CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        await CreateTestPostAsync(user, "Post 1");
        await CreateTestPostAsync(user, "Post 2");
        await CreateTestPostAsync(user, "Post 3");

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var count = await builder.CountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task QueryBuilder_AnyAsync_ShouldReturnCorrectResult()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act - No posts exist
        var builder1 = new QueryBuilder<Post>(_context);
        var hasAny1 = await builder1.AnyAsync();

        // Arrange - Add a post
        await CreateTestPostAsync(user);

        // Act - Posts exist
        var builder2 = new QueryBuilder<Post>(_context);
        var hasAny2 = await builder2.AnyAsync();

        // Assert
        hasAny1.Should().BeFalse();
        hasAny2.Should().BeTrue();
    }

    [Fact]
    public async Task QueryBuilder_AsQueryable_ShouldReturnQueryable()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var post = await CreateTestPostAsync(user);

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var queryable = builder
            .Where(p => p.UserId == user.Id)
            .AsQueryable();

        var result = await queryable.ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == post.Id);
    }

    #endregion

    #region Complex Chaining Tests

    [Fact]
    public async Task QueryBuilder_ComplexChaining_ShouldWorkCorrectly()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1");
        var user2 = await CreateTestUserAsync("user2");
        
        var posts = new List<Post>();
        for (int i = 0; i < 10; i++)
        {
            var user = i % 2 == 0 ? user1 : user2;
            var post = await CreateTestPostAsync(user, $"Post {i}", PostPrivacy.Public, DateTime.UtcNow.AddHours(-i));
            posts.Add(post);
        }

        // Act
        var builder = new QueryBuilder<Post>(_context);
        var result = await builder
            .Include(p => p.User)
            .Where(p => p.UserId == user1.Id)
            .Where(p => p.Content.Contains("Post"))
            .OrderByDescending(p => p.CreatedAt)
            .Paginate(1, 3)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(p => p.UserId == user1.Id);
        result.Should().OnlyContain(p => p.User != null);
        result.Should().BeInDescendingOrder(p => p.CreatedAt);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
