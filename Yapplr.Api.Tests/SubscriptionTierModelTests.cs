using Xunit;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;

namespace Yapplr.Api.Tests;

public class SubscriptionTierModelTests
{
    #region Model Validation Tests

    [Fact]
    public void SubscriptionTier_WithValidData_PassesValidation()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = "Premium",
            Description = "Premium subscription tier",
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = false,
            SortOrder = 1,
            ShowAdvertisements = false,
            HasVerifiedBadge = true,
            Features = "{\"premium\": true}"
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void SubscriptionTier_WithEmptyName_FailsValidation()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = "", // Invalid - required
            Description = "Test description",
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void SubscriptionTier_WithNullName_FailsValidation()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = null!, // Invalid - required
            Description = "Test description",
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void SubscriptionTier_WithNameTooLong_FailsValidation()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = new string('A', 101), // Invalid - max length 100
            Description = "Test description",
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void SubscriptionTier_WithDescriptionTooLong_FailsValidation()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = "Premium",
            Description = new string('A', 501), // Invalid - max length 500
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Description"));
    }

    [Fact]
    public void SubscriptionTier_WithEmptyCurrency_FailsValidation()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = "Premium",
            Description = "Test description",
            Price = 9.99m,
            Currency = "", // Invalid - required
            BillingCycleMonths = 1
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Currency"));
    }

    [Fact]
    public void SubscriptionTier_WithCurrencyTooLong_FailsValidation()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = "Premium",
            Description = "Test description",
            Price = 9.99m,
            Currency = "VERYLONGCURRENCY", // Invalid - max length 10
            BillingCycleMonths = 1
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Currency"));
    }

    [Fact]
    public void SubscriptionTier_WithFeaturesTooLong_FailsValidation()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = "Premium",
            Description = "Test description",
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1,
            Features = new string('A', 2001) // Invalid - max length 2000
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Features"));
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void SubscriptionTier_NewInstance_HasCorrectDefaults()
    {
        // Arrange & Act
        var tier = new SubscriptionTier();

        // Assert
        tier.Currency.Should().Be("USD");
        tier.BillingCycleMonths.Should().Be(1);
        tier.IsActive.Should().BeTrue();
        tier.IsDefault.Should().BeFalse();
        tier.SortOrder.Should().Be(0);
        tier.ShowAdvertisements.Should().BeTrue();
        tier.HasVerifiedBadge.Should().BeFalse();
        tier.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tier.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tier.Users.Should().NotBeNull();
        tier.Users.Should().BeEmpty();
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public void SubscriptionTier_WithZeroPrice_IsValid()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = "Free",
            Description = "Free tier",
            Price = 0m, // Valid - free tier
            Currency = "USD",
            BillingCycleMonths = 1
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void SubscriptionTier_WithNegativePrice_IsValid()
    {
        // Arrange - Some business models might have negative prices (credits/refunds)
        var tier = new SubscriptionTier
        {
            Name = "Credit",
            Description = "Credit tier",
            Price = -5.00m,
            Currency = "USD",
            BillingCycleMonths = 1
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void SubscriptionTier_WithLargePrice_IsValid()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = "Enterprise",
            Description = "Enterprise tier",
            Price = 99999999.99m, // Maximum allowed by precision(10,2)
            Currency = "USD",
            BillingCycleMonths = 12
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void SubscriptionTier_WithDifferentBillingCycles_IsValid()
    {
        // Arrange & Act & Assert
        var monthlyCycles = new[] { 1, 3, 6, 12, 24, 36 };
        
        foreach (var cycle in monthlyCycles)
        {
            var tier = new SubscriptionTier
            {
                Name = $"Tier-{cycle}",
                Description = $"Tier with {cycle} month billing",
                Price = 9.99m,
                Currency = "USD",
                BillingCycleMonths = cycle
            };

            var validationResults = ValidateModel(tier);
            validationResults.Should().BeEmpty($"Billing cycle of {cycle} months should be valid");
        }
    }

    [Fact]
    public void SubscriptionTier_WithDifferentCurrencies_IsValid()
    {
        // Arrange & Act & Assert
        var currencies = new[] { "USD", "EUR", "GBP", "CAD", "AUD", "JPY" };
        
        foreach (var currency in currencies)
        {
            var tier = new SubscriptionTier
            {
                Name = $"Tier-{currency}",
                Description = $"Tier in {currency}",
                Price = 9.99m,
                Currency = currency,
                BillingCycleMonths = 1
            };

            var validationResults = ValidateModel(tier);
            validationResults.Should().BeEmpty($"Currency {currency} should be valid");
        }
    }

    [Fact]
    public void SubscriptionTier_WithValidJsonFeatures_IsValid()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Name = "Premium",
            Description = "Premium tier",
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1,
            Features = "{\"noAds\": true, \"verifiedBadge\": true, \"maxPosts\": 1000}"
        };

        // Act
        var validationResults = ValidateModel(tier);

        // Assert
        validationResults.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    #endregion
}
