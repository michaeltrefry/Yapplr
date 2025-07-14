using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Yapplr.Api.Configuration;

namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Manages notification providers with intelligent fallback and health monitoring.
/// Consolidates provider management logic from CompositeNotificationService.
/// </summary>
public class NotificationProviderManager : INotificationProviderManager
{
    private readonly ILogger<NotificationProviderManager> _logger;
    private readonly IEnumerable<IRealtimeNotificationProvider> _providers;
    private readonly IOptionsMonitor<NotificationProvidersConfiguration> _config;
    
    // Provider management
    private readonly List<IRealtimeNotificationProvider> _orderedProviders;
    private IRealtimeNotificationProvider? _activeProvider;
    private DateTime _lastProviderCheck = DateTime.MinValue;
    private readonly TimeSpan _providerCheckInterval = TimeSpan.FromMinutes(5);
    
    // Health monitoring
    private readonly ConcurrentDictionary<string, ProviderHealth> _providerHealth = new();
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();
    
    // Statistics
    private readonly ConcurrentDictionary<string, ProviderStats> _providerStats = new();
    private readonly object _statsLock = new object();

    public NotificationProviderManager(
        ILogger<NotificationProviderManager> logger,
        IEnumerable<IRealtimeNotificationProvider> providers,
        IOptionsMonitor<NotificationProvidersConfiguration> config)
    {
        _logger = logger;
        _providers = providers;
        _config = config;
        
        // Order providers by priority
        _orderedProviders = _providers.OrderBy(GetProviderPriority).ToList();
        
        // Initialize health tracking for all providers
        foreach (var provider in _orderedProviders)
        {
            _providerHealth[provider.ProviderName] = new ProviderHealth
            {
                ProviderName = provider.ProviderName,
                IsEnabled = IsProviderEnabled(provider.ProviderName),
                LastChecked = DateTime.UtcNow
            };
            
            _circuitBreakers[provider.ProviderName] = new CircuitBreakerState();
            _providerStats[provider.ProviderName] = new ProviderStats();
        }
        
        _logger.LogInformation("NotificationProviderManager initialized with {ProviderCount} providers: {Providers}",
            _orderedProviders.Count, string.Join(", ", _orderedProviders.Select(p => p.ProviderName)));
    }

    #region Provider Operations

    public async Task<bool> SendNotificationAsync(NotificationDeliveryRequest request)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogDebug("Sending notification to user {UserId} via provider manager", request.UserId);
            
            // Ensure we have an active provider
            await EnsureActiveProviderAsync();
            
            // Get available providers for this request
            var availableProviders = await GetAvailableProvidersForRequestAsync(request);
            
            if (!availableProviders.Any())
            {
                _logger.LogWarning("No available providers for notification to user {UserId}", request.UserId);
                return false;
            }
            
            // Try providers in order until one succeeds
            foreach (var provider in availableProviders)
            {
                if (await TryProviderAsync(provider, request, startTime))
                {
                    return true;
                }
            }
            
