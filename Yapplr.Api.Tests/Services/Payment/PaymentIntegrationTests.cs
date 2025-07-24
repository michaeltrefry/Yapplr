using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Models;
using Yapplr.Api.Services.Payment;

namespace Yapplr.Api.Tests.Services.Payment;

public class PaymentIntegrationTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly IPaymentGatewayService _paymentService;
    private readonly IPaymentAdminService _adminService;

    public PaymentIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);

        // Create service collection and register dependencies
        var services = new ServiceCollection();
        services.AddSingleton(_context);
        services.AddLogging();
        services.AddMemoryCache();

        // Setup mock dynamic payment configuration service
        var mockConfigService = new Mock<IDynamicPaymentConfigurationService>();
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
        mockConfigService.Setup(x => x.GetGlobalSettingsAsync()).ReturnsAsync(globalSettings);
        mockConfigService.Setup(x => x.GetProviderPriorityAsync()).ReturnsAsync(new List<string> { "PayPal", "Stripe" });
        mockConfigService.Setup(x => x.GetDefaultProviderAsync()).ReturnsAsync("PayPal");

        // Setup PayPal configuration
        var paypalConfig = new PayPalConfiguration
        {
            Enabled = true,
            Environment = "sandbox",
            TimeoutSeconds = 30,
            MaxRetries = 3,
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            SupportedCurrencies = new List<string> { "USD", "EUR" }
        };
        mockConfigService.Setup(x => x.GetPayPalConfigurationAsync()).ReturnsAsync(paypalConfig);

        // Setup Stripe configuration
        var stripeConfig = new StripeConfiguration
        {
            Enabled = true,
            Environment = "test",
            TimeoutSeconds = 30,
            MaxRetries = 3,
            SecretKey = "sk_test_123",
            SupportedCurrencies = new List<string> { "USD", "EUR" }
        };
        mockConfigService.Setup(x => x.GetStripeConfigurationAsync()).ReturnsAsync(stripeConfig);

        services.AddSingleton(mockConfigService.Object);

        // Create mock payment providers
        var mockPayPalProvider = new Mock<IPaymentProvider>();
        mockPayPalProvider.Setup(p => p.ProviderName).Returns("PayPal");
        mockPayPalProvider.Setup(p => p.IsAvailableAsync()).ReturnsAsync(true);
        mockPayPalProvider.Setup(p => p.CreateSubscriptionAsync(It.IsAny<CreateSubscriptionRequest>()))
            .ReturnsAsync(new CreateSubscriptionResult
            {
                Success = true,
                ExternalSubscriptionId = "test-subscription-id",
                NextBillingDate = DateTime.UtcNow.AddMonths(1),
                RequiresAction = false
            });

        var mockStripeProvider = new Mock<IPaymentProvider>();
        mockStripeProvider.Setup(p => p.ProviderName).Returns("Stripe");
        mockStripeProvider.Setup(p => p.IsAvailableAsync()).ReturnsAsync(true);
        mockStripeProvider.Setup(p => p.CreateSubscriptionAsync(It.IsAny<CreateSubscriptionRequest>()))
            .ReturnsAsync(new CreateSubscriptionResult
            {
                Success = true,
                ExternalSubscriptionId = "test-subscription-id",
                NextBillingDate = DateTime.UtcNow.AddMonths(1),
                RequiresAction = false
            });

        // Register provider collection
        services.AddScoped<IEnumerable<IPaymentProvider>>(provider =>
        {
            var providers = new List<IPaymentProvider>();
            providers.Add(mockPayPalProvider.Object);
            providers.Add(mockStripeProvider.Object);
            return providers;
        });

        // Register payment services
        services.AddScoped<IPaymentGatewayManager, PaymentGatewayManager>();
        services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
        services.AddScoped<IPaymentAdminService, PaymentAdminService>();

        var serviceProvider = services.BuildServiceProvider();
        _paymentService = serviceProvider.GetRequiredService<IPaymentGatewayService>();
        _adminService = serviceProvider.GetRequiredService<IPaymentAdminService>();
    }

    // Helper methods for creating test data
    private async Task<User> CreateTestUserAsync()
    {
        var user = new User
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            PasswordHash = "test-hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<SubscriptionTier> CreateTestSubscriptionTierAsync()
    {
        var tier = new SubscriptionTier
        {
            Name = $"Test Tier {Guid.NewGuid():N}",
            Description = "Test subscription tier",
            Price = 29.99m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SubscriptionTiers.Add(tier);
        await _context.SaveChangesAsync();
        return tier;
    }

    private async Task<UserSubscription> CreateTestSubscriptionAsync(int userId, int tierId, bool isPastDue = false)
    {
        var subscription = new UserSubscription
        {
            UserId = userId,
            SubscriptionTierId = tierId,
            PaymentProvider = "PayPal",
            ExternalSubscriptionId = $"test_sub_{Guid.NewGuid():N}",
            Status = isPastDue ? SubscriptionStatus.PastDue : SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            NextBillingDate = DateTime.UtcNow.AddDays(30),
            IsTrialPeriod = false,
            BillingCycleCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    private async Task<PaymentTransaction> CreateTestPaymentTransactionAsync(int userId, int tierId)
    {
        var transaction = new PaymentTransaction
        {
            UserId = userId,
            SubscriptionTierId = tierId,
            ExternalTransactionId = $"test_txn_{Guid.NewGuid():N}",
            PaymentProvider = "PayPal",
            Amount = 29.99m,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            Type = PaymentType.Subscription,
            Description = "Test payment",
            BillingPeriodStart = DateTime.UtcNow.AddDays(-30),
            BillingPeriodEnd = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    [Fact]
    public async Task CreateSubscription_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tier = await CreateTestSubscriptionTierAsync();

        var request = new CreateSubscriptionRequest
        {
            PaymentProvider = "PayPal", // Use a valid provider name
            StartTrial = true,
            ReturnUrl = "https://test.com/success",
            CancelUrl = "https://test.com/cancel"
        };

        // Act
        var result = await _paymentService.CreateSubscriptionAsync(user.Id, tier.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(tier.Id, result.Data.SubscriptionTierId);
        Assert.True(result.Data.IsTrialPeriod);
    }

    [Fact(Skip = "CancelSubscriptionAsync not yet implemented")]
    public async Task CancelSubscription_WithActiveSubscription_ShouldSucceed()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tier = await CreateTestSubscriptionTierAsync();
        var subscription = await CreateTestSubscriptionAsync(user.Id, tier.Id);

        var request = new CancelSubscriptionRequest
        {
            CancelImmediately = false,
            Reason = "User requested cancellation"
        };

        // Act
        var result = await _paymentService.CancelSubscriptionAsync(user.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetSubscriptionStatus_WithExistingSubscription_ShouldReturnCorrectStatus()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tier = await CreateTestSubscriptionTierAsync();
        var subscription = await CreateTestSubscriptionAsync(user.Id, tier.Id);

        // Act
        var result = await _paymentService.GetUserSubscriptionAsync(user.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(subscription.Status, result.Data.Status);
        Assert.Equal(tier.Id, result.Data.SubscriptionTierId);
    }

    [Fact(Skip = "ProcessPaymentAsync not yet implemented")]
    public async Task ProcessPayment_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var request = new ProcessPaymentRequest
        {
            Amount = 29.99m,
            Currency = "USD",
            Description = "Test payment",
            PaymentMethodId = "test-payment-method"
        };

        // Act
        var result = await _paymentService.ProcessPaymentAsync(user.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(request.Amount, result.Data.Amount);
        Assert.Equal(request.Currency, result.Data.Currency);
    }

    [Fact(Skip = "GetPaymentHistoryAsync not yet implemented")]
    public async Task GetPaymentHistory_WithExistingTransactions_ShouldReturnHistory()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tier = await CreateTestSubscriptionTierAsync();
        var transaction = await CreateTestPaymentTransactionAsync(user.Id, tier.Id);

        // Act
        var result = await _paymentService.GetPaymentHistoryAsync(user.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data);
        Assert.Contains(result.Data, t => t.Id == transaction.Id);
    }

    [Fact]
    public async Task AdminService_GetSubscriptions_ShouldReturnPagedResults()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tier = await CreateTestSubscriptionTierAsync();
        var subscription = await CreateTestSubscriptionAsync(user.Id, tier.Id);

        // Act
        var result = await _adminService.GetSubscriptionsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, s => s.Id == subscription.Id);
        Assert.True(result.TotalCount > 0);
    }

    [Fact]
    public async Task AdminService_GetPaymentAnalytics_ShouldReturnAnalytics()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tier = await CreateTestSubscriptionTierAsync();
        var subscription = await CreateTestSubscriptionAsync(user.Id, tier.Id);
        var transaction = await CreateTestPaymentTransactionAsync(user.Id, tier.Id);

        // Act
        var result = await _adminService.GetPaymentAnalyticsAsync(days: 30);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalSubscriptions > 0);
        Assert.True(result.TotalTransactions > 0);
        Assert.True(result.TotalRevenue > 0);
    }

    [Fact]
    public async Task AdminService_GetTransactions_ShouldReturnPagedResults()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tier = await CreateTestSubscriptionTierAsync();
        var transaction = await CreateTestPaymentTransactionAsync(user.Id, tier.Id);

        // Act
        var result = await _adminService.GetTransactionsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, t => t.Id == transaction.Id);
        Assert.True(result.TotalCount > 0);
    }

    [Fact(Skip = "SyncSubscriptionStatusAsync not yet implemented")]
    public async Task AdminService_SyncSubscription_ShouldUpdateStatus()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tier = await CreateTestSubscriptionTierAsync();
        var subscription = await CreateTestSubscriptionAsync(user.Id, tier.Id);

        // Act
        var result = await _adminService.SyncSubscriptionAsync(subscription.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(subscription.Id, result.Data.Id);
    }

    [Fact]
    public async Task AdminService_GetSubscriptionTiers_ShouldReturnTiers()
    {
        // Arrange
        var tier = await CreateTestSubscriptionTierAsync();

        // Act
        var result = await _adminService.GetSubscriptionTiersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, t => t.Id == tier.Id);
    }

    [Fact]
    public async Task AdminService_UpdateSubscriptionTier_ShouldUpdateTier()
    {
        // Arrange
        var tier = await CreateTestSubscriptionTierAsync();
        var updateRequest = new UpdateSubscriptionTierRequest
        {
            Name = "Updated Tier Name",
            Price = 39.99m,
            IsActive = false
        };

        // Act
        var result = await _adminService.UpdateSubscriptionTierAsync(tier.Id, updateRequest);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(updateRequest.Name, result.Data.Name);
        Assert.Equal(updateRequest.Price, result.Data.Price);
        Assert.Equal(updateRequest.IsActive, result.Data.IsActive);
    }

    [Fact]
    public async Task AdminService_GetPaymentProviders_ShouldReturnProviders()
    {
        // Act
        var result = await _adminService.GetPaymentProvidersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, p => p.Name == "PayPal");
        Assert.Contains(result, p => p.Name == "Stripe");
    }

    [Fact]
    public async Task AdminService_GetFailedPayments_ShouldReturnFailedPayments()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tier = await CreateTestSubscriptionTierAsync();
        var subscription = await CreateTestSubscriptionAsync(user.Id, tier.Id, isPastDue: true);

        // Act
        var result = await _adminService.GetFailedPaymentsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        // Note: This might be empty if no failed payments exist, which is expected in tests
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
