using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Unified;
using QueuedNotification = Yapplr.Api.Services.Unified.QueuedNotification;

namespace Yapplr.Tests.Services.Unified;

public class NotificationServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<INotificationPreferencesService> _mockPreferencesService;
    private readonly Mock<ISignalRConnectionPool> _mockConnectionPool;
    private readonly Mock<ICountCacheService> _mockCountCache;
    private readonly Mock<IActiveConversationTracker> _mockConversationTracker;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly Mock<INotificationProviderManager> _mockProviderManager;
    private readonly Mock<INotificationQueue> _mockNotificationQueue;
    private readonly Mock<INotificationEnhancementService> _mockEnhancementService;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new YapplrDbContext(options);

        // Setup mocks
        _mockPreferencesService = new Mock<INotificationPreferencesService>();
        _mockConnectionPool = new Mock<ISignalRConnectionPool>();
        _mockCountCache = new Mock<ICountCacheService>();
        _mockConversationTracker = new Mock<IActiveConversationTracker>();
        _mockLogger = new Mock<ILogger<NotificationService>>();
        _mockProviderManager = new Mock<INotificationProviderManager>();
        _mockNotificationQueue = new Mock<INotificationQueue>();
        _mockEnhancementService = new Mock<INotificationEnhancementService>();

        // Create service with all dependencies
        _service = new NotificationService(
            _context,
            _mockPreferencesService.Object,
            _mockConnectionPool.Object,
            _mockCountCache.Object,
            _mockConversationTracker.Object,
            _mockLogger.Object,
            _mockProviderManager.Object,
            _mockNotificationQueue.Object,
            _mockEnhancementService.Object
        );

        // Setup test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Status = UserStatus.Active
        };

        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task SendNotificationAsync_WithValidRequest_ShouldCreateDatabaseNotification()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "This is a test notification",
            Priority = NotificationPriority.Normal
        };

        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(1, "test"))
            .ReturnsAsync(true);

        _mockEnhancementService
            .Setup(x => x.CheckRateLimitAsync(1, "test"))
            .ReturnsAsync(new RateLimitResult { IsAllowed = true });

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        result.Should().BeTrue();
        
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == 1);
        
        notification.Should().NotBeNull();
        notification!.Message.Should().Contain("This is a test notification"); // Uses Body, not Title
        notification.UserId.Should().Be(1);
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task SendNotificationAsync_WhenUserPreferencesDisallow_ShouldReturnFalse()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "This is a test notification"
        };

        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(1, "test"))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        result.Should().BeFalse();
        
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == 1);
        
        notification.Should().BeNull();
    }

    [Fact]
    public async Task SendNotificationAsync_WhenRateLimited_ShouldReturnFalse()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "This is a test notification"
        };

        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(1, "test"))
            .ReturnsAsync(true);

        _mockEnhancementService
            .Setup(x => x.CheckRateLimitAsync(1, "test"))
            .ReturnsAsync(new RateLimitResult { IsAllowed = false, ViolationType = "Rate limit exceeded" });

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        result.Should().BeFalse();
        
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == 1);
        
        notification.Should().BeNull();
    }

    [Fact]
    public async Task SendNotificationAsync_WhenUserOnlineAndProviderSucceeds_ShouldDeliverImmediately()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "This is a test notification"
        };

        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(1, "test"))
            .ReturnsAsync(true);

        _mockEnhancementService
            .Setup(x => x.CheckRateLimitAsync(1, "test"))
            .ReturnsAsync(new RateLimitResult { IsAllowed = true });

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(true);

        _mockProviderManager
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        result.Should().BeTrue();
        
        _mockProviderManager.Verify(
            x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()),
            Times.Once);
        
        _mockNotificationQueue.Verify(
            x => x.QueueNotificationAsync(It.IsAny<QueuedNotification>()),
            Times.Never);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenUserOnlineButProviderFails_ShouldQueueNotification()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "This is a test notification"
        };

        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(1, "test"))
            .ReturnsAsync(true);

        _mockEnhancementService
            .Setup(x => x.CheckRateLimitAsync(1, "test"))
            .ReturnsAsync(new RateLimitResult { IsAllowed = true });

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(true);

        _mockProviderManager
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        result.Should().BeTrue();
        
        _mockProviderManager.Verify(
            x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()),
            Times.Once);
        
        _mockNotificationQueue.Verify(
            x => x.QueueNotificationAsync(It.IsAny<QueuedNotification>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenUserOffline_ShouldQueueNotification()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "This is a test notification"
        };

        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(1, "test"))
            .ReturnsAsync(true);

        _mockEnhancementService
            .Setup(x => x.CheckRateLimitAsync(1, "test"))
            .ReturnsAsync(new RateLimitResult { IsAllowed = true });

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        result.Should().BeTrue();
        
        _mockProviderManager.Verify(
            x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()),
            Times.Never);
        
        _mockNotificationQueue.Verify(
            x => x.QueueNotificationAsync(It.IsAny<QueuedNotification>()),
            Times.Once);
    }

    [Fact]
    public async Task SendTestNotificationAsync_WithValidUserId_ShouldSendTestNotification()
    {
        // Arrange
        var userId = 1;

        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(userId, "test"))
            .ReturnsAsync(true);

        _mockEnhancementService
            .Setup(x => x.CheckRateLimitAsync(userId, "test"))
            .ReturnsAsync(new RateLimitResult { IsAllowed = true });

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(userId))
            .ReturnsAsync(true);

        _mockProviderManager
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendTestNotificationAsync(userId);

        // Assert
        result.Should().BeTrue();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == userId);

        notification.Should().NotBeNull();
        notification!.Message.Should().Contain("This is a test notification from the unified notification service!");
    }

    [Fact]
    public async Task SendMulticastNotificationAsync_WithMultipleUsers_ShouldSendToAllUsers()
    {
        // Arrange
        var userIds = new List<int> { 1, 2, 3 };
        var request = new NotificationRequest
        {
            NotificationType = "test",
            Title = "Multicast Test",
            Body = "This is a multicast test"
        };

        // Add additional test users
        _context.Users.AddRange(
            new User { Id = 2, Username = "user2", Email = "user2@example.com", Status = UserStatus.Active },
            new User { Id = 3, Username = "user3", Email = "user3@example.com", Status = UserStatus.Active }
        );
        await _context.SaveChangesAsync();

        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(It.IsAny<int>(), "test"))
            .ReturnsAsync(true);

        _mockEnhancementService
            .Setup(x => x.CheckRateLimitAsync(It.IsAny<int>(), "test"))
            .ReturnsAsync(new RateLimitResult { IsAllowed = true });

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SendMulticastNotificationAsync(userIds, request);

        // Assert
        result.Should().BeTrue();

        var notifications = await _context.Notifications
            .Where(n => userIds.Contains(n.UserId))
            .ToListAsync();

        notifications.Should().HaveCount(3);
        notifications.All(n => n.Message.Contains("This is a multicast test")).Should().BeTrue();
    }

    [Fact]
    public async Task SendMessageNotificationAsync_ShouldCreateMessageNotification()
    {
        // Arrange
        var userId = 1;
        var senderUsername = "sender";
        var messageContent = "Hello there!";
        var conversationId = 123;

        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(userId, "message"))
            .ReturnsAsync(true);

        _mockEnhancementService
            .Setup(x => x.CheckRateLimitAsync(userId, "message"))
            .ReturnsAsync(new RateLimitResult { IsAllowed = true });

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(userId))
            .ReturnsAsync(false);

        // Act
        await _service.SendMessageNotificationAsync(userId, senderUsername, messageContent, conversationId);

        // Assert
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == userId && n.Type == NotificationType.SystemMessage);

        notification.Should().NotBeNull();
        notification!.Message.Should().Contain(senderUsername);
        notification.Message.Should().Contain("Hello there!"); // Message format is "@sender: Hello there!"
    }

    [Fact]
    public async Task SendMentionNotificationAsync_ShouldCreateMentionNotification()
    {
        // Arrange
        var userId = 1;
        var mentionerUsername = "mentioner";
        var postId = 456;

        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(userId, "mention"))
            .ReturnsAsync(true);

        _mockEnhancementService
            .Setup(x => x.CheckRateLimitAsync(userId, "mention"))
            .ReturnsAsync(new RateLimitResult { IsAllowed = true });

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(userId))
            .ReturnsAsync(false);

        // Act
        await _service.SendMentionNotificationAsync(userId, mentionerUsername, postId);

        // Assert
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.UserId == userId && n.Type == NotificationType.Mention);

        notification.Should().NotBeNull();
        notification!.Message.Should().Contain(mentionerUsername);
        notification.Message.Should().Contain("mentioned you");
        // Note: PostId is not currently set by CreateDatabaseNotificationAsync - it's only in Data
        notification.PostId.Should().BeNull();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenAllComponentsHealthy_ShouldReturnTrue()
    {
        // Arrange
        _mockPreferencesService
            .Setup(x => x.GetUserPreferencesAsync(1))
            .ReturnsAsync(new NotificationPreferences { UserId = 1 });

        _mockProviderManager
            .Setup(x => x.HasAvailableProvidersAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _service.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenDatabaseUnavailable_ShouldReturnFalse()
    {
        // Arrange
        _context.Dispose(); // Simulate database unavailability

        // Act
        var result = await _service.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnNotificationStats()
    {
        // Arrange
        // Send some notifications through the service to generate stats
        _mockPreferencesService
            .Setup(x => x.ShouldSendNotificationAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockEnhancementService
            .Setup(x => x.CheckRateLimitAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new RateLimitResult { IsAllowed = true });

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        // Send a few notifications to increment counters
        await _service.SendNotificationAsync(new NotificationRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test",
            Body = "Test notification"
        });

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalNotificationsSent.Should().BeGreaterThan(0);
        stats.NotificationTypeBreakdown.Should().NotBeEmpty();
        stats.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
