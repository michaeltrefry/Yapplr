using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

/// <summary>
/// Composite notification service that tries Firebase first, then falls back to SignalR
/// </summary>
public class CompositeNotificationService : ICompositeNotificationService
{
    private readonly ILogger<CompositeNotificationService> _logger;
    private readonly IEnumerable<IRealtimeNotificationProvider> _providers;
    private readonly INotificationPreferencesService? _preferencesService;
    private readonly INotificationDeliveryService? _deliveryService;
    private readonly ISmartRetryService? _retryService;
    private readonly INotificationCompressionService? _compressionService;
    private readonly IOfflineNotificationService? _offlineService;
    private readonly INotificationRateLimitService? _rateLimitService;
    private readonly INotificationContentFilterService? _contentFilterService;
    private readonly INotificationAuditService? _auditService;
    private IRealtimeNotificationProvider? _activeProvider;
    private DateTime _lastProviderCheck = DateTime.MinValue;
    private readonly TimeSpan _providerCheckInterval = TimeSpan.FromMinutes(5);

    public CompositeNotificationService(
        ILogger<CompositeNotificationService> logger,
        IEnumerable<IRealtimeNotificationProvider> providers,
        INotificationPreferencesService? preferencesService = null,
        INotificationDeliveryService? deliveryService = null,
        ISmartRetryService? retryService = null,
        INotificationCompressionService? compressionService = null,
        IOfflineNotificationService? offlineService = null,
        INotificationRateLimitService? rateLimitService = null,
        INotificationContentFilterService? contentFilterService = null,
        INotificationAuditService? auditService = null)
    {
        _logger = logger;
        _providers = providers.OrderBy(GetProviderPriority).ToList();
        _preferencesService = preferencesService;
        _deliveryService = deliveryService;
        _retryService = retryService;
        _compressionService = compressionService;
        _offlineService = offlineService;
        _rateLimitService = rateLimitService;
        _contentFilterService = contentFilterService;
        _auditService = auditService;
    }

    public string ProviderName => _activeProvider?.ProviderName ?? "Composite";

    public IRealtimeNotificationProvider? ActiveProvider => _activeProvider;

    public IEnumerable<IRealtimeNotificationProvider> AvailableProviders => _providers;

    /// <summary>
    /// Gets provider priority (lower number = higher priority)
    /// Firebase = 1, SignalR = 2, others = 10
    /// </summary>
    private int GetProviderPriority(IRealtimeNotificationProvider provider)
    {
        return provider.ProviderName.ToLower() switch
        {
            "firebase" => 1,
            "signalr" => 2,
            _ => 10
        };
    }

    public async Task<bool> IsAvailableAsync()
    {
        await EnsureActiveProviderAsync();
        return _activeProvider != null;
    }

    public async Task RefreshProviderStatusAsync()
    {
        _lastProviderCheck = DateTime.MinValue;
        await EnsureActiveProviderAsync();
    }

    public async Task<Dictionary<string, bool>> GetProviderStatusAsync()
    {
        var status = new Dictionary<string, bool>();
        
        foreach (var provider in _providers)
        {
            try
            {
                status[provider.ProviderName] = await provider.IsAvailableAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check availability for provider {ProviderName}", provider.ProviderName);
                status[provider.ProviderName] = false;
            }
        }

        return status;
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
        foreach (var provider in _providers)
        {
            try
            {
                if (await provider.IsAvailableAsync())
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
            }
        }

        if (_activeProvider != null)
        {
            _logger.LogWarning("No notification providers available, keeping current provider: {ProviderName}", _activeProvider.ProviderName);
        }
        else
        {
            _logger.LogError("No notification providers available");
        }
    }

