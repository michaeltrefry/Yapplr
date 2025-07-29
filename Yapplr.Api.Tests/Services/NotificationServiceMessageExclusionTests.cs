using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Unified;
using Xunit;

namespace Yapplr.Api.Tests.Services;

public class NotificationServiceMessageExclusionTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly NotificationService _service;
    private readonly Mock<IUnifiedNotificationService> _mockNotificationService;
    private readonly Mock<ICountCacheService> _mockCountCache;

    public NotificationServiceMessageExclusionTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockNotificationService = new Mock<IUnifiedNotificationService>();
        _mockCountCache = new Mock<ICountCacheService>();
        var mockLogger = new Mock<ILogger<NotificationService>>();

        _service = new NotificationService(
            _context,
            _mockNotificationService.Object,
            _mockCountCache.Object,
            mockLogger.Object
        );

        // Add test user
        _context.Users.Add(new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Status = UserStatus.Active
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldExcludeMessageNotifications()
    {
        // Arrange
        var userId = 1;

        // Add various notification types including message notifications
        var notifications = new[]
        {
            new Notification
            {
                Type = NotificationType.Like,
                Message = "Someone liked your post",
                UserId = userId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new Notification
            {
                Type = NotificationType.Message,
                Message = "New message from user",
                UserId = userId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-3)
            },
            new Notification
            {
                Type = NotificationType.Follow,
                Message = "Someone followed you",
                UserId = userId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new Notification
            {
                Type = NotificationType.Message,
                Message = "Another message",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserNotificationsAsync(userId);

        // Assert
        Assert.Equal(2, result.Notifications.Count()); // Should only include Like and Follow notifications
        Assert.Equal(2, result.TotalCount); // Total count should exclude message notifications
        Assert.DoesNotContain(result.Notifications, n => n.Type == NotificationType.Message);
        Assert.Contains(result.Notifications, n => n.Type == NotificationType.Like);
        Assert.Contains(result.Notifications, n => n.Type == NotificationType.Follow);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_UnreadCount_ShouldExcludeMessageNotifications()
    {
        // Arrange
        var userId = 1;

        // Add unread notifications including message notifications
        var notifications = new[]
        {
            new Notification
            {
                Type = NotificationType.Like,
                Message = "Someone liked your post",
                UserId = userId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new Notification
            {
                Type = NotificationType.Message,
                Message = "Unread message",
                UserId = userId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-3)
            },
            new Notification
            {
                Type = NotificationType.Follow,
                Message = "Someone followed you",
                UserId = userId,
                IsRead = true, // This one is read
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            }
        };

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserNotificationsAsync(userId);

        // Assert
        Assert.Equal(1, result.UnreadCount); // Should only count the unread Like notification
    }

    [Fact]
    public async Task MarkAllNotificationsAsReadAsync_ShouldExcludeMessageNotifications()
    {
        // Arrange
        var userId = 1;

        var notifications = new[]
        {
            new Notification
            {
                Type = NotificationType.Like,
                Message = "Someone liked your post",
                UserId = userId,
                IsRead = false
            },
            new Notification
            {
                Type = NotificationType.Message,
                Message = "Unread message",
                UserId = userId,
                IsRead = false
            },
            new Notification
            {
                Type = NotificationType.Follow,
                Message = "Someone followed you",
                UserId = userId,
                IsRead = false
            }
        };

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.MarkAllNotificationsAsReadAsync(userId);

        // Assert
        Assert.True(result);

        // Verify that only non-message notifications were marked as read
        var updatedNotifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync();

        var likeNotification = updatedNotifications.First(n => n.Type == NotificationType.Like);
        var messageNotification = updatedNotifications.First(n => n.Type == NotificationType.Message);
        var followNotification = updatedNotifications.First(n => n.Type == NotificationType.Follow);

        Assert.True(likeNotification.IsRead);
        Assert.False(messageNotification.IsRead); // Message notification should remain unread
        Assert.True(followNotification.IsRead);
    }

    [Fact]
    public async Task MarkAllNotificationsAsSeenAsync_ShouldExcludeMessageNotifications()
    {
        // Arrange
        var userId = 1;

        var notifications = new[]
        {
            new Notification
            {
                Type = NotificationType.Like,
                Message = "Someone liked your post",
                UserId = userId,
                IsSeen = false
            },
            new Notification
            {
                Type = NotificationType.Message,
                Message = "Unseen message",
                UserId = userId,
                IsSeen = false
            },
            new Notification
            {
                Type = NotificationType.Follow,
                Message = "Someone followed you",
                UserId = userId,
                IsSeen = false
            }
        };

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.MarkAllNotificationsAsSeenAsync(userId);

        // Assert
        Assert.True(result);

        // Verify that only non-message notifications were marked as seen
        var updatedNotifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync();

        var likeNotification = updatedNotifications.First(n => n.Type == NotificationType.Like);
        var messageNotification = updatedNotifications.First(n => n.Type == NotificationType.Message);
        var followNotification = updatedNotifications.First(n => n.Type == NotificationType.Follow);

        Assert.True(likeNotification.IsSeen);
        Assert.False(messageNotification.IsSeen); // Message notification should remain unseen
        Assert.True(followNotification.IsSeen);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
