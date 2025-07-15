using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Tests.Common;

namespace Yapplr.Api.Tests;

public class TagServicePostDeletionTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly TagService _tagService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IBlockService> _mockBlockService;
    private readonly Mock<ICountCacheService> _mockCountCacheService;

    public TagServicePostDeletionTests()
    {
        _context = new TestYapplrDbContext();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockBlockService = new Mock<IBlockService>();
        _mockCountCacheService = new Mock<ICountCacheService>();

        _tagService = new TagService(
            _context,
            _mockHttpContextAccessor.Object,
            _mockBlockService.Object,
            _mockCountCacheService.Object
        );

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test users
        var user1 = new User
        {
            Id = 1,
            Username = "testuser1",
            Email = "test1@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            Status = UserStatus.Active,
            TrustScore = 0.8f,
            Role = UserRole.User
        };

        var user2 = new User
        {
            Id = 2,
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            Status = UserStatus.Active,
            TrustScore = 0.9f,
            Role = UserRole.User
        };

        _context.Users.AddRange(user1, user2);

        // Create test tags
        var testTag = new Tag
        {
            Id = 1,
            Name = "testtag",
            PostCount = 3,
            CreatedAt = DateTime.UtcNow
        };

        var anotherTag = new Tag
        {
            Id = 2,
            Name = "anothertag",
            PostCount = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tags.AddRange(testTag, anotherTag);

        // Create test posts
        var post1 = new Post
        {
            Id = 1,
            Content = "Post with #testtag",
            UserId = 1,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            User = user1,
            IsHidden = false,
            HiddenReasonType = PostHiddenReasonType.None
        };

        var post2 = new Post
        {
            Id = 2,
            Content = "Another post with #testtag",
            UserId = 2,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            User = user2,
            IsHidden = false,
            HiddenReasonType = PostHiddenReasonType.None
        };

        var post3 = new Post
        {
            Id = 3,
            Content = "Deleted post with #testtag",
            UserId = 1,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            User = user1,
            IsHidden = true,
            HiddenReasonType = PostHiddenReasonType.DeletedByUser,
            HiddenAt = DateTime.UtcNow.AddMinutes(-30),
            HiddenByUserId = 1,
            HiddenReason = "Post deleted by user"
        };

        var post4 = new Post
        {
            Id = 4,
            Content = "Post with #anothertag",
            UserId = 2,
            Privacy = PostPrivacy.Public,
            CreatedAt = DateTime.UtcNow,
            User = user2,
            IsHidden = false,
            HiddenReasonType = PostHiddenReasonType.None
        };

        _context.Posts.AddRange(post1, post2, post3, post4);

        // Create post-tag relationships
        var postTag1 = new PostTag { PostId = 1, TagId = 1, Post = post1, Tag = testTag };
        var postTag2 = new PostTag { PostId = 2, TagId = 1, Post = post2, Tag = testTag };
        var postTag3 = new PostTag { PostId = 3, TagId = 1, Post = post3, Tag = testTag }; // Deleted post
        var postTag4 = new PostTag { PostId = 4, TagId = 2, Post = post4, Tag = anotherTag };

        _context.PostTags.AddRange(postTag1, postTag2, postTag3, postTag4);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetPostsByTagAsync_ExcludesDeletedPosts()
    {
        // Arrange
        var tagName = "testtag";
        var currentUserId = 1;

        // Act
        var posts = await _tagService.GetPostsByTagAsync(tagName, currentUserId);
        var postList = posts.ToList();

        // Assert
        Assert.Equal(2, postList.Count); // Should only return 2 non-deleted posts
        Assert.Contains(postList, p => p.Id == 1);
        Assert.Contains(postList, p => p.Id == 2);
        Assert.DoesNotContain(postList, p => p.Id == 3); // Deleted post should not appear
    }

    [Fact]
    public async Task GetPostsByTagAsync_ExcludesDeletedPostsForUnauthenticatedUsers()
    {
        // Arrange
        var tagName = "testtag";
        int? currentUserId = null; // Unauthenticated user

        // Act
        var posts = await _tagService.GetPostsByTagAsync(tagName, currentUserId);
        var postList = posts.ToList();

        // Assert
        Assert.Equal(2, postList.Count); // Should only return 2 non-deleted posts
        Assert.DoesNotContain(postList, p => p.Id == 3); // Deleted post should not appear
    }

    [Fact]
    public async Task GetPostsByTagAsync_ExcludesDeletedPostsForOtherUsers()
    {
        // Arrange
        var tagName = "testtag";
        var currentUserId = 2; // Different user than the post owner

        // Act
        var posts = await _tagService.GetPostsByTagAsync(tagName, currentUserId);
        var postList = posts.ToList();

        // Assert
        Assert.Equal(2, postList.Count); // Should only return 2 non-deleted posts
        Assert.DoesNotContain(postList, p => p.Id == 3); // Deleted post should not appear even to other users
    }

    [Fact]
    public async Task GetPostsByTagAsync_DeletedPostNotVisibleEvenToAuthor()
    {
        // Arrange
        var tagName = "testtag";
        var currentUserId = 1; // Author of the deleted post

        // Act
        var posts = await _tagService.GetPostsByTagAsync(tagName, currentUserId);
        var postList = posts.ToList();

        // Assert
        Assert.Equal(2, postList.Count); // Should only return 2 non-deleted posts
        Assert.DoesNotContain(postList, p => p.Id == 3); // Deleted post should not appear even to author
    }

    [Fact]
    public async Task GetPostsByTagAsync_ReturnsEmptyForNonExistentTag()
    {
        // Arrange
        var tagName = "nonexistenttag";
        var currentUserId = 1;

        // Act
        var posts = await _tagService.GetPostsByTagAsync(tagName, currentUserId);
        var postList = posts.ToList();

        // Assert
        Assert.Empty(postList);
    }

    [Fact]
    public async Task GetPostsByTagAsync_ReturnsEmptyForEmptyTagName()
    {
        // Arrange
        var tagName = "";
        var currentUserId = 1;

        // Act
        var posts = await _tagService.GetPostsByTagAsync(tagName, currentUserId);
        var postList = posts.ToList();

        // Assert
        Assert.Empty(postList);
    }

    [Fact]
    public async Task GetPostsByTagAsync_ReturnsEmptyForNullTagName()
    {
        // Arrange
        string? tagName = null;
        var currentUserId = 1;

        // Act
        var posts = await _tagService.GetPostsByTagAsync(tagName!, currentUserId);
        var postList = posts.ToList();

        // Assert
        Assert.Empty(postList);
    }

    [Fact]
    public async Task GetPostsByTagAsync_HandlesTagNameWithHashPrefix()
    {
        // Arrange
        var tagName = "#testtag"; // With hash prefix
        var currentUserId = 1;

        // Act
        var posts = await _tagService.GetPostsByTagAsync(tagName, currentUserId);
        var postList = posts.ToList();

        // Assert
        Assert.Equal(2, postList.Count); // Should still find posts despite hash prefix
        Assert.DoesNotContain(postList, p => p.Id == 3); // Deleted post should not appear
    }

    [Fact]
    public async Task GetPostsByTagAsync_IsCaseInsensitive()
    {
        // Arrange
        var tagName = "TESTTAG"; // Different case
        var currentUserId = 1;

        // Act
        var posts = await _tagService.GetPostsByTagAsync(tagName, currentUserId);
        var postList = posts.ToList();

        // Assert
        Assert.Equal(2, postList.Count); // Should find posts despite case difference
        Assert.DoesNotContain(postList, p => p.Id == 3); // Deleted post should not appear
    }

    [Fact]
    public async Task GetPostsByTagAsync_RespectsPrivacySettings()
    {
        // Arrange
        var tagName = "testtag";
        var currentUserId = 2;

        // Make post 1 followers-only
        var post1 = await _context.Posts.FindAsync(1);
        post1!.Privacy = PostPrivacy.Followers;
        await _context.SaveChangesAsync();

        // Act
        var posts = await _tagService.GetPostsByTagAsync(tagName, currentUserId);
        var postList = posts.ToList();

        // Assert
        Assert.Single(postList); // Should only return post 2 (public)
        Assert.Contains(postList, p => p.Id == 2);
        Assert.DoesNotContain(postList, p => p.Id == 1); // Followers-only post not visible
        Assert.DoesNotContain(postList, p => p.Id == 3); // Deleted post not visible
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
