using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MassTransit;
using Moq;
using Xunit;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Shared.Messages;

namespace Yapplr.Api.Tests
{
    public class RepostTests : IDisposable
    {
        private readonly YapplrDbContext _context;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IBlockService> _mockBlockService;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<ILinkPreviewService> _mockLinkPreviewService;
        private readonly Mock<IContentModerationService> _mockContentModerationService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly Mock<ICountCacheService> _mockCountCacheService;
        private readonly Mock<ITrustScoreService> _mockTrustScoreService;
        private readonly Mock<ITrustBasedModerationService> _mockTrustBasedModerationService;
        private readonly Mock<IAnalyticsService> _mockAnalyticsService;
        private readonly Mock<ILogger<PostService>> _mockLogger;
        private readonly PostService _postService;

        public RepostTests()
        {
            var options = new DbContextOptionsBuilder<YapplrDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestYapplrDbContext(options);
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockBlockService = new Mock<IBlockService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLinkPreviewService = new Mock<ILinkPreviewService>();
            _mockContentModerationService = new Mock<IContentModerationService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _mockCountCacheService = new Mock<ICountCacheService>();
            _mockTrustScoreService = new Mock<ITrustScoreService>();
            _mockTrustBasedModerationService = new Mock<ITrustBasedModerationService>();
            _mockAnalyticsService = new Mock<IAnalyticsService>();
            _mockLogger = new Mock<ILogger<PostService>>();

            _postService = new PostService(
                _context,
                _mockHttpContextAccessor.Object,
                _mockBlockService.Object,
                _mockNotificationService.Object,
                _mockLinkPreviewService.Object,
                _mockContentModerationService.Object,
                _mockConfiguration.Object,
                _mockPublishEndpoint.Object,
                _mockCountCacheService.Object,
                _mockTrustScoreService.Object,
                _mockTrustBasedModerationService.Object,
                _mockAnalyticsService.Object,
                _mockLogger.Object
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CreateRepost_WithValidData_ReturnsCreatedPost()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                Bio = "Test bio",
                Pronouns = "they/them",
                Tagline = "Test tagline",
                CreatedAt = DateTime.UtcNow,
                EmailVerified = true
            };
            _context.Users.Add(user);

            var originalPost = new Post
            {
                Content = "This is the original post",
                CreatedAt = DateTime.UtcNow,
                Privacy = PostPrivacy.Public,
                UserId = user.Id,
                PostType = PostType.Post
            };
            _context.Posts.Add(originalPost);
            await _context.SaveChangesAsync();

            var repostData = new CreateRepostDto
            {
                Content = "This is my repost comment",
                RepostedPostId = originalPost.Id,
                Privacy = PostPrivacy.Public
            };

            // Mock the current user
            _mockHttpContextAccessor.Setup(x => x.HttpContext.User.FindFirst("userId"))
                .Returns(new System.Security.Claims.Claim("userId", user.Id.ToString()));

            // Mock trust-based moderation to allow the action
            _mockTrustBasedModerationService.Setup(x => x.CanPerformActionAsync(user.Id, It.IsAny<TrustRequiredAction>()))
                .ReturnsAsync(true);

            // Act
            var result = await _postService.CreateRepostAsync(user.Id, repostData);

            // Assert - Check if post was created in database
            var createdPost = await _context.Posts
                .Include(p => p.RepostedPost)
                .FirstOrDefaultAsync(p => p.PostType == PostType.Repost && p.Content == "This is my repost comment");

            createdPost.Should().NotBeNull();
            createdPost.Content.Should().Be("This is my repost comment");
            createdPost.PostType.Should().Be(PostType.Repost);
            createdPost.RepostedPostId.Should().Be(originalPost.Id);
            createdPost.RepostedPost.Should().NotBeNull();
            createdPost.RepostedPost.Content.Should().Be("This is the original post");
        }

        [Fact]
        public async Task CreateRepostWithMedia_WithValidData_ReturnsCreatedPost()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser2",
                Email = "test2@example.com",
                Bio = "Test bio",
                Pronouns = "they/them",
                Tagline = "Test tagline",
                CreatedAt = DateTime.UtcNow,
                EmailVerified = true
            };
            _context.Users.Add(user);

