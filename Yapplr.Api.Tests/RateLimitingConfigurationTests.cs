using Microsoft.Extensions.Options;
using Xunit;
using Yapplr.Api.Configuration;

namespace Yapplr.Api.Tests;

public class RateLimitingConfigurationTests
{
    [Fact]
    public void RateLimitingConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new RateLimitingConfiguration();

        // Assert
        Assert.True(config.Enabled);
        Assert.True(config.TrustBasedEnabled);
        Assert.True(config.BurstProtectionEnabled);
        Assert.True(config.AutoBlockingEnabled);
        Assert.Equal(15, config.AutoBlockViolationThreshold);
        Assert.Equal(2, config.AutoBlockDurationHours);
        Assert.False(config.ApplyToAdmins);
        Assert.False(config.ApplyToModerators);
        Assert.Equal(1.0f, config.FallbackMultiplier);
    }

    [Fact]
    public void RateLimitingConfiguration_CanBeConfigured()
    {
        // Arrange
        var config = new RateLimitingConfiguration
        {
            Enabled = false,
            TrustBasedEnabled = false,
            BurstProtectionEnabled = false,
            AutoBlockingEnabled = false,
            AutoBlockViolationThreshold = 10,
            AutoBlockDurationHours = 4,
            ApplyToAdmins = true,
            ApplyToModerators = true,
            FallbackMultiplier = 0.5f
        };

        // Act & Assert
        Assert.False(config.Enabled);
        Assert.False(config.TrustBasedEnabled);
        Assert.False(config.BurstProtectionEnabled);
        Assert.False(config.AutoBlockingEnabled);
        Assert.Equal(10, config.AutoBlockViolationThreshold);
        Assert.Equal(4, config.AutoBlockDurationHours);
        Assert.True(config.ApplyToAdmins);
        Assert.True(config.ApplyToModerators);
        Assert.Equal(0.5f, config.FallbackMultiplier);
    }

    [Fact]
    public void RateLimitingConfiguration_SectionName_IsCorrect()
    {
        // Act & Assert
        Assert.Equal("RateLimiting", RateLimitingConfiguration.SectionName);
    }

    [Theory]
    [InlineData(0.1f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    [InlineData(5.0f)]
    public void RateLimitingConfiguration_FallbackMultiplier_AcceptsValidValues(float multiplier)
    {
        // Arrange
        var config = new RateLimitingConfiguration();

        // Act
        config.FallbackMultiplier = multiplier;

        // Assert
        Assert.Equal(multiplier, config.FallbackMultiplier);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void RateLimitingConfiguration_AutoBlockViolationThreshold_AcceptsValidValues(int threshold)
    {
        // Arrange
        var config = new RateLimitingConfiguration();

        // Act
        config.AutoBlockViolationThreshold = threshold;

        // Assert
        Assert.Equal(threshold, config.AutoBlockViolationThreshold);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(24)]
    public void RateLimitingConfiguration_AutoBlockDurationHours_AcceptsValidValues(int hours)
    {
        // Arrange
        var config = new RateLimitingConfiguration();

        // Act
        config.AutoBlockDurationHours = hours;

        // Assert
        Assert.Equal(hours, config.AutoBlockDurationHours);
    }
}
