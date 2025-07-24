using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Models;
using Yapplr.Api.Services.Payment;

namespace Yapplr.Api.Tests.Services.Payment;

public class PaymentGatewayServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<IPaymentGatewayManager> _mockGatewayManager;
    private readonly Mock<ILogger<PaymentGatewayService>> _mockLogger;
    private readonly Mock<IDynamicPaymentConfigurationService> _mockConfigService;
    private readonly PaymentGatewayService _service;
    private readonly Mock<IPaymentProvider> _mockPaymentProvider;

    public PaymentGatewayServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockGatewayManager = new Mock<IPaymentGatewayManager>();
        _mockLogger = new Mock<ILogger<PaymentGatewayService>>();
        _mockConfigService = new Mock<IDynamicPaymentConfigurationService>();
        _mockPaymentProvider = new Mock<IPaymentProvider>();

        var globalSettings = new PaymentGlobalSettings
        {
            EnableTrialPeriods = true,
            DefaultTrialDays = 14,
            DefaultCurrency = "USD",
            GracePeriodDays = 7,
            MaxPaymentRetries = 3,
            RetryIntervalDays = 3,
            EnableProration = true,
            WebhookTimeoutSeconds = 10,
            VerifyWebhookSignatures = true
        };

        _mockConfigService.Setup(x => x.GetGlobalSettingsAsync()).ReturnsAsync(globalSettings);

        _service = new PaymentGatewayService(_context, _mockGatewayManager.Object, _mockLogger.Object, _mockConfigService.Object);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithValidData_ShouldCreateSubscription()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        var subscriptionTier = new SubscriptionTier
        {
            Id = 1,
            Name = "Premium",
            Description = "Premium subscription",
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SubscriptionTiers.Add(subscriptionTier);
        await _context.SaveChangesAsync();

        var request = new CreateSubscriptionRequest
        {
            PaymentProvider = "PayPal",
            StartTrial = true
        };

        _mockPaymentProvider.Setup(x => x.ProviderName).Returns("PayPal");
        _mockPaymentProvider.Setup(x => x.CreateSubscriptionAsync(It.IsAny<CreateSubscriptionRequest>()))
            .ReturnsAsync(new CreateSubscriptionResult
            {
                Success = true,
                ExternalSubscriptionId = "sub_123",
                Status = "ACTIVE",
                NextBillingDate = DateTime.UtcNow.AddMonths(1)
            });

        _mockGatewayManager.Setup(x => x.GetBestProviderAsync(It.IsAny<string>()))
            .ReturnsAsync(_mockPaymentProvider.Object);

        // Act
        var result = await _service.CreateSubscriptionAsync(user.Id, subscriptionTier.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(user.Id);
        result.Data.SubscriptionTierId.Should().Be(subscriptionTier.Id);
        result.Data.PaymentProvider.Should().Be("PayPal");
        result.Data.ExternalSubscriptionId.Should().Be("sub_123");
        result.Data.IsTrialPeriod.Should().BeTrue();
        result.Data.TrialEndDate.Should().NotBeNull();

        // Verify database changes
        var userSubscription = await _context.Set<UserSubscription>()
            .FirstOrDefaultAsync(s => s.UserId == user.Id);
        userSubscription.Should().NotBeNull();
        userSubscription!.ExternalSubscriptionId.Should().Be("sub_123");

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.UserId == user.Id);
        transaction.Should().NotBeNull();
        transaction!.PaymentProvider.Should().Be("PayPal");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Arrange
        var request = new CreateSubscriptionRequest
        {
            PaymentProvider = "PayPal"
        };

        // Act
        var result = await _service.CreateSubscriptionAsync(999, 1, request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithNonExistentTier_ShouldReturnError()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new CreateSubscriptionRequest
        {
            PaymentProvider = "PayPal"
        };

        // Act
        var result = await _service.CreateSubscriptionAsync(user.Id, 999, request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("TIER_NOT_FOUND");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithExistingActiveSubscription_ShouldReturnError()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        var subscriptionTier = new SubscriptionTier
        {
            Id = 1,
            Name = "Premium",
            Description = "Premium subscription",
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var existingSubscription = new UserSubscription
        {
            Id = 1,
            UserId = user.Id,
            SubscriptionTierId = subscriptionTier.Id,
            PaymentProvider = "PayPal",
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SubscriptionTiers.Add(subscriptionTier);
        _context.Set<UserSubscription>().Add(existingSubscription);
        await _context.SaveChangesAsync();

        var request = new CreateSubscriptionRequest
        {
            PaymentProvider = "PayPal"
        };

        // Act
        var result = await _service.CreateSubscriptionAsync(user.Id, subscriptionTier.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("SUBSCRIPTION_EXISTS");
    }

    [Fact]
    public async Task GetUserSubscriptionAsync_WithActiveSubscription_ShouldReturnSubscription()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        var subscriptionTier = new SubscriptionTier
        {
            Id = 1,
            Name = "Premium",
            Description = "Premium subscription",
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var userSubscription = new UserSubscription
        {
            Id = 1,
            UserId = user.Id,
            SubscriptionTierId = subscriptionTier.Id,
            PaymentProvider = "PayPal",
            ExternalSubscriptionId = "sub_123",
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SubscriptionTiers.Add(subscriptionTier);
        _context.Set<UserSubscription>().Add(userSubscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserSubscriptionAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(user.Id);
        result.Data.SubscriptionTierName.Should().Be("Premium");
        result.Data.PaymentProvider.Should().Be("PayPal");
        result.Data.ExternalSubscriptionId.Should().Be("sub_123");
    }

    [Fact]
    public async Task GetUserSubscriptionAsync_WithNoSubscription_ShouldReturnError()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserSubscriptionAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NO_SUBSCRIPTION");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
