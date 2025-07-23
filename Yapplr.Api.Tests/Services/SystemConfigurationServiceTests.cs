using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Services;

namespace Yapplr.Api.Tests.Services;

public class SystemConfigurationServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<ILogger<SystemConfigurationService>> _mockLogger;
    private readonly SystemConfigurationService _service;

    public SystemConfigurationServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockLogger = new Mock<ILogger<SystemConfigurationService>>();
        _service = new SystemConfigurationService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_ReturnsAllConfigurations()
    {
        // Arrange
        var config1 = new SystemConfiguration
        {
            Key = "test_key_1",
            Value = "test_value_1",
            Description = "Test description 1",
            Category = "Test"
        };
        var config2 = new SystemConfiguration
        {
            Key = "test_key_2",
            Value = "test_value_2",
            Description = "Test description 2",
            Category = "Other"
        };

        _context.SystemConfigurations.AddRange(config1, config2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllConfigurationsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, c => c.Key == "test_key_1");
        Assert.Contains(result, c => c.Key == "test_key_2");
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_WithCategory_ReturnsFilteredConfigurations()
    {
        // Arrange
        var config1 = new SystemConfiguration
        {
            Key = "test_key_1",
            Value = "test_value_1",
            Category = "Test"
        };
        var config2 = new SystemConfiguration
        {
            Key = "test_key_2",
            Value = "test_value_2",
            Category = "Other"
        };

        _context.SystemConfigurations.AddRange(config1, config2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllConfigurationsAsync("Test");

        // Assert
        Assert.Single(result);
        Assert.Equal("test_key_1", result.First().Key);
    }

    [Fact]
    public async Task GetConfigurationAsync_ExistingKey_ReturnsConfiguration()
    {
        // Arrange
        var config = new SystemConfiguration
        {
            Key = "test_key",
            Value = "test_value",
            Description = "Test description"
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetConfigurationAsync("test_key");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_key", result.Key);
        Assert.Equal("test_value", result.Value);
    }

    [Fact]
    public async Task GetConfigurationAsync_NonExistingKey_ReturnsNull()
    {
        // Act
        var result = await _service.GetConfigurationAsync("non_existing_key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetConfigurationValueAsync_ExistingKey_ReturnsValue()
    {
        // Arrange
        var config = new SystemConfiguration
        {
            Key = "test_key",
            Value = "test_value"
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetConfigurationValueAsync("test_key");

        // Assert
        Assert.Equal("test_value", result);
    }

    [Fact]
    public async Task GetConfigurationBoolAsync_TrueValue_ReturnsTrue()
    {
        // Arrange
        var config = new SystemConfiguration
        {
            Key = "test_bool",
            Value = "true"
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetConfigurationBoolAsync("test_bool");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetConfigurationBoolAsync_FalseValue_ReturnsFalse()
    {
        // Arrange
        var config = new SystemConfiguration
        {
            Key = "test_bool",
            Value = "false"
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetConfigurationBoolAsync("test_bool");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetConfigurationBoolAsync_NonExistingKey_ReturnsDefault()
    {
        // Act
        var result = await _service.GetConfigurationBoolAsync("non_existing", true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CreateConfigurationAsync_ValidDto_CreatesConfiguration()
    {
        // Arrange
        var createDto = new CreateSystemConfigurationDto
        {
            Key = "new_key",
            Value = "new_value",
            Description = "New description",
            Category = "Test"
        };

        // Act
        var result = await _service.CreateConfigurationAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new_key", result.Key);
        Assert.Equal("new_value", result.Value);

        var dbConfig = await _context.SystemConfigurations.FirstOrDefaultAsync(c => c.Key == "new_key");
        Assert.NotNull(dbConfig);
    }

    [Fact]
    public async Task CreateConfigurationAsync_DuplicateKey_ThrowsException()
    {
        // Arrange
        var existingConfig = new SystemConfiguration
        {
            Key = "existing_key",
            Value = "existing_value"
        };

        _context.SystemConfigurations.Add(existingConfig);
        await _context.SaveChangesAsync();

        var createDto = new CreateSystemConfigurationDto
        {
            Key = "existing_key",
            Value = "new_value"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CreateConfigurationAsync(createDto));
    }

    [Fact]
    public async Task UpdateConfigurationAsync_ExistingKey_UpdatesConfiguration()
    {
        // Arrange
        var config = new SystemConfiguration
        {
            Key = "test_key",
            Value = "old_value",
            Description = "Old description"
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateSystemConfigurationDto
        {
            Value = "new_value",
            Description = "New description",
            Category = "Updated"
        };

        // Act
        var result = await _service.UpdateConfigurationAsync("test_key", updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new_value", result.Value);
        Assert.Equal("New description", result.Description);
        Assert.Equal("Updated", result.Category);
    }

    [Fact]
    public async Task SetConfigurationValueAsync_ExistingKey_UpdatesValue()
    {
        // Arrange
        var config = new SystemConfiguration
        {
            Key = "test_key",
            Value = "old_value"
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SetConfigurationValueAsync("test_key", "new_value");

        // Assert
        Assert.True(result);

        var updatedConfig = await _context.SystemConfigurations.FirstOrDefaultAsync(c => c.Key == "test_key");
        Assert.Equal("new_value", updatedConfig.Value);
    }

    [Fact]
    public async Task DeleteConfigurationAsync_ExistingKey_DeletesConfiguration()
    {
        // Arrange
        var config = new SystemConfiguration
        {
            Key = "test_key",
            Value = "test_value"
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteConfigurationAsync("test_key");

        // Assert
        Assert.True(result);

        var deletedConfig = await _context.SystemConfigurations.FirstOrDefaultAsync(c => c.Key == "test_key");
        Assert.Null(deletedConfig);
    }

    [Fact]
    public async Task IsSubscriptionSystemEnabledAsync_EnabledConfiguration_ReturnsTrue()
    {
        // Arrange
        var config = new SystemConfiguration
        {
            Key = SystemConfigurationService.SUBSCRIPTION_SYSTEM_ENABLED,
            Value = "true"
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsSubscriptionSystemEnabledAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsSubscriptionSystemEnabledAsync_DisabledConfiguration_ReturnsFalse()
    {
        // Arrange
        var config = new SystemConfiguration
        {
            Key = SystemConfigurationService.SUBSCRIPTION_SYSTEM_ENABLED,
            Value = "false"
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsSubscriptionSystemEnabledAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsSubscriptionSystemEnabledAsync_NoConfiguration_ReturnsDefaultTrue()
    {
        // Act
        var result = await _service.IsSubscriptionSystemEnabledAsync();

        // Assert
        Assert.True(result); // Default should be true
    }

    [Fact]
    public async Task SetSubscriptionSystemEnabledAsync_CreatesConfigurationIfNotExists()
    {
        // Act
        var result = await _service.SetSubscriptionSystemEnabledAsync(false);

        // Assert
        Assert.True(result);

        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == SystemConfigurationService.SUBSCRIPTION_SYSTEM_ENABLED);
        Assert.NotNull(config);
        Assert.Equal("false", config.Value);
    }

    [Fact]
    public async Task SetSubscriptionSystemEnabledAsync_UpdatesExistingConfiguration()
    {
        // Arrange
        var config = new SystemConfiguration
        {
            Key = SystemConfigurationService.SUBSCRIPTION_SYSTEM_ENABLED,
            Value = "true"
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SetSubscriptionSystemEnabledAsync(false);

        // Assert
        Assert.True(result);

        var updatedConfig = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == SystemConfigurationService.SUBSCRIPTION_SYSTEM_ENABLED);
        Assert.Equal("false", updatedConfig.Value);
    }

    [Fact]
    public async Task InitializeDefaultConfigurationsAsync_CreatesDefaultConfigurations()
    {
        // Act
        await _service.InitializeDefaultConfigurationsAsync();

        // Assert
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == SystemConfigurationService.SUBSCRIPTION_SYSTEM_ENABLED);
        Assert.NotNull(config);
        Assert.Equal("true", config.Value);
        Assert.Equal("Subscriptions", config.Category);
    }

    [Fact]
    public async Task InitializeDefaultConfigurationsAsync_DoesNotDuplicateExistingConfigurations()
    {
        // Arrange
        var existingConfig = new SystemConfiguration
        {
            Key = SystemConfigurationService.SUBSCRIPTION_SYSTEM_ENABLED,
            Value = "false"
        };

        _context.SystemConfigurations.Add(existingConfig);
        await _context.SaveChangesAsync();

        // Act
        await _service.InitializeDefaultConfigurationsAsync();

        // Assert
        var configs = await _context.SystemConfigurations
            .Where(c => c.Key == SystemConfigurationService.SUBSCRIPTION_SYSTEM_ENABLED)
            .ToListAsync();
        Assert.Single(configs);
        Assert.Equal("false", configs.First().Value); // Should not overwrite existing
    }
}
