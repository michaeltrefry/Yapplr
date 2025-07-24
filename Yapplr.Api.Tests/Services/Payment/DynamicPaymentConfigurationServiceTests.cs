using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services.Payment;

namespace Yapplr.Api.Tests.Services.Payment;

public class DynamicPaymentConfigurationServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<DynamicPaymentConfigurationService>> _mockLogger;
    private readonly DynamicPaymentConfigurationService _service;

    public DynamicPaymentConfigurationServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<DynamicPaymentConfigurationService>>();

        _service = new DynamicPaymentConfigurationService(_context, _cache, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Seed global configuration
        var globalConfig = new PaymentGlobalConfiguration
        {
            DefaultProvider = "PayPal",
            DefaultCurrency = "USD",
            GracePeriodDays = 7,
            MaxPaymentRetries = 3,
            RetryIntervalDays = 3,
            EnableTrialPeriods = true,
            DefaultTrialDays = 14,
            EnableProration = true,
            WebhookTimeoutSeconds = 10,
            VerifyWebhookSignatures = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.PaymentGlobalConfigurations.Add(globalConfig);

        // Seed PayPal configuration
        var paypalProvider = new PaymentProviderConfiguration
        {
            ProviderName = "PayPal",
            IsEnabled = true,
            Environment = "sandbox",
            Priority = 1,
            TimeoutSeconds = 30,
            MaxRetries = 3,
            SupportedCurrencies = JsonSerializer.Serialize(new[] { "USD", "EUR", "GBP" }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Settings = new List<PaymentProviderSetting>
            {
                new PaymentProviderSetting
                {
                    Key = "ClientId",
                    Value = "test-client-id",
                    IsSensitive = false,
                    Description = "PayPal Client ID",
                    Category = "Authentication",
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new PaymentProviderSetting
                {
                    Key = "ClientSecret",
                    Value = "test-client-secret",
                    IsSensitive = true,
                    Description = "PayPal Client Secret",
                    Category = "Authentication",
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };
        _context.PaymentProviderConfigurations.Add(paypalProvider);

        // Seed Stripe configuration (disabled)
        var stripeProvider = new PaymentProviderConfiguration
        {
            ProviderName = "Stripe",
            IsEnabled = false,
            Environment = "test",
            Priority = 2,
            TimeoutSeconds = 30,
            MaxRetries = 3,
            SupportedCurrencies = JsonSerializer.Serialize(new[] { "USD", "EUR" }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Settings = new List<PaymentProviderSetting>
            {
                new PaymentProviderSetting
                {
                    Key = "SecretKey",
                    Value = "sk_test_123",
                    IsSensitive = true,
                    Description = "Stripe Secret Key",
                    Category = "Authentication",
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };
        _context.PaymentProviderConfigurations.Add(stripeProvider);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetPayPalConfigurationAsync_ShouldReturnCorrectConfiguration()
    {
        // Act
        var result = await _service.GetPayPalConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Enabled.Should().BeTrue();
        result.Environment.Should().Be("sandbox");
        result.TimeoutSeconds.Should().Be(30);
        result.MaxRetries.Should().Be(3);
        result.ClientId.Should().Be("test-client-id");
        result.ClientSecret.Should().Be("test-client-secret");
        result.SupportedCurrencies.Should().Contain(new[] { "USD", "EUR", "GBP" });
    }

    [Fact]
    public async Task GetStripeConfigurationAsync_ShouldReturnCorrectConfiguration()
    {
        // Act
        var result = await _service.GetStripeConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Enabled.Should().BeFalse();
        result.Environment.Should().Be("test");
        result.SecretKey.Should().Be("sk_test_123");
        result.SupportedCurrencies.Should().Contain(new[] { "USD", "EUR" });
    }

    [Fact]
    public async Task GetGlobalSettingsAsync_ShouldReturnCorrectSettings()
    {
        // Act
        var result = await _service.GetGlobalSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.DefaultCurrency.Should().Be("USD");
        result.GracePeriodDays.Should().Be(7);
        result.MaxPaymentRetries.Should().Be(3);
        result.EnableTrialPeriods.Should().BeTrue();
        result.DefaultTrialDays.Should().Be(14);
        result.VerifyWebhookSignatures.Should().BeTrue();
    }

    [Fact]
    public async Task GetDefaultProviderAsync_ShouldReturnPayPal()
    {
        // Act
        var result = await _service.GetDefaultProviderAsync();

        // Assert
        result.Should().Be("PayPal");
    }

    [Fact]
    public async Task GetProviderPriorityAsync_ShouldReturnEnabledProvidersInOrder()
    {
        // Act
        var result = await _service.GetProviderPriorityAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("PayPal");
        result.Should().NotContain("Stripe"); // Stripe is disabled
    }

    [Fact]
    public async Task UpdateGlobalSettingsAsync_ShouldUpdateAndClearCache()
    {
        // Arrange
        var newSettings = new PaymentGlobalSettings
        {
            DefaultCurrency = "EUR",
            GracePeriodDays = 10,
            MaxPaymentRetries = 5,
            RetryIntervalDays = 2,
            EnableTrialPeriods = false,
            DefaultTrialDays = 7,
            EnableProration = false,
            WebhookTimeoutSeconds = 15,
            VerifyWebhookSignatures = false
        };

        // Act
        var result = await _service.UpdateGlobalSettingsAsync(newSettings);

        // Assert
        result.Should().BeTrue();

        // Verify the settings were updated
        var updatedSettings = await _service.GetGlobalSettingsAsync();
        updatedSettings.DefaultCurrency.Should().Be("EUR");
        updatedSettings.GracePeriodDays.Should().Be(10);
        updatedSettings.EnableTrialPeriods.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshCacheAsync_ShouldClearAllCacheEntries()
    {
        // Arrange - First call to populate cache
        await _service.GetPayPalConfigurationAsync();
        await _service.GetGlobalSettingsAsync();

        // Act
        await _service.RefreshCacheAsync();

        // Assert - Verify cache was cleared by checking if fresh data is loaded
        var paypalConfig = await _service.GetPayPalConfigurationAsync();
        paypalConfig.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPaymentProvidersConfigurationAsync_ShouldReturnCompleteConfiguration()
    {
        // Act
        var result = await _service.GetPaymentProvidersConfigurationAsync();

        // Assert
        result.Should().NotBeNull();
        result.Global.Should().NotBeNull();
        result.PayPal.Should().NotBeNull();
        result.Stripe.Should().NotBeNull();
        result.DefaultProvider.Should().Be("PayPal");
        result.ProviderPriority.Should().Contain("PayPal");
    }

    [Fact]
    public async Task ConfigurationChanged_EventShouldBeFired_WhenGlobalSettingsUpdated()
    {
        // Arrange
        var eventFired = false;
        PaymentConfigurationChangedEventArgs? eventArgs = null;

        _service.ConfigurationChanged += (sender, args) =>
        {
            eventFired = true;
            eventArgs = args;
        };

        var newSettings = new PaymentGlobalSettings
        {
            DefaultCurrency = "EUR",
            GracePeriodDays = 10,
            MaxPaymentRetries = 5,
            RetryIntervalDays = 2,
            EnableTrialPeriods = false,
            DefaultTrialDays = 7,
            EnableProration = false,
            WebhookTimeoutSeconds = 15,
            VerifyWebhookSignatures = false
        };

        // Act
        await _service.UpdateGlobalSettingsAsync(newSettings);

        // Assert
        eventFired.Should().BeTrue();
        eventArgs.Should().NotBeNull();
        eventArgs!.ProviderName.Should().Be("Global");
        eventArgs.ChangeType.Should().Be("Updated");
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }
}
