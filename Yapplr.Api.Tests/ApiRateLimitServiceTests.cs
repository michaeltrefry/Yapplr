using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Yapplr.Api.Services;
using Yapplr.Api.Configuration;

namespace Yapplr.Api.Tests;

public class ApiRateLimitServiceTests : IDisposable
{
    private readonly Mock<ILogger<ApiRateLimitService>> _mockLogger;
    private readonly Mock<ITrustBasedModerationService> _mockTrustBasedModerationService;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IOptions<RateLimitingConfiguration>> _mockRateLimitingOptions;
    private readonly ApiRateLimitService _service;

    public ApiRateLimitServiceTests()
    {
        _mockLogger = new Mock<ILogger<ApiRateLimitService>>();
        _mockTrustBasedModerationService = new Mock<ITrustBasedModerationService>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockRateLimitingOptions = new Mock<IOptions<RateLimitingConfiguration>>();

        // Setup default configuration
        _mockRateLimitingOptions.Setup(x => x.Value).Returns(new RateLimitingConfiguration
        {
            Enabled = true,
            TrustBasedEnabled = true,
            BurstProtectionEnabled = true,
            AutoBlockingEnabled = true,
            AutoBlockViolationThreshold = 15,
            AutoBlockDurationHours = 2,
            ApplyToAdmins = false,
            ApplyToModerators = false,
            FallbackMultiplier = 1.0f
        });

        _service = new ApiRateLimitService(
            _mockLogger.Object,
            _mockServiceScopeFactory.Object,
            _mockRateLimitingOptions.Object);
    }

    [Theory(Skip = "Service scope factory mocking needs to be implemented")]
    [InlineData(ApiOperation.CreatePost, 1.0f, 5)] // Normal trust = 5 posts per minute
    [InlineData(ApiOperation.CreatePost, 2.0f, 10)] // High trust = 10 posts per minute
    [InlineData(ApiOperation.CreatePost, 0.5f, 2)] // Low trust = 2 posts per minute (minimum 1)
    [InlineData(ApiOperation.LikePost, 1.0f, 30)] // Normal trust = 30 likes per minute
    [InlineData(ApiOperation.LikePost, 2.0f, 60)] // High trust = 60 likes per minute
    [InlineData(ApiOperation.LikePost, 0.25f, 7)] // Very low trust = 7 likes per minute
    public async Task CheckRateLimitAsync_AppliesTrustMultiplierCorrectly(ApiOperation operation, float trustMultiplier, int expectedLimit)
    {
        // Arrange
        var userId = 1;
        _mockTrustBasedModerationService.Setup(x => x.GetRateLimitMultiplierAsync(userId))
            .ReturnsAsync(trustMultiplier);

        // Act
        var result = await _service.CheckRateLimitAsync(userId, operation);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.True(result.RemainingRequests >= expectedLimit - 1); // Should be close to expected limit
    }

    [Fact(Skip = "Service scope factory mocking needs to be implemented")]
    public async Task CheckRateLimitAsync_AllowsRequestsWithinLimit()
    {
        // Arrange
        var userId = 1;
        var operation = ApiOperation.CreatePost;
        _mockTrustBasedModerationService.Setup(x => x.GetRateLimitMultiplierAsync(userId))
            .ReturnsAsync(1.0f);

        // Act
        var result = await _service.CheckRateLimitAsync(userId, operation);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.True(result.RemainingRequests >= 0);
        Assert.NotNull(result.ResetTime);
    }

    [Fact(Skip = "Service scope factory mocking needs to be implemented")]
    public async Task CheckRateLimitAsync_BlocksRequestsWhenLimitExceeded()
    {
        // Arrange
        var userId = 1;
        var operation = ApiOperation.CreatePost;
        _mockTrustBasedModerationService.Setup(x => x.GetRateLimitMultiplierAsync(userId))
            .ReturnsAsync(1.0f);

        // Make requests up to the burst limit first (3 for CreatePost)
        for (int i = 0; i < 3; i++)
        {
            await _service.RecordRequestAsync(userId, operation);
        }

        // Act
        var result = await _service.CheckRateLimitAsync(userId, operation);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal("burst", result.ViolationType);
        Assert.NotNull(result.RetryAfter);
    }

    [Fact(Skip = "Service scope factory mocking needs to be implemented")]
    public async Task CheckRateLimitAsync_HandlesBurstProtection()
    {
        // Arrange
        var userId = 1;
        var operation = ApiOperation.CreatePost;
        _mockTrustBasedModerationService.Setup(x => x.GetRateLimitMultiplierAsync(userId))
            .ReturnsAsync(1.0f);

        // Make burst requests (3 is the burst threshold for CreatePost)
        for (int i = 0; i < 3; i++)
        {
            await _service.RecordRequestAsync(userId, operation);
        }

        // Act
        var result = await _service.CheckRateLimitAsync(userId, operation);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal("burst", result.ViolationType);
        Assert.Equal(TimeSpan.FromSeconds(10), result.RetryAfter);
    }

