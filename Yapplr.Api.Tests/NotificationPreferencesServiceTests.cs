using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Services;

namespace Yapplr.Api.Tests;

public class NotificationPreferencesServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly NotificationPreferencesService _service;
    private readonly Mock<ILogger<NotificationPreferencesService>> _mockLogger;

    public NotificationPreferencesServiceTests()
    {
        _context = new TestYapplrDbContext();
        _mockLogger = new Mock<ILogger<NotificationPreferencesService>>();
        _service = new NotificationPreferencesService(_context, _mockLogger.Object);
        
        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetUserPreferencesAsync_WhenPreferencesExist_ShouldReturnPreferences()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            UserId = 1,
            EnableLikeNotifications = true,
            EnableCommentNotifications = false,
            PreferredMethod = NotificationDeliveryMethod.SignalROnly
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserPreferencesAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.EnableLikeNotifications.Should().BeTrue();
        result.EnableCommentNotifications.Should().BeFalse();
        result.PreferredMethod.Should().Be(NotificationDeliveryMethod.SignalROnly);
    }

    [Fact]
    public async Task GetUserPreferencesAsync_WhenPreferencesDoNotExist_ShouldCreateDefaultPreferences()
    {
        // Act
        var result = await _service.GetUserPreferencesAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.EnableLikeNotifications.Should().BeTrue(); // Default value
        result.EnableCommentNotifications.Should().BeTrue(); // Default value
        result.PreferredMethod.Should().Be(NotificationDeliveryMethod.Auto); // Default value
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var existingPreferences = new NotificationPreferences
        {
            UserId = 1,
            EnableLikeNotifications = true,
            EnableCommentNotifications = true,
            EnableFollowNotifications = true,
            MaxNotificationsPerHour = 10
        };

        _context.NotificationPreferences.Add(existingPreferences);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateNotificationPreferencesDto
        {
            EnableLikeNotifications = false,
            MaxNotificationsPerHour = 20
            // Note: EnableCommentNotifications is not provided, should remain unchanged
        };

        // Act
        var result = await _service.UpdateUserPreferencesAsync(1, updateDto);

        // Assert
        result.EnableLikeNotifications.Should().BeFalse(); // Updated
        result.EnableCommentNotifications.Should().BeTrue(); // Unchanged
        result.EnableFollowNotifications.Should().BeTrue(); // Unchanged
        result.MaxNotificationsPerHour.Should().Be(20); // Updated
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_WhenNotificationTypeEnabled_ShouldReturnTrue()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            UserId = 1,
            EnableLikeNotifications = true,
            EnableCommentNotifications = false
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act & Assert
        var likeResult = await _service.ShouldSendNotificationAsync(1, "like");
        var commentResult = await _service.ShouldSendNotificationAsync(1, "comment");

        likeResult.Should().BeTrue();
        commentResult.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_WhenInQuietHours_ShouldReturnFalse()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var quietStart = new TimeOnly(now.Hour, 0); // Current hour
        var quietEnd = new TimeOnly((now.Hour + 1) % 24, 0); // Next hour

        var preferences = new NotificationPreferences
        {
            UserId = 1,
            EnableLikeNotifications = true,
            EnableQuietHours = true,
            QuietHoursStart = quietStart,
            QuietHoursEnd = quietEnd,
            QuietHoursTimezone = "UTC"
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ShouldSendNotificationAsync(1, "like");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPreferredDeliveryMethodAsync_ShouldReturnCorrectMethod()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            UserId = 1,
            PreferredMethod = NotificationDeliveryMethod.Auto,
            LikeDeliveryMethod = NotificationDeliveryMethod.FirebaseOnly,
            CommentDeliveryMethod = NotificationDeliveryMethod.Auto
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var likeMethod = await _service.GetPreferredDeliveryMethodAsync(1, "like");
        var commentMethod = await _service.GetPreferredDeliveryMethodAsync(1, "comment");

        // Assert
        likeMethod.Should().Be(NotificationDeliveryMethod.FirebaseOnly); // Specific method
        commentMethod.Should().Be(NotificationDeliveryMethod.Auto); // Falls back to general preference
    }

    [Fact]
    public async Task IsInQuietHoursAsync_WhenQuietHoursDisabled_ShouldReturnFalse()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            UserId = 1,
            EnableQuietHours = false
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsInQuietHoursAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("message", true)]
    [InlineData("mention", true)]
    [InlineData("reply", true)]
    [InlineData("comment", true)]
    [InlineData("follow", true)]
    [InlineData("like", true)]
    [InlineData("repost", true)]
    [InlineData("follow_request", true)]
    [InlineData("unknown_type", true)] // Should default to enabled
    public async Task ShouldSendNotificationAsync_WithDefaultPreferences_ShouldReturnExpectedResult(
        string notificationType, bool expected)
    {
        // Act
        var result = await _service.ShouldSendNotificationAsync(1, notificationType);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithQuietHoursSettings_ShouldUpdateCorrectly()
    {
        // Arrange
        var updateDto = new UpdateNotificationPreferencesDto
        {
            EnableQuietHours = true,
            QuietHoursStart = new TimeOnly(22, 0),
            QuietHoursEnd = new TimeOnly(8, 0),
            QuietHoursTimezone = "America/New_York"
        };

        // Act
        var result = await _service.UpdateUserPreferencesAsync(1, updateDto);

        // Assert
        result.EnableQuietHours.Should().BeTrue();
        result.QuietHoursStart.Should().Be(new TimeOnly(22, 0));
        result.QuietHoursEnd.Should().Be(new TimeOnly(8, 0));
        result.QuietHoursTimezone.Should().Be("America/New_York");
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithFrequencyLimits_ShouldUpdateCorrectly()
    {
        // Arrange
        var updateDto = new UpdateNotificationPreferencesDto
        {
            EnableFrequencyLimits = true,
            MaxNotificationsPerHour = 5,
            MaxNotificationsPerDay = 50
        };

        // Act
        var result = await _service.UpdateUserPreferencesAsync(1, updateDto);

        // Assert
        result.EnableFrequencyLimits.Should().BeTrue();
        result.MaxNotificationsPerHour.Should().Be(5);
        result.MaxNotificationsPerDay.Should().Be(50);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithAdvancedOptions_ShouldUpdateCorrectly()
    {
        // Arrange
        var updateDto = new UpdateNotificationPreferencesDto
        {
            RequireDeliveryConfirmation = true,
            EnableReadReceipts = true,
            EnableMessageHistory = false,
            MessageHistoryDays = 7,
            EnableOfflineReplay = false
        };

        // Act
        var result = await _service.UpdateUserPreferencesAsync(1, updateDto);

        // Assert
        result.RequireDeliveryConfirmation.Should().BeTrue();
        result.EnableReadReceipts.Should().BeTrue();
        result.EnableMessageHistory.Should().BeFalse();
        result.MessageHistoryDays.Should().Be(7);
        result.EnableOfflineReplay.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithEmailSettings_ShouldUpdateCorrectly()
    {
        // Arrange
        var updateDto = new UpdateNotificationPreferencesDto
        {
            EnableEmailNotifications = true,
            EnableEmailDigest = true,
            EmailDigestFrequencyHours = 12,
            EnableInstantEmailNotifications = false
        };

        // Act
        var result = await _service.UpdateUserPreferencesAsync(1, updateDto);

        // Assert
        result.EnableEmailNotifications.Should().BeTrue();
        result.EnableEmailDigest.Should().BeTrue();
        result.EmailDigestFrequencyHours.Should().Be(12);
        result.EnableInstantEmailNotifications.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldSendEmailNotificationAsync_WhenEmailNotificationsEnabled_ShouldReturnTrue()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            UserId = 1,
            EnableEmailNotifications = true,
            EnableInstantEmailNotifications = true,
            EnableLikeNotifications = true
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ShouldSendEmailNotificationAsync(1, "like");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSendEmailNotificationAsync_WhenEmailNotificationsDisabled_ShouldReturnFalse()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            UserId = 1,
            EnableEmailNotifications = false,
            EnableInstantEmailNotifications = true,
            EnableLikeNotifications = true
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ShouldSendEmailNotificationAsync(1, "like");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldSendEmailNotificationAsync_WhenInstantEmailDisabled_ShouldReturnFalse()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            UserId = 1,
            EnableEmailNotifications = true,
            EnableInstantEmailNotifications = false,
            EnableLikeNotifications = true
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ShouldSendEmailNotificationAsync(1, "like");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(NotificationDeliveryMethod.Auto)]
    [InlineData(NotificationDeliveryMethod.FirebaseOnly)]
    [InlineData(NotificationDeliveryMethod.SignalROnly)]
    [InlineData(NotificationDeliveryMethod.PollingOnly)]
    [InlineData(NotificationDeliveryMethod.EmailOnly)]
    [InlineData(NotificationDeliveryMethod.Disabled)]
    public async Task UpdateUserPreferencesAsync_WithDeliveryMethod_ShouldUpdateCorrectly(NotificationDeliveryMethod method)
    {
        // Arrange
        var updateDto = new UpdateNotificationPreferencesDto
        {
            PreferredMethod = method
        };

        // Act
        var result = await _service.UpdateUserPreferencesAsync(1, updateDto);

        // Assert
        result.PreferredMethod.Should().Be(method);
    }

    [Fact]
    public async Task ShouldSendEmailNotificationAsync_WithEmailOnlyDeliveryMethod_ShouldReturnTrue()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            UserId = 1,
            EnableEmailNotifications = true,
            EnableInstantEmailNotifications = true,
            EnableLikeNotifications = true,
            PreferredMethod = NotificationDeliveryMethod.EmailOnly
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ShouldSendEmailNotificationAsync(1, "like");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSendEmailNotificationAsync_WithAutoDeliveryMethod_ShouldReturnTrue()
    {
        // Arrange - Auto delivery method should allow email as fallback
        var preferences = new NotificationPreferences
        {
            UserId = 1,
            EnableEmailNotifications = true,
            EnableInstantEmailNotifications = true,
            EnableLikeNotifications = true,
            PreferredMethod = NotificationDeliveryMethod.Auto
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ShouldSendEmailNotificationAsync(1, "like");

        // Assert
        result.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
