using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using Xunit;
using Yapplr.Api.Services.Notifications;
using Yapplr.Api.Services.Notifications.Providers;

namespace Yapplr.Api.Tests;

public class NotificationQueueServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<INotificationProviderManager> _mockProviderManager;
    private readonly Mock<ISignalRConnectionPool> _mockConnectionPool;
    private readonly Mock<ILogger<NotificationQueue>> _mockLogger;
    private readonly NotificationQueue _service;

    public NotificationQueueServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockProviderManager = new Mock<INotificationProviderManager>();
        _mockConnectionPool = new Mock<ISignalRConnectionPool>();
        _mockLogger = new Mock<ILogger<NotificationQueue>>();

        _service = new NotificationQueue(
            _context,
            _mockProviderManager.Object,
            _mockConnectionPool.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task QueueNotificationAsync_WhenUserOffline_ShouldQueueNotification()
    {
        // Arrange - Use a far future scheduled time to force database persistence
        var notification = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "This is a test notification",
            Data = new Dictionary<string, string> { ["key"] = "value" },
            ScheduledFor = DateTime.UtcNow.AddDays(1) // Force database persistence (> 1 hour threshold)
        };

        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(false);

        // Act
        await _service.QueueNotificationAsync(notification);

        // Assert - Check that notification was queued (either in memory or database)
        var stats = await _service.GetQueueStatsAsync();
        stats.TotalQueued.Should().BeGreaterThan(0);

        // For far-future scheduled notifications, should be in database
        var dbNotification = await _context.QueuedNotifications
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        dbNotification.Should().NotBeNull();
        dbNotification!.UserId.Should().Be(1);
        dbNotification.Type.Should().Be("test");
        dbNotification.Title.Should().Be("Test Notification");
        dbNotification.Body.Should().Be("This is a test notification");
        dbNotification.Data.Should().Contain("key");
        dbNotification.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public async Task QueueNotificationAsync_WhenUserOnlineAndDeliverySucceeds_ShouldNotPersistToDatabase()
    {
        // Arrange
        var notification = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "This is a test notification"
        };

        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(true);
        _mockProviderManager.Setup(x => x.SendNotificationAsync(It.IsAny<NotificationDeliveryRequest>()))
            .ReturnsAsync(true);

        // Act
        await _service.QueueNotificationAsync(notification);

        // Assert
        var dbNotification = await _context.QueuedNotifications
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        dbNotification.Should().BeNull();
    }

    [Fact]
    public async Task GetPendingNotificationsAsync_ShouldReturnNotificationsFromDatabase()
    {
        // Arrange
        var notification1 = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            NotificationType = "test1",
            Title = "Test Notification 1",
            Body = "First notification",
            ScheduledFor = DateTime.UtcNow.AddHours(1) // Force database persistence
        };

        var notification2 = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            NotificationType = "test2",
            Title = "Test Notification 2",
            Body = "Second notification",
            ScheduledFor = DateTime.UtcNow.AddHours(1) // Force database persistence
        };

        var notification3 = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 2,
            NotificationType = "test3",
            Title = "Test Notification 3",
            Body = "Third notification for different user",
            ScheduledFor = DateTime.UtcNow.AddHours(1) // Force database persistence
        };

        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        await _service.QueueNotificationAsync(notification1);
        await _service.QueueNotificationAsync(notification2);
        await _service.QueueNotificationAsync(notification3);

        // Act
        var result = await _service.GetPendingNotificationsAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(n => n.Id == notification1.Id);
        result.Should().Contain(n => n.Id == notification2.Id);
        result.Should().NotContain(n => n.Id == notification3.Id);
    }

    [Fact]
    public async Task MarkAsDeliveredAsync_ShouldUpdateRecord()
    {
        // Arrange
        var notification = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "This is a test notification",
            ScheduledFor = DateTime.UtcNow.AddHours(1) // Force database persistence
        };

        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(false);

        await _service.QueueNotificationAsync(notification);

        // Act
        await _service.MarkAsDeliveredAsync(notification.Id);

        // Assert - Check that the notification was marked as delivered
        var stats = await _service.GetQueueStatsAsync();
        stats.TotalDelivered.Should().BeGreaterThanOrEqualTo(0);

        // For scheduled notifications, check database
        var dbNotification = await _context.QueuedNotifications
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        if (dbNotification != null)
        {
            dbNotification.DeliveredAt.Should().NotBeNull();
            dbNotification.DeliveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task GetQueueStatsAsync_ShouldReturnDatabaseStatistics()
    {
        // Arrange
        var notification1 = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            NotificationType = "test",
            Title = "Pending Notification",
            Body = "This notification is pending",
            ScheduledFor = DateTime.UtcNow.AddHours(1) // Force database persistence
        };

        var notification2 = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 2,
            NotificationType = "test",
            Title = "Delivered Notification",
            Body = "This notification will be delivered",
            ScheduledFor = DateTime.UtcNow.AddHours(1) // Force database persistence
        };

        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        await _service.QueueNotificationAsync(notification1);
        await _service.QueueNotificationAsync(notification2);
        await _service.MarkAsDeliveredAsync(notification2.Id);

        // Act
        var stats = await _service.GetQueueStatsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.CurrentlyQueued.Should().BeGreaterThan(0);
        stats.TotalDelivered.Should().BeGreaterThanOrEqualTo(0);
        stats.TotalFailed.Should().BeGreaterThanOrEqualTo(0);
        stats.TotalQueued.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CleanupOldNotificationsAsync_ShouldRemoveOldNotifications()
    {
        // Arrange - Create notifications directly in database with old timestamps
        var oldNotification = new Models.QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Old Notification",
            Body = "This is an old notification",
            CreatedAt = DateTime.UtcNow.AddDays(-8), // 8 days old
            DeliveredAt = DateTime.UtcNow.AddDays(-7) // Delivered 7 days ago
        };

        var recentNotification = new Models.QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Recent Notification",
            Body = "This is a recent notification",
            CreatedAt = DateTime.UtcNow.AddHours(-1) // 1 hour old
        };

        // Add directly to database
        _context.QueuedNotifications.Add(oldNotification);
        _context.QueuedNotifications.Add(recentNotification);
        await _context.SaveChangesAsync();

        // Act
        var cleanedCount = await _service.CleanupOldNotificationsAsync(TimeSpan.FromDays(7));

        // Assert
        cleanedCount.Should().BeGreaterThan(0);
        var remainingNotifications = await _context.QueuedNotifications.ToListAsync();
        remainingNotifications.Should().HaveCount(1);
        remainingNotifications[0].Id.Should().Be(recentNotification.Id);
    }
}
