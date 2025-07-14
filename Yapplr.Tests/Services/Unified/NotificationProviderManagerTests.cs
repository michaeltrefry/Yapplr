using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Unified;
using Yapplr.Api.Configuration;

namespace Yapplr.Tests.Services.Unified;

public class NotificationProviderManagerTests
{
    private readonly Mock<ILogger<NotificationProviderManager>> _mockLogger;
    private readonly Mock<IRealtimeNotificationProvider> _mockFirebaseProvider;
    private readonly Mock<IRealtimeNotificationProvider> _mockSignalRProvider;
    private readonly Mock<IRealtimeNotificationProvider> _mockExpoProvider;
    private readonly Mock<IOptionsMonitor<NotificationProvidersConfiguration>> _mockConfig;
    private readonly List<IRealtimeNotificationProvider> _providers;
    private readonly NotificationProviderManager _manager;

    public NotificationProviderManagerTests()
    {
        _mockLogger = new Mock<ILogger<NotificationProviderManager>>();
        _mockConfig = new Mock<IOptionsMonitor<NotificationProvidersConfiguration>>();

        // Setup configuration
        var config = new NotificationProvidersConfiguration
        {
            Firebase = new FirebaseConfiguration { Enabled = true },
            SignalR = new SignalRConfiguration { Enabled = true },
            Expo = new ExpoConfiguration { Enabled = true }
        };
        _mockConfig.Setup(x => x.CurrentValue).Returns(config);

        // Setup mock providers
        _mockFirebaseProvider = new Mock<IRealtimeNotificationProvider>();
        _mockFirebaseProvider.Setup(x => x.ProviderName).Returns("Firebase");
        _mockFirebaseProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);

