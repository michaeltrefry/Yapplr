using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
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
/// Performance and load testing for the unified notification system.
/// Tests system behavior under various load conditions and validates performance targets.
/// </summary>
public class NotificationPerformanceIntegrationTests : IDisposable
{
    private readonly TestServer _server;
    private readonly IServiceProvider _serviceProvider;
    private readonly YapplrDbContext _dbContext;
    private readonly IUnifiedNotificationService _unifiedService;
    private readonly INotificationQueue _notificationQueue;
    private readonly INotificationEnhancementService _enhancementService;

    // Performance targets from requirements
    private const int TARGET_NOTIFICATION_CREATION_MS = 100;
    private const int TARGET_PROVIDER_DELIVERY_MS = 500;
    private const int TARGET_THROUGHPUT_PER_MINUTE = 1000;

    public NotificationPerformanceIntegrationTests()
    {
        var builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .ConfigureServices(services =>
            {
                // Use in-memory database for testing
                services.AddDbContext<YapplrDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

                // Add logging with minimal level for performance
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

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
                services.AddScoped<IUnifiedNotificationService, UnifiedNotificationService>();

                // Add required services
                services.AddScoped<ISignalRConnectionPool, SignalRConnectionPool>();
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
        
        _dbContext = _serviceProvider.GetRequiredService<YapplrDbContext>();
        _dbContext.Database.EnsureCreated();

        _unifiedService = _serviceProvider.GetRequiredService<IUnifiedNotificationService>();
        _notificationQueue = _serviceProvider.GetRequiredService<INotificationQueue>();
        _enhancementService = _serviceProvider.GetRequiredService<INotificationEnhancementService>();

        SeedPerformanceTestData();
    }

    private void SeedPerformanceTestData()
    {
        // Create multiple test users for load testing
        var users = new List<User>();
        var preferences = new List<NotificationPreferences>();

        for (int i = 1; i <= 100; i++)
        {
            var user = new User
            {
                Id = i,
                Username = $"perfuser{i}",
                Email = $"perf{i}@example.com",
                PasswordHash = "hash",
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.User,
                Status = UserStatus.Active
            };
            users.Add(user);

            var prefs = new NotificationPreferences
            {
                UserId = i,
                EnableLikeNotifications = true,
                EnableCommentNotifications = true,
                EnableFollowNotifications = true,
                EnableMentionNotifications = true,
                EnableMessageNotifications = true
            };
            preferences.Add(prefs);
        }

        _dbContext.Users.AddRange(users);
        _dbContext.NotificationPreferences.AddRange(preferences);
        _dbContext.SaveChanges();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Notification_Creation_Should_Meet_Latency_Target()
    {
        // Arrange
        var request = new NotificationRequest
        {
            UserId = 1,
            NotificationType = "performance_test",
            Title = "Performance Test",
            Body = "Testing notification creation latency",
            Priority = NotificationPriority.Normal
        };

        // Act & Assert - Measure notification creation time
        var stopwatch = Stopwatch.StartNew();
        var result = await _unifiedService.SendNotificationAsync(request);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(TARGET_NOTIFICATION_CREATION_MS, 
            $"Notification creation should be under {TARGET_NOTIFICATION_CREATION_MS}ms");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Bulk_Notification_Creation_Should_Handle_High_Volume()
    {
        // Arrange - Create 100 notifications
        var tasks = new List<Task<bool>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Send notifications concurrently
        for (int i = 1; i <= 100; i++)
        {
            var request = new NotificationRequest
            {
                UserId = (i % 10) + 1, // Distribute across first 10 users
                NotificationType = "bulk_test",
                Title = $"Bulk Test {i}",
                Body = $"Testing bulk notification {i}",
                Priority = NotificationPriority.Normal
            };

            tasks.Add(_unifiedService.SendNotificationAsync(request));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().AllSatisfy(r => r.Should().BeTrue());
        
        // Calculate throughput (notifications per minute)
        var throughputPerMinute = (100.0 / stopwatch.ElapsedMilliseconds) * 60000;
        throughputPerMinute.Should().BeGreaterThan(TARGET_THROUGHPUT_PER_MINUTE,
            $"Throughput should exceed {TARGET_THROUGHPUT_PER_MINUTE} notifications per minute");

        // Verify all notifications were created in database (bulk_test maps to SystemMessage)
        var dbNotifications = await _dbContext.Notifications
            .Where(n => n.Type == NotificationType.SystemMessage)
            .CountAsync();

        dbNotifications.Should().Be(100);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Queue_Processing_Should_Handle_Large_Volumes()
    {
        // Arrange - Queue 500 notifications
        var queueTasks = new List<Task>();
        
        for (int i = 1; i <= 500; i++)
        {
            var queuedNotification = new QueuedNotification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = (i % 50) + 1, // Distribute across first 50 users
                NotificationType = "queue_load_test",
                Title = $"Queue Load Test {i}",
                Body = $"Testing queue load {i}",
                Priority = NotificationPriority.Normal,
                CreatedAt = DateTime.UtcNow
            };

            queueTasks.Add(_notificationQueue.QueueNotificationAsync(queuedNotification));
        }

        await Task.WhenAll(queueTasks);

        // Act - Process all queued notifications
        var stopwatch = Stopwatch.StartNew();
        var processedCount = await _notificationQueue.ProcessPendingNotificationsAsync();
        stopwatch.Stop();

        // Assert
        processedCount.Should().BeGreaterThan(0);
        
        // Check processing speed
        var processingRate = (processedCount / (double)stopwatch.ElapsedMilliseconds) * 1000; // per second
        processingRate.Should().BeGreaterThan(10, "Should process at least 10 notifications per second");

        // Verify queue stats
        var queueStats = await _notificationQueue.GetQueueStatsAsync();
        queueStats.Should().NotBeNull();
        queueStats.TotalQueued.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Multicast_Notifications_Should_Scale_Efficiently()
    {
        // Arrange - Send to 50 users simultaneously
        var userIds = Enumerable.Range(1, 50).ToList();
        var request = new NotificationRequest
        {
            NotificationType = "multicast_performance",
            Title = "Multicast Performance Test",
            Body = "Testing multicast scalability",
            Priority = NotificationPriority.Normal
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _unifiedService.SendMulticastNotificationAsync(userIds, request);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        
        // Should be faster than sending individual notifications
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(TARGET_NOTIFICATION_CREATION_MS * 10,
            "Multicast should be more efficient than individual sends");

        // Verify all users received the notification (multicast_performance maps to SystemMessage)
        var notificationCount = await _dbContext.Notifications
            .Where(n => n.Type == NotificationType.SystemMessage)
            .CountAsync();

        notificationCount.Should().Be(50);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Enhancement_Service_Should_Handle_High_Metrics_Volume()
    {
        // Arrange - Record many events
        var eventTasks = new List<Task>();
        
        for (int i = 1; i <= 200; i++)
        {
            var notificationEvent = new NotificationEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                EventType = "performance_metric",
                UserId = (i % 20) + 1,
                NotificationType = "metrics_load_test",
                Provider = "test",
                Success = i % 10 != 0, // 10% failure rate
                ProcessingTime = TimeSpan.FromMilliseconds(Random.Shared.Next(50, 200))
            };

            eventTasks.Add(_enhancementService.RecordNotificationEventAsync(notificationEvent));
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(eventTasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
            "Recording 200 events should complete within 5 seconds");

        // Verify metrics can be retrieved efficiently
        var metricsStopwatch = Stopwatch.StartNew();
        var metrics = await _enhancementService.GetMetricsAsync();
        metricsStopwatch.Stop();

        metrics.Should().NotBeNull();
        metricsStopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
            "Metrics retrieval should be under 1 second");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task System_Should_Maintain_Performance_Under_Mixed_Load()
    {
        // Arrange - Simulate mixed workload
        var tasks = new List<Task>();

        // Add notification creation tasks
        for (int i = 1; i <= 50; i++)
        {
            var request = new NotificationRequest
            {
                UserId = (i % 25) + 1,
                NotificationType = "mixed_load_test",
                Title = $"Mixed Load {i}",
                Body = $"Testing mixed load {i}",
                Priority = NotificationPriority.Normal
            };
            tasks.Add(_unifiedService.SendNotificationAsync(request));
        }

        // Add queue processing tasks
        for (int i = 1; i <= 25; i++)
        {
            var queuedNotification = new QueuedNotification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = (i % 15) + 1,
                NotificationType = "mixed_queue_test",
                Title = $"Mixed Queue {i}",
                Body = $"Testing mixed queue {i}",
                Priority = NotificationPriority.Normal
            };
            tasks.Add(_notificationQueue.QueueNotificationAsync(queuedNotification));
        }

        // Add metrics recording tasks
        for (int i = 1; i <= 25; i++)
        {
            var notificationEvent = new NotificationEvent
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = "mixed_metrics",
                UserId = (i % 10) + 1,
                NotificationType = "mixed_test",
                Provider = "test",
                Success = true
            };
            tasks.Add(_enhancementService.RecordNotificationEventAsync(notificationEvent));
        }

        // Act - Execute all tasks concurrently
        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - System should handle mixed load efficiently
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000,
            "Mixed load of 100 operations should complete within 10 seconds");

        // Verify system health after load
        var isHealthy = await _unifiedService.IsHealthyAsync();
        isHealthy.Should().BeTrue("System should remain healthy after mixed load");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Memory_Usage_Should_Remain_Stable_Under_Load()
    {
        // Arrange - Get initial memory usage
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var initialMemory = GC.GetTotalMemory(false);

        // Act - Perform intensive operations
        for (int batch = 0; batch < 10; batch++)
        {
            var batchTasks = new List<Task>();
            
            for (int i = 1; i <= 50; i++)
            {
                var request = new NotificationRequest
                {
                    UserId = (i % 20) + 1,
                    NotificationType = "memory_test",
                    Title = $"Memory Test Batch {batch} Item {i}",
                    Body = $"Testing memory usage batch {batch} item {i}",
                    Priority = NotificationPriority.Normal
                };
                batchTasks.Add(_unifiedService.SendNotificationAsync(request));
            }
            
            await Task.WhenAll(batchTasks);
        }

        // Force garbage collection and measure final memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert - Memory increase should be reasonable (less than 50MB)
        memoryIncrease.Should().BeLessThan(50 * 1024 * 1024,
            "Memory usage should not increase significantly under load");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _server?.Dispose();
    }
}
