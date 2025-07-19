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
/// Comprehensive tests to verify that group posts are properly isolated from general feeds
/// and only appear in group-specific queries
/// </summary>
public class GroupPostIsolationTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<ILogger<GroupPostIsolationTests>> _mockLogger;

    public GroupPostIsolationTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestYapplrDbContext(options);
        _mockLogger = new Mock<ILogger<GroupPostIsolationTests>>();
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

    private async Task<Group> CreateTestGroupAsync(User owner, string name = "TestGroup")
    {
        var group = new Group
        {
            Name = name,
            Description = "Test group description",
            UserId = owner.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Add owner as admin member
        var membership = new GroupMember
        {
            GroupId = group.Id,
            UserId = owner.Id,
            Role = GroupMemberRole.Admin,
            JoinedAt = DateTime.UtcNow
        };

        _context.GroupMembers.Add(membership);
        await _context.SaveChangesAsync();

        return group;
    }

    private async Task<Post> CreateTestPostAsync(
        User author,
        string content = "Test post content",
        PostPrivacy privacy = PostPrivacy.Public,
        Group? group = null)
    {
        var post = new Post
        {
            Content = content,
            Privacy = privacy,
            UserId = author.Id,
            GroupId = group?.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    private async Task<PostMedia> CreateTestPostMediaAsync(Post post, MediaType mediaType = MediaType.Image)
    {
        var media = new PostMedia
        {
            PostId = post.Id,
            MediaType = mediaType,
            CreatedAt = DateTime.UtcNow
        };

        if (mediaType == MediaType.Image)
        {
            media.ImageFileName = $"test-image-{post.Id}.jpg";
            media.ImageWidth = 800;
            media.ImageHeight = 600;
            media.ImageFileSizeBytes = 1024000;
        }
        else if (mediaType == MediaType.Video)
        {
            media.VideoFileName = $"test-video-{post.Id}.mp4";
            media.VideoWidth = 1920;
            media.VideoHeight = 1080;
            media.VideoFileSizeBytes = 5120000;
            media.VideoDuration = TimeSpan.FromMinutes(2);
        }

        _context.PostMedia.Add(media);
        await _context.SaveChangesAsync();
        return media;
    }

    #endregion

    #region ApplyVisibilityFilters Tests

    [Fact]
    public async Task ApplyVisibilityFilters_ShouldExcludeGroupPosts_FromGeneralFeeds()
    {
        // Arrange
        var user = await CreateTestUserAsync("testuser");
        var group = await CreateTestGroupAsync(user);
        
        var regularPost = await CreateTestPostAsync(user, "Regular post");
        var groupPost = await CreateTestPostAsync(user, "Group post", group: group);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(user.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == regularPost.Id);
        result.Should().NotContain(p => p.Id == groupPost.Id);
    }

    [Fact]
    public async Task ApplyPublicVisibilityFilters_ShouldExcludeGroupPosts_FromPublicTimeline()
    {
        // Arrange
        var user = await CreateTestUserAsync("testuser");
        var group = await CreateTestGroupAsync(user);
        
        var regularPost = await CreateTestPostAsync(user, "Regular post");
        var groupPost = await CreateTestPostAsync(user, "Group post", group: group);

        var blockedUserIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == regularPost.Id);
        result.Should().NotContain(p => p.Id == groupPost.Id);
    }

    [Theory]
    [InlineData(MediaType.Image)]
    [InlineData(MediaType.Video)]
    public async Task ApplyVisibilityFilters_ShouldExcludeGroupPostsWithMedia_FromUserMediaLibrary(MediaType mediaType)
    {
        // Arrange
        var user = await CreateTestUserAsync("testuser");
        var group = await CreateTestGroupAsync(user);
        
        var regularPost = await CreateTestPostAsync(user, "Regular post with media");
        var regularMedia = await CreateTestPostMediaAsync(regularPost, mediaType);
        
        var groupPost = await CreateTestPostAsync(user, "Group post with media", group: group);
        var groupMedia = await CreateTestPostMediaAsync(groupPost, mediaType);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int> { user.Id };

        // Act - Simulate user media library query
        var result = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.PostMedia)
            .Where(p => p.UserId == user.Id && p.PostMedia.Any(pm => pm.MediaType == mediaType))
            .ApplyVisibilityFilters(user.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == regularPost.Id);
        result.Should().NotContain(p => p.Id == groupPost.Id);
    }

    #endregion

    #region Group-Specific Query Tests

    [Fact]
    public async Task GroupSpecificQuery_ShouldOnlyReturnGroupPosts()
    {
        // Arrange
        var user = await CreateTestUserAsync("testuser");
        var group = await CreateTestGroupAsync(user);
        
        var regularPost = await CreateTestPostAsync(user, "Regular post");
        var groupPost1 = await CreateTestPostAsync(user, "Group post 1", group: group);
        var groupPost2 = await CreateTestPostAsync(user, "Group post 2", group: group);

        // Act - Simulate GetGroupPostsAsync query
        var result = await _context.Posts
            .Where(p => p.GroupId == group.Id && !p.IsHidden && !p.IsDeletedByUser)
            .Include(p => p.User)
            .Include(p => p.Group)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Id == groupPost1.Id);
        result.Should().Contain(p => p.Id == groupPost2.Id);
        result.Should().NotContain(p => p.Id == regularPost.Id);
    }

    #endregion

    #region Multiple Groups Tests

    [Fact]
    public async Task ApplyVisibilityFilters_ShouldExcludePostsFromAllGroups()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1");
        var user2 = await CreateTestUserAsync("user2");
        
        var group1 = await CreateTestGroupAsync(user1, "Group1");
        var group2 = await CreateTestGroupAsync(user2, "Group2");
        
        var regularPost = await CreateTestPostAsync(user1, "Regular post");
        var group1Post = await CreateTestPostAsync(user1, "Group 1 post", group: group1);
        var group2Post = await CreateTestPostAsync(user2, "Group 2 post", group: group2);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(user1.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == regularPost.Id);
        result.Should().NotContain(p => p.Id == group1Post.Id);
        result.Should().NotContain(p => p.Id == group2Post.Id);
    }

    #endregion

    #region Tag Search Tests

    [Fact]
    public async Task TagSearch_ShouldExcludeGroupPosts_FromTagResults()
    {
        // Arrange
        var user = await CreateTestUserAsync("testuser");
        var group = await CreateTestGroupAsync(user);

        var regularPost = await CreateTestPostAsync(user, "Regular post with #testtag");
        var groupPost = await CreateTestPostAsync(user, "Group post with #testtag", group: group);

        // Create tag
        var tag = new Tag { Name = "testtag", CreatedAt = DateTime.UtcNow };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        // Create post tags
        _context.PostTags.AddRange(
            new PostTag { PostId = regularPost.Id, TagId = tag.Id },
            new PostTag { PostId = groupPost.Id, TagId = tag.Id }
        );
        await _context.SaveChangesAsync();

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act - Simulate tag search query
        var result = await _context.GetPostsForFeed()
            .Where(p => p.PostTags.Any(pt => pt.Tag.Name == "testtag"))
            .ApplyVisibilityFilters(user.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == regularPost.Id);
        result.Should().NotContain(p => p.Id == groupPost.Id);
    }

    #endregion

    #region User Profile Tests

    [Fact]
    public async Task UserProfileFeed_ShouldExcludeGroupPosts_FromUserTimeline()
    {
        // Arrange
        var user = await CreateTestUserAsync("testuser");
        var viewer = await CreateTestUserAsync("viewer");
        var group = await CreateTestGroupAsync(user);

        var regularPost = await CreateTestPostAsync(user, "Regular post");
        var groupPost = await CreateTestPostAsync(user, "Group post", group: group);

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act - Simulate GetUserPostsAsync query
        var result = await _context.GetPostsForFeed()
            .Where(p => p.UserId == user.Id)
            .ApplyVisibilityFilters(viewer.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == regularPost.Id);
        result.Should().NotContain(p => p.Id == groupPost.Id);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ApplyVisibilityFilters_WithNullGroupId_ShouldIncludeRegularPosts()
    {
        // Arrange
        var user = await CreateTestUserAsync("testuser");

        var regularPost = await CreateTestPostAsync(user, "Regular post");
        // Explicitly ensure GroupId is null
        regularPost.GroupId = null;
        _context.Posts.Update(regularPost);
        await _context.SaveChangesAsync();

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(user.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == regularPost.Id);
    }

    [Fact]
    public async Task ApplyVisibilityFilters_WithDeletedGroupPost_ShouldStillExcludeFromGeneralFeeds()
    {
        // Arrange
        var user = await CreateTestUserAsync("testuser");
        var group = await CreateTestGroupAsync(user);

        var regularPost = await CreateTestPostAsync(user, "Regular post");
        var groupPost = await CreateTestPostAsync(user, "Group post", group: group);

        // Mark group post as deleted
        groupPost.IsDeletedByUser = true;
        groupPost.DeletedByUserAt = DateTime.UtcNow;
        _context.Posts.Update(groupPost);
        await _context.SaveChangesAsync();

        var blockedUserIds = new HashSet<int>();
        var followingIds = new HashSet<int>();

        // Act
        var result = await _context.Posts
            .Include(p => p.User)
            .ApplyVisibilityFilters(user.Id, blockedUserIds, followingIds)
            .ToListAsync();

        // Assert - Should only include regular post, group post should be excluded regardless of deletion status
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == regularPost.Id);
        result.Should().NotContain(p => p.Id == groupPost.Id);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
