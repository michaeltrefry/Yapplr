using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using Yapplr.Api.Controllers.Admin;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Models;
using Yapplr.Api.Services.Payment;

namespace Yapplr.Api.Tests.Controllers.Admin;

public class PaymentConfigurationControllerTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<IDynamicPaymentConfigurationService> _mockConfigService;
    private readonly Mock<ILogger<PaymentConfigurationController>> _mockLogger;
    private readonly PaymentConfigurationController _controller;

    public PaymentConfigurationControllerTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockConfigService = new Mock<IDynamicPaymentConfigurationService>();
        _mockLogger = new Mock<ILogger<PaymentConfigurationController>>();

        _controller = new PaymentConfigurationController(_context, _mockConfigService.Object, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
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

        var paypalProvider = new PaymentProviderConfiguration
        {
            ProviderName = "PayPal",
            IsEnabled = true,
            Environment = "sandbox",
            Priority = 1,
            TimeoutSeconds = 30,
            MaxRetries = 3,
            SupportedCurrencies = JsonSerializer.Serialize(new[] { "USD", "EUR" }),
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
                }
            }
        };
        _context.PaymentProviderConfigurations.Add(paypalProvider);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetProviderConfigurations_ShouldReturnAllProviders()
    {
        // Act
        var result = await _controller.GetProviderConfigurations();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var providers = okResult.Value.Should().BeAssignableTo<List<PaymentProviderConfigurationDto>>().Subject;
        providers.Should().HaveCount(1);
        providers.First().ProviderName.Should().Be("PayPal");
    }

    [Fact]
    public async Task GetProviderConfiguration_WithValidProvider_ShouldReturnProvider()
    {
        // Act
        var result = await _controller.GetProviderConfiguration("PayPal");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var provider = okResult.Value.Should().BeAssignableTo<PaymentProviderConfigurationDto>().Subject;
        provider.ProviderName.Should().Be("PayPal");
        provider.IsEnabled.Should().BeTrue();
        provider.Environment.Should().Be("sandbox");
    }

    [Fact]
    public async Task GetProviderConfiguration_WithInvalidProvider_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.GetProviderConfiguration("InvalidProvider");

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateOrUpdateProviderConfiguration_WithNewProvider_ShouldCreateProvider()
    {
        // Arrange
        var dto = new CreateUpdatePaymentProviderConfigurationDto
        {
            ProviderName = "Stripe",
            IsEnabled = true,
            Environment = "test",
            Priority = 2,
            TimeoutSeconds = 30,
            MaxRetries = 3,
            SupportedCurrencies = new List<string> { "USD", "EUR" },
            Settings = new List<CreateUpdatePaymentProviderSettingDto>
            {
                new CreateUpdatePaymentProviderSettingDto
                {
                    Key = "SecretKey",
                    Value = "sk_test_123",
                    IsSensitive = true,
                    Description = "Stripe Secret Key",
                    Category = "Authentication",
                    IsRequired = true
                }
            }
        };

        // Act
        var result = await _controller.CreateOrUpdateProviderConfiguration("Stripe", dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var provider = okResult.Value.Should().BeAssignableTo<PaymentProviderConfigurationDto>().Subject;
        provider.ProviderName.Should().Be("Stripe");
        provider.IsEnabled.Should().BeTrue();

        // Verify it was saved to database
        var savedProvider = await _context.PaymentProviderConfigurations
            .Include(p => p.Settings)
            .FirstOrDefaultAsync(p => p.ProviderName == "Stripe");
        savedProvider.Should().NotBeNull();
        savedProvider!.Settings.Should().HaveCount(1);
        savedProvider.Settings.First().Key.Should().Be("SecretKey");
    }

    [Fact]
    public async Task CreateOrUpdateProviderConfiguration_WithExistingProvider_ShouldUpdateProvider()
    {
        // Arrange
        var dto = new CreateUpdatePaymentProviderConfigurationDto
        {
            ProviderName = "PayPal",
            IsEnabled = false, // Changed from true
            Environment = "live", // Changed from sandbox
            Priority = 1,
            TimeoutSeconds = 60, // Changed from 30
            MaxRetries = 5, // Changed from 3
            SupportedCurrencies = new List<string> { "USD", "EUR", "GBP" },
            Settings = new List<CreateUpdatePaymentProviderSettingDto>
            {
                new CreateUpdatePaymentProviderSettingDto
                {
                    Key = "ClientId",
                    Value = "updated-client-id", // Changed value
                    IsSensitive = false,
                    Description = "PayPal Client ID",
                    Category = "Authentication",
                    IsRequired = true
                }
            }
        };

        // Act
        var result = await _controller.CreateOrUpdateProviderConfiguration("PayPal", dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var provider = okResult.Value.Should().BeAssignableTo<PaymentProviderConfigurationDto>().Subject;
        provider.IsEnabled.Should().BeFalse();
        provider.Environment.Should().Be("live");
        provider.TimeoutSeconds.Should().Be(60);

        // Verify changes were saved
        var updatedProvider = await _context.PaymentProviderConfigurations
            .Include(p => p.Settings)
            .FirstOrDefaultAsync(p => p.ProviderName == "PayPal");
        updatedProvider!.IsEnabled.Should().BeFalse();
        updatedProvider.Environment.Should().Be("live");
        updatedProvider.Settings.First().Value.Should().Be("updated-client-id");
    }

    [Fact]
    public async Task DeleteProviderConfiguration_WithValidProvider_ShouldDeleteProvider()
    {
        // Act
        var result = await _controller.DeleteProviderConfiguration("PayPal");

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify it was deleted from database
        var deletedProvider = await _context.PaymentProviderConfigurations
            .FirstOrDefaultAsync(p => p.ProviderName == "PayPal");
        deletedProvider.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProviderConfiguration_WithInvalidProvider_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.DeleteProviderConfiguration("InvalidProvider");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetGlobalConfiguration_ShouldReturnGlobalConfig()
    {
        // Act
        var result = await _controller.GetGlobalConfiguration();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var config = okResult.Value.Should().BeAssignableTo<PaymentGlobalConfigurationDto>().Subject;
        config.DefaultProvider.Should().Be("PayPal");
        config.DefaultCurrency.Should().Be("USD");
        config.EnableTrialPeriods.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateGlobalConfiguration_ShouldUpdateAndReturnConfig()
    {
        // Arrange
        var dto = new UpdatePaymentGlobalConfigurationDto
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
            VerifyWebhookSignatures = false
        };

        // Act
        var result = await _controller.UpdateGlobalConfiguration(dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var config = okResult.Value.Should().BeAssignableTo<PaymentGlobalConfigurationDto>().Subject;
        config.DefaultProvider.Should().Be("Stripe");
        config.DefaultCurrency.Should().Be("EUR");
        config.EnableTrialPeriods.Should().BeFalse();

        // Verify changes were saved
        var updatedConfig = await _context.PaymentGlobalConfigurations.FirstOrDefaultAsync();
        updatedConfig!.DefaultProvider.Should().Be("Stripe");
        updatedConfig.DefaultCurrency.Should().Be("EUR");
        updatedConfig.EnableTrialPeriods.Should().BeFalse();
    }

    [Fact]
    public async Task TestProviderConfiguration_WithValidProvider_ShouldReturnTestResult()
    {
        // Act
        var result = await _controller.TestProviderConfiguration("PayPal");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var testResult = okResult.Value.Should().BeAssignableTo<PaymentProviderTestResultDto>().Subject;
        testResult.ProviderName.Should().Be("PayPal");
        testResult.Should().NotBeNull();
    }

    [Fact]
    public async Task TestProviderConfiguration_WithInvalidProvider_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.TestProviderConfiguration("InvalidProvider");

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
