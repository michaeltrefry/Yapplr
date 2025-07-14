namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Optional enhancement service that provides cross-cutting concerns for notifications.
/// Replaces NotificationMetricsService, NotificationAuditService, NotificationRateLimitService,
/// NotificationContentFilterService, and NotificationCompressionService.
/// </summary>
public interface INotificationEnhancementService
{
    #region Metrics and Analytics
    
    /// <summary>
    /// Records a notification event for metrics tracking
    /// </summary>
    /// <param name="notificationEvent">The event to record</param>
    Task RecordNotificationEventAsync(NotificationEvent notificationEvent);
    
    /// <summary>
    /// Gets comprehensive metrics about notification performance
    /// </summary>
    /// <param name="timeWindow">Optional time window to filter metrics</param>
    /// <returns>Notification metrics</returns>
    Task<NotificationMetrics> GetMetricsAsync(TimeSpan? timeWindow = null);
    
    /// <summary>
    /// Gets recent delivery events for monitoring
    /// </summary>
    /// <param name="count">Number of recent events to return</param>
    /// <returns>List of recent delivery events</returns>
    Task<List<DeliveryEvent>> GetRecentDeliveryEventsAsync(int count = 100);
    
    /// <summary>
    /// Gets performance insights and recommendations
    /// </summary>
    /// <returns>Performance insights</returns>
    Task<PerformanceInsights> GetPerformanceInsightsAsync();
    
    #endregion
    
    #region Security and Auditing
    
    /// <summary>
    /// Checks if a notification should be allowed based on security policies
    /// </summary>
    /// <param name="userId">The target user ID</param>
    /// <param name="notificationType">The type of notification</param>
    /// <param name="content">The notification content</param>
    /// <returns>True if the notification should be allowed</returns>
    Task<bool> ShouldAllowNotificationAsync(int userId, string notificationType, string content);
    
    /// <summary>
    /// Logs a security event related to notifications
    /// </summary>
    /// <param name="securityEvent">The security event to log</param>
    Task LogSecurityEventAsync(SecurityEvent securityEvent);
    
    /// <summary>
    /// Gets audit logs for notification events
    /// </summary>
    /// <param name="queryParams">Query parameters for filtering audit logs</param>
    /// <returns>List of audit log entries</returns>
    Task<List<Models.NotificationAuditLog>> GetAuditLogsAsync(AuditQueryParams queryParams);
    
    /// <summary>
    /// Gets security statistics and threat information
    /// </summary>
    /// <returns>Security statistics</returns>
    Task<SecurityStats> GetSecurityStatsAsync();

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">The user ID to get audit logs for</param>
    /// <param name="count">Number of audit logs to return</param>
    /// <returns>List of audit log entries</returns>
    Task<List<Models.NotificationAuditLog>> GetUserAuditLogsAsync(int userId, int count);

