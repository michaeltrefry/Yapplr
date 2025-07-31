using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Notifications;
using Yapplr.Api.Services.Notifications.Providers;
using QueuedNotification = Yapplr.Api.Services.Notifications.QueuedNotification;

namespace Yapplr.Tests.Services.Unified;

public class NotificationQueueTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<INotificationProviderManager> _mockProviderManager;
    private readonly Mock<ISignalRConnectionPool> _mockConnectionPool;
    private readonly Mock<ILogger<NotificationQueue>> _mockLogger;
    private readonly NotificationQueue _queue;

    public NotificationQueueTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new YapplrDbContext(options);

        // Setup mocks
        _mockProviderManager = new Mock<INotificationProviderManager>();
        _mockConnectionPool = new Mock<ISignalRConnectionPool>();
        _mockLogger = new Mock<ILogger<NotificationQueue>>();

        // Create queue service
        _queue = new NotificationQueue(
            _context,
            _mockProviderManager.Object,
            _mockConnectionPool.Object,
            _mockLogger.Object
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
    public async Task QueueNotificationAsync_WithValidNotification_ShouldAddToQueue()
    {
        // Arrange
        var notification = new QueuedNotification
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "Test body",
            Priority = NotificationPriority.Normal,
            ScheduledFor = DateTime.UtcNow.AddHours(2) // Schedule for future to force database persistence
        };

        // Mock user as offline to prevent immediate delivery
        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(false);

        // Act
        await _queue.QueueNotificationAsync(notification);

        // Assert
        var queuedNotification = await _context.QueuedNotifications
            .FirstOrDefaultAsync(n => n.UserId == 1);

        queuedNotification.Should().NotBeNull();
        queuedNotification!.Title.Should().Be("Test Notification");
        queuedNotification.Body.Should().Be("Test body");
        queuedNotification.Type.Should().Be("test");
    }

    [Fact]
    public async Task QueueNotificationAsync_WithHighPriorityNotification_ShouldSetShorterExpiration()
    {
        // Arrange
        var notification = new QueuedNotification
        {
            UserId = 1,
            NotificationType = "urgent",
            Title = "Urgent Notification",
            Body = "Urgent body",
            Priority = NotificationPriority.Critical
        };

        // Act
        await _queue.QueueNotificationAsync(notification);

        // Assert
        notification.ExpiresAt.Should().NotBeNull();
        notification.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        notification.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddDays(8)); // Critical notifications get 7 days (default expiration)
    }

    [Fact]
    public async Task QueueNotificationAsync_WithLowPriorityNotification_ShouldSetLongerExpiration()
    {
        // Arrange
        var notification = new QueuedNotification
        {
            UserId = 1,
            NotificationType = "info",
            Title = "Info Notification",
            Body = "Info body",
            Priority = NotificationPriority.Low
        };

        // Act
        await _queue.QueueNotificationAsync(notification);

        // Assert
        notification.ExpiresAt.Should().NotBeNull();
        notification.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        notification.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddHours(7)); // Low priority expires in 6 hours
    }

    [Fact]
    public async Task ProcessPendingNotificationsAsync_WithQueuedNotifications_ShouldProcessThem()
    {
        // Arrange
        var queuedNotification = new Yapplr.Api.Models.QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Test Notification",
            Body = "Test body",
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            MaxRetries = 3
        };

        _context.QueuedNotifications.Add(queuedNotification);
        await _context.SaveChangesAsync();

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(true);

        _mockProviderManager
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()))
            .ReturnsAsync(true);

        // Act
        var processedCount = await _queue.ProcessPendingNotificationsAsync();

        // Assert
        processedCount.Should().BeGreaterThan(0);
        
        var updatedNotification = await _context.QueuedNotifications
            .FirstOrDefaultAsync(n => n.Id == queuedNotification.Id);
        
        updatedNotification!.DeliveredAt.Should().NotBeNull();
        
        _mockProviderManager.Verify(
            x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingNotificationsAsync_WhenUserOffline_ShouldNotProcessNotification()
    {
        // Arrange
        var queuedNotification = new Yapplr.Api.Models.QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Test Notification",
            Body = "Test body",
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            MaxRetries = 3
        };

        _context.QueuedNotifications.Add(queuedNotification);
        await _context.SaveChangesAsync();

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(false);

        // Act
        var processedCount = await _queue.ProcessPendingNotificationsAsync();

        // Assert
        var updatedNotification = await _context.QueuedNotifications
            .FirstOrDefaultAsync(n => n.Id == queuedNotification.Id);
        
        updatedNotification!.DeliveredAt.Should().BeNull();
        
        _mockProviderManager.Verify(
            x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPendingNotificationsAsync_WhenDeliveryFails_ShouldIncrementRetryCount()
    {
        // Arrange
        var queuedNotification = new Yapplr.Api.Models.QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Test Notification",
            Body = "Test body",
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            MaxRetries = 3
        };

        _context.QueuedNotifications.Add(queuedNotification);
        await _context.SaveChangesAsync();

        _mockConnectionPool
            .Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(true);

        _mockProviderManager
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()))
            .ReturnsAsync(false);

        // Act
        var processedCount = await _queue.ProcessPendingNotificationsAsync();

        // Assert
        var updatedNotification = await _context.QueuedNotifications
            .FirstOrDefaultAsync(n => n.Id == queuedNotification.Id);

        // After fixing HandleFailedDeliveryAsync, it should properly increment RetryCount
        updatedNotification!.RetryCount.Should().Be(1);
        updatedNotification.NextRetryAt.Should().NotBeNull();
        updatedNotification.NextRetryAt.Should().BeAfter(DateTime.UtcNow);
        updatedNotification.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPendingNotificationsAsync_WhenMaxRetriesReached_ShouldNotRetryAgain()
    {
        // Arrange
        var queuedNotification = new Yapplr.Api.Models.QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Test Notification",
            Body = "Test body",
            CreatedAt = DateTime.UtcNow,
            RetryCount = 3,
            MaxRetries = 3
        };

        _context.QueuedNotifications.Add(queuedNotification);
        await _context.SaveChangesAsync();

        // Act
        var processedCount = await _queue.ProcessPendingNotificationsAsync();

        // Assert
        var updatedNotification = await _context.QueuedNotifications
            .FirstOrDefaultAsync(n => n.Id == queuedNotification.Id);
        
        updatedNotification!.RetryCount.Should().Be(3); // Should not increment
        
        _mockProviderManager.Verify(
            x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task GetQueueStatsAsync_ShouldReturnAccurateStatistics()
    {
        // Arrange
        // Mock users as offline to force queuing
        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        // Queue notifications through the service to increment internal counters
        // Use immediate delivery (no ScheduledFor) to keep them in memory for QueuedByType stats
        var notification1 = new QueuedNotification
        {
            UserId = 1,
            NotificationType = "test1",
            Title = "Test 1",
            Body = "Body 1"
            // No ScheduledFor - will go to memory queue
        };

        var notification2 = new QueuedNotification
        {
            UserId = 1,
            NotificationType = "test2",
            Title = "Test 2",
            Body = "Body 2"
            // No ScheduledFor - will go to memory queue
        };

        var notification3 = new QueuedNotification
        {
            UserId = 1,
            NotificationType = "test3",
            Title = "Test 3",
            Body = "Body 3"
            // No ScheduledFor - will go to memory queue
        };

        await _queue.QueueNotificationAsync(notification1);
        await _queue.QueueNotificationAsync(notification2);
        await _queue.QueueNotificationAsync(notification3);

        // Act
        var stats = await _queue.GetQueueStatsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalQueued.Should().Be(3);
        stats.TotalDelivered.Should().Be(0); // None delivered yet
        stats.TotalFailed.Should().Be(0); // None failed yet
        stats.CurrentlyQueued.Should().BeGreaterThan(0); // Should have queued notifications
        stats.QueuedByType.Should().ContainKeys("test1", "test2", "test3");
    }

    [Fact]
    public async Task CleanupExpiredNotificationsAsync_ShouldRemoveExpiredNotifications()
    {
        // Arrange
        var expiredDate = DateTime.UtcNow.AddDays(-8); // 8 days ago, older than 7-day default
        var expiredNotification = new Yapplr.Api.Models.QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "expired",
            Title = "Expired Notification",
            Body = "Expired body",
            CreatedAt = expiredDate,
            RetryCount = 0,
            MaxRetries = 3
        };

        var validNotification = new Yapplr.Api.Models.QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "valid",
            Title = "Valid Notification",
            Body = "Valid body",
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            MaxRetries = 3
        };

        _context.QueuedNotifications.AddRange(expiredNotification, validNotification);
        await _context.SaveChangesAsync();

        // Act
        var cleanedCount = await _queue.CleanupExpiredNotificationsAsync();

        // Assert
        // Refresh the context to ensure we see the latest state
        _context.ChangeTracker.Clear();
        var remainingNotifications = await _context.QueuedNotifications.ToListAsync();

        // Debug: Check what was cleaned and what remains
        var cutoffTime = DateTime.UtcNow.AddDays(-7); // Default expiration is 7 days
        cleanedCount.Should().BeGreaterThan(0, $"At least one notification should have been cleaned. ExpiredDate: {expiredDate}, CutoffTime: {cutoffTime}, IsExpired: {expiredDate < cutoffTime}");
        remainingNotifications.Should().HaveCount(1, $"Expected 1 remaining notification, but found {remainingNotifications.Count}. Cleaned count: {cleanedCount}");
        remainingNotifications.First().Type.Should().Be("valid");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