    private async Task<bool> TryWithFallbackAsync(Func<IRealtimeNotificationProvider, Task<bool>> operation, string operationName)
    {
        await EnsureActiveProviderAsync();

        // Log available providers for debugging
        var availableProviders = new List<string>();
        foreach (var provider in _providers)
        {
            var isAvailable = await provider.IsAvailableAsync();
            if (isAvailable)
            {
                availableProviders.Add(provider.ProviderName);
            }
        }

        _logger.LogInformation("🔔 Starting operation {OperationName}. Available providers: [{Providers}]",
            operationName, string.Join(", ", availableProviders));

        if (_activeProvider == null)
        {
            _logger.LogWarning("❌ No notification providers available for operation: {OperationName}", operationName);
            return false;
        }

        // Try the active provider first
        _logger.LogInformation("🎯 Attempting operation {OperationName} with active provider: {ProviderName}",
            operationName, _activeProvider.ProviderName);

        try
        {
            var result = await operation(_activeProvider);
            if (result)
            {
                _logger.LogInformation("✅ Operation {OperationName} succeeded with active provider: {ProviderName}",
                    operationName, _activeProvider.ProviderName);
                return true;
            }

            _logger.LogWarning("❌ Active provider {ProviderName} failed for operation: {OperationName}", _activeProvider.ProviderName, operationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Active provider {ProviderName} threw exception for operation: {OperationName}", _activeProvider.ProviderName, operationName);
        }

        // Try fallback providers
        var fallbackProviders = _providers.Where(p => p != _activeProvider).ToList();
        if (fallbackProviders.Any())
        {
            _logger.LogInformation("🔄 Trying fallback providers for operation: {OperationName}", operationName);
        }

        foreach (var provider in fallbackProviders)
        {
            try
            {
                if (await provider.IsAvailableAsync())
                {
                    _logger.LogInformation("🔄 Trying fallback provider {ProviderName} for operation: {OperationName}", provider.ProviderName, operationName);
                    var result = await operation(provider);
                    if (result)
                    {
                        _logger.LogInformation("✅ Fallback provider {ProviderName} succeeded for operation: {OperationName}", provider.ProviderName, operationName);
                        return true;
                    }
                    _logger.LogWarning("❌ Fallback provider {ProviderName} failed for operation: {OperationName}", provider.ProviderName, operationName);
                }
                else
                {
                    _logger.LogInformation("⏭️ Fallback provider {ProviderName} is not available for operation: {OperationName}", provider.ProviderName, operationName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "💥 Fallback provider {ProviderName} failed for operation: {OperationName}", provider.ProviderName, operationName);
            }
        }

        _logger.LogError("🚫 All notification providers failed for operation: {OperationName}", operationName);
        return false;
    }

    public async Task<bool> SendTestNotificationAsync(int userId)
    {
        return await TryWithFallbackAsync(
            provider => provider.SendTestNotificationAsync(userId),
            nameof(SendTestNotificationAsync));
    }

    public async Task<bool> SendNotificationAsync(int userId, string title, string body, Dictionary<string, string>? data = null)
    {
        return await SendNotificationWithPreferencesAsync(userId, data?["type"] ?? "generic", title, body, data);
    }

    public async Task<bool> SendNotificationWithPreferencesAsync(int userId, string notificationType, string title, string body, Dictionary<string, string>? data = null)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Security checks first
            var securityResult = await PerformSecurityChecksAsync(userId, notificationType, title, body, data);
            if (!securityResult.IsAllowed)
            {
                await LogSecurityViolationAsync(userId, notificationType, securityResult.Reason ?? "Security check failed", title, body);
                return false;
            }

            // Check if user preferences allow this notification
            if (_preferencesService != null)
            {
                var shouldSend = await _preferencesService.ShouldSendNotificationAsync(userId, notificationType);
                if (!shouldSend)
                {
                    _logger.LogDebug("Notification blocked by user preferences for user {UserId}, type {NotificationType}", userId, notificationType);
                    await _auditService?.LogEventAsync(AuditEventTypes.NotificationBlocked, userId, notificationType,
                        new { reason = "user_preferences" }, false)!;
                    return false;
                }

                // Get preferred delivery method
                var preferredMethod = await _preferencesService.GetPreferredDeliveryMethodAsync(userId, notificationType);
                if (preferredMethod == NotificationDeliveryMethod.Disabled)
                {
                    _logger.LogDebug("Notification type {NotificationType} is disabled for user {UserId}", notificationType, userId);
                    await _auditService?.LogEventAsync(AuditEventTypes.NotificationBlocked, userId, notificationType,
                        new { reason = "notification_type_disabled" }, false)!;
                    return false;
                }
            }

            // Save to history if delivery service is available
            if (_deliveryService != null)
            {
                await _deliveryService.SaveToHistoryAsync(userId, notificationType, title, body, data);
            }

            // Try to send with UX enhancements
            var result = await SendWithUXEnhancementsAsync(userId, notificationType, securityResult.SanitizedTitle ?? title, securityResult.SanitizedBody ?? body, data);

            // Log the result
            var processingTime = DateTime.UtcNow - startTime;
            await _auditService?.LogNotificationSentAsync(userId, notificationType, title, body,
                GetActiveProviderName(), result, null, processingTime)!;

            return result;
        }
        catch (Exception ex)
        {
            var processingTime = DateTime.UtcNow - startTime;
            await _auditService?.LogNotificationSentAsync(userId, notificationType, title, body,
                GetActiveProviderName(), false, ex.Message, processingTime)!;
            throw;
        }
    }