    /// <summary>
    /// Gets audit statistics
    /// </summary>
    /// <param name="startDate">Optional start date for filtering</param>
    /// <param name="endDate">Optional end date for filtering</param>
    /// <returns>Audit statistics</returns>
    Task<Dictionary<string, object>> GetAuditStatsAsync(DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// Gets security events since a specific time
    /// </summary>
    /// <param name="since">Optional timestamp to filter events since</param>
    /// <returns>List of security events</returns>
    Task<List<SecurityEvent>> GetSecurityEventsAsync(DateTime? since);

    /// <summary>
    /// Blocks a user for a specified duration
    /// </summary>
    /// <param name="userId">The user ID to block</param>
    /// <param name="duration">Duration of the block</param>
    /// <param name="reason">Reason for blocking</param>
    /// <returns>True if blocking was successful</returns>
    Task<bool> BlockUserAsync(int userId, TimeSpan duration, string reason);

    /// <summary>
    /// Unblocks a previously blocked user
    /// </summary>
    /// <param name="userId">The user ID to unblock</param>
    /// <returns>True if unblocking was successful</returns>
    Task<bool> UnblockUserAsync(int userId);

    /// <summary>
    /// Cleans up old audit logs
    /// </summary>
    /// <param name="maxAge">Maximum age of logs to keep</param>
    /// <returns>Number of logs cleaned up</returns>
    Task<int> CleanupOldLogsAsync(TimeSpan maxAge);
    
    #endregion
    
    #region Rate Limiting
    
    /// <summary>
    /// Checks if a notification request should be rate limited
    /// </summary>
    /// <param name="userId">The user ID making the request</param>
    /// <param name="notificationType">The type of notification</param>
    /// <returns>Rate limit result</returns>
    Task<RateLimitResult> CheckRateLimitAsync(int userId, string notificationType);
    
    /// <summary>
    /// Records that a notification was sent for rate limiting purposes
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="notificationType">The type of notification</param>
    Task RecordNotificationSentAsync(int userId, string notificationType);
    
    /// <summary>
    /// Gets recent rate limit violations
    /// </summary>
    /// <param name="userId">Optional user ID to filter by</param>
    /// <returns>List of recent violations</returns>
    Task<List<RateLimitViolation>> GetRecentViolationsAsync(int? userId = null);

    /// <summary>
    /// Gets rate limit violations for a specific user
    /// </summary>
    /// <param name="userId">The user ID to get violations for</param>
    /// <returns>List of rate limit violations</returns>
    Task<List<RateLimitViolation>> GetRateLimitViolationsAsync(int userId);

    /// <summary>
    /// Gets rate limiting statistics
    /// </summary>
    /// <returns>Rate limiting statistics</returns>
    Task<Dictionary<string, object>> GetRateLimitStatsAsync();

    /// <summary>
    /// Resets rate limits for a specific user
    /// </summary>
    /// <param name="userId">The user ID to reset limits for</param>
    Task ResetUserRateLimitsAsync(int userId);
    
    #endregion
    
    #region Content Processing
    
    /// <summary>
    /// Filters and validates notification content
    /// </summary>
    /// <param name="content">The content to filter</param>
    /// <returns>Content filter result</returns>
    Task<ContentValidationResult> FilterContentAsync(string content);

    /// <summary>
    /// Validates content for security and policy compliance
    /// </summary>
    /// <param name="content">The content to validate</param>
    /// <param name="contentType">The type of content being validated</param>
    /// <returns>Content validation result</returns>
    Task<ContentValidationResult> ValidateContentAsync(string content, string contentType);

    /// <summary>
    /// Gets content filtering statistics
    /// </summary>
    /// <returns>Content filtering statistics</returns>
    Task<Dictionary<string, object>> GetContentFilterStatsAsync();
    
    /// <summary>
    /// Compresses a notification payload for efficient transmission
    /// </summary>
    /// <param name="payload">The payload to compress</param>
    /// <param name="settings">Optional compression settings</param>
    /// <returns>Compressed payload</returns>
    Task<CompressedNotificationPayload> CompressPayloadAsync(object payload, OptimizationSettings? settings = null);
    
    /// <summary>
    /// Decompresses a notification payload
    /// </summary>
    /// <param name="compressedPayload">The compressed payload</param>
    /// <returns>Decompressed payload</returns>
    Task<T> DecompressPayloadAsync<T>(CompressedNotificationPayload compressedPayload);
    
    /// <summary>
    /// Optimizes notification content for different delivery methods
    /// </summary>
    /// <param name="content">The content to optimize</param>
    /// <param name="deliveryMethod">The target delivery method</param>
    /// <returns>Optimized content</returns>
    Task<OptimizedContent> OptimizeContentAsync(string content, string deliveryMethod);
    
    #endregion
    
    #region Configuration and Health
    
    /// <summary>
    /// Gets the current configuration for enhancement features
    /// </summary>
    /// <returns>Enhancement configuration</returns>
    Task<EnhancementConfiguration> GetConfigurationAsync();
    
    /// <summary>
    /// Updates the configuration for enhancement features
    /// </summary>
    /// <param name="configuration">The new configuration</param>
    Task UpdateConfigurationAsync(EnhancementConfiguration configuration);
    
    /// <summary>
    /// Checks if the enhancement service is healthy
    /// </summary>
    /// <returns>True if healthy</returns>
    Task<bool> IsHealthyAsync();
    
    /// <summary>
    /// Gets detailed health information about enhancement features
    /// </summary>
    /// <returns>Health report</returns>
    Task<EnhancementHealthReport> GetHealthReportAsync();
    
    #endregion
}