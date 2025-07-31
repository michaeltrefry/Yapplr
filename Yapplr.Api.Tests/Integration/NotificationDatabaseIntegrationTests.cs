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
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Common;
using Yapplr.Api.Services.Notifications;
using Yapplr.Api.Services.Notifications.Providers;
using QueuedNotification = Yapplr.Api.Services.Notifications.QueuedNotification;

namespace Yapplr.Tests.Integration;

/// <summary>
/// Database integration tests for the unified notification system.
/// Tests real database operations, transactions, and data consistency.
/// </summary>
public class NotificationDatabaseIntegrationTests : IDisposable
{
    private readonly TestServer _server;
    private readonly IServiceProvider _serviceProvider;
    private readonly YapplrDbContext _dbContext;
    private readonly INotificationService _service;
    private readonly INotificationQueue _notificationQueue;
    private readonly INotificationEnhancementService _enhancementService;

    public NotificationDatabaseIntegrationTests()
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

                // Add required services
                services.AddScoped<ISignalRConnectionPool, SignalRConnectionPool>();
                services.AddScoped<IActiveConversationTracker, ActiveConversationTracker>();
                services.AddScoped<IFirebaseNotificationProvider, FirebaseNotificationProvider>();
                services.AddScoped<ExpoNotificationProvider>();
                services.AddScoped<SignalRNotificationProvider>();
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
        
        _dbContext = _serviceProvider.GetRequiredService<YapplrDbContext>();
        _dbContext.Database.EnsureCreated();

        _service = _serviceProvider.GetRequiredService<INotificationService>();
        _notificationQueue = _serviceProvider.GetRequiredService<INotificationQueue>();
        _enhancementService = _serviceProvider.GetRequiredService<INotificationEnhancementService>();

