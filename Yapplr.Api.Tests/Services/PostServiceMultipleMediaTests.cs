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
using Yapplr.Api.Services.Notifications;
using Yapplr.Shared.Messages;

namespace Yapplr.Api.Tests.Services;

public class PostServiceMultipleMediaTests : IDisposable
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

    public PostServiceMultipleMediaTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
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

        // Setup default mocks
        _mockTrustBasedModerationService
            .Setup(x => x.CanPerformActionAsync(It.IsAny<int>(), It.IsAny<TrustRequiredAction>()))
            .ReturnsAsync(true);

        _mockTrustBasedModerationService
            .Setup(x => x.ShouldAutoHideContentAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockCountCacheService
            .Setup(x => x.InvalidateUserCountsAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

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

        // Seed test user
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreatePostWithMediaAsync_WithMultipleImages_ShouldCreatePostWithMediaRecords()
    {
        // Arrange
        var createDto = new CreatePostWithMediaDto(
            "Test post with multiple images",
            PostPrivacy.Public,
            new List<MediaFileDto>
            {
                new("image1.jpg", MediaType.Image, 1920, 1080, 1024000),
                new("image2.png", MediaType.Image, 1280, 720, 512000)
            }
        );

        // Act
        var result = await _postService.CreatePostWithMediaAsync(1, createDto);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Test post with multiple images");
        result.MediaItems.Should().HaveCount(2);
        result.MediaItems.Should().AllSatisfy(m => m.MediaType.Should().Be(MediaType.Image));

        // Verify database records
        var post = await _context.Posts
            .Include(p => p.PostMedia)
            .FirstAsync(p => p.Id == result.Id);

        post.PostMedia.Should().HaveCount(2);
        post.PostMedia.Should().AllSatisfy(m => m.MediaType.Should().Be(MediaType.Image));
    }

    [Fact]
    public async Task CreatePostWithMediaAsync_WithMultipleVideos_ShouldCreatePostWithVideoProcessing()
    {
        // Arrange
        var createDto = new CreatePostWithMediaDto(
            "Test post with multiple videos",
            PostPrivacy.Public,
            new List<MediaFileDto>
            {
                new("video1.mp4", MediaType.Video, 1920, 1080, 10240000, TimeSpan.FromSeconds(30)),
                new("video2.mp4", MediaType.Video, 1280, 720, 5120000, TimeSpan.FromSeconds(15))
            }
        );

        // Act
        var result = await _postService.CreatePostWithMediaAsync(1, createDto);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Test post with multiple videos");
        result.MediaItems.Should().HaveCount(2);
        result.MediaItems.Should().AllSatisfy(m => m.MediaType.Should().Be(MediaType.Video));

        // Verify post is hidden during video processing
        var post = await _context.Posts.FirstAsync(p => p.Id == result.Id);
        post.IsHiddenDuringVideoProcessing.Should().BeTrue();

        // Verify video processing was triggered via RabbitMQ
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<VideoProcessingRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2) // Should be called twice for two videos
        );
    }

    [Fact]
    public async Task CreatePostWithMediaAsync_WithMixedMedia_ShouldCreatePostWithBothTypes()
    {
        // Arrange
        var createDto = new CreatePostWithMediaDto(
            "Test post with mixed media",
            PostPrivacy.Public,
            new List<MediaFileDto>
            {
                new("image1.jpg", MediaType.Image, 1920, 1080, 1024000),
                new("video1.mp4", MediaType.Video, 1920, 1080, 10240000, TimeSpan.FromSeconds(30)),
                new("image2.png", MediaType.Image, 1280, 720, 512000)
            }
        );

        // Act
        var result = await _postService.CreatePostWithMediaAsync(1, createDto);

        // Assert
        result.Should().NotBeNull();
        result.MediaItems.Should().HaveCount(3);
        result.MediaItems.Should().Contain(m => m.MediaType == MediaType.Image);
        result.MediaItems.Should().Contain(m => m.MediaType == MediaType.Video);

        // Verify database records
        var post = await _context.Posts
            .Include(p => p.PostMedia)
            .FirstAsync(p => p.Id == result.Id);

        post.PostMedia.Should().HaveCount(3);
        post.PostMedia.Count(m => m.MediaType == MediaType.Image).Should().Be(2);
        post.PostMedia.Count(m => m.MediaType == MediaType.Video).Should().Be(1);

        // Should be hidden due to video
        post.IsHiddenDuringVideoProcessing.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePostWithMediaAsync_WithTooManyFiles_ShouldThrowException()
    {
        // Arrange
        var mediaFiles = new List<MediaFileDto>();
        for (int i = 0; i < 11; i++)
        {
            mediaFiles.Add(new($"image{i}.jpg", MediaType.Image, 1920, 1080, 1024000));
        }

        var createDto = new CreatePostWithMediaDto(
            "Test post with too many files",
            PostPrivacy.Public,
            mediaFiles
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _postService.CreatePostWithMediaAsync(1, createDto)
        );
    }

    [Fact]
    public async Task CreatePostWithMediaAsync_WithNoMedia_ShouldCreatePostWithoutMedia()
    {
        // Arrange
        var createDto = new CreatePostWithMediaDto(
            "Test post without media",
            PostPrivacy.Public,
            null
        );

        // Act
        var result = await _postService.CreatePostWithMediaAsync(1, createDto);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Test post without media");
        result.MediaItems.Should().BeEmpty();

        // Verify database
        var post = await _context.Posts
            .Include(p => p.PostMedia)
            .FirstAsync(p => p.Id == result.Id);

        post.PostMedia.Should().BeEmpty();
        post.IsHiddenDuringVideoProcessing.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePostWithMediaAsync_WithInsufficientTrust_ShouldThrowException()
    {
        // Arrange
        _mockTrustBasedModerationService
            .Setup(x => x.CanPerformActionAsync(1, TrustRequiredAction.CreatePost))
            .ReturnsAsync(false);

        var createDto = new CreatePostWithMediaDto(
            "Test post",
            PostPrivacy.Public,
            new List<MediaFileDto> { new("image1.jpg", MediaType.Image, 1920, 1080, 1024000) }
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _postService.CreatePostWithMediaAsync(1, createDto)
        );
    }

    [Fact]
    public async Task CreatePostAsync_WithMediaFileNames_ShouldCreateMultipleMediaRecords()
    {
        // Arrange
        var createDto = new CreatePostDto(
            "Test post with legacy multiple media",
            null,
            null,
            PostPrivacy.Public,
            new List<string> { "image1.jpg", "video1.mp4", "image2.png" }
        );

        // Act
        var result = await _postService.CreatePostAsync(1, createDto);

        // Assert
        result.Should().NotBeNull();
        result.MediaItems.Should().HaveCount(3);

        // Verify database records
        var post = await _context.Posts
            .Include(p => p.PostMedia)
            .FirstAsync(p => p.Id == result.Id);

        post.PostMedia.Should().HaveCount(3);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
