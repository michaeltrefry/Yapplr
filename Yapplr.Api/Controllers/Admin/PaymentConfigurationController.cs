using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Models;
using Yapplr.Api.Services.Payment;

namespace Yapplr.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/payment-configuration")]
[Authorize(Policy = "Admin")]
public class PaymentConfigurationController : ControllerBase
{
    private readonly YapplrDbContext _context;
    private readonly IDynamicPaymentConfigurationService _configService;
    private readonly ILogger<PaymentConfigurationController> _logger;

    public PaymentConfigurationController(
        YapplrDbContext context,
        IDynamicPaymentConfigurationService configService,
        ILogger<PaymentConfigurationController> logger)
    {
        _context = context;
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Get payment configuration summary
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<PaymentConfigurationSummaryDto>> GetConfigurationSummary()
    {
        try
        {
            var globalConfig = await _context.PaymentGlobalConfigurations.FirstOrDefaultAsync();
            var providers = await _context.PaymentProviderConfigurations
                .Include(p => p.Settings)
                .OrderBy(p => p.Priority)
                .ToListAsync();

            var summary = new PaymentConfigurationSummaryDto
            {
                GlobalConfiguration = globalConfig != null ? MapToGlobalDto(globalConfig) : new PaymentGlobalConfigurationDto(),
                Providers = providers.Select(MapToProviderDto).ToList(),
                ActiveProvider = await _configService.GetDefaultProviderAsync(),
                ProviderPriority = await _configService.GetProviderPriorityAsync(),
                TotalProviders = providers.Count,
                EnabledProviders = providers.Count(p => p.IsEnabled),
                LastUpdated = providers.Any() ? providers.Max(p => p.UpdatedAt) : DateTime.UtcNow
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment configuration summary");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all payment provider configurations
    /// </summary>
    [HttpGet("providers")]
    public async Task<ActionResult<List<PaymentProviderConfigurationDto>>> GetProviderConfigurations()
    {
        try
        {
            var providers = await _context.PaymentProviderConfigurations
                .Include(p => p.Settings)
                .OrderBy(p => p.Priority)
                .ToListAsync();

            var dtos = providers.Select(MapToProviderDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment provider configurations");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get specific payment provider configuration
    /// </summary>
    [HttpGet("providers/{providerName}")]
    public async Task<ActionResult<PaymentProviderConfigurationDto>> GetProviderConfiguration(string providerName)
    {
        try
        {
            var provider = await _context.PaymentProviderConfigurations
                .Include(p => p.Settings)
                .FirstOrDefaultAsync(p => p.ProviderName == providerName);

            if (provider == null)
            {
                return NotFound($"Provider '{providerName}' not found");
            }

            return Ok(MapToProviderDto(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment provider configuration for {ProviderName}", providerName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create or update payment provider configuration
    /// </summary>
    [HttpPut("providers/{providerName}")]
    public async Task<ActionResult<PaymentProviderConfigurationDto>> CreateOrUpdateProviderConfiguration(
        string providerName, 
        CreateUpdatePaymentProviderConfigurationDto dto)
    {
        try
        {
            var provider = await _context.PaymentProviderConfigurations
                .Include(p => p.Settings)
                .FirstOrDefaultAsync(p => p.ProviderName == providerName);

            if (provider == null)
            {
                // Create new provider configuration
                provider = new PaymentProviderConfiguration
                {
                    ProviderName = providerName,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PaymentProviderConfigurations.Add(provider);
            }

            // Update provider properties
            provider.IsEnabled = dto.IsEnabled;
            provider.Environment = dto.Environment;
            provider.Priority = dto.Priority;
            provider.TimeoutSeconds = dto.TimeoutSeconds;
            provider.MaxRetries = dto.MaxRetries;
            provider.SupportedCurrencies = JsonSerializer.Serialize(dto.SupportedCurrencies);
            provider.UpdatedAt = DateTime.UtcNow;

            // Update settings
            await UpdateProviderSettingsAsync(provider, dto.Settings);

            await _context.SaveChangesAsync();

            // Refresh configuration cache
            await _configService.RefreshCacheAsync();

            _logger.LogInformation("Updated payment provider configuration: {ProviderName}", providerName);

            return Ok(MapToProviderDto(provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment provider configuration for {ProviderName}", providerName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete payment provider configuration
    /// </summary>
    [HttpDelete("providers/{providerName}")]
    public async Task<ActionResult> DeleteProviderConfiguration(string providerName)
    {
        try
        {
            var provider = await _context.PaymentProviderConfigurations
                .Include(p => p.Settings)
                .FirstOrDefaultAsync(p => p.ProviderName == providerName);

            if (provider == null)
            {
                return NotFound($"Provider '{providerName}' not found");
            }

            _context.PaymentProviderConfigurations.Remove(provider);
            await _context.SaveChangesAsync();

            // Refresh configuration cache
            await _configService.RefreshCacheAsync();

            _logger.LogInformation("Deleted payment provider configuration: {ProviderName}", providerName);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment provider configuration for {ProviderName}", providerName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get global payment configuration
    /// </summary>
    [HttpGet("global")]
    public async Task<ActionResult<PaymentGlobalConfigurationDto>> GetGlobalConfiguration()
    {
        try
        {
            var globalConfig = await _context.PaymentGlobalConfigurations.FirstOrDefaultAsync();
            
            if (globalConfig == null)
            {
                // Return default configuration
                return Ok(new PaymentGlobalConfigurationDto
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
                    VerifyWebhookSignatures = true
                });
            }

            return Ok(MapToGlobalDto(globalConfig));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global payment configuration");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update global payment configuration
    /// </summary>
    [HttpPut("global")]
    public async Task<ActionResult<PaymentGlobalConfigurationDto>> UpdateGlobalConfiguration(
        UpdatePaymentGlobalConfigurationDto dto)
    {
        try
        {
            var globalConfig = await _context.PaymentGlobalConfigurations.FirstOrDefaultAsync();
            
            if (globalConfig == null)
            {
                globalConfig = new PaymentGlobalConfiguration
                {
                    CreatedAt = DateTime.UtcNow
                };
                _context.PaymentGlobalConfigurations.Add(globalConfig);
            }

            // Update properties
            globalConfig.DefaultProvider = dto.DefaultProvider;
            globalConfig.DefaultCurrency = dto.DefaultCurrency;
            globalConfig.GracePeriodDays = dto.GracePeriodDays;
            globalConfig.MaxPaymentRetries = dto.MaxPaymentRetries;
            globalConfig.RetryIntervalDays = dto.RetryIntervalDays;
            globalConfig.EnableTrialPeriods = dto.EnableTrialPeriods;
            globalConfig.DefaultTrialDays = dto.DefaultTrialDays;
            globalConfig.EnableProration = dto.EnableProration;
            globalConfig.WebhookTimeoutSeconds = dto.WebhookTimeoutSeconds;
            globalConfig.VerifyWebhookSignatures = dto.VerifyWebhookSignatures;
            globalConfig.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Refresh configuration cache
            await _configService.RefreshCacheAsync();

            _logger.LogInformation("Updated global payment configuration");

            return Ok(MapToGlobalDto(globalConfig));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global payment configuration");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Test payment provider connectivity
    /// </summary>
    [HttpPost("providers/{providerName}/test")]
    public async Task<ActionResult<PaymentProviderTestResultDto>> TestProviderConfiguration(string providerName)
    {
        try
        {
            var provider = await _context.PaymentProviderConfigurations
                .Include(p => p.Settings)
                .FirstOrDefaultAsync(p => p.ProviderName == providerName);

            if (provider == null)
            {
                return NotFound($"Provider '{providerName}' not found");
            }

            var result = new PaymentProviderTestResultDto
            {
                ProviderName = providerName,
                TestedAt = DateTime.UtcNow
            };

            // Basic validation
            if (!provider.IsEnabled)
            {
                result.IsSuccessful = false;
                result.Message = "Provider is disabled";
                return Ok(result);
            }

            // Check required settings based on provider type
            var missingSettings = GetMissingRequiredSettings(provider);
            if (missingSettings.Any())
            {
                result.IsSuccessful = false;
                result.Message = $"Missing required settings: {string.Join(", ", missingSettings)}";
                result.Details["missing_settings"] = missingSettings;
                return Ok(result);
            }

            // Perform actual connectivity test
            var connectivityResult = await TestProviderConnectivityAsync(provider);
            result.IsSuccessful = connectivityResult.IsSuccessful;
            result.Message = connectivityResult.Message;
            result.Details = connectivityResult.Details;

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing payment provider configuration for {ProviderName}", providerName);
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task UpdateProviderSettingsAsync(PaymentProviderConfiguration provider, List<CreateUpdatePaymentProviderSettingDto> settingDtos)
    {
        // Remove existing settings that are not in the update
        var existingKeys = provider.Settings.Select(s => s.Key).ToList();
        var newKeys = settingDtos.Select(s => s.Key).ToList();
        var keysToRemove = existingKeys.Except(newKeys).ToList();

        foreach (var keyToRemove in keysToRemove)
        {
            var settingToRemove = provider.Settings.First(s => s.Key == keyToRemove);
            provider.Settings.Remove(settingToRemove);
        }

        // Update or create settings
        foreach (var settingDto in settingDtos)
        {
            var existingSetting = provider.Settings.FirstOrDefault(s => s.Key == settingDto.Key);

            if (existingSetting != null)
            {
                // Update existing setting
                existingSetting.Value = settingDto.Value;
                existingSetting.IsSensitive = settingDto.IsSensitive;
                existingSetting.Description = settingDto.Description;
                existingSetting.Category = settingDto.Category;
                existingSetting.IsRequired = settingDto.IsRequired;
                existingSetting.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new setting
                var newSetting = new PaymentProviderSetting
                {
                    PaymentProviderConfigurationId = provider.Id,
                    Key = settingDto.Key,
                    Value = settingDto.Value,
                    IsSensitive = settingDto.IsSensitive,
                    Description = settingDto.Description,
                    Category = settingDto.Category,
                    IsRequired = settingDto.IsRequired,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                provider.Settings.Add(newSetting);
            }
        }
    }

    private async Task<PaymentProviderTestResultDto> TestProviderConnectivityAsync(PaymentProviderConfiguration provider)
    {
        var result = new PaymentProviderTestResultDto
        {
            ProviderName = provider.ProviderName,
            TestedAt = DateTime.UtcNow
        };

        try
        {
            switch (provider.ProviderName.ToLower())
            {
                case "paypal":
                    return await TestPayPalConnectivityAsync(provider, result);
                case "stripe":
                    return await TestStripeConnectivityAsync(provider, result);
                default:
                    result.IsSuccessful = false;
                    result.Message = "Unsupported provider for connectivity testing";
                    return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connectivity for {ProviderName}", provider.ProviderName);
            result.IsSuccessful = false;
            result.Message = $"Connectivity test failed: {ex.Message}";
            return result;
        }
    }

    private async Task<PaymentProviderTestResultDto> TestPayPalConnectivityAsync(PaymentProviderConfiguration provider, PaymentProviderTestResultDto result)
    {
        var clientId = provider.Settings.FirstOrDefault(s => s.Key == "ClientId")?.Value;
        var clientSecret = provider.Settings.FirstOrDefault(s => s.Key == "ClientSecret")?.Value;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            result.IsSuccessful = false;
            result.Message = "Missing PayPal credentials";
            return result;
        }

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds);

            var baseUrl = provider.Environment == "live"
                ? "https://api-m.paypal.com"
                : "https://api-m.sandbox.paypal.com";

            httpClient.BaseAddress = new Uri(baseUrl);

            // Test authentication
            var authString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            var content = new StringContent("grant_type=client_credentials", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authString}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await httpClient.PostAsync("/v1/oauth2/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                result.IsSuccessful = true;
                result.Message = "PayPal connectivity test successful";
                result.Details["environment"] = provider.Environment;
                result.Details["base_url"] = baseUrl;
                result.Details["response_status"] = (int)response.StatusCode;
            }
            else
            {
                result.IsSuccessful = false;
                result.Message = $"PayPal authentication failed: {response.StatusCode}";
                result.Details["error_response"] = responseContent;
                result.Details["response_status"] = (int)response.StatusCode;
            }
        }
        catch (HttpRequestException ex)
        {
            result.IsSuccessful = false;
            result.Message = $"PayPal connectivity error: {ex.Message}";
            result.Details["error_type"] = "network_error";
        }
        catch (TaskCanceledException)
        {
            result.IsSuccessful = false;
            result.Message = "PayPal request timed out";
            result.Details["error_type"] = "timeout";
        }

        return result;
    }

    private async Task<PaymentProviderTestResultDto> TestStripeConnectivityAsync(PaymentProviderConfiguration provider, PaymentProviderTestResultDto result)
    {
        var secretKey = provider.Settings.FirstOrDefault(s => s.Key == "SecretKey")?.Value;

        if (string.IsNullOrEmpty(secretKey))
        {
            result.IsSuccessful = false;
            result.Message = "Missing Stripe secret key";
            return result;
        }

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds);
            httpClient.BaseAddress = new Uri("https://api.stripe.com");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");
            httpClient.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");

            // Test by retrieving account information
            var response = await httpClient.GetAsync("/v1/account");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                result.IsSuccessful = true;
                result.Message = "Stripe connectivity test successful";
                result.Details["environment"] = provider.Environment;
                result.Details["response_status"] = (int)response.StatusCode;

                // Try to parse account info for additional details
                try
                {
                    var accountInfo = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (accountInfo.TryGetProperty("country", out var country))
                    {
                        result.Details["account_country"] = country.GetString();
                    }
                    if (accountInfo.TryGetProperty("default_currency", out var currency))
                    {
                        result.Details["default_currency"] = currency.GetString();
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }
            else
            {
                result.IsSuccessful = false;
                result.Message = $"Stripe authentication failed: {response.StatusCode}";
                result.Details["error_response"] = responseContent;
                result.Details["response_status"] = (int)response.StatusCode;
            }
        }
        catch (HttpRequestException ex)
        {
            result.IsSuccessful = false;
            result.Message = $"Stripe connectivity error: {ex.Message}";
            result.Details["error_type"] = "network_error";
        }
        catch (TaskCanceledException)
        {
            result.IsSuccessful = false;
            result.Message = "Stripe request timed out";
            result.Details["error_type"] = "timeout";
        }

        return result;
    }

    private List<string> GetMissingRequiredSettings(PaymentProviderConfiguration provider)
    {
        var requiredSettings = provider.ProviderName.ToLower() switch
        {
            "paypal" => new[] { "ClientId", "ClientSecret" },
            "stripe" => new[] { "SecretKey" },
            _ => Array.Empty<string>()
        };

        var existingKeys = provider.Settings.Where(s => !string.IsNullOrEmpty(s.Value)).Select(s => s.Key).ToList();
        return requiredSettings.Except(existingKeys).ToList();
    }

    private PaymentProviderConfigurationDto MapToProviderDto(PaymentProviderConfiguration provider)
    {
        List<string> supportedCurrencies;
        try
        {
            supportedCurrencies = JsonSerializer.Deserialize<List<string>>(provider.SupportedCurrencies) ?? new List<string>();
        }
        catch
        {
            supportedCurrencies = new List<string> { "USD" };
        }

        return new PaymentProviderConfigurationDto
        {
            Id = provider.Id,
            ProviderName = provider.ProviderName,
            IsEnabled = provider.IsEnabled,
            Environment = provider.Environment,
            Priority = provider.Priority,
            TimeoutSeconds = provider.TimeoutSeconds,
            MaxRetries = provider.MaxRetries,
            SupportedCurrencies = supportedCurrencies,
            Settings = provider.Settings.Select(MapToSettingDto).ToList(),
            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt
        };
    }

    private PaymentProviderSettingDto MapToSettingDto(PaymentProviderSetting setting)
    {
        return new PaymentProviderSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.IsSensitive ? "***" : setting.Value, // Mask sensitive values
            IsSensitive = setting.IsSensitive,
            Description = setting.Description,
            Category = setting.Category,
            IsRequired = setting.IsRequired,
            CreatedAt = setting.CreatedAt,
            UpdatedAt = setting.UpdatedAt
        };
    }

    private PaymentGlobalConfigurationDto MapToGlobalDto(PaymentGlobalConfiguration config)
    {
        return new PaymentGlobalConfigurationDto
        {
            Id = config.Id,
            DefaultProvider = config.DefaultProvider,
            DefaultCurrency = config.DefaultCurrency,
            GracePeriodDays = config.GracePeriodDays,
            MaxPaymentRetries = config.MaxPaymentRetries,
            RetryIntervalDays = config.RetryIntervalDays,
            EnableTrialPeriods = config.EnableTrialPeriods,
            DefaultTrialDays = config.DefaultTrialDays,
            EnableProration = config.EnableProration,
            WebhookTimeoutSeconds = config.WebhookTimeoutSeconds,
            VerifyWebhookSignatures = config.VerifyWebhookSignatures,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }
}
