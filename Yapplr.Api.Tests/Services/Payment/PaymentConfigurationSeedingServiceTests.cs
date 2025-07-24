using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services.Payment;

namespace Yapplr.Api.Tests.Services.Payment;

public class PaymentConfigurationSeedingServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<ILogger<PaymentConfigurationSeedingService>> _mockLogger;
    private readonly PaymentConfigurationSeedingService _service;

    public PaymentConfigurationSeedingServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockLogger = new Mock<ILogger<PaymentConfigurationSeedingService>>();

        _service = new PaymentConfigurationSeedingService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task SeedDefaultConfigurationAsync_WithEmptyDatabase_ShouldSeedAllConfigurations()
    {
        // Arrange - Database is empty

        // Act
        await _service.SeedDefaultConfigurationAsync();

        // Assert
        // Check global configuration was seeded
        var globalConfig = await _context.PaymentGlobalConfigurations.FirstOrDefaultAsync();
        globalConfig.Should().NotBeNull();
        globalConfig!.DefaultProvider.Should().Be("PayPal");
        globalConfig.DefaultCurrency.Should().Be("USD");
        globalConfig.EnableTrialPeriods.Should().BeTrue();
        globalConfig.DefaultTrialDays.Should().Be(14);

        // Check provider configurations were seeded
        var providers = await _context.PaymentProviderConfigurations
            .Include(p => p.Settings)
            .ToListAsync();
        providers.Should().HaveCount(2);

        // Check PayPal provider
        var paypalProvider = providers.FirstOrDefault(p => p.ProviderName == "PayPal");
        paypalProvider.Should().NotBeNull();
        paypalProvider!.IsEnabled.Should().BeFalse(); // Disabled by default
        paypalProvider.Environment.Should().Be("sandbox");
        paypalProvider.Priority.Should().Be(1);
        paypalProvider.Settings.Should().HaveCountGreaterThan(0);

        // Check PayPal settings
        var clientIdSetting = paypalProvider.Settings.FirstOrDefault(s => s.Key == "ClientId");
        clientIdSetting.Should().NotBeNull();
        clientIdSetting!.IsRequired.Should().BeTrue();
        clientIdSetting.IsSensitive.Should().BeFalse();

        var clientSecretSetting = paypalProvider.Settings.FirstOrDefault(s => s.Key == "ClientSecret");
        clientSecretSetting.Should().NotBeNull();
        clientSecretSetting!.IsRequired.Should().BeTrue();
        clientSecretSetting.IsSensitive.Should().BeTrue();

        // Check Stripe provider
        var stripeProvider = providers.FirstOrDefault(p => p.ProviderName == "Stripe");
        stripeProvider.Should().NotBeNull();
        stripeProvider!.IsEnabled.Should().BeFalse(); // Disabled by default
        stripeProvider.Environment.Should().Be("test");
        stripeProvider.Priority.Should().Be(2);
        stripeProvider.Settings.Should().HaveCountGreaterThan(0);

        // Check Stripe settings
        var secretKeySetting = stripeProvider.Settings.FirstOrDefault(s => s.Key == "SecretKey");
        secretKeySetting.Should().NotBeNull();
        secretKeySetting!.IsRequired.Should().BeTrue();
        secretKeySetting.IsSensitive.Should().BeTrue();
    }

    [Fact]
    public async Task SeedDefaultConfigurationAsync_WithExistingGlobalConfig_ShouldOnlySeedProviders()
    {
        // Arrange - Add existing global configuration
        var existingGlobalConfig = new PaymentGlobalConfiguration
        {
            DefaultProvider = "Stripe",
            DefaultCurrency = "EUR",
            GracePeriodDays = 10,
            MaxPaymentRetries = 5,
            RetryIntervalDays = 2,
            EnableTrialPeriods = false,
            DefaultTrialDays = 7,
            EnableProration = false,
            WebhookTimeoutSeconds = 15,
            VerifyWebhookSignatures = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.PaymentGlobalConfigurations.Add(existingGlobalConfig);
        await _context.SaveChangesAsync();

        // Act
        await _service.SeedDefaultConfigurationAsync();

        // Assert
        // Global configuration should remain unchanged
        var globalConfig = await _context.PaymentGlobalConfigurations.FirstOrDefaultAsync();
        globalConfig.Should().NotBeNull();
        globalConfig!.DefaultProvider.Should().Be("Stripe"); // Should remain unchanged
        globalConfig.DefaultCurrency.Should().Be("EUR"); // Should remain unchanged

        // Provider configurations should be seeded
        var providers = await _context.PaymentProviderConfigurations.ToListAsync();
        providers.Should().HaveCount(2);
        providers.Should().Contain(p => p.ProviderName == "PayPal");
        providers.Should().Contain(p => p.ProviderName == "Stripe");
    }

    [Fact]
    public async Task SeedDefaultConfigurationAsync_WithExistingProviders_ShouldOnlySeedGlobalConfig()
    {
        // Arrange - Add existing provider configuration
        var existingProvider = new PaymentProviderConfiguration
        {
            ProviderName = "CustomProvider",
            IsEnabled = true,
            Environment = "production",
            Priority = 1,
            TimeoutSeconds = 60,
            MaxRetries = 5,
            SupportedCurrencies = JsonSerializer.Serialize(new[] { "USD" }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.PaymentProviderConfigurations.Add(existingProvider);
        await _context.SaveChangesAsync();

        // Act
        await _service.SeedDefaultConfigurationAsync();

        // Assert
        // Global configuration should be seeded
        var globalConfig = await _context.PaymentGlobalConfigurations.FirstOrDefaultAsync();
        globalConfig.Should().NotBeNull();
        globalConfig!.DefaultProvider.Should().Be("PayPal");

        // Provider configurations should remain unchanged (only the existing one)
        var providers = await _context.PaymentProviderConfigurations.ToListAsync();
        providers.Should().HaveCount(1);
        providers.First().ProviderName.Should().Be("CustomProvider");
    }

    [Fact]
    public async Task SeedDefaultConfigurationAsync_WithExistingData_ShouldSkipSeeding()
    {
        // Arrange - Add both global and provider configurations
        var existingGlobalConfig = new PaymentGlobalConfiguration
        {
            DefaultProvider = "CustomProvider",
            DefaultCurrency = "EUR",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.PaymentGlobalConfigurations.Add(existingGlobalConfig);

        var existingProvider = new PaymentProviderConfiguration
        {
            ProviderName = "CustomProvider",
            IsEnabled = true,
            Environment = "production",
            Priority = 1,
            TimeoutSeconds = 60,
            MaxRetries = 5,
            SupportedCurrencies = JsonSerializer.Serialize(new[] { "USD" }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.PaymentProviderConfigurations.Add(existingProvider);
        await _context.SaveChangesAsync();

        // Act
        await _service.SeedDefaultConfigurationAsync();

        // Assert
        // Nothing should change
        var globalConfigs = await _context.PaymentGlobalConfigurations.ToListAsync();
        globalConfigs.Should().HaveCount(1);
        globalConfigs.First().DefaultProvider.Should().Be("CustomProvider");

        var providers = await _context.PaymentProviderConfigurations.ToListAsync();
        providers.Should().HaveCount(1);
        providers.First().ProviderName.Should().Be("CustomProvider");
    }

    [Fact]
    public async Task SeedDefaultConfigurationAsync_ShouldCreateCorrectPayPalWebhookSettings()
    {
        // Act
        await _service.SeedDefaultConfigurationAsync();

        // Assert
        var paypalProvider = await _context.PaymentProviderConfigurations
            .Include(p => p.Settings)
            .FirstOrDefaultAsync(p => p.ProviderName == "PayPal");

        paypalProvider.Should().NotBeNull();

        // Check webhook-related settings
        var webhookEnabledEventsSetting = paypalProvider!.Settings
            .FirstOrDefault(s => s.Key == "WebhookEnabledEvents");
        webhookEnabledEventsSetting.Should().NotBeNull();

        // Verify the webhook events are properly serialized
        var enabledEvents = JsonSerializer.Deserialize<string[]>(webhookEnabledEventsSetting!.Value);
        enabledEvents.Should().NotBeNull();
        enabledEvents!.Should().Contain("BILLING.SUBSCRIPTION.CREATED");
        enabledEvents.Should().Contain("PAYMENT.SALE.COMPLETED");
        enabledEvents.Should().Contain("PAYMENT.SALE.REFUNDED");
    }

    [Fact]
    public async Task SeedDefaultConfigurationAsync_ShouldCreateCorrectSupportedCurrencies()
    {
        // Act
        await _service.SeedDefaultConfigurationAsync();

        // Assert
        var providers = await _context.PaymentProviderConfigurations.ToListAsync();

        // Check PayPal currencies
        var paypalProvider = providers.FirstOrDefault(p => p.ProviderName == "PayPal");
        var paypalCurrencies = JsonSerializer.Deserialize<string[]>(paypalProvider!.SupportedCurrencies);
        paypalCurrencies.Should().Contain(new[] { "USD", "EUR", "GBP", "CAD", "AUD" });

        // Check Stripe currencies
        var stripeProvider = providers.FirstOrDefault(p => p.ProviderName == "Stripe");
        var stripeCurrencies = JsonSerializer.Deserialize<string[]>(stripeProvider!.SupportedCurrencies);
        stripeCurrencies.Should().Contain(new[] { "USD", "EUR", "GBP", "CAD", "AUD" });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