            var originalPost = new Post
            {
                Content = "Original post with media",
                CreatedAt = DateTime.UtcNow,
                Privacy = PostPrivacy.Public,
                UserId = user.Id,
                PostType = PostType.Post
            };
            _context.Posts.Add(originalPost);
            await _context.SaveChangesAsync();

            var repostData = new CreateRepostWithMediaDto
            {
                Content = "Repost with media",
                RepostedPostId = originalPost.Id,
                Privacy = PostPrivacy.Public,
                MediaFiles = new List<MediaFileDto>
                {
                    new MediaFileDto(
                        FileName: "test-image.jpg",
                        MediaType: MediaType.Image,
                        Width: 800,
                        Height: 600,
                        FileSizeBytes: 1024000
                    )
                }
            };

            // Mock the current user
            _mockHttpContextAccessor.Setup(x => x.HttpContext.User.FindFirst("userId"))
                .Returns(new System.Security.Claims.Claim("userId", user.Id.ToString()));

            // Mock trust-based moderation to allow the action
            _mockTrustBasedModerationService.Setup(x => x.CanPerformActionAsync(user.Id, It.IsAny<TrustRequiredAction>()))
                .ReturnsAsync(true);

            // Act
            var result = await _postService.CreateRepostWithMediaAsync(user.Id, repostData);

            // Assert - Check if post was created in database
            var createdPost = await _context.Posts
                .Include(p => p.RepostedPost)
                .Include(p => p.PostMedia)
                .FirstOrDefaultAsync(p => p.PostType == PostType.Repost && p.Content == "Repost with media");

