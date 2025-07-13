using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;
using Xunit;

namespace Yapplr.Api.Tests;

public class NotificationQueueServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<ICompositeNotificationService> _mockNotificationService;
    private readonly Mock<ISignalRConnectionPool> _mockConnectionPool;
    private readonly Mock<ILogger<NotificationQueueService>> _mockLogger;
    private readonly NotificationQueueService _service;

    public NotificationQueueServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockNotificationService = new Mock<ICompositeNotificationService>();
        _mockConnectionPool = new Mock<ISignalRConnectionPool>();
        _mockLogger = new Mock<ILogger<NotificationQueueService>>();

        _service = new NotificationQueueService(
            _context,
            _mockNotificationService.Object,
            _mockConnectionPool.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task QueueNotificationAsync_WhenUserOffline_ShouldPersistToDatabase()
    {
        // Arrange
        var notification = new QueuedNotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Test Notification",
            Body = "This is a test notification",
            Data = new Dictionary<string, string> { ["key"] = "value" }
        };

        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(false);

        // Act
        await _service.QueueNotificationAsync(notification);

        // Assert
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
        var notification = new QueuedNotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Test Notification",
            Body = "This is a test notification"
        };

        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(true);
        _mockNotificationService.Setup(x => x.SendNotificationAsync(1, "Test Notification", "This is a test notification", null))
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
        var notification1 = new QueuedNotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test1",
            Title = "Test Notification 1",
            Body = "First notification"
        };

        var notification2 = new QueuedNotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test2",
            Title = "Test Notification 2",
            Body = "Second notification"
        };

        var notification3 = new QueuedNotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 2,
            Type = "test3",
            Title = "Test Notification 3",
            Body = "Third notification for different user"
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
    public async Task MarkAsDeliveredAsync_ShouldUpdateDatabaseRecord()
    {
        // Arrange
        var notification = new QueuedNotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Test Notification",
            Body = "This is a test notification"
        };

        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(1))
            .ReturnsAsync(false);

        await _service.QueueNotificationAsync(notification);

        // Act
        await _service.MarkAsDeliveredAsync(notification.Id);

        // Assert
        var dbNotification = await _context.QueuedNotifications
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        dbNotification.Should().NotBeNull();
        dbNotification!.DeliveredAt.Should().NotBeNull();
        dbNotification.DeliveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnDatabaseStatistics()
    {
        // Arrange
        var notification1 = new QueuedNotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Pending Notification",
            Body = "This notification is pending"
        };

        var notification2 = new QueuedNotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 2,
            Type = "test",
            Title = "Delivered Notification",
            Body = "This notification will be delivered"
        };

        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        await _service.QueueNotificationAsync(notification1);
        await _service.QueueNotificationAsync(notification2);
        await _service.MarkAsDeliveredAsync(notification2.Id);

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.PendingInDatabase.Should().Be(1);
        stats.DeliveredInDatabase.Should().Be(1);
        stats.FailedInDatabase.Should().Be(0);
        stats.TotalInDatabase.Should().Be(2);
    }

    [Fact]
    public async Task CleanupOldNotificationsAsync_ShouldRemoveOldDeliveredNotifications()
    {
        // Arrange
        var oldNotification = new QueuedNotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Old Notification",
            Body = "This is an old notification",
            CreatedAt = DateTime.UtcNow.AddDays(-8) // 8 days old
        };

        var recentNotification = new QueuedNotificationDto
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            Type = "test",
            Title = "Recent Notification",
            Body = "This is a recent notification",
            CreatedAt = DateTime.UtcNow.AddHours(-1) // 1 hour old
        };

        _mockConnectionPool.Setup(x => x.IsUserOnlineAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        await _service.QueueNotificationAsync(oldNotification);
        await _service.QueueNotificationAsync(recentNotification);
        await _service.MarkAsDeliveredAsync(oldNotification.Id);
        // Don't mark recent notification as delivered - it should remain pending

        // Act
        await _service.CleanupOldNotificationsAsync(TimeSpan.FromDays(7));

        // Assert
        var remainingNotifications = await _context.QueuedNotifications.ToListAsync();
        remainingNotifications.Should().HaveCount(1);
        remainingNotifications[0].Id.Should().Be(recentNotification.Id);
    }
}