            _logger.LogError("All providers failed for notification to user {UserId}", request.UserId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", request.UserId);
            return false;
        }
    }

    public async Task<bool> SendTestNotificationAsync(int userId, string? providerName = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(providerName))
            {
                // Test specific provider
                var provider = _orderedProviders.FirstOrDefault(p => 
                    p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                
                if (provider == null)
                {
                    _logger.LogWarning("Provider {ProviderName} not found for test notification", providerName);
                    return false;
                }
                
                return await provider.SendTestNotificationAsync(userId);
            }
            
            // Test with best available provider
            var bestProvider = await GetBestProviderAsync(userId, "test");
            if (bestProvider == null)
            {
                _logger.LogWarning("No available providers for test notification to user {UserId}", userId);
                return false;
            }
            
            return await bestProvider.SendTestNotificationAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test notification to user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendMulticastNotificationAsync(MulticastDeliveryRequest request)
    {
        try
        {
            _logger.LogDebug("Sending multicast notification to {UserCount} users", request.UserIds.Count);
            
            // For multicast, try to use a provider that supports it efficiently
            var provider = await GetBestProviderForMulticastAsync(request);
            if (provider == null)
            {
                _logger.LogWarning("No available providers for multicast notification");
                return false;
            }
            
            // Check if provider supports native multicast
            if (provider is IFirebaseService firebaseService)
            {
                // Use Firebase's native multicast capability
                var fcmTokens = await GetFcmTokensForUsersAsync(request.UserIds);
                if (fcmTokens.Any())
                {
                    return await firebaseService.SendMulticastNotificationAsync(fcmTokens, 
                        request.Title, request.Body, request.Data);
                }
            }
            
            // Fall back to individual notifications
            var tasks = request.UserIds.Select(async userId =>
            {
                var individualRequest = new NotificationDeliveryRequest
                {
                    UserId = userId,
                    NotificationType = request.NotificationType,
                    Title = request.Title,
                    Body = request.Body,
                    Data = request.Data,
                    Priority = request.Priority,
                    RequireDeliveryConfirmation = request.RequireDeliveryConfirmation,
                    PreferredProvider = request.PreferredProvider
                };
                
                return await SendNotificationAsync(individualRequest);
            });
            
            var results = await Task.WhenAll(tasks);
            var successCount = results.Count(r => r);
            
            _logger.LogInformation("Multicast notification completed: {SuccessCount}/{TotalCount} successful",
                successCount, request.UserIds.Count);
            
            return successCount > 0; // Consider successful if at least one delivery succeeded
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send multicast notification");
            return false;
        }
    }

    #endregion

    #region Provider Management

    public async Task<List<IRealtimeNotificationProvider>> GetAvailableProvidersAsync()
    {
        var availableProviders = new List<IRealtimeNotificationProvider>();
        
        foreach (var provider in _orderedProviders)
        {
            try
            {
                if (IsProviderEnabled(provider.ProviderName) && 
                    !IsCircuitBreakerOpen(provider.ProviderName) &&
                    await provider.IsAvailableAsync())
                {
                    availableProviders.Add(provider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check availability for provider {ProviderName}", provider.ProviderName);
                await RecordProviderFailureAsync(provider.ProviderName, ex.Message);
            }
        }
        
        return availableProviders;
    }

    public async Task<IRealtimeNotificationProvider?> GetBestProviderAsync(int userId, string notificationType)
    {
        await EnsureActiveProviderAsync();
        
        // Check if there's a preferred provider for this notification type
        var preferredProvider = GetPreferredProviderForType(notificationType);
        if (preferredProvider != null && 
            IsProviderEnabled(preferredProvider.ProviderName) &&
            !IsCircuitBreakerOpen(preferredProvider.ProviderName))
        {
            try
            {
                if (await preferredProvider.IsAvailableAsync())
                {
                    return preferredProvider;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Preferred provider {ProviderName} failed availability check", 
                    preferredProvider.ProviderName);
                await RecordProviderFailureAsync(preferredProvider.ProviderName, ex.Message);
            }
        }
        
        // Fall back to active provider
        return _activeProvider;
    }

    public async Task<List<IRealtimeNotificationProvider>> GetAllProvidersAsync()
    {
        return _orderedProviders.ToList();
    }

    #endregion

    #region Health Monitoring

    public async Task RefreshProviderHealthAsync()
    {
        _logger.LogInformation("Refreshing provider health status");
        
        foreach (var provider in _orderedProviders)
        {
            await UpdateProviderHealthAsync(provider);
        }
        
        // Reset provider check to force re-evaluation
        _lastProviderCheck = DateTime.MinValue;
        await EnsureActiveProviderAsync();
    }

    public async Task<Dictionary<string, ProviderHealth>> GetProviderHealthAsync()
    {
        // Update health for all providers
        foreach (var provider in _orderedProviders)
        {
            await UpdateProviderHealthAsync(provider);
        }
        
        return _providerHealth.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public async Task<ProviderHealth?> GetProviderHealthAsync(string providerName)
    {
        var provider = _orderedProviders.FirstOrDefault(p => 
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        
        if (provider == null)
        {
            return null;
        }
        
        await UpdateProviderHealthAsync(provider);
        return _providerHealth.TryGetValue(providerName, out var health) ? health : null;
    }

    public async Task<bool> HasAvailableProvidersAsync()
    {
        var availableProviders = await GetAvailableProvidersAsync();
        return availableProviders.Any();
    }

    #endregion

    #region Configuration

    public async Task UpdateProviderPriorityAsync(string providerName, int priority)
    {
        // This would typically update configuration in a persistent store
        // For now, we'll log the change and note that it requires restart
        _logger.LogInformation("Provider priority update requested: {ProviderName} -> {Priority} (requires restart)",
            providerName, priority);

        // In a production system, this would update the configuration store
        // and potentially trigger a configuration reload
        await Task.CompletedTask;
    }

    public async Task EnableProviderAsync(string providerName, bool enabled)
    {
        _logger.LogInformation("Provider enable/disable requested: {ProviderName} -> {Enabled}",
            providerName, enabled);

        if (_providerHealth.TryGetValue(providerName, out var health))
        {
            health.IsEnabled = enabled;
            health.LastChecked = DateTime.UtcNow;

            if (!enabled)
            {
                health.Status = "Disabled";
                health.IsHealthy = false;
                health.IsAvailable = false;
            }
        }

        // Reset active provider check to re-evaluate
        _lastProviderCheck = DateTime.MinValue;
        await EnsureActiveProviderAsync();
    }

    public async Task<Dictionary<string, ProviderConfiguration>> GetProviderConfigurationsAsync()
    {
        var configurations = new Dictionary<string, ProviderConfiguration>();

        foreach (var provider in _orderedProviders)
        {
            configurations[provider.ProviderName] = new ProviderConfiguration
            {
                ProviderName = provider.ProviderName,
                IsEnabled = IsProviderEnabled(provider.ProviderName),
                Priority = GetProviderPriority(provider),
                MaxRetries = 3, // Default values - would come from configuration
                Timeout = TimeSpan.FromSeconds(30),
                EnableCircuitBreaker = true,
                CircuitBreakerThreshold = 5,
                CircuitBreakerTimeout = TimeSpan.FromMinutes(5)
            };
        }

        return configurations;
    }

    #endregion

    #region Private Helper Methods

    private async Task<bool> TryProviderAsync(IRealtimeNotificationProvider provider,
        NotificationDeliveryRequest request, DateTime startTime)
    {
        var providerName = provider.ProviderName;

        try
        {
            _logger.LogDebug("Attempting notification delivery via {ProviderName} for user {UserId}",
                providerName, request.UserId);

            // Check circuit breaker
            if (IsCircuitBreakerOpen(providerName))
            {
                _logger.LogWarning("Circuit breaker is open for provider {ProviderName}, skipping", providerName);
                return false;
            }

            // Record attempt
            RecordProviderAttempt(providerName);

            // Try to send notification
            var success = await provider.SendNotificationAsync(request.UserId, request.Title, request.Body, request.Data);

            if (success)
            {
                var duration = DateTime.UtcNow - startTime;
                await RecordProviderSuccessAsync(providerName, duration);
                _logger.LogInformation("Notification delivered successfully via {ProviderName} for user {UserId} in {Duration}ms",
                    providerName, request.UserId, duration.TotalMilliseconds);
                return true;
            }
            else
            {
                await RecordProviderFailureAsync(providerName, "Provider returned false");
                _logger.LogWarning("Provider {ProviderName} returned false for user {UserId}", providerName, request.UserId);
                return false;
            }
        }
        catch (Exception ex)
        {
            await RecordProviderFailureAsync(providerName, ex.Message);
            _logger.LogError(ex, "Provider {ProviderName} failed for user {UserId}", providerName, request.UserId);
            return false;
        }
    }

    private async Task<List<IRealtimeNotificationProvider>> GetAvailableProvidersForRequestAsync(
        NotificationDeliveryRequest request)
    {
        var availableProviders = new List<IRealtimeNotificationProvider>();

        // If a preferred provider is specified, try it first
        if (!string.IsNullOrEmpty(request.PreferredProvider))
        {
            var preferredProvider = _orderedProviders.FirstOrDefault(p =>
                p.ProviderName.Equals(request.PreferredProvider, StringComparison.OrdinalIgnoreCase));

            if (preferredProvider != null &&
                IsProviderEnabled(preferredProvider.ProviderName) &&
                !IsCircuitBreakerOpen(preferredProvider.ProviderName))
            {
                try
                {
                    if (await preferredProvider.IsAvailableAsync())
                    {
                        availableProviders.Add(preferredProvider);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Preferred provider {ProviderName} failed availability check",
                        preferredProvider.ProviderName);
                }
            }
        }

        // Add other available providers (excluding preferred and excluded ones)
        foreach (var provider in _orderedProviders)
        {
            if (availableProviders.Contains(provider))
                continue;

            if (request.ExcludedProviders?.Contains(provider.ProviderName) == true)
                continue;

            if (!IsProviderEnabled(provider.ProviderName) || IsCircuitBreakerOpen(provider.ProviderName))
                continue;

            try
            {
                if (await provider.IsAvailableAsync())
                {
                    availableProviders.Add(provider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {ProviderName} failed availability check", provider.ProviderName);
                await RecordProviderFailureAsync(provider.ProviderName, ex.Message);
            }
        }

        return availableProviders;
    }

    private async Task EnsureActiveProviderAsync()
    {
        // Check if we need to refresh the active provider
        if (_activeProvider != null && DateTime.UtcNow - _lastProviderCheck < _providerCheckInterval)
        {
            return;
        }

        _lastProviderCheck = DateTime.UtcNow;

        // Find the first available provider in priority order
        foreach (var provider in _orderedProviders)
        {
            try
            {
                if (IsProviderEnabled(provider.ProviderName) &&
                    !IsCircuitBreakerOpen(provider.ProviderName) &&
                    await provider.IsAvailableAsync())
                {
                    if (_activeProvider?.ProviderName != provider.ProviderName)
                    {
                        _logger.LogInformation("Switching to notification provider: {ProviderName}", provider.ProviderName);
                        _activeProvider = provider;
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {ProviderName} availability check failed", provider.ProviderName);
                await RecordProviderFailureAsync(provider.ProviderName, ex.Message);
            }
        }

        // No providers available
        if (_activeProvider != null)
        {
            _logger.LogWarning("No notification providers available, clearing active provider");
            _activeProvider = null;
        }
    }

    private async Task UpdateProviderHealthAsync(IRealtimeNotificationProvider provider)
    {
        var providerName = provider.ProviderName;
        var health = _providerHealth.GetOrAdd(providerName, _ => new ProviderHealth { ProviderName = providerName });

        try
        {
            health.IsEnabled = IsProviderEnabled(providerName);
            health.IsAvailable = health.IsEnabled && await provider.IsAvailableAsync();
            health.IsHealthy = health.IsAvailable && !IsCircuitBreakerOpen(providerName);
            health.Status = GetProviderStatus(providerName);
            health.LastChecked = DateTime.UtcNow;
            health.ErrorMessage = null;

            // Update metrics from stats
            if (_providerStats.TryGetValue(providerName, out var stats))
            {
                health.ConsecutiveFailures = stats.ConsecutiveFailures;
                health.SuccessRate = stats.TotalAttempts > 0
                    ? (double)stats.SuccessfulAttempts / stats.TotalAttempts * 100
                    : 0;
                health.AverageLatencyMs = stats.TotalLatencyMs > 0 && stats.SuccessfulAttempts > 0
                    ? stats.TotalLatencyMs / stats.SuccessfulAttempts
                    : 0;
                health.LastSuccessfulDelivery = stats.LastSuccessfulDelivery;
                health.LastFailedDelivery = stats.LastFailedDelivery;
            }
        }
        catch (Exception ex)
        {
            health.IsHealthy = false;
            health.IsAvailable = false;
            health.Status = "Error";
            health.ErrorMessage = ex.Message;
            health.LastChecked = DateTime.UtcNow;

            _logger.LogWarning(ex, "Failed to update health for provider {ProviderName}", providerName);
        }
    }

    private int GetProviderPriority(IRealtimeNotificationProvider provider)
    {
        return provider.ProviderName.ToLower() switch
        {
            "firebase" => 1,
            "signalr" => 2,
            "expo" => 3,
            _ => 10
        };
    }

    private bool IsProviderEnabled(string providerName)
    {
        var config = _config.CurrentValue;
        return providerName.ToLower() switch
        {
            "firebase" => config.Firebase?.Enabled ?? false,
            "signalr" => config.SignalR?.Enabled ?? false,
            "expo" => config.Expo?.Enabled ?? false,
            _ => true // Default to enabled for unknown providers
        };
    }

    private string GetProviderStatus(string providerName)
    {
        if (!IsProviderEnabled(providerName))
            return "Disabled";

        if (IsCircuitBreakerOpen(providerName))
            return "Circuit Breaker Open";

        return "Available";
    }

    private IRealtimeNotificationProvider? GetPreferredProviderForType(string notificationType)
    {
        // Different notification types might prefer different providers
        return notificationType.ToLower() switch
        {
            "message" => _orderedProviders.FirstOrDefault(p => p.ProviderName.ToLower() == "firebase"),
            "urgent" => _orderedProviders.FirstOrDefault(p => p.ProviderName.ToLower() == "firebase"),
            _ => null // No specific preference
        };
    }

    private async Task<IRealtimeNotificationProvider?> GetBestProviderForMulticastAsync(MulticastDeliveryRequest request)
    {
        // Firebase is best for multicast due to native support
        var firebaseProvider = _orderedProviders.FirstOrDefault(p => p.ProviderName.ToLower() == "firebase");
        if (firebaseProvider != null &&
            IsProviderEnabled(firebaseProvider.ProviderName) &&
            !IsCircuitBreakerOpen(firebaseProvider.ProviderName))
        {
            try
            {
                if (await firebaseProvider.IsAvailableAsync())
                {
                    return firebaseProvider;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firebase provider failed availability check for multicast");
            }
        }

        // Fall back to best available provider
        return await GetBestProviderAsync(0, request.NotificationType);
    }

    private async Task<List<string>> GetFcmTokensForUsersAsync(List<int> userIds)
    {
        // This would typically query the database for FCM tokens
        // For now, return empty list as placeholder
        await Task.CompletedTask;
        return new List<string>();
    }

    #endregion

    #region Circuit Breaker and Statistics

    private bool IsCircuitBreakerOpen(string providerName)
    {
        if (!_circuitBreakers.TryGetValue(providerName, out var state))
            return false;

        if (state.State == CircuitBreakerStateEnum.Open)
        {
            // Check if timeout has passed
            if (DateTime.UtcNow - state.LastFailureTime > TimeSpan.FromMinutes(5))
            {
                state.State = CircuitBreakerStateEnum.HalfOpen;
                state.ConsecutiveFailures = 0;
                _logger.LogInformation("Circuit breaker for {ProviderName} moved to half-open state", providerName);
            }
        }

        return state.State == CircuitBreakerStateEnum.Open;
    }

    private void RecordProviderAttempt(string providerName)
    {
        var stats = _providerStats.GetOrAdd(providerName, _ => new ProviderStats());
        Interlocked.Increment(ref stats.TotalAttempts);
    }

    private async Task RecordProviderSuccessAsync(string providerName, TimeSpan duration)
    {
        var stats = _providerStats.GetOrAdd(providerName, _ => new ProviderStats());

        Interlocked.Increment(ref stats.SuccessfulAttempts);
        Interlocked.Add(ref stats.TotalLatencyMs, (long)duration.TotalMilliseconds);
        stats.LastSuccessfulDelivery = DateTime.UtcNow;
        stats.ConsecutiveFailures = 0;

        // Reset circuit breaker on success
        if (_circuitBreakers.TryGetValue(providerName, out var circuitBreaker))
        {
            if (circuitBreaker.State == CircuitBreakerStateEnum.HalfOpen)
            {
                circuitBreaker.State = CircuitBreakerStateEnum.Closed;
                circuitBreaker.ConsecutiveFailures = 0;
                _logger.LogInformation("Circuit breaker for {ProviderName} closed after successful delivery", providerName);
            }
        }

        await Task.CompletedTask;
    }

    private async Task RecordProviderFailureAsync(string providerName, string error)
    {
        var stats = _providerStats.GetOrAdd(providerName, _ => new ProviderStats());

        Interlocked.Increment(ref stats.FailedAttempts);
        stats.LastFailedDelivery = DateTime.UtcNow;
        stats.LastError = error;
        Interlocked.Increment(ref stats.ConsecutiveFailures);

        // Update circuit breaker
        var circuitBreaker = _circuitBreakers.GetOrAdd(providerName, _ => new CircuitBreakerState());
        circuitBreaker.ConsecutiveFailures++;
        circuitBreaker.LastFailureTime = DateTime.UtcNow;

        // Open circuit breaker if threshold exceeded
        if (circuitBreaker.ConsecutiveFailures >= 5 && circuitBreaker.State != CircuitBreakerStateEnum.Open)
        {
            circuitBreaker.State = CircuitBreakerStateEnum.Open;
            _logger.LogWarning("Circuit breaker opened for provider {ProviderName} after {Failures} consecutive failures",
                providerName, circuitBreaker.ConsecutiveFailures);
        }

        await Task.CompletedTask;
    }

    #endregion
}

#region Supporting Classes

internal class ProviderStats
{
    public long TotalAttempts;
    public long SuccessfulAttempts;
    public long FailedAttempts;
    public long TotalLatencyMs;
    public int ConsecutiveFailures;
    public DateTime? LastSuccessfulDelivery;
    public DateTime? LastFailedDelivery;
    public string? LastError;
}

internal class CircuitBreakerState
{
    public CircuitBreakerStateEnum State { get; set; } = CircuitBreakerStateEnum.Closed;
    public int ConsecutiveFailures { get; set; }
    public DateTime LastFailureTime { get; set; }
}

internal enum CircuitBreakerStateEnum
{
    Closed,
    Open,
    HalfOpen
}

#endregion