            createdPost.Should().NotBeNull();
            createdPost.Content.Should().Be("Repost with media");
            createdPost.PostType.Should().Be(PostType.Repost);
            createdPost.RepostedPostId.Should().Be(originalPost.Id);
            createdPost.RepostedPost.Should().NotBeNull();
            createdPost.PostMedia.Should().NotBeNull();
            createdPost.PostMedia.Should().HaveCount(1);
            createdPost.PostMedia.First().MediaType.Should().Be(MediaType.Image);
        }

        [Fact]
        public async Task GetReposts_ForExistingPost_ReturnsReposts()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser3",
                Email = "test3@example.com",
                Bio = "Test bio",
                Pronouns = "they/them",
                Tagline = "Test tagline",
                CreatedAt = DateTime.UtcNow,
                EmailVerified = true
            };
            _context.Users.Add(user);

            var originalPost = new Post
            {
                Content = "Original post to be reposted",
                CreatedAt = DateTime.UtcNow,
                Privacy = PostPrivacy.Public,
                UserId = user.Id,
                PostType = PostType.Post
            };
            _context.Posts.Add(originalPost);

            var repost = new Post
            {
                Content = "This is a repost",
                CreatedAt = DateTime.UtcNow,
                Privacy = PostPrivacy.Public,
                UserId = user.Id,
                PostType = PostType.Repost,
                RepostedPostId = originalPost.Id
            };
            _context.Posts.Add(repost);
            await _context.SaveChangesAsync();

            // Mock the current user
            _mockHttpContextAccessor.Setup(x => x.HttpContext.User.FindFirst("userId"))
                .Returns(new System.Security.Claims.Claim("userId", user.Id.ToString()));

            // Act
            var result = await _postService.GetRepostsAsync(originalPost.Id, user.Id, 1, 20);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Content.Should().Be("This is a repost");
            result.First().PostType.Should().Be(PostType.Repost);
        }

        [Fact]
        public async Task CreateRepost_WithInvalidRepostedPostId_ThrowsException()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser4",
                Email = "test4@example.com",
                Bio = "Test bio",
                Pronouns = "they/them",
                Tagline = "Test tagline",
                CreatedAt = DateTime.UtcNow,
                EmailVerified = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var repostData = new CreateRepostDto
            {
                Content = "Repost with invalid post ID",
                RepostedPostId = 99999, // Non-existent post ID
                Privacy = PostPrivacy.Public
            };

            // Mock the current user
            _mockHttpContextAccessor.Setup(x => x.HttpContext.User.FindFirst("userId"))
                .Returns(new System.Security.Claims.Claim("userId", user.Id.ToString()));

            // Mock trust-based moderation to allow the action (but the post won't exist)
            _mockTrustBasedModerationService.Setup(x => x.CanPerformActionAsync(user.Id, It.IsAny<TrustRequiredAction>()))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _postService.CreateRepostAsync(user.Id, repostData)
            );
        }

        [Fact]
        public async Task GetTimelineWithReposts_IncludesReposts_ReturnsCorrectTimelineItems()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser5",
                Email = "test5@example.com",
                Bio = "Test bio",
                Pronouns = "they/them",
                Tagline = "Test tagline",
                CreatedAt = DateTime.UtcNow,
                EmailVerified = true
            };
            _context.Users.Add(user);

            var originalPost = new Post
            {
                Content = "Original post for timeline test",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                Privacy = PostPrivacy.Public,
                UserId = user.Id,
                PostType = PostType.Post
            };
            _context.Posts.Add(originalPost);

            var repost = new Post
            {
                Content = "Repost for timeline test",
                CreatedAt = DateTime.UtcNow,
                Privacy = PostPrivacy.Public,
                UserId = user.Id,
                PostType = PostType.Repost,
                RepostedPostId = originalPost.Id
            };
            _context.Posts.Add(repost);
            await _context.SaveChangesAsync();

            // Mock the current user
            _mockHttpContextAccessor.Setup(x => x.HttpContext.User.FindFirst("userId"))
                .Returns(new System.Security.Claims.Claim("userId", user.Id.ToString()));

            // Act
            var result = await _postService.GetTimelineWithRepostsAsync(user.Id, 1, 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain(item => item.Type == "repost");

            var repostItem = result.First(item => item.Type == "repost");
            repostItem.Post.Content.Should().Be("Repost for timeline test");
            repostItem.Post.RepostedPost.Should().NotBeNull();
            repostItem.Post.RepostedPost.Content.Should().Be("Original post for timeline test");
        }

        [Fact]
        public async Task CreateRepost_WithEmptyContent_CreatesSimpleRepost()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser6",
                Email = "test6@example.com",
                Bio = "Test bio",
                Pronouns = "they/them",
                Tagline = "Test tagline",
                CreatedAt = DateTime.UtcNow,
                EmailVerified = true
            };
            _context.Users.Add(user);

            var originalPost = new Post
            {
                Content = "Original post for simple repost",
                CreatedAt = DateTime.UtcNow,
                Privacy = PostPrivacy.Public,
                UserId = user.Id,
                PostType = PostType.Post
            };
            _context.Posts.Add(originalPost);
            await _context.SaveChangesAsync();

            var repostData = new CreateRepostDto
            {
                Content = "", // Empty content for simple repost
                RepostedPostId = originalPost.Id,
                Privacy = PostPrivacy.Public
            };

            // Mock the current user
            _mockHttpContextAccessor.Setup(x => x.HttpContext.User.FindFirst("userId"))
                .Returns(new System.Security.Claims.Claim("userId", user.Id.ToString()));

            // Mock trust-based moderation to allow the action
            _mockTrustBasedModerationService.Setup(x => x.CanPerformActionAsync(user.Id, It.IsAny<TrustRequiredAction>()))
                .ReturnsAsync(true);

            // Act
            var result = await _postService.CreateRepostAsync(user.Id, repostData);

            // Assert - Check if post was created in database
            var createdPost = await _context.Posts
                .Include(p => p.RepostedPost)
                .FirstOrDefaultAsync(p => p.PostType == PostType.Repost && p.Content == "");

            createdPost.Should().NotBeNull();
            createdPost.Content.Should().Be("");
            createdPost.PostType.Should().Be(PostType.Repost);
            createdPost.RepostedPostId.Should().Be(originalPost.Id);
            createdPost.RepostedPost.Should().NotBeNull();
            createdPost.RepostedPost.Content.Should().Be("Original post for simple repost");
        }
    }
}