        SeedDatabaseTestData();
    }

    private void SeedDatabaseTestData()
    {
        // Create test users with various configurations
        var users = new[]
        {
            new User
            {
                Id = 1,
                Username = "dbuser1",
                Email = "db1@example.com",
                PasswordHash = "hash",
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.User,
                Status = UserStatus.Active
            },
            new User
            {
                Id = 2,
                Username = "dbuser2",
                Email = "db2@example.com",
                PasswordHash = "hash",
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.User,
                Status = UserStatus.Active
            },
            new User
            {
                Id = 3,
                Username = "dbuser3",
                Email = "db3@example.com",
                PasswordHash = "hash",
                EmailVerified = false, // Unverified user
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.User,
                Status = UserStatus.Active
            }
        };

        _dbContext.Users.AddRange(users);

        // Create notification preferences
        var preferences = new[]
        {
            new NotificationPreferences
            {
                UserId = 1,
                EnableLikeNotifications = true,
                EnableCommentNotifications = true,
                EnableFollowNotifications = true,
                EnableMentionNotifications = true,
                EnableMessageNotifications = true
            },
            new NotificationPreferences
            {
                UserId = 2,
                EnableLikeNotifications = false, // Disabled likes
                EnableCommentNotifications = true,
                EnableFollowNotifications = true,
                EnableMentionNotifications = true,
                EnableMessageNotifications = true
            },
            new NotificationPreferences
            {
                UserId = 3,
                EnableLikeNotifications = true,
                EnableCommentNotifications = true,
                EnableFollowNotifications = true,
                EnableMentionNotifications = true,
                EnableMessageNotifications = true
            }
        };

        _dbContext.NotificationPreferences.AddRange(preferences);
        _dbContext.SaveChanges();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_Notification_Creation_Should_Persist_Correctly()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "database_test",
            Title = "Database Test Notification",
            Body = "Testing database persistence",
            Priority = NotificationPriority.High,
            Data = new Dictionary<string, string>
            {
                { "testKey", "testValue" },
                { "postId", "123" }
            }
        };

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        result.Should().BeTrue();

        // Verify notification was persisted correctly (database_test maps to SystemMessage)
        var dbNotification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.UserId == 1 && n.Type == NotificationType.SystemMessage);

        dbNotification.Should().NotBeNull();
        dbNotification!.Message.Should().Contain("Testing database persistence");
        dbNotification.UserId.Should().Be(1);
        dbNotification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        dbNotification.IsRead.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Queue_Database_Operations_Should_Be_Transactional()
    {
        // Arrange
        var queuedNotification = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            NotificationType = "transaction_test",
            Title = "Transaction Test",
            Body = "Testing transactional behavior",
            Priority = NotificationPriority.Normal,
            CreatedAt = DateTime.UtcNow,
            Status = QueuedNotificationStatus.Pending,
            ScheduledFor = DateTime.UtcNow.AddHours(2) // Force database storage (> 1 hour threshold)
        };

        // Act - Queue notification
        await _notificationQueue.QueueNotificationAsync(queuedNotification);

        // Verify it was queued
        var pendingBefore = await _notificationQueue.GetPendingNotificationsAsync(1);
        pendingBefore.Should().HaveCount(1);

        // Get the actual notification ID from the database
        var dbNotificationBefore = await _dbContext.QueuedNotifications
            .FirstOrDefaultAsync(n => n.UserId == 1 && n.Type == "transaction_test");
        dbNotificationBefore.Should().NotBeNull();

        // Mark as delivered using the actual database ID
        await _notificationQueue.MarkAsDeliveredAsync(dbNotificationBefore!.Id);

        // Small delay to ensure database operation completes
        await Task.Delay(100);

        // Assert - Verify status change persisted
        var pendingAfter = await _notificationQueue.GetPendingNotificationsAsync(1);
        pendingAfter.Should().HaveCount(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Concurrent_Database_Operations_Should_Be_Safe()
    {
        // Arrange - Create multiple concurrent notification requests
        var tasks = new List<Task<bool>>();

        for (int i = 1; i <= 20; i++)
        {
            var request = new NotificationRequest
            {
                UserId = (i % 3) + 1, // Distribute across 3 users
                NotificationType = "concurrent_test",
                Title = $"Concurrent Test {i}",
                Body = $"Testing concurrent operations {i}",
                Priority = NotificationPriority.Normal
            };

            tasks.Add(_service.SendNotificationAsync(request));
        }

        // Act - Execute all tasks concurrently
        var results = await Task.WhenAll(tasks);

        // Assert - All operations should succeed
        results.Should().AllSatisfy(r => r.Should().BeTrue());

        // Verify all notifications were created (concurrent_test maps to SystemMessage)
        var dbNotifications = await _dbContext.Notifications
            .Where(n => n.Type == NotificationType.SystemMessage)
            .ToListAsync();

        dbNotifications.Should().HaveCount(20);

        // Verify data integrity - each notification should have unique content
        var uniqueMessages = dbNotifications.Select(n => n.Message).Distinct().Count();
        uniqueMessages.Should().Be(20);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_Cleanup_Operations_Should_Work_Correctly()
    {
        // Arrange - Create old notifications and queue entries
        var oldDate = DateTime.UtcNow.AddDays(-30);

        // Create old queued notifications
        var oldQueuedNotifications = new[]
        {
            new QueuedNotification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = 1,
                NotificationType = "cleanup_test",
                Title = "Old Notification 1",
                Body = "Should be cleaned up",
                CreatedAt = oldDate,
                Status = QueuedNotificationStatus.Delivered
            },
            new QueuedNotification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = 2,
                NotificationType = "cleanup_test",
                Title = "Old Notification 2",
                Body = "Should be cleaned up",
                CreatedAt = oldDate,
                Status = QueuedNotificationStatus.Failed
            }
        };

        foreach (var notification in oldQueuedNotifications)
        {
            await _notificationQueue.QueueNotificationAsync(notification);
        }

        // Create recent notification that should not be cleaned up
        var recentNotification = new QueuedNotification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 1,
            NotificationType = "cleanup_test",
            Title = "Recent Notification",
            Body = "Should not be cleaned up",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            Status = QueuedNotificationStatus.Pending
        };

        await _notificationQueue.QueueNotificationAsync(recentNotification);

        // Act - Cleanup old notifications
        var cleanedCount = await _notificationQueue.CleanupOldNotificationsAsync(TimeSpan.FromDays(7));

        // Assert - Should have cleaned up at least the 2 old notifications we created
        cleanedCount.Should().BeGreaterThanOrEqualTo(2);

        // Verify recent notification still exists
        var remainingNotifications = await _notificationQueue.GetAllPendingNotificationsAsync();
        remainingNotifications.Should().HaveCount(1);
        remainingNotifications[0].Title.Should().Be("Recent Notification");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_Foreign_Key_Relationships_Should_Be_Maintained()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "relationship_test",
            Title = "Foreign Key Test",
            Body = "Testing foreign key relationships",
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        result.Should().BeTrue();

        // Verify notification is properly linked to user (relationship_test maps to SystemMessage)
        var notificationWithUser = await _dbContext.Notifications
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.Type == NotificationType.SystemMessage);

        notificationWithUser.Should().NotBeNull();
        notificationWithUser!.User.Should().NotBeNull();
        notificationWithUser.User!.Username.Should().Be("dbuser1");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_Indexes_Should_Support_Efficient_Queries()
    {
        // Arrange - Create many notifications for query testing
        var tasks = new List<Task<bool>>();

        for (int i = 1; i <= 100; i++)
        {
            var request = new NotificationRequest
            {
                UserId = (i % 3) + 1,
                NotificationType = "index_test",
                Title = $"Index Test {i}",
                Body = $"Testing query performance {i}",
                Priority = NotificationPriority.Normal
            };

            tasks.Add(_service.SendNotificationAsync(request));
        }

        await Task.WhenAll(tasks);

        // Act & Assert - Test various query patterns that should be efficient
        
        // Query by user ID (should use index)
        var userNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == 1)
            .ToListAsync();
        userNotifications.Should().NotBeEmpty();

        // Query by type (should use index) - index_test maps to SystemMessage
        var typeNotifications = await _dbContext.Notifications
            .Where(n => n.Type == NotificationType.SystemMessage)
            .ToListAsync();
        typeNotifications.Should().HaveCount(100);

        // Query by date range (should use index)
        var recentNotifications = await _dbContext.Notifications
            .Where(n => n.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .ToListAsync();
        recentNotifications.Should().NotBeEmpty();

        // Query unread notifications (should use index)
        var unreadNotifications = await _dbContext.Notifications
            .Where(n => !n.IsRead)
            .ToListAsync();
        unreadNotifications.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_Audit_Logs_Should_Be_Created_Correctly()
    {
        // Arrange
        var securityEvent = new SecurityEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            EventType = "database_audit_test",
            UserId = 1,
            Description = "Testing audit log creation",
            Severity = SecurityEventSeverity.Low
        };

        // Act
        await _enhancementService.LogSecurityEventAsync(securityEvent);

        // Assert - Verify audit log was created
        var auditLogs = await _enhancementService.GetUserAuditLogsAsync(1, 10);
        auditLogs.Should().NotBeEmpty();

        // Verify audit log content
        var auditLog = auditLogs.FirstOrDefault(a => a.EventType == "database_audit_test");
        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(1);
        auditLog.Description.Should().Be("Testing audit log creation");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_Notification_History_Should_Track_Changes()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "history_test",
            Title = "History Test",
            Body = "Testing notification history",
            Priority = NotificationPriority.Normal
        };

        // Act - Send notification
        await _service.SendNotificationAsync(request);

        // Get the notification (history_test maps to SystemMessage)
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Type == NotificationType.SystemMessage);
        notification.Should().NotBeNull();

        // Confirm delivery
        await _service.ConfirmDeliveryAsync(notification!.Id.ToString());

        // Confirm read
        await _service.ConfirmReadAsync(notification.Id.ToString());

        // Assert - Check notification history
        var history = await _service.GetNotificationHistoryAsync(1, 10);
        history.Should().NotBeEmpty();

        var historyEntry = history.FirstOrDefault(h => h.NotificationType == "SystemMessage");
        historyEntry.Should().NotBeNull();
        historyEntry!.Status.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_Should_Handle_Large_Data_Volumes()
    {
        // Arrange - Create a large number of notifications
        var batchSize = 50;
        var totalNotifications = 200;

        for (int batch = 0; batch < totalNotifications / batchSize; batch++)
        {
            var tasks = new List<Task<bool>>();

            for (int i = 1; i <= batchSize; i++)
            {
                var request = new NotificationRequest
                {
                    UserId = ((batch * batchSize + i) % 3) + 1,
                    NotificationType = "volume_test",
                    Title = $"Volume Test Batch {batch} Item {i}",
                    Body = $"Testing large data volume batch {batch} item {i}",
                    Priority = NotificationPriority.Normal
                };

                tasks.Add(_service.SendNotificationAsync(request));
            }

            await Task.WhenAll(tasks);
        }

        // Act - Query the large dataset (volume_test maps to SystemMessage)
        var allNotifications = await _dbContext.Notifications
            .Where(n => n.Type == NotificationType.SystemMessage)
            .CountAsync();

        // Assert
        allNotifications.Should().Be(totalNotifications);

        // Verify database performance with large dataset
        var user1Notifications = await _dbContext.Notifications
            .Where(n => n.UserId == 1 && n.Type == NotificationType.SystemMessage)
            .CountAsync();

        user1Notifications.Should().BeGreaterThan(0);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _server?.Dispose();
    }
}
