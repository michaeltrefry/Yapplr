using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Xunit;
using FluentAssertions;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Services.Unified;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Common;
using Yapplr.Api.Hubs;
using QueuedNotification = Yapplr.Api.Services.Unified.QueuedNotification;

namespace Yapplr.Tests.Integration;

/// <summary>
/// Integration tests for the unified notification system.
/// Tests real service-to-service communication with actual database connections.
/// </summary>
public class UnifiedNotificationIntegrationTests : IDisposable
{
    private readonly TestServer _server;
    private readonly IServiceProvider _serviceProvider;
    private readonly YapplrDbContext _dbContext;
    private readonly INotificationService _service;
    private readonly INotificationProviderManager _providerManager;
    private readonly INotificationQueue _notificationQueue;
    private readonly INotificationEnhancementService _enhancementService;

    public UnifiedNotificationIntegrationTests()
    {
        var builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .ConfigureServices(services =>
            {
                // Use in-memory database for testing
                services.AddDbContext<YapplrDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

                // Add logging
                services.AddLogging(builder => builder.AddConsole());

                // Add memory cache
                services.AddMemoryCache();

                // Add SignalR services for testing
                services.AddSignalR();

                // Mock IHubContext<NotificationHub> for testing
                var mockHubContext = new Mock<IHubContext<NotificationHub>>();
                var mockClients = new Mock<IHubClients>();
                var mockClientProxy = new Mock<IClientProxy>();

                mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
                mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
                mockClients.Setup(x => x.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(mockClientProxy.Object);
                mockClientProxy.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                services.AddSingleton(mockHubContext.Object);

                // Register unified notification services
                services.AddScoped<INotificationProviderManager, NotificationProviderManager>();
                services.AddScoped<INotificationQueue, NotificationQueue>();
                services.AddScoped<INotificationEnhancementService, NotificationEnhancementService>();
                services.AddScoped<INotificationService, NotificationService>();

                // Add other required services
                services.AddScoped<ISignalRConnectionPool, SignalRConnectionPool>();
                services.AddScoped<IActiveConversationTracker, ActiveConversationTracker>();
                services.AddScoped<IFirebaseService, FirebaseService>();
                services.AddScoped<ExpoNotificationService>();
                services.AddScoped<SignalRNotificationService>();
                services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();
                services.AddScoped<IUserService, UserService>();
                services.AddScoped<IAuthService, AuthService>();
                services.AddScoped<ITrustScoreService, TrustScoreService>();
                services.AddScoped<IApiRateLimitService, ApiRateLimitService>();
                services.AddScoped<IContentModerationService, ContentModerationService>();
                services.AddScoped<IAuditService, AuditService>();
                services.AddScoped<ICachingService, MemoryCachingService>();
                services.AddScoped<ICountCacheService, CountCacheService>();
            })
            .Configure(app => { });

        _server = new TestServer(builder);
        _serviceProvider = _server.Services;
        
        // Get database context and ensure it's created
        _dbContext = _serviceProvider.GetRequiredService<YapplrDbContext>();
        _dbContext.Database.EnsureCreated();

        // Get the unified notification services
        _service = _serviceProvider.GetRequiredService<INotificationService>();
        _providerManager = _serviceProvider.GetRequiredService<INotificationProviderManager>();
        _notificationQueue = _serviceProvider.GetRequiredService<INotificationQueue>();
        _enhancementService = _serviceProvider.GetRequiredService<INotificationEnhancementService>();

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test users
        var testUser1 = new User
        {
            Id = 1,
            Username = "testuser1",
            Email = "test1@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            Role = UserRole.User,
            Status = UserStatus.Active
        };

        var testUser2 = new User
        {
            Id = 2,
            Username = "testuser2", 
            Email = "test2@example.com",
            PasswordHash = "hash",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            Role = UserRole.User,
            Status = UserStatus.Active
        };

        _dbContext.Users.AddRange(testUser1, testUser2);

        // Create notification preferences
        var prefs1 = new NotificationPreferences
        {
            UserId = 1,
            EnableLikeNotifications = true,
            EnableCommentNotifications = true,
            EnableFollowNotifications = true,
            EnableMentionNotifications = true,
            EnableMessageNotifications = true
        };

        var prefs2 = new NotificationPreferences
        {
            UserId = 2,
            EnableLikeNotifications = true,
            EnableCommentNotifications = true,
            EnableFollowNotifications = true,
            EnableMentionNotifications = true,
            EnableMessageNotifications = true
        };

        _dbContext.NotificationPreferences.AddRange(prefs1, prefs2);
        _dbContext.SaveChanges();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UnifiedNotificationService_Should_Integrate_With_All_Services()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "test_integration",
            Title = "Integration Test",
            Body = "Testing service integration",
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        result.Should().BeTrue();

        // Verify database notification was created (test_integration maps to SystemMessage)
        var dbNotification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.UserId == 1 && n.Type == NotificationType.SystemMessage);

        dbNotification.Should().NotBeNull();
        dbNotification!.Message.Should().Contain("Testing service integration");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task NotificationQueue_Should_Integrate_With_Database()
    {
        // Arrange
        var queuedNotification = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 2,
            NotificationType = "test_queue",
            Title = "Queue Test",
            Body = "Testing queue integration",
            Priority = NotificationPriority.High,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _notificationQueue.QueueNotificationAsync(queuedNotification);

        // Assert
        var pendingNotifications = await _notificationQueue.GetPendingNotificationsAsync(2);
        pendingNotifications.Should().HaveCount(1);
        pendingNotifications[0].Title.Should().Be("Queue Test");
        pendingNotifications[0].Priority.Should().Be(NotificationPriority.High);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ProviderManager_Should_Handle_Provider_Fallback()
    {
        // Arrange
        var deliveryRequest = new NotificationDeliveryRequest
        {
            UserId = 1,
            NotificationType = "test_provider",
            Title = "Provider Test",
            Body = "Testing provider fallback",
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _providerManager.SendNotificationAsync(deliveryRequest);

        // Assert - Should handle gracefully even if providers fail
        result.Should().BeFalse(); // Expected since we don't have real providers configured

        // Verify provider health can be checked
        var health = await _providerManager.GetProviderHealthAsync();
        health.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task EnhancementService_Should_Process_Security_And_Metrics()
    {
        // Arrange
        var notificationEvent = new NotificationEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            EventType = "sent",
            UserId = 1,
            NotificationType = "test_enhancement",
            Provider = "test",
            Success = true,
            ProcessingTime = TimeSpan.FromMilliseconds(100)
        };

        // Act
        await _enhancementService.RecordNotificationEventAsync(notificationEvent);

        // Assert
        var metrics = await _enhancementService.GetMetricsAsync();
        metrics.Should().NotBeNull();

        // Test security validation
        var shouldAllow = await _enhancementService.ShouldAllowNotificationAsync(1, "test", "safe content");
        shouldAllow.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Complete_Notification_Flow_Should_Work_End_To_End()
    {
        // Arrange - Create a complete notification scenario
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "like",
            Title = "New ReactReaction",
            Body = "Someone liked your post",
            Priority = NotificationPriority.Normal,
            Data = new Dictionary<string, string> { { "postId", "123" } }
        };

        // Act - Send notification through unified service
        var sendResult = await _service.SendNotificationAsync(request);

        // Assert - Verify complete flow
        sendResult.Should().BeTrue();

        // Check database notification was created (like maps to ReactReact)
        var dbNotification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.UserId == 1 && n.Type == NotificationType.React);

        dbNotification.Should().NotBeNull();
        dbNotification!.Message.Should().Contain("Someone liked your post");

        // Check system health
        var isHealthy = await _service.IsHealthyAsync();
        isHealthy.Should().BeTrue();

        // Check stats are updated
        var stats = await _service.GetStatsAsync();
        stats.Should().NotBeNull();
        stats.TotalNotificationsSent.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Multicast_Notification_Should_Work_With_Multiple_Users()
    {
        // Arrange
        var userIds = new List<int> { 1, 2 };
        var request = new NotificationRequest
        {
            NotificationType = "announcement",
            Title = "System Announcement",
            Body = "Important system update",
            Priority = NotificationPriority.High
        };

        // Act
        var result = await _service.SendMulticastNotificationAsync(userIds, request);

        // Assert
        result.Should().BeTrue();

        // Verify both users received notifications (announcement maps to SystemMessage)
        var user1Notifications = await _dbContext.Notifications
            .Where(n => n.UserId == 1 && n.Type == NotificationType.SystemMessage)
            .ToListAsync();

        var user2Notifications = await _dbContext.Notifications
            .Where(n => n.UserId == 2 && n.Type == NotificationType.SystemMessage)
            .ToListAsync();

        user1Notifications.Should().HaveCount(1);
        user2Notifications.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Queue_Processing_Should_Handle_User_Connectivity()
    {
        // Arrange - Queue a notification for offline user
        var queuedNotification = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            NotificationType = "message",
            Title = "New Message",
            Body = "You have a new message",
            Priority = NotificationPriority.Normal
        };

        await _notificationQueue.QueueNotificationAsync(queuedNotification);

        // Act - Mark user as online and process notifications
        await _notificationQueue.MarkUserOnlineAsync(1, "SignalR");
        var processedCount = await _notificationQueue.ProcessUserNotificationsAsync(1);

        // Assert
        processedCount.Should().BeGreaterThan(0);

        // Verify user connectivity status
        var connectivity = await _notificationQueue.GetUserConnectivityAsync(1);
        connectivity.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateNotifications_ShouldUseMediaSpecificMessages()
    {
        // Arrange - Create users
        var user1 = new User { Username = "testuser1", Email = "test1@test.com" };
        var user2 = new User { Username = "testuser2", Email = "test2@test.com" };

        _dbContext.Users.AddRange(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Create a post with a photo
        var photoPost = new Post
        {
            Content = "Photo post",
            UserId = user1.Id
        };
        _dbContext.Posts.Add(photoPost);
        await _dbContext.SaveChangesAsync();

        // Add photo media to the post
        var photoMedia = new PostMedia
        {
            PostId = photoPost.Id,
            MediaType = MediaType.Image,
            ImageFileName = "photo.jpg"
        };
        _dbContext.PostMedia.Add(photoMedia);
        await _dbContext.SaveChangesAsync();

        // Act - Create a like notification for the photo post
        await _service.CreateReactionNotificationAsync(user1.Id, user2.Id, photoPost.Id, ReactionType.Heart);

        // Assert - Verify the notification message mentions "photo"
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.PostId == photoPost.Id && n.Type == NotificationType.React);

        notification.Should().NotBeNull();
        notification!.Message.Should().Contain("photo", "Photo post notifications should mention 'photo'");
        notification.Message.Should().Be("@testuser2 reacted ❤️ to your photo");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _server?.Dispose();
    }
}
