using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Analytics;

namespace Yapplr.Api.Tests;

public class TrustBasedModerationServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<ITrustScoreService> _mockTrustScoreService;
    private readonly Mock<IAnalyticsService> _mockAnalyticsService;
    private readonly Mock<ILogger<TrustBasedModerationService>> _mockLogger;
    private readonly TrustBasedModerationService _service;

    public TrustBasedModerationServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockTrustScoreService = new Mock<ITrustScoreService>();
        _mockAnalyticsService = new Mock<IAnalyticsService>();
        _mockLogger = new Mock<ILogger<TrustBasedModerationService>>();

        _service = new TrustBasedModerationService(
            _context, 
            _mockTrustScoreService.Object, 
            _mockAnalyticsService.Object, 
            _mockLogger.Object);
    }

    [Theory]
    [InlineData(0.9f, 2.0f)] // High trust = 2x rate limit
    [InlineData(0.6f, 1.5f)] // Medium trust = 1.5x rate limit
    [InlineData(0.4f, 1.0f)] // Low trust = normal rate limit
    [InlineData(0.2f, 0.5f)] // Very low trust = 0.5x rate limit
    [InlineData(0.05f, 0.25f)] // Extremely low trust = 0.25x rate limit
    public async Task GetRateLimitMultiplierAsync_ReturnsCorrectMultiplier(float trustScore, float expectedMultiplier)
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ReturnsAsync(trustScore);

        // Act
        var result = await _service.GetRateLimitMultiplierAsync(userId);

        // Assert
        Assert.Equal(expectedMultiplier, result);
    }

    [Theory]
    [InlineData(0.05f, true)]  // Very low trust should auto-hide
    [InlineData(0.15f, false)] // Low trust should not auto-hide
    [InlineData(0.5f, false)]  // Medium trust should not auto-hide
    [InlineData(0.9f, false)]  // High trust should not auto-hide
    public async Task ShouldAutoHideContentAsync_ReturnsCorrectDecision(float trustScore, bool shouldHide)
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ReturnsAsync(trustScore);

        // Act
        var result = await _service.ShouldAutoHideContentAsync(userId, "post");

        // Assert
        Assert.Equal(shouldHide, result);
    }

    [Theory]
    [InlineData(0.05f, 1)] // Very low trust = highest priority
    [InlineData(0.2f, 2)]  // Low trust = high priority
    [InlineData(0.4f, 3)]  // Medium trust = medium priority
    [InlineData(0.7f, 4)]  // High trust = low priority
    [InlineData(0.9f, 5)]  // Very high trust = lowest priority
    public async Task GetModerationPriorityAsync_ReturnsCorrectPriority(float trustScore, int expectedPriority)
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ReturnsAsync(trustScore);

        // Act
        var result = await _service.GetModerationPriorityAsync(userId, "post");

        // Assert
        Assert.Equal(expectedPriority, result);
    }

    [Theory]
    [InlineData(TrustRequiredAction.CreatePost, 0.15f, true)]
    [InlineData(TrustRequiredAction.CreatePost, 0.05f, false)]
    [InlineData(TrustRequiredAction.CreateComment, 0.15f, true)]
    [InlineData(TrustRequiredAction.CreateComment, 0.05f, false)]
    [InlineData(TrustRequiredAction.LikeContent, 0.06f, true)]
    [InlineData(TrustRequiredAction.LikeContent, 0.04f, false)]
    [InlineData(TrustRequiredAction.ReportContent, 0.25f, true)]
    [InlineData(TrustRequiredAction.ReportContent, 0.15f, false)]
    [InlineData(TrustRequiredAction.SendMessage, 0.35f, true)]
    [InlineData(TrustRequiredAction.SendMessage, 0.25f, false)]
    public async Task CanPerformActionAsync_ReturnsCorrectPermission(TrustRequiredAction action, float trustScore, bool canPerform)
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ReturnsAsync(trustScore);

        // Act
        var result = await _service.CanPerformActionAsync(userId, action);

        // Assert
        Assert.Equal(canPerform, result);
    }

    [Theory]
    [InlineData(0.9f, ContentVisibilityLevel.FullVisibility)]
    [InlineData(0.6f, ContentVisibilityLevel.NormalVisibility)]
    [InlineData(0.4f, ContentVisibilityLevel.ReducedVisibility)]
    [InlineData(0.2f, ContentVisibilityLevel.LimitedVisibility)]
    [InlineData(0.05f, ContentVisibilityLevel.Hidden)]
    public async Task GetContentVisibilityLevelAsync_ReturnsCorrectLevel(float trustScore, ContentVisibilityLevel expectedLevel)
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ReturnsAsync(trustScore);

        // Act
        var result = await _service.GetContentVisibilityLevelAsync(userId);

        // Assert
        Assert.Equal(expectedLevel, result);
    }

    [Theory]
    [InlineData(0.9f, 0.3f)] // High trust = low threshold (reports taken seriously)
    [InlineData(0.6f, 0.5f)] // Medium trust = medium threshold
    [InlineData(0.4f, 0.7f)] // Low trust = high threshold
    [InlineData(0.2f, 0.9f)] // Very low trust = very high threshold
    public async Task GetReportReviewThresholdAsync_ReturnsCorrectThreshold(float trustScore, float expectedThreshold)
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ReturnsAsync(trustScore);

        // Act
        var result = await _service.GetReportReviewThresholdAsync(userId);

        // Assert
        Assert.Equal(expectedThreshold, result);
    }

    [Fact]
    public async Task GetRateLimitMultiplierAsync_OnError_ReturnsDefaultMultiplier()
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetRateLimitMultiplierAsync(userId);

        // Assert
        Assert.Equal(1.0f, result); // Should return default multiplier on error
    }

    [Fact]
    public async Task ShouldAutoHideContentAsync_OnError_ReturnsFalse()
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.ShouldAutoHideContentAsync(userId, "post");

        // Assert
        Assert.False(result); // Should not auto-hide on error
    }

    [Fact]
    public async Task GetModerationPriorityAsync_OnError_ReturnsDefaultPriority()
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetModerationPriorityAsync(userId, "post");

        // Assert
        Assert.Equal(3, result); // Should return medium priority on error
    }

    [Fact]
    public async Task CanPerformActionAsync_OnError_ReturnsTrue()
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.CanPerformActionAsync(userId, TrustRequiredAction.CreatePost);

        // Assert
        Assert.True(result); // Should allow action on error to avoid blocking users
    }

    [Fact]
    public async Task GetContentVisibilityLevelAsync_OnError_ReturnsNormalVisibility()
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetContentVisibilityLevelAsync(userId);

        // Assert
        Assert.Equal(ContentVisibilityLevel.NormalVisibility, result);
    }

    [Fact]
    public async Task GetReportReviewThresholdAsync_OnError_ReturnsDefaultThreshold()
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.GetReportReviewThresholdAsync(userId);

        // Assert
        Assert.Equal(0.5f, result); // Should return default threshold on error
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