    [Fact(Skip = "Service scope factory mocking needs to be implemented")]
    public async Task RecordRequestAsync_IncrementsRequestCount()
    {
        // Arrange
        var userId = 1;
        var operation = ApiOperation.CreatePost;
        _mockTrustBasedModerationService.Setup(x => x.GetRateLimitMultiplierAsync(userId))
            .ReturnsAsync(1.0f);

        // Act
        await _service.RecordRequestAsync(userId, operation);
        var result = await _service.CheckRateLimitAsync(userId, operation);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(3, result.RemainingRequests); // 5 - 1 - 1 (for the check) = 3 remaining
    }

    [Fact(Skip = "Service scope factory mocking needs to be implemented")]
    public async Task BlockUserAsync_PreventsAllRequests()
    {
        // Arrange
        var userId = 1;
        var operation = ApiOperation.CreatePost;
        _mockTrustBasedModerationService.Setup(x => x.GetRateLimitMultiplierAsync(userId))
            .ReturnsAsync(1.0f);

        // Act
        await _service.BlockUserAsync(userId, TimeSpan.FromHours(1), "Test block");
        var result = await _service.CheckRateLimitAsync(userId, operation);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal("blocked", result.ViolationType);
        Assert.True(result.RetryAfter > TimeSpan.Zero);
    }

    [Fact(Skip = "Service scope factory mocking needs to be implemented")]
    public async Task IsUserBlockedAsync_ReturnsTrueForBlockedUser()
    {
        // Arrange
        var userId = 1;

        // Act
        await _service.BlockUserAsync(userId, TimeSpan.FromHours(1), "Test block");
        var isBlocked = await _service.IsUserBlockedAsync(userId);

        // Assert
        Assert.True(isBlocked);
    }

    [Fact(Skip = "Service scope factory mocking needs to be implemented")]
    public async Task UnblockUserAsync_RemovesBlock()
    {
        // Arrange
        var userId = 1;
        await _service.BlockUserAsync(userId, TimeSpan.FromHours(1), "Test block");

        // Act
        await _service.UnblockUserAsync(userId);
        var isBlocked = await _service.IsUserBlockedAsync(userId);

        // Assert
        Assert.False(isBlocked);
    }

    [Fact(Skip = "Service scope factory mocking needs to be implemented")]
    public async Task ResetUserLimitsAsync_ClearsAllUserData()
    {
        // Arrange
        var userId = 1;
        var operation = ApiOperation.CreatePost;
        _mockTrustBasedModerationService.Setup(x => x.GetRateLimitMultiplierAsync(userId))
            .ReturnsAsync(1.0f);

        // Make some requests and block user
        await _service.RecordRequestAsync(userId, operation);
        await _service.BlockUserAsync(userId, TimeSpan.FromHours(1), "Test block");

        // Act
        await _service.ResetUserLimitsAsync(userId);

        // Assert
        var isBlocked = await _service.IsUserBlockedAsync(userId);
        var violations = await _service.GetRecentViolationsAsync(userId);
        var result = await _service.CheckRateLimitAsync(userId, operation);

        Assert.False(isBlocked);
        Assert.Empty(violations);
        Assert.True(result.IsAllowed);
        Assert.Equal(4, result.RemainingRequests); // Should be back to normal (5 - 1 for the check)
    }

    [Theory(Skip = "Service scope factory mocking needs to be implemented")]
    [InlineData(ApiOperation.CreatePost)]
    [InlineData(ApiOperation.CreateComment)]
    [InlineData(ApiOperation.LikePost)]
    [InlineData(ApiOperation.FollowUser)]
    [InlineData(ApiOperation.ReportContent)]
    [InlineData(ApiOperation.SendMessage)]
    [InlineData(ApiOperation.UploadMedia)]
    [InlineData(ApiOperation.Search)]
    public async Task CheckRateLimitAsync_HandlesAllOperationTypes(ApiOperation operation)
    {
        // Arrange
        var userId = 1;
        _mockTrustBasedModerationService.Setup(x => x.GetRateLimitMultiplierAsync(userId))
            .ReturnsAsync(1.0f);

        // Act
        var result = await _service.CheckRateLimitAsync(userId, operation);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.True(result.RemainingRequests >= 0);
    }

    [Fact]
    public async Task GetRateLimitStatsAsync_ReturnsValidStats()
    {
        // Arrange
        var userId = 1;
        var operation = ApiOperation.CreatePost;

        await _service.RecordRequestAsync(userId, operation);

        // Act
        var stats = await _service.GetRateLimitStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.ContainsKey("total_requests"));
        Assert.True(stats.ContainsKey("total_violations"));
        Assert.True(stats.ContainsKey("base_rate_limit_configs"));
        Assert.True((long)stats["total_requests"] > 0);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
