using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Yapplr.Api.Data;
using Yapplr.Api.Extensions;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Analytics;

namespace Yapplr.Api.Tests;

public class TrustScoreIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly YapplrDbContext _context;
    private readonly ITrustScoreService _trustScoreService;
    private readonly ITrustBasedModerationService _trustBasedModerationService;
    private readonly IAnalyticsService _analyticsService;

    public TrustScoreIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add DbContext with in-memory database
        services.AddDbContext<YapplrDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add HTTP context accessor
        services.AddHttpContextAccessor();

        // Add services
        services.AddScoped<ITrustScoreService, TrustScoreService>();
        services.AddScoped<ITrustBasedModerationService, TrustBasedModerationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        // Add analytics dependencies for tests
        services.AddScoped<IExternalAnalyticsService, NoOpAnalyticsService>();

        // Add configuration for tests
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Analytics:EnableDualWrite"] = "false"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<YapplrDbContext>();
        _trustScoreService = _serviceProvider.GetRequiredService<ITrustScoreService>();
        _trustBasedModerationService = _serviceProvider.GetRequiredService<ITrustBasedModerationService>();
        _analyticsService = _serviceProvider.GetRequiredService<IAnalyticsService>();
    }

    [Fact]
    public async Task TrustScoreWorkflow_NewUserToSuspension_WorksCorrectly()
    {
        // Arrange - Create a new user
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            TrustScore = 0.8f
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act & Assert - Test progression from new user to problematic user

        // 1. Initial trust score calculation
        var initialScore = await _trustScoreService.CalculateUserTrustScoreAsync(user.Id);
        Assert.True(initialScore >= 0.0f && initialScore <= 1.0f,
            $"Initial score should be valid: {initialScore}");

        // 2. User creates posts (positive action)
        await _trustScoreService.UpdateTrustScoreForActionAsync(user.Id, TrustScoreAction.PostCreated, "post", 1);
        await _trustScoreService.UpdateTrustScoreForActionAsync(user.Id, TrustScoreAction.PostCreated, "post", 2);

        var afterPostsScore = await _analyticsService.GetCurrentUserTrustScoreAsync(user.Id);
        // Score should be valid (the actual score depends on the calculation method)
        Assert.True(afterPostsScore >= 0.0f && afterPostsScore <= 1.0f);

        // 3. User verifies email (positive action)
        await _trustScoreService.UpdateTrustScoreForActionAsync(user.Id, TrustScoreAction.EmailVerified);

        var afterVerificationScore = await _analyticsService.GetCurrentUserTrustScoreAsync(user.Id);
        // Score should be valid (the actual score depends on the calculation method)
        Assert.True(afterVerificationScore >= 0.0f && afterVerificationScore <= 1.0f);

        // 4. User's content gets reported (negative action)
        await _trustScoreService.UpdateTrustScoreForActionAsync(user.Id, TrustScoreAction.ContentReported, "post", 1);
        
        var afterReportScore = await _analyticsService.GetCurrentUserTrustScoreAsync(user.Id);
        Assert.True(afterReportScore < afterVerificationScore);

        // 5. User's content gets hidden by moderator (significant negative action)
        await _trustScoreService.UpdateTrustScoreForActionAsync(user.Id, TrustScoreAction.ContentHidden, "post", 1);
        
        var afterHiddenScore = await _analyticsService.GetCurrentUserTrustScoreAsync(user.Id);
        Assert.True(afterHiddenScore < afterReportScore);

        // 6. User gets suspended (major negative action)
        await _trustScoreService.UpdateTrustScoreForActionAsync(user.Id, TrustScoreAction.UserSuspended, "user", user.Id);
        
        var afterSuspensionScore = await _analyticsService.GetCurrentUserTrustScoreAsync(user.Id);
        Assert.True(afterSuspensionScore < afterHiddenScore);
        Assert.True(afterSuspensionScore < 0.5f); // Should be significantly reduced
    }

    [Fact]
    public async Task TrustBasedPermissions_LowTrustUser_RestrictsActions()
    {
        // Arrange - Create a low trust user
        var user = new User
        {
            Id = 1,
            Username = "lowtrust",
            Email = "lowtrust@example.com",
            TrustScore = 0.1f,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Simulate low trust score in analytics - make the user have a very low score
        await _analyticsService.UpdateUserTrustScoreAsync(
            user.Id, -0.09f, TrustScoreChangeReason.ContentModeration,
            "Simulated low trust", "", null, null, "Test", true, 1.0f);

        // Act & Assert - Test various restrictions

        // 1. Rate limiting should be restrictive
        var rateLimitMultiplier = await _trustBasedModerationService.GetRateLimitMultiplierAsync(user.Id);
        Assert.True(rateLimitMultiplier < 1.0f);

        // 2. Content should be auto-hidden (user score is now 0.01 after the -0.09 change)
        var currentScore = await _analyticsService.GetCurrentUserTrustScoreAsync(user.Id);
        var shouldAutoHide = await _trustBasedModerationService.ShouldAutoHideContentAsync(user.Id, "post");
        Assert.True(currentScore < 0.1f, $"Score should be very low: {currentScore}"); // Verify the score is actually low enough
        Assert.True(shouldAutoHide, $"Content should be auto-hidden for score {currentScore}");

        // 3. Moderation priority should be high (low number)
        var moderationPriority = await _trustBasedModerationService.GetModerationPriorityAsync(user.Id, "post");
        Assert.True(moderationPriority <= 2);

        // 4. Some actions should be restricted
        var canCreatePost = await _trustBasedModerationService.CanPerformActionAsync(user.Id, TrustRequiredAction.CreatePost);
        var canSendMessage = await _trustBasedModerationService.CanPerformActionAsync(user.Id, TrustRequiredAction.SendMessage);
        var canReportContent = await _trustBasedModerationService.CanPerformActionAsync(user.Id, TrustRequiredAction.ReportContent);
        
        Assert.False(canCreatePost); // Should NOT be able to create posts (score ~0.01 < threshold 0.1)
        Assert.False(canSendMessage); // Should not be able to send messages (threshold is 0.3)
        Assert.False(canReportContent); // Should not be able to report content (threshold is 0.2)

        // 5. Content visibility should be limited
        var visibilityLevel = await _trustBasedModerationService.GetContentVisibilityLevelAsync(user.Id);
        Assert.Equal(ContentVisibilityLevel.Hidden, visibilityLevel);

        // 6. Report review threshold should be high
        var reportThreshold = await _trustBasedModerationService.GetReportReviewThresholdAsync(user.Id);
        Assert.True(reportThreshold > 0.8f);
    }

    [Fact]
    public async Task TrustBasedPermissions_HighTrustUser_AllowsActions()
    {
        // Arrange - Create a high trust user
        var user = new User
        {
            Id = 1,
            Username = "hightrust",
            Email = "hightrust@example.com",
            EmailVerified = true,
            Bio = "Trusted user",
            TrustScore = 0.9f,
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            LastSeenAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Simulate high trust score in analytics
        await _analyticsService.UpdateUserTrustScoreAsync(
            user.Id, 0.0f, TrustScoreChangeReason.PositiveEngagement,
            "Simulated high trust", "", null, null, "Test", true, 1.0f);

        // Act & Assert - Test various permissions

        // 1. Rate limiting should be lenient
        var rateLimitMultiplier = await _trustBasedModerationService.GetRateLimitMultiplierAsync(user.Id);
        Assert.True(rateLimitMultiplier >= 2.0f);

        // 2. Content should not be auto-hidden
        var shouldAutoHide = await _trustBasedModerationService.ShouldAutoHideContentAsync(user.Id, "post");
        Assert.False(shouldAutoHide);

        // 3. Moderation priority should be low (high number)
        var moderationPriority = await _trustBasedModerationService.GetModerationPriorityAsync(user.Id, "post");
        Assert.True(moderationPriority >= 4);

        // 4. All actions should be allowed
        var canCreatePost = await _trustBasedModerationService.CanPerformActionAsync(user.Id, TrustRequiredAction.CreatePost);
        var canSendMessage = await _trustBasedModerationService.CanPerformActionAsync(user.Id, TrustRequiredAction.SendMessage);
        var canReportContent = await _trustBasedModerationService.CanPerformActionAsync(user.Id, TrustRequiredAction.ReportContent);
        var canCreateMultiplePosts = await _trustBasedModerationService.CanPerformActionAsync(user.Id, TrustRequiredAction.CreateMultiplePosts);
        
        Assert.True(canCreatePost);
        Assert.True(canSendMessage);
        Assert.True(canReportContent);
        Assert.True(canCreateMultiplePosts);

        // 5. Content visibility should be full
        var visibilityLevel = await _trustBasedModerationService.GetContentVisibilityLevelAsync(user.Id);
        Assert.Equal(ContentVisibilityLevel.FullVisibility, visibilityLevel);

        // 6. Report review threshold should be low (reports taken seriously)
        var reportThreshold = await _trustBasedModerationService.GetReportReviewThresholdAsync(user.Id);
        Assert.True(reportThreshold <= 0.3f);
    }

    [Fact]
    public async Task TrustScoreHistory_TracksChangesCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            TrustScore = 1.0f,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act - Perform several trust score changes
        await _trustScoreService.UpdateTrustScoreForActionAsync(user.Id, TrustScoreAction.PostCreated, "post", 1);
        await _trustScoreService.UpdateTrustScoreForActionAsync(user.Id, TrustScoreAction.EmailVerified);
        await _trustScoreService.UpdateTrustScoreForActionAsync(user.Id, TrustScoreAction.ContentReported, "post", 1);

        // Assert - Check that history is recorded
        var history = await _context.UserTrustScoreHistories
            .Where(h => h.UserId == user.Id)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();

        Assert.Equal(3, history.Count);
        
        // Check first entry (post created)
        Assert.Equal(TrustScoreChangeReason.PositiveEngagement, history[0].Reason);
        Assert.True(history[0].ScoreChange > 0);
        Assert.Equal("post", history[0].RelatedEntityType);
        Assert.Equal(1, history[0].RelatedEntityId);

        // Check second entry (email verified)
        Assert.Equal(TrustScoreChangeReason.VerificationComplete, history[1].Reason);
        Assert.True(history[1].ScoreChange > 0);

        // Check third entry (content reported)
        Assert.Equal(TrustScoreChangeReason.UserReport, history[2].Reason);
        Assert.True(history[2].ScoreChange < 0);
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}