    private async Task<bool> SendWithUXEnhancementsAsync(int userId, string notificationType, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            Func<Task<bool>> sendOperation = () => SendNotificationDirectAsync(userId, title, body, data);
            
            // Use smart retry if available
            return _retryService != null
                ? await _retryService.ExecuteWithRetryAsync(sendOperation, $"SendNotification_{notificationType}_{userId}")
                : await sendOperation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId} after all retry attempts", userId);

            // Queue for offline delivery if service is available
            if (_offlineService != null)
            {
                await QueueForOfflineDelivery(userId, notificationType, title, body, data);
                return true; // Consider it successful since it's queued
            }

            return false;
        }
    }

    private async Task QueueForOfflineDelivery(int userId, string notificationType, string title, string body, Dictionary<string, string>? data)
    {
        var offlineNotification = new OfflineNotification
        {
            UserId = userId,
            NotificationType = notificationType,
            Title = title,
            Body = body,
            Data = data,
            Priority = GetNotificationPriority(notificationType)
        };

        await _offlineService!.QueueOfflineNotificationAsync(offlineNotification);
        _logger.LogInformation("Queued notification for offline delivery to user {UserId}", userId);
    }

    private async Task<bool> SendNotificationDirectAsync(int userId, string title, string body, Dictionary<string, string>? data = null)
    {
        // Optimize payload if compression service is available
        if (_compressionService != null)
        {
            try
            {
                var payload = new { title, body, data };
                var optimizedPayload = await _compressionService.OptimizePayloadAsync(payload);

                // Extract optimized values
                if (optimizedPayload is Dictionary<string, object> optimizedDict)
                {
                    title = optimizedDict.TryGetValue("title", out var t) ? t?.ToString() ?? title : title;
                    body = optimizedDict.TryGetValue("body", out var b) ? b?.ToString() ?? body : body;

                    if (optimizedDict.TryGetValue("data", out var d) && d is Dictionary<string, object> dataDict)
                    {
                        data = dataDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to optimize notification payload, using original");
            }
        }

        return await TryWithFallbackAsync(
            provider => provider.SendNotificationAsync(userId, title, body, data),
            nameof(SendNotificationAsync));
    }

    private static NotificationPriority GetNotificationPriority(string notificationType)
    {
        return notificationType.ToLower() switch
        {
            "message" => NotificationPriority.High,
            "mention" => NotificationPriority.High,
            "follow_request" => NotificationPriority.Normal,
            "reply" => NotificationPriority.Normal,
            "comment" => NotificationPriority.Normal,
            "follow" => NotificationPriority.Low,
            "like" => NotificationPriority.Low,
            "repost" => NotificationPriority.Low,
            _ => NotificationPriority.Normal
        };
    }

    private async Task<SecurityCheckResult> PerformSecurityChecksAsync(int userId, string notificationType, string title, string body, Dictionary<string, string>? data)
    {
        // Check rate limits
        if (_rateLimitService != null)
        {
            var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(userId, notificationType);
            if (!rateLimitResult.IsAllowed)
            {
                await _rateLimitService.RecordRequestAsync(userId, notificationType); // Record the attempt
                await _auditService?.LogSecurityEventAsync(AuditEventTypes.RateLimitExceeded, userId,
                    $"Rate limit exceeded for {notificationType}: {rateLimitResult.ViolationType}",
                    new { rateLimitResult })!;

                return new SecurityCheckResult
                {
                    IsAllowed = false,
                    Reason = $"Rate limit exceeded: {rateLimitResult.ViolationType}"
                };
            }

            // Record successful rate limit check
            await _rateLimitService.RecordRequestAsync(userId, notificationType);
        }

        // Check content filtering
        if (_contentFilterService != null)
        {
            var contentResult = await _contentFilterService.ValidateNotificationAsync(title, body, data);
            if (!contentResult.IsValid)
            {
                await _auditService?.LogSecurityEventAsync(AuditEventTypes.ContentFiltered, userId,
                    $"Content filtered for {notificationType}: {string.Join(", ", contentResult.Violations)}",
                    new { contentResult })!;

                return new SecurityCheckResult
                {
                    IsAllowed = false,
                    Reason = $"Content filtered: {string.Join(", ", contentResult.Violations)}"
                };
            }

            // Use sanitized content if available
            if (!string.IsNullOrEmpty(contentResult.SanitizedContent))
            {
                var parts = contentResult.SanitizedContent.Split('|');
                if (parts.Length >= 2)
                {
                    return new SecurityCheckResult
                    {
                        IsAllowed = true,
                        SanitizedTitle = parts[0],
                        SanitizedBody = parts[1]
                    };
                }
            }
        }

        return new SecurityCheckResult { IsAllowed = true };
    }

    private async Task LogSecurityViolationAsync(int userId, string notificationType, string reason, string title, string body)
    {
        await _auditService?.LogSecurityEventAsync(AuditEventTypes.SecurityViolation, userId,
            $"Security violation for {notificationType}: {reason}",
            new { notificationType, title, body, reason })!;
    }

    private string GetActiveProviderName()
    {
        return _activeProvider?.ProviderName ?? "Unknown";
    }

    private class SecurityCheckResult
    {
        public bool IsAllowed { get; set; }
        public string? Reason { get; set; }
        public string? SanitizedTitle { get; set; }
        public string? SanitizedBody { get; set; }
    }

    public async Task<bool> SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId)
    {
        var title = "New Message";
        var body = $"@{senderUsername}: {TruncateMessage(messageContent)}";
        var data = new Dictionary<string, string>
        {
            ["type"] = "message",
            ["conversationId"] = conversationId.ToString(),
            ["senderUsername"] = senderUsername
        };

        return await SendNotificationWithPreferencesAsync(userId, "message", title, body, data);
    }

    public async Task<bool> SendMentionNotificationAsync(int userId, string mentionerUsername, int postId, int? commentId = null)
    {
        return await TryWithFallbackAsync(
            provider => provider.SendMentionNotificationAsync(userId, mentionerUsername, postId, commentId),
            nameof(SendMentionNotificationAsync));
    }

    public async Task<bool> SendReplyNotificationAsync(int userId, string replierUsername, int postId, int commentId)
    {
        return await TryWithFallbackAsync(
            provider => provider.SendReplyNotificationAsync(userId, replierUsername, postId, commentId),
            nameof(SendReplyNotificationAsync));
    }

    public async Task<bool> SendCommentNotificationAsync(int userId, string commenterUsername, int postId, int commentId)
    {
        return await TryWithFallbackAsync(
            provider => provider.SendCommentNotificationAsync(userId, commenterUsername, postId, commentId),
            nameof(SendCommentNotificationAsync));
    }

    public async Task<bool> SendFollowNotificationAsync(int userId, string followerUsername)
    {
        return await TryWithFallbackAsync(
            provider => provider.SendFollowNotificationAsync(userId, followerUsername),
            nameof(SendFollowNotificationAsync));
    }

    public async Task<bool> SendFollowRequestNotificationAsync(int userId, string requesterUsername)
    {
        return await TryWithFallbackAsync(
            provider => provider.SendFollowRequestNotificationAsync(userId, requesterUsername),
            nameof(SendFollowRequestNotificationAsync));
    }

    public async Task<bool> SendFollowRequestApprovedNotificationAsync(int userId, string approverUsername)
    {
        return await TryWithFallbackAsync(
            provider => provider.SendFollowRequestApprovedNotificationAsync(userId, approverUsername),
            nameof(SendFollowRequestApprovedNotificationAsync));
    }

    public async Task<bool> SendLikeNotificationAsync(int userId, string likerUsername, int postId)
    {
        return await TryWithFallbackAsync(
            provider => provider.SendLikeNotificationAsync(userId, likerUsername, postId),
            nameof(SendLikeNotificationAsync));
    }

    public async Task<bool> SendRepostNotificationAsync(int userId, string reposterUsername, int postId)
    {
        return await TryWithFallbackAsync(
            provider => provider.SendRepostNotificationAsync(userId, reposterUsername, postId),
            nameof(SendRepostNotificationAsync));
    }

    public async Task<bool> SendMulticastNotificationAsync(List<int> userIds, string title, string body, Dictionary<string, string>? data = null)
    {
        return await TryWithFallbackAsync(
            provider => provider.SendMulticastNotificationAsync(userIds, title, body, data),
            nameof(SendMulticastNotificationAsync));
    }

    private static string TruncateMessage(string message, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        return message.Length <= maxLength ? message : message[..maxLength] + "...";
    }
}
