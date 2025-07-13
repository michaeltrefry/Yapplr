using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Services;

namespace Yapplr.Api.Tests;

public class TrustScoreServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<IAnalyticsService> _mockAnalyticsService;
    private readonly Mock<ILogger<TrustScoreService>> _mockLogger;
    private readonly TrustScoreService _trustScoreService;

    public TrustScoreServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockAnalyticsService = new Mock<IAnalyticsService>();
        _mockLogger = new Mock<ILogger<TrustScoreService>>();

        _trustScoreService = new TrustScoreService(_context, _mockAnalyticsService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CalculateUserTrustScoreAsync_NewUser_ReturnsDefaultScore()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastSeenAt = DateTime.UtcNow,
            TrustScore = 1.0f
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _trustScoreService.CalculateUserTrustScoreAsync(user.Id);

        // Assert
        Assert.True(result >= 0.0f && result <= 1.0f);
        Assert.True(result > 0.5f); // New users should have reasonable starting score
    }

    [Fact]
    public async Task CalculateUserTrustScoreAsync_VerifiedUser_HasHigherScore()
    {
        // Arrange
        var unverifiedUser = new User
        {
            Id = 1,
            Username = "unverified",
            Email = "unverified@example.com",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastSeenAt = DateTime.UtcNow,
            TrustScore = 0.7f
        };

        var verifiedUser = new User
        {
            Id = 2,
            Username = "verified",
            Email = "verified@example.com",
            EmailVerified = true,
            Bio = "I'm a verified user",
            Tagline = "Trusted member",
            ProfileImageFileName = "profile.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastSeenAt = DateTime.UtcNow,
            TrustScore = 0.7f
        };

        _context.Users.AddRange(unverifiedUser, verifiedUser);
        await _context.SaveChangesAsync();

        // Act
        var unverifiedScore = await _trustScoreService.CalculateUserTrustScoreAsync(unverifiedUser.Id);
        var verifiedScore = await _trustScoreService.CalculateUserTrustScoreAsync(verifiedUser.Id);

        // Assert
        // Verified user should have higher score due to email verification and complete profile
        Assert.True(verifiedScore >= unverifiedScore,
            $"Verified user score ({verifiedScore}) should be >= unverified user score ({unverifiedScore})");
    }

    [Fact]
    public async Task UpdateTrustScoreForActionAsync_PositiveAction_IncreasesScore()
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.UpdateUserTrustScoreAsync(
            It.IsAny<int>(), It.IsAny<float>(), It.IsAny<TrustScoreChangeReason>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<float>()))
            .Returns(Task.CompletedTask);

        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ReturnsAsync(1.01f);

        // Act
        var result = await _trustScoreService.UpdateTrustScoreForActionAsync(
            userId, TrustScoreAction.PostCreated, "post", 1, "Test post");

        // Assert
        Assert.Equal(1.01f, result);
        _mockAnalyticsService.Verify(x => x.UpdateUserTrustScoreAsync(
            userId, 0.01f, TrustScoreChangeReason.PositiveEngagement,
            "Action: PostCreated", It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<int?>(), "TrustScoreService", true, 0.7f), Times.Once);
    }

    [Fact]
    public async Task UpdateTrustScoreForActionAsync_NegativeAction_DecreasesScore()
    {
        // Arrange
        var userId = 1;
        _mockAnalyticsService.Setup(x => x.UpdateUserTrustScoreAsync(
            It.IsAny<int>(), It.IsAny<float>(), It.IsAny<TrustScoreChangeReason>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<float>()))
            .Returns(Task.CompletedTask);

        _mockAnalyticsService.Setup(x => x.GetCurrentUserTrustScoreAsync(userId))
            .ReturnsAsync(0.9f);

        // Act
        var result = await _trustScoreService.UpdateTrustScoreForActionAsync(
            userId, TrustScoreAction.ContentHidden, "post", 1, "Inappropriate content");

        // Assert
        Assert.Equal(0.9f, result);
        _mockAnalyticsService.Verify(x => x.UpdateUserTrustScoreAsync(
            userId, -0.1f, TrustScoreChangeReason.ContentModeration,
            "Action: ContentHidden", It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<int?>(), "TrustScoreService", true, 0.9f), Times.Once);
    }

    [Fact]
    public async Task GetTrustScoreFactorsAsync_ValidUser_ReturnsFactors()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            EmailVerified = true,
            Bio = "Test bio",
            Birthday = DateTime.UtcNow.AddYears(-25),
            Pronouns = "they/them",
            Tagline = "Test tagline",
            ProfileImageFileName = "profile.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastSeenAt = DateTime.UtcNow,
            TrustScore = 1.0f
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var factors = await _trustScoreService.GetTrustScoreFactorsAsync(user.Id);

        // Assert
        Assert.NotNull(factors);
        Assert.Contains("baseScore", factors.Keys);
        Assert.Contains("accountAge", factors.Keys);
        Assert.Contains("emailVerified", factors.Keys);
        Assert.Contains("profileCompleteness", factors.Keys);
        Assert.Equal(1.0f, factors["emailVerified"]);
        Assert.Equal(1.0f, factors["profileCompleteness"]); // All profile fields filled
    }

    [Fact]
    public async Task ApplyInactivityDecayAsync_InactiveUsers_ReducesTrustScore()
    {
        // Arrange
        var activeUser = new User
        {
            Id = 1,
            Username = "active",
            Email = "active@example.com",
            LastSeenAt = DateTime.UtcNow.AddDays(-5),
            TrustScore = 0.8f
        };

        var inactiveUser = new User
        {
            Id = 2,
            Username = "inactive",
            Email = "inactive@example.com",
            LastSeenAt = DateTime.UtcNow.AddDays(-35),
            TrustScore = 0.8f
        };

        _context.Users.AddRange(activeUser, inactiveUser);
        await _context.SaveChangesAsync();

        _mockAnalyticsService.Setup(x => x.UpdateUserTrustScoreAsync(
            It.IsAny<int>(), It.IsAny<float>(), It.IsAny<TrustScoreChangeReason>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<float>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _trustScoreService.ApplyInactivityDecayAsync(30, 0.01f);

        // Assert
        Assert.Equal(1, result); // Only one user should be affected
        _mockAnalyticsService.Verify(x => x.UpdateUserTrustScoreAsync(
            inactiveUser.Id, It.IsAny<float>(), It.IsAny<TrustScoreChangeReason>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<float>()), Times.Once);
    }

    [Fact]
    public async Task GetUsersWithLowTrustScoresAsync_ReturnsLowTrustUsers()
    {
        // Arrange
        var highTrustUser = new User
        {
            Id = 1,
            Username = "high",
            Email = "high@example.com",
            TrustScore = 0.8f
        };

        var lowTrustUser = new User
        {
            Id = 2,
            Username = "low",
            Email = "low@example.com",
            TrustScore = 0.2f
        };

        _context.Users.AddRange(highTrustUser, lowTrustUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _trustScoreService.GetUsersWithLowTrustScoresAsync(0.3f, 10);

        // Assert
        Assert.Single(result);
        Assert.Contains(lowTrustUser.Id, result);
        Assert.DoesNotContain(highTrustUser.Id, result);
    }

    [Fact]
    public async Task GetTrustScoreStatisticsAsync_ReturnsValidStatistics()
    {
        // Arrange
        var users = new[]
        {
            new User { Id = 1, Username = "user1", Email = "user1@example.com", TrustScore = 0.2f },
            new User { Id = 2, Username = "user2", Email = "user2@example.com", TrustScore = 0.5f },
            new User { Id = 3, Username = "user3", Email = "user3@example.com", TrustScore = 0.8f },
            new User { Id = 4, Username = "user4", Email = "user4@example.com", TrustScore = 0.9f }
        };

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _trustScoreService.GetTrustScoreStatisticsAsync();

        // Assert
        Assert.Equal(4, (int)stats["totalUsers"]);
        Assert.Equal(0.6f, (float)stats["averageScore"], 1);
        Assert.Equal(0.2f, (float)stats["minScore"]);
        Assert.Equal(0.9f, (float)stats["maxScore"]);
        
        var distribution = stats["distribution"] as Dictionary<string, int>;
        Assert.NotNull(distribution);
        Assert.Equal(0, distribution["0.0-0.2"]); // 0.2f is not < 0.2f
        Assert.Equal(1, distribution["0.2-0.4"]); // 0.2f falls here
        Assert.Equal(1, distribution["0.4-0.6"]); // 0.5f falls here
        Assert.Equal(2, distribution["0.8-1.0"]); // 0.8f and 0.9f fall here
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