        _mockSignalRProvider = new Mock<IRealtimeNotificationProvider>();
        _mockSignalRProvider.Setup(x => x.ProviderName).Returns("SignalR");
        _mockSignalRProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);

        _mockExpoProvider = new Mock<IRealtimeNotificationProvider>();
        _mockExpoProvider.Setup(x => x.ProviderName).Returns("Expo");
        _mockExpoProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);

        _providers = new List<IRealtimeNotificationProvider>
        {
            _mockFirebaseProvider.Object,
            _mockSignalRProvider.Object,
            _mockExpoProvider.Object
        };

        _manager = new NotificationProviderManager(_mockLogger.Object, _providers, _mockConfig.Object);
    }

    [Fact]
    public async Task SendNotificationAsync_WithAvailableProviders_ShouldUseHighestPriorityProvider()
    {
        // Arrange
        var request = new NotificationDeliveryRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "Test body",
            Data = new Dictionary<string, string> { { "key", "value" } }
        };

        _mockFirebaseProvider
            .Setup(x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _manager.SendNotificationAsync(request);

        // Assert
        result.Should().BeTrue();
        
        // Firebase should be tried first (highest priority)
        _mockFirebaseProvider.Verify(
            x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Once);

        // Other providers should not be called
        _mockSignalRProvider.Verify(
            x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenFirstProviderFails_ShouldFallbackToSecondProvider()
    {
        // Arrange
        var request = new NotificationDeliveryRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "Test body"
        };

        _mockFirebaseProvider
            .Setup(x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(false);

        _mockSignalRProvider
            .Setup(x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _manager.SendNotificationAsync(request);

        // Assert
        result.Should().BeTrue();
        
        // Firebase should be tried first
        _mockFirebaseProvider.Verify(
            x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Once);

        // SignalR should be tried as fallback
        _mockSignalRProvider.Verify(
            x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Once);

        // Expo should not be called
        _mockExpoProvider.Verify(
            x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenAllProvidersFail_ShouldReturnFalse()
    {
        // Arrange
        var request = new NotificationDeliveryRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "Test body"
        };

        _mockFirebaseProvider
            .Setup(x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(false);

        _mockSignalRProvider
            .Setup(x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(false);

        _mockExpoProvider
            .Setup(x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _manager.SendNotificationAsync(request);

        // Assert
        result.Should().BeFalse();
        
        // All providers should be tried
        _mockFirebaseProvider.Verify(
            x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Once);

        _mockSignalRProvider.Verify(
            x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Once);

        _mockExpoProvider.Verify(
            x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenProviderThrowsException_ShouldFallbackToNextProvider()
    {
        // Arrange
        var request = new NotificationDeliveryRequest
        {
            UserId = 1,
            NotificationType = "test",
            Title = "Test Notification",
            Body = "Test body"
        };

        _mockFirebaseProvider
            .Setup(x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(new Exception("Firebase error"));

        _mockSignalRProvider
            .Setup(x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _manager.SendNotificationAsync(request);

        // Assert
        result.Should().BeTrue();
        
        // Firebase should be tried first (and fail)
        _mockFirebaseProvider.Verify(
            x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Once);

        // SignalR should be tried as fallback
        _mockSignalRProvider.Verify(
            x => x.SendNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAvailableProvidersAsync_ShouldReturnOnlyAvailableProviders()
    {
        // Arrange
        _mockFirebaseProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        _mockSignalRProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(false);
        _mockExpoProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);

        // Act
        var availableProviders = await _manager.GetAvailableProvidersAsync();

        // Assert
        availableProviders.Should().HaveCount(2);
        availableProviders.Should().Contain(p => p.ProviderName == "Firebase");
        availableProviders.Should().Contain(p => p.ProviderName == "Expo");
        availableProviders.Should().NotContain(p => p.ProviderName == "SignalR");
    }

    [Fact]
    public async Task GetBestProviderAsync_ShouldReturnHighestPriorityAvailableProvider()
    {
        // Arrange
        var userId = 1;
        var notificationType = "test";

        _mockFirebaseProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        _mockSignalRProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);

        // Act
        var bestProvider = await _manager.GetBestProviderAsync(userId, notificationType);

        // Assert
        bestProvider.Should().NotBeNull();
        bestProvider!.ProviderName.Should().Be("Firebase"); // Highest priority
    }

    [Fact]
    public async Task GetBestProviderAsync_WhenNoProvidersAvailable_ShouldReturnNull()
    {
        // Arrange
        var userId = 1;
        var notificationType = "test";

        _mockFirebaseProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(false);
        _mockSignalRProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(false);
        _mockExpoProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(false);

        // Act
        var bestProvider = await _manager.GetBestProviderAsync(userId, notificationType);

        // Assert
        bestProvider.Should().BeNull();
    }

    [Fact]
    public async Task SendTestNotificationAsync_WithSpecificProvider_ShouldUseSpecifiedProvider()
    {
        // Arrange
        var userId = 1;
        var providerName = "SignalR";

        _mockSignalRProvider
            .Setup(x => x.SendTestNotificationAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _manager.SendTestNotificationAsync(userId, providerName);

        // Assert
        result.Should().BeTrue();

        // Only SignalR should be called
        _mockSignalRProvider.Verify(
            x => x.SendTestNotificationAsync(It.IsAny<int>()),
            Times.Once);

        _mockFirebaseProvider.Verify(
            x => x.SendTestNotificationAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SendTestNotificationAsync_WithoutSpecificProvider_ShouldUseBestAvailableProvider()
    {
        // Arrange
        var userId = 1;

        _mockFirebaseProvider
            .Setup(x => x.SendTestNotificationAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _manager.SendTestNotificationAsync(userId);

        // Assert
        result.Should().BeTrue();

        // Firebase should be used (highest priority)
        _mockFirebaseProvider.Verify(
            x => x.SendTestNotificationAsync(It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task HasAvailableProvidersAsync_WhenProvidersAvailable_ShouldReturnTrue()
    {
        // Arrange
        _mockFirebaseProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);

        // Act
        var hasProviders = await _manager.HasAvailableProvidersAsync();

        // Assert
        hasProviders.Should().BeTrue();
    }

    [Fact]
    public async Task HasAvailableProvidersAsync_WhenNoProvidersAvailable_ShouldReturnFalse()
    {
        // Arrange
        _mockFirebaseProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(false);
        _mockSignalRProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(false);
        _mockExpoProvider.Setup(x => x.IsAvailableAsync()).ReturnsAsync(false);

        // Act
        var hasProviders = await _manager.HasAvailableProvidersAsync();

        // Assert
        hasProviders.Should().BeFalse();
    }
}
