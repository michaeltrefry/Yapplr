using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Notifications;

namespace Yapplr.Api.Tests.Services.Unified;

public class NotificationEnhancementServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<ILogger<NotificationEnhancementService>> _mockLogger;
    private readonly NotificationEnhancementService _service;

    public NotificationEnhancementServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new YapplrDbContext(options);

        // Setup mocks
        _mockLogger = new Mock<ILogger<NotificationEnhancementService>>();

        // Create service
        _service = new NotificationEnhancementService(_context, _mockLogger.Object);

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
    public async Task RecordNotificationEventAsync_WithValidEvent_ShouldRecordMetrics()
    {
        // Arrange
        var trackingId = Guid.NewGuid().ToString();
        var notificationEvent = new NotificationEvent
        {
            EventType = "start", // Implementation expects "start" to increment sent counter
            TrackingId = trackingId,
            UserId = 1,
            NotificationType = "test",
            Provider = "Firebase",
            Success = true,
            ProcessingTime = TimeSpan.FromMilliseconds(150),
            LatencyMs = 150.0
        };

        // Act
        await _service.RecordNotificationEventAsync(notificationEvent);

        // Record completion event to increment delivered counter
        var completionEvent = new NotificationEvent
        {
            EventType = "complete",
            TrackingId = trackingId,
            UserId = 1,
            NotificationType = "test",
            Provider = "Firebase",
            Success = true,
            LatencyMs = 150.0
        };
        await _service.RecordNotificationEventAsync(completionEvent);

        // Assert
        var metrics = await _service.GetMetricsAsync();

        metrics.Should().NotBeNull();
        metrics.TotalNotificationsSent.Should().Be(1);
        metrics.TotalNotificationsDelivered.Should().Be(1);
        metrics.TotalNotificationsFailed.Should().Be(0);
        metrics.NotificationTypeBreakdown.Should().ContainKey("test");
        metrics.ProviderBreakdown.Should().ContainKey("Firebase");
    }

    [Fact]
    public async Task RecordNotificationEventAsync_WithFailedEvent_ShouldRecordFailure()
    {
        // Arrange
        var trackingId = Guid.NewGuid().ToString();

        // First record a start event
        var startEvent = new NotificationEvent
        {
            EventType = "start",
            TrackingId = trackingId,
            UserId = 1,
            NotificationType = "test",
            Provider = "Firebase",
            Success = false
        };

        var failedEvent = new NotificationEvent
        {
            EventType = "complete",
            TrackingId = trackingId,
            UserId = 1,
            NotificationType = "test",
            Provider = "Firebase",
            Success = false,
            ErrorMessage = "Network timeout"
        };

        // Act
        await _service.RecordNotificationEventAsync(startEvent);
        await _service.RecordNotificationEventAsync(failedEvent);

        // Assert
        var metrics = await _service.GetMetricsAsync();

        metrics.TotalNotificationsSent.Should().Be(1);
        metrics.TotalNotificationsDelivered.Should().Be(0);
        metrics.TotalNotificationsFailed.Should().Be(1);
        metrics.NotificationTypeBreakdown.Should().ContainKey("test");
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithinLimits_ShouldAllowNotification()
    {
        // Arrange
        var userId = 1;
        var notificationType = "test";

        // Act
        var result = await _service.CheckRateLimitAsync(userId, notificationType);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeTrue();
        result.ViolationType.Should().BeNull();
        result.RetryAfter.Should().BeNull();
    }

    [Fact]
    public async Task CheckRateLimitAsync_ExceedingBurstLimit_ShouldDenyNotification()
    {
        // Arrange
        var userId = 1;
        var notificationType = "test";

        // Send multiple notifications quickly to exceed burst limit
        for (int i = 0; i < 15; i++) // Assuming burst limit is 10
        {
            await _service.RecordNotificationSentAsync(userId, notificationType);
        }

        // Act
        var result = await _service.CheckRateLimitAsync(userId, notificationType);

        // Assert
        result.Should().NotBeNull();
        result.IsAllowed.Should().BeFalse();
        result.ViolationType.Should().Contain("burst");
    }

    [Fact]
    public async Task FilterContentAsync_WithCleanContent_ShouldPassThrough()
    {
        // Arrange
        var content = "This is a clean notification message.";

        // Act
        var result = await _service.FilterContentAsync(content);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.SanitizedContent.Should().Be(content);
        result.RiskLevel.Should().Be(ContentRiskLevel.Low);
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterContentAsync_WithProfanity_ShouldFilterContent()
    {
        // Arrange - Use a word that's actually in the profanity list
        var content = "This is a spam message with fraud content.";

        // Act
        var result = await _service.FilterContentAsync(content);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse(); // Profanity causes high risk level, blocking content
        result.Violations.Should().Contain("Content contains inappropriate language");
        result.RiskLevel.Should().Be(ContentRiskLevel.High);
        // Note: Current implementation doesn't replace profanity with *** in sanitization
        result.SanitizedContent.Should().Be(content); // Sanitization only removes HTML/scripts
    }

    [Fact]
    public async Task FilterContentAsync_WithMaliciousURL_ShouldBlockContent()
    {
        // Arrange - Use a URL that matches the suspicious patterns in implementation
        var content = "Check out this link: http://phishing.example.com/login";

        // Act
        var result = await _service.FilterContentAsync(content);

        // Assert
        result.Should().NotBeNull();
        result.RiskLevel.Should().Be(ContentRiskLevel.Critical);
        result.Violations.Should().Contain(v => v.Contains("suspicious links"));
        result.IsValid.Should().BeFalse(); // Critical risk level should block content
    }

    [Fact]
    public async Task CompressPayloadAsync_WithLargePayload_ShouldCompressContent()
    {
        // Arrange
        var largePayload = new Dictionary<string, object>
        {
            { "title", "This is a very long notification title that should be compressed" },
            { "body", "This is a very long notification body with lots of text that should benefit from compression. ".PadRight(1000, 'x') },
            { "data", new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } } }
        };

        // Act
        var result = await _service.CompressPayloadAsync(largePayload);

        // Assert
        result.Should().NotBeNull();
        result.CompressionMethod.Should().Be("gzip");
        result.OriginalSize.Should().BeGreaterThan(0);
        result.CompressedSize.Should().BeGreaterThan(0);
        result.CompressedSize.Should().BeLessThan(result.OriginalSize);
        result.CompressionRatio.Should().BeGreaterThan(0);
        result.CompressedData.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompressPayloadAsync_WithSmallPayload_ShouldNotCompress()
    {
        // Arrange
        var smallPayload = new Dictionary<string, object>
        {
            { "title", "Short title" },
            { "body", "Short body" }
        };

        // Act
        var result = await _service.CompressPayloadAsync(smallPayload);

        // Assert
        result.Should().NotBeNull();
        result.CompressionMethod.Should().Be("none");
        result.OriginalSize.Should().BeGreaterThan(0);
        result.CompressedSize.Should().Be(result.OriginalSize);
        result.CompressionRatio.Should().Be(1.0);
    }

    [Fact]
    public async Task LogSecurityEventAsync_WithSecurityEvent_ShouldCreateAuditLog()
    {
        // Arrange
        var securityEvent = new SecurityEvent
        {
            EventType = "rate_limit_exceeded",
            UserId = 1,
            Severity = SecurityEventSeverity.Medium,
            Description = "User exceeded rate limit",
            IpAddress = "192.168.1.1",
            UserAgent = "Test User Agent",
            AdditionalData = new Dictionary<string, object> { { "attempts", 15 } }
        };

        // Act
        await _service.LogSecurityEventAsync(securityEvent);

        // Assert
        var auditLogs = await _context.NotificationAuditLogs
            .Where(log => log.UserId == 1)
            .ToListAsync();

        auditLogs.Should().HaveCount(1);
        auditLogs.First().EventType.Should().Be("rate_limit_exceeded");
        auditLogs.First().Severity.Should().Be("Medium");
        auditLogs.First().Description.Should().Be("User exceeded rate limit");
    }

    [Fact]
    public async Task GetMetricsAsync_WithTimeWindow_ShouldFilterMetrics()
    {
        // Arrange
        var oldEvent = new NotificationEvent
        {
            EventType = "start", // Use "start" to increment sent counter
            UserId = 1,
            NotificationType = "old",
            Provider = "Firebase",
            Success = true,
            Timestamp = DateTime.UtcNow.AddHours(-2)
        };

        var recentEvent = new NotificationEvent
        {
            EventType = "start", // Use "start" to increment sent counter
            UserId = 1,
            NotificationType = "recent",
            Provider = "Firebase",
            Success = true,
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        };

        await _service.RecordNotificationEventAsync(oldEvent);
        await _service.RecordNotificationEventAsync(recentEvent);

        // Act
        var metrics = await _service.GetMetricsAsync(TimeSpan.FromHours(1));

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalNotificationsSent.Should().Be(1); // Only recent event within time window
        metrics.NotificationTypeBreakdown.Should().ContainKey("recent");
        metrics.NotificationTypeBreakdown.Should().NotContainKey("old");
    }

    [Fact]
    public async Task GetHealthReportAsync_ShouldReturnHealthStatus()
    {
        // Act
        var healthReport = await _service.GetHealthReportAsync();

        // Assert
        healthReport.Should().NotBeNull();
        healthReport.IsHealthy.Should().BeTrue();
        healthReport.FeaturesEnabled.Should().NotBeEmpty();
        healthReport.FeaturesEnabled.Should().ContainKey("metrics");
        healthReport.FeaturesEnabled.Should().ContainKey("auditing");
        healthReport.FeaturesEnabled.Should().ContainKey("rate_limiting");
        healthReport.FeaturesEnabled.Should().ContainKey("content_filtering");
        healthReport.FeaturesEnabled.Should().ContainKey("compression");
        healthReport.LastChecked.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ShouldAllowNotificationAsync_WithValidRequest_ShouldAllowNotification()
    {
        // Arrange
        var userId = 1;
        var notificationType = "test";

        // Act
        var result = await _service.ShouldAllowNotificationAsync(userId, notificationType, "Test content");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetPerformanceInsightsAsync_ShouldReturnInsights()
    {
        // Arrange
        // Record some events to generate insights
        var trackingId1 = Guid.NewGuid().ToString();
        var trackingId2 = Guid.NewGuid().ToString();
        var trackingId3 = Guid.NewGuid().ToString();

        var events = new[]
        {
            // Start events
            new NotificationEvent { EventType = "start", TrackingId = trackingId1, Provider = "Firebase", Success = true },
            new NotificationEvent { EventType = "start", TrackingId = trackingId2, Provider = "SignalR", Success = true },
            new NotificationEvent { EventType = "start", TrackingId = trackingId3, Provider = "Firebase", Success = false },

            // Complete events
            new NotificationEvent { EventType = "complete", TrackingId = trackingId1, Provider = "Firebase", Success = true, LatencyMs = 100 },
            new NotificationEvent { EventType = "complete", TrackingId = trackingId2, Provider = "SignalR", Success = true, LatencyMs = 50 },
            new NotificationEvent { EventType = "complete", TrackingId = trackingId3, Provider = "Firebase", Success = false, ErrorMessage = "Timeout" }
        };

        foreach (var evt in events)
        {
            await _service.RecordNotificationEventAsync(evt);
        }

        // Act
        var insights = await _service.GetPerformanceInsightsAsync();

        // Assert
        insights.Should().NotBeNull();
        insights.BestPerformingProvider.Should().NotBeNullOrEmpty();
        insights.WorstPerformingProvider.Should().NotBeNullOrEmpty();
        insights.Recommendations.Should().NotBeEmpty();
        insights.OverallSuccessRate.Should().BeGreaterThan(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
