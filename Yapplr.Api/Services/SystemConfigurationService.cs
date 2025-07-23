using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class SystemConfigurationService : ISystemConfigurationService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<SystemConfigurationService> _logger;

    // Configuration keys
    public const string SUBSCRIPTION_SYSTEM_ENABLED = "subscription_system_enabled";

    public SystemConfigurationService(YapplrDbContext context, ILogger<SystemConfigurationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<SystemConfigurationDto>> GetAllConfigurationsAsync(string? category = null)
    {
        var query = _context.SystemConfigurations.AsQueryable();
        
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(c => c.Category == category);
        }
        
        var configurations = await query
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Key)
            .ToListAsync();
            
        return configurations.Select(MapToDto);
    }

    public async Task<SystemConfigurationDto?> GetConfigurationAsync(string key)
    {
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);
            
        return config == null ? null : MapToDto(config);
    }

    public async Task<string?> GetConfigurationValueAsync(string key)
    {
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);
            
        return config?.Value;
    }

    public async Task<bool> GetConfigurationBoolAsync(string key, bool defaultValue = false)
    {
        var value = await GetConfigurationValueAsync(key);
        
        if (string.IsNullOrEmpty(value))
            return defaultValue;
            
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<int> GetConfigurationIntAsync(string key, int defaultValue = 0)
    {
        var value = await GetConfigurationValueAsync(key);
        
        if (string.IsNullOrEmpty(value))
            return defaultValue;
            
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<SystemConfigurationDto> CreateConfigurationAsync(CreateSystemConfigurationDto createDto)
    {
        // Check if configuration already exists
        var existing = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == createDto.Key);
            
        if (existing != null)
        {
            throw new InvalidOperationException($"Configuration with key '{createDto.Key}' already exists");
        }

        var config = new SystemConfiguration
        {
            Key = createDto.Key,
            Value = createDto.Value,
            Description = createDto.Description,
            Category = createDto.Category,
            IsVisible = createDto.IsVisible,
            IsEditable = createDto.IsEditable,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SystemConfigurations.Add(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created system configuration: {Key} = {Value}", createDto.Key, createDto.Value);
        
        return MapToDto(config);
    }

    public async Task<SystemConfigurationDto?> UpdateConfigurationAsync(string key, UpdateSystemConfigurationDto updateDto)
    {
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);
            
        if (config == null)
            return null;

        config.Value = updateDto.Value;
        config.Description = updateDto.Description;
        config.Category = updateDto.Category;
        config.IsVisible = updateDto.IsVisible;
        config.IsEditable = updateDto.IsEditable;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated system configuration: {Key} = {Value}", key, updateDto.Value);
        
        return MapToDto(config);
    }

    public async Task<bool> SetConfigurationValueAsync(string key, string value)
    {
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);
            
        if (config == null)
            return false;

        config.Value = value;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated system configuration value: {Key} = {Value}", key, value);
        
        return true;
    }

    public async Task<bool> DeleteConfigurationAsync(string key)
    {
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);
            
        if (config == null)
            return false;

        _context.SystemConfigurations.Remove(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted system configuration: {Key}", key);
        
        return true;
    }

    public async Task<bool> BulkUpdateConfigurationsAsync(SystemConfigurationBulkUpdateDto bulkUpdateDto)
    {
        var keys = bulkUpdateDto.Configurations.Keys.ToList();
        var configs = await _context.SystemConfigurations
            .Where(c => keys.Contains(c.Key))
            .ToListAsync();

        foreach (var config in configs)
        {
            if (bulkUpdateDto.Configurations.TryGetValue(config.Key, out var newValue))
            {
                config.Value = newValue;
                config.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk updated {Count} system configurations", configs.Count);
        
        return true;
    }

    public async Task<bool> IsSubscriptionSystemEnabledAsync()
    {
        return await GetConfigurationBoolAsync(SUBSCRIPTION_SYSTEM_ENABLED, true); // Default to enabled
    }

    public async Task<bool> SetSubscriptionSystemEnabledAsync(bool enabled)
    {
        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == SUBSCRIPTION_SYSTEM_ENABLED);

        if (config == null)
        {
            // Create the configuration if it doesn't exist
            config = new SystemConfiguration
            {
                Key = SUBSCRIPTION_SYSTEM_ENABLED,
                Value = enabled.ToString().ToLowerInvariant(),
                Description = "Enable or disable the subscription system",
                Category = "Subscriptions",
                IsVisible = false, // Hidden from generic config list since it has dedicated toggle
                IsEditable = false, // Use dedicated toggle instead of text editing
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.SystemConfigurations.Add(config);
        }
        else
        {
            config.Value = enabled.ToString().ToLowerInvariant();
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription system {Status}", enabled ? "enabled" : "disabled");
        
        return true;
    }

    public async Task InitializeDefaultConfigurationsAsync()
    {
        var defaultConfigs = new[]
        {
            new SystemConfiguration
            {
                Key = SUBSCRIPTION_SYSTEM_ENABLED,
                Value = "true",
                Description = "Enable or disable the subscription system",
                Category = "Subscriptions",
                IsVisible = false, // Hidden from generic config list since it has dedicated toggle
                IsEditable = false // Use dedicated toggle instead of text editing
            }
        };

        foreach (var defaultConfig in defaultConfigs)
        {
            var existing = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == defaultConfig.Key);

            if (existing == null)
            {
                defaultConfig.CreatedAt = DateTime.UtcNow;
                defaultConfig.UpdatedAt = DateTime.UtcNow;
                _context.SystemConfigurations.Add(defaultConfig);
            }
            else
            {
                // Update existing subscription system config to be hidden from generic list
                if (defaultConfig.Key == SUBSCRIPTION_SYSTEM_ENABLED)
                {
                    existing.IsVisible = false;
                    existing.IsEditable = false;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private static SystemConfigurationDto MapToDto(SystemConfiguration config)
    {
        return new SystemConfigurationDto
        {
            Id = config.Id,
            Key = config.Key,
            Value = config.Value,
            Description = config.Description,
            Category = config.Category,
            IsVisible = config.IsVisible,
            IsEditable = config.IsEditable,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }
}
