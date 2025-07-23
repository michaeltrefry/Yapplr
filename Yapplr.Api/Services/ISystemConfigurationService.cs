using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public interface ISystemConfigurationService
{
    // Configuration Management
    Task<IEnumerable<SystemConfigurationDto>> GetAllConfigurationsAsync(string? category = null);
    Task<SystemConfigurationDto?> GetConfigurationAsync(string key);
    Task<string?> GetConfigurationValueAsync(string key);
    Task<bool> GetConfigurationBoolAsync(string key, bool defaultValue = false);
    Task<int> GetConfigurationIntAsync(string key, int defaultValue = 0);
    Task<SystemConfigurationDto> CreateConfigurationAsync(CreateSystemConfigurationDto createDto);
    Task<SystemConfigurationDto?> UpdateConfigurationAsync(string key, UpdateSystemConfigurationDto updateDto);
    Task<bool> SetConfigurationValueAsync(string key, string value);
    Task<bool> DeleteConfigurationAsync(string key);
    Task<bool> BulkUpdateConfigurationsAsync(SystemConfigurationBulkUpdateDto bulkUpdateDto);
    
    // Subscription System Configuration
    Task<bool> IsSubscriptionSystemEnabledAsync();
    Task<bool> SetSubscriptionSystemEnabledAsync(bool enabled);
    
    // Configuration Initialization
    Task InitializeDefaultConfigurationsAsync();
}
