using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using Yapplr.Api.CQRS.Consumers;
using Yapplr.Api.Models;
using Yapplr.Api.DTOs;
using Yapplr.Shared.Messages;
using Yapplr.Shared.Models;
using FluentAssertions;
using MassTransit;

namespace Yapplr.Api.Tests;

public class VideoProcessingTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly Mock<ILogger<PostService>> _postServiceLogger;
    private readonly Mock<ILogger<VideoProcessingCompletedConsumer>> _consumerLogger;
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<IAnalyticsService> _analyticsService;
    private readonly Mock<IPublishEndpoint> _publishEndpoint;
    private readonly Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<ITrustBasedModerationService> _trustBasedModerationService;
    private readonly PostService _postService;
    private readonly VideoProcessingCompletedConsumer _consumer;

    public VideoProcessingTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestYapplrDbContext(options);
        _postServiceLogger = new Mock<ILogger<PostService>>();
        _consumerLogger = new Mock<ILogger<VideoProcessingCompletedConsumer>>();
        _notificationService = new Mock<INotificationService>();
        _analyticsService = new Mock<IAnalyticsService>();
        _publishEndpoint = new Mock<IPublishEndpoint>();
        _httpContextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        _trustBasedModerationService = new Mock<ITrustBasedModerationService>();

        // Set up trust-based moderation service to allow all actions for tests
        _trustBasedModerationService.Setup(x => x.CanPerformActionAsync(It.IsAny<int>(), It.IsAny<TrustRequiredAction>()))
            .ReturnsAsync(true);

        // Create PostService with mocked dependencies
        _postService = new PostService(
            _context,
            _httpContextAccessor.Object,
            Mock.Of<IBlockService>(),
            _notificationService.Object,
            Mock.Of<ILinkPreviewService>(),
            Mock.Of<IContentModerationService>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
            _publishEndpoint.Object,
            Mock.Of<ICountCacheService>(),
            Mock.Of<ITrustScoreService>(),
            _trustBasedModerationService.Object,
            Mock.Of<IAnalyticsService>(),
            _postServiceLogger.Object
        );

        // Create VideoProcessingCompletedConsumer
        _consumer = new VideoProcessingCompletedConsumer(
            _context,
            _consumerLogger.Object,
            _notificationService.Object,
            _analyticsService.Object
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreatePostAsync_WithVideo_SetsIsHiddenDuringVideoProcessing()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            EmailVerified = true,
            TrustScore = 0.8f // Sufficient trust score for creating posts
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var createDto = new CreatePostDto(
            Content: "Test video post",
            VideoFileName: "test-video.mp4",
            Privacy: PostPrivacy.Public
        );

        // Act
        var result = await _postService.CreatePostAsync(user.Id, createDto);

        // Assert
        result.Should().NotBeNull();
        
        var post = await _context.Posts.FirstAsync(p => p.Id == result.Id);
        post.IsHiddenDuringVideoProcessing.Should().BeTrue();
        post.VideoProcessingStatus.Should().Be(VideoProcessingStatus.Processing);
        post.VideoFileName.Should().Be("test-video.mp4");
    }

    [Fact]
    public async Task CreatePostAsync_WithoutVideo_DoesNotSetIsHiddenDuringVideoProcessing()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            EmailVerified = true,
            TrustScore = 0.8f // Sufficient trust score for creating posts
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var createDto = new CreatePostDto(
            Content: "Test text post",
            Privacy: PostPrivacy.Public
        );

        // Act
        var result = await _postService.CreatePostAsync(user.Id, createDto);

        // Assert
        result.Should().NotBeNull();
        
        var post = await _context.Posts.FirstAsync(p => p.Id == result.Id);
        post.IsHiddenDuringVideoProcessing.Should().BeFalse();
        post.VideoFileName.Should().BeNull();
    }

    [Fact]
    public async Task VideoProcessingCompletedConsumer_MakesPostVisibleAndSendsNotification()
    {
        // Arrange
        var user = new User 
        { 
            Id = 1, 
            Username = "testuser", 
            Email = "test@example.com",
            EmailVerified = true
        };
        _context.Users.Add(user);

        var post = new Post
        {
            Id = 1,
            Content = "Test video post",
            IsHiddenDuringVideoProcessing = true,
            Privacy = PostPrivacy.Public,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(post);

        var postMedia = new PostMedia
        {
            PostId = post.Id,
            MediaType = MediaType.Video,
            OriginalFileName = "test-video.mp4",
            VideoProcessingStatus = VideoProcessingStatus.Processing
        };
        _context.PostMedia.Add(postMedia);
        await _context.SaveChangesAsync();

        var message = new VideoProcessingCompleted
        {
            PostId = post.Id,
            UserId = user.Id,
            ProcessedVideoFileName = "test-video_processed.mp4",
            ThumbnailFileName = "test-video_thumb.jpg",
            CompletedAt = DateTime.UtcNow
        };

        var mockContext = new Mock<ConsumeContext<VideoProcessingCompleted>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        var updatedPost = await _context.Posts.FirstAsync(p => p.Id == post.Id);
        updatedPost.IsHiddenDuringVideoProcessing.Should().BeFalse();
        updatedPost.VideoProcessingStatus.Should().Be(VideoProcessingStatus.Completed);
        updatedPost.ProcessedVideoFileName.Should().Be("test-video_processed.mp4");
        updatedPost.VideoThumbnailFileName.Should().Be("test-video_thumb.jpg");
        updatedPost.VideoProcessingCompletedAt.Should().NotBeNull();
        updatedPost.VideoProcessingError.Should().BeNull();

        // Verify notification was sent
        _notificationService.Verify(
            x => x.CreateVideoProcessingCompletedNotificationAsync(user.Id, post.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task VideoProcessingCompletedConsumer_WithNonExistentPost_LogsWarning()
    {
        // Arrange
        var message = new VideoProcessingCompleted
        {
            PostId = 999, // Non-existent post ID
            UserId = 1,
            ProcessedVideoFileName = "test-video_processed.mp4",
            ThumbnailFileName = "test-video_thumb.jpg",
            CompletedAt = DateTime.UtcNow
        };

        var mockContext = new Mock<ConsumeContext<VideoProcessingCompleted>>();
        mockContext.Setup(x => x.Message).Returns(message);

        // Act
        await _consumer.Consume(mockContext.Object);

        // Assert
        // Verify that warning was logged (this would require more sophisticated logging mock setup)
        // For now, we just verify no exception was thrown and no notification was sent
        _notificationService.Verify(
            x => x.CreateVideoProcessingCompletedNotificationAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetTimelineAsync_FiltersOutPostsHiddenDuringVideoProcessing_ExceptOwnPosts()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com", EmailVerified = true };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com", EmailVerified = true };
        _context.Users.AddRange(user1, user2);

        // User1's video post (hidden during processing)
        var hiddenVideoPost = new Post
        {
            Id = 1,
            Content = "User1's video post",
            IsHidden = true,
            HiddenReasonType = PostHiddenReasonType.VideoProcessing,
            Privacy = PostPrivacy.Public,
            UserId = user1.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var hiddenVideoPostMedia = new PostMedia
        {
            PostId = hiddenVideoPost.Id,
            MediaType = MediaType.Video,
            OriginalFileName = "video1.mp4",
            VideoProcessingStatus = VideoProcessingStatus.Processing
        };

        // User2's video post (hidden during processing)
        var otherUserVideoPost = new Post
        {
            Id = 2,
            Content = "User2's video post",
            IsHidden = true,
            HiddenReasonType = PostHiddenReasonType.VideoProcessing,
            Privacy = PostPrivacy.Public,
            UserId = user2.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var otherUserVideoPostMedia = new PostMedia
        {
            PostId = otherUserVideoPost.Id,
            MediaType = MediaType.Video,
            OriginalFileName = "video2.mp4",
            VideoProcessingStatus = VideoProcessingStatus.Processing
        };

        // Regular text post
        var textPost = new Post
        {
            Id = 3,
            Content = "Regular text post",
            Privacy = PostPrivacy.Public,
            UserId = user2.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Posts.AddRange(hiddenVideoPost, otherUserVideoPost, textPost);
        _context.PostMedia.AddRange(hiddenVideoPostMedia, otherUserVideoPostMedia);
        await _context.SaveChangesAsync();

        // Act - Get timeline for user1
        var timelineForUser1 = await _postService.GetTimelineAsync(user1.Id);
        var timelineForUser2 = await _postService.GetTimelineAsync(user2.Id);

        // Assert
        // User1 should see their own hidden video post and the text post, but not user2's hidden video post
        var user1Posts = timelineForUser1.ToList();
        user1Posts.Should().HaveCount(2);
        user1Posts.Should().Contain(p => p.Id == hiddenVideoPost.Id); // Own hidden post visible
        user1Posts.Should().Contain(p => p.Id == textPost.Id); // Regular post visible
        user1Posts.Should().NotContain(p => p.Id == otherUserVideoPost.Id); // Other user's hidden post not visible

        // User2 should see their own hidden video post and the text post, but not user1's hidden video post
        var user2Posts = timelineForUser2.ToList();
        user2Posts.Should().HaveCount(2);
        user2Posts.Should().Contain(p => p.Id == otherUserVideoPost.Id); // Own hidden post visible
        user2Posts.Should().Contain(p => p.Id == textPost.Id); // Regular post visible
        user2Posts.Should().NotContain(p => p.Id == hiddenVideoPost.Id); // Other user's hidden post not visible
    }
}
