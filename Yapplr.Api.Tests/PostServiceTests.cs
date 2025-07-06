using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.DTOs;

namespace Yapplr.Api.Tests;

public class PostServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly PostService _postService;
    private readonly Mock<IBlockService> _mockBlockService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

    public PostServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.TransactionStarted))
            .Options;

        _context = new TestYapplrDbContext(options);
        _mockBlockService = new Mock<IBlockService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        // Setup default HTTP context
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        mockRequest.Setup(r => r.Scheme).Returns("https");
        mockRequest.Setup(r => r.Host).Returns(new HostString("localhost"));
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);
        
        _postService = new PostService(_context, _mockBlockService.Object, _mockNotificationService.Object, _mockHttpContextAccessor.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreatePostAsync_WithValidData_CreatesPostAndReturnsDto()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var createDto = new CreatePostDto
        {
            Content = "This is a test post with #hashtag",
            Privacy = PostPrivacy.Public
        };

        _mockNotificationService.Setup(n => n.CreateMentionNotificationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                                .Returns(Task.CompletedTask);

        // Act
        var result = await _postService.CreatePostAsync(1, createDto);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("This is a test post with #hashtag");
        result.Privacy.Should().Be(PostPrivacy.Public);
        result.User.Username.Should().Be("testuser");
        result.LikesCount.Should().Be(0);
        result.CommentsCount.Should().Be(0);
        result.RepostsCount.Should().Be(0);

        // Verify post was created in database
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Content == "This is a test post with #hashtag");
        post.Should().NotBeNull();
        post!.UserId.Should().Be(1);
    }

    [Fact]
    public async Task CreatePostAsync_WithHashtags_CreatesTagsAndAssociations()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var createDto = new CreatePostDto
        {
            Content = "Post with #technology and #programming tags",
            Privacy = PostPrivacy.Public
        };

        _mockNotificationService.Setup(n => n.CreateMentionNotificationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                                .Returns(Task.CompletedTask);

        // Act
        var result = await _postService.CreatePostAsync(1, createDto);

        // Assert
        result.Should().NotBeNull();

        // Verify tags were created
        var tags = await _context.Tags.ToListAsync();
        tags.Should().HaveCount(2);
        tags.Should().Contain(t => t.Name == "technology");
        tags.Should().Contain(t => t.Name == "programming");

        // Verify post-tag associations were created
        var postTags = await _context.PostTags.ToListAsync();
        postTags.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPostByIdAsync_WithExistingPost_ReturnsPostDto()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var post = new Post
        {
            Id = 1,
            Content = "Test post",
            Privacy = PostPrivacy.Public,
            UserId = 1,
            User = user,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        _mockBlockService.Setup(b => b.IsBlockedByUserAsync(It.IsAny<int>(), It.IsAny<int>()))
                         .ReturnsAsync(false);

        // Act
        var result = await _postService.GetPostByIdAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Content.Should().Be("Test post");
        result.User.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetPostByIdAsync_WithNonExistentPost_ReturnsNull()
    {
        // Act
        var result = await _postService.GetPostByIdAsync(999, 1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPostByIdAsync_WithBlockedUser_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var post = new Post
        {
            Id = 1,
            Content = "Test post",
            Privacy = PostPrivacy.Public,
            UserId = 1,
            User = user,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        _mockBlockService.Setup(b => b.IsBlockedByUserAsync(1, 2))
                         .ReturnsAsync(true);

        // Act
        var result = await _postService.GetPostByIdAsync(1, 2);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePostAsync_WithValidData_UpdatesPostAndReturnsDto()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var post = new Post
        {
            Id = 1,
            Content = "Original content",
            Privacy = PostPrivacy.Public,
            UserId = 1,
            User = user,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        var updateDto = new UpdatePostDto
        {
            Content = "Updated content",
            Privacy = PostPrivacy.Followers
        };

        // Act
        var result = await _postService.UpdatePostAsync(1, 1, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("Updated content");
        result.Privacy.Should().Be(PostPrivacy.Followers);
        result.IsEdited.Should().BeTrue();

        // Verify database was updated
        var updatedPost = await _context.Posts.FindAsync(1);
        updatedPost!.Content.Should().Be("Updated content");
        updatedPost.Privacy.Should().Be(PostPrivacy.Followers);
        updatedPost.IsEdited.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePostAsync_WithNonOwner_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var post = new Post
        {
            Id = 1,
            Content = "Original content",
            Privacy = PostPrivacy.Public,
            UserId = 1,
            User = user,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        var updateDto = new UpdatePostDto
        {
            Content = "Updated content"
        };

        // Act
        var result = await _postService.UpdatePostAsync(1, 2, updateDto); // Different user ID

        // Assert
        result.Should().BeNull();

        // Verify database was not updated
        var unchangedPost = await _context.Posts.FindAsync(1);
        unchangedPost!.Content.Should().Be("Original content");
        unchangedPost.IsEdited.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePostAsync_WithValidOwner_DeletesPostAndReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var post = new Post
        {
            Id = 1,
            Content = "Test post",
            Privacy = PostPrivacy.Public,
            UserId = 1,
            User = user,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Act
        var result = await _postService.DeletePostAsync(1, 1);

        // Assert
        result.Should().BeTrue();

        // Verify post was deleted
        var deletedPost = await _context.Posts.FindAsync(1);
        deletedPost.Should().BeNull();
    }

    [Fact]
    public async Task DeletePostAsync_WithNonOwner_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var post = new Post
        {
            Id = 1,
            Content = "Test post",
            Privacy = PostPrivacy.Public,
            UserId = 1,
            User = user,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Act
        var result = await _postService.DeletePostAsync(1, 2); // Different user ID

        // Assert
        result.Should().BeFalse();

        // Verify post was not deleted
        var existingPost = await _context.Posts.FindAsync(1);
        existingPost.Should().NotBeNull();
    }

    [Fact]
    public async Task LikePostAsync_WithValidPost_CreatesLikeAndReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var post = new Post
        {
            Id = 1,
            Content = "Test post",
            Privacy = PostPrivacy.Public,
            UserId = 1,
            User = user,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        _mockBlockService.Setup(b => b.IsBlockedByUserAsync(It.IsAny<int>(), It.IsAny<int>()))
                         .ReturnsAsync(false);

        // Act
        var result = await _postService.LikePostAsync(1, 1);

        // Assert
        result.Should().BeTrue();

        // Verify like was created
        var like = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == 1 && l.UserId == 1);
        like.Should().NotBeNull();
    }

    [Fact]
    public async Task LikePostAsync_WhenAlreadyLiked_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var post = new Post
        {
            Id = 1,
            Content = "Test post",
            Privacy = PostPrivacy.Public,
            UserId = 1,
            User = user,
            CreatedAt = DateTime.UtcNow
        };
        var existingLike = new Like
        {
            PostId = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        _context.Likes.Add(existingLike);
        await _context.SaveChangesAsync();

        // Act
        var result = await _postService.LikePostAsync(1, 1);

        // Assert
        result.Should().BeFalse();

        // Verify only one like exists
        var likeCount = await _context.Likes.CountAsync(l => l.PostId == 1 && l.UserId == 1);
        likeCount.Should().Be(1);
    }

    [Fact]
    public async Task UnlikePostAsync_WithExistingLike_RemovesLikeAndReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var post = new Post
        {
            Id = 1,
            Content = "Test post",
            Privacy = PostPrivacy.Public,
            UserId = 1,
            User = user,
            CreatedAt = DateTime.UtcNow
        };
        var existingLike = new Like
        {
            PostId = 1,
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        _context.Likes.Add(existingLike);
        await _context.SaveChangesAsync();

        // Act
        var result = await _postService.UnlikePostAsync(1, 1);

        // Assert
        result.Should().BeTrue();

        // Verify like was removed
        var like = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == 1 && l.UserId == 1);
        like.Should().BeNull();
    }

    [Fact]
    public async Task UnlikePostAsync_WithNoExistingLike_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var post = new Post
        {
            Id = 1,
            Content = "Test post",
            Privacy = PostPrivacy.Public,
            UserId = 1,
            User = user,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Act
        var result = await _postService.UnlikePostAsync(1, 1);

        // Assert
        result.Should().BeFalse();
    }
}
