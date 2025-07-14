using Yapplr.Api.Services;

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

/// <summary>
/// Represents a notification event for metrics tracking
/// </summary>
public class NotificationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string? TrackingId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty; // sent, delivered, failed, queued, etc.
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool Success { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
    public double? LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a delivery event for monitoring
/// </summary>
public class DeliveryEvent
{
    public string Id { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime StartTime { get; set; }
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool Success { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public double LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Performance insights and recommendations
/// </summary>
public class PerformanceInsights
{
    public double OverallSuccessRate { get; set; }
    public double AverageDeliveryTime { get; set; }
    public string BestPerformingProvider { get; set; } = string.Empty;
    public string WorstPerformingProvider { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> DetailedMetrics { get; set; } = new();
}

/// <summary>
/// Security event for auditing
/// </summary>
public class SecurityEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public SecurityEventSeverity Severity { get; set; } = SecurityEventSeverity.Low;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Security event severity levels
/// </summary>
public enum SecurityEventSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Security statistics
/// </summary>
public class SecurityStats
{
    public long TotalSecurityEvents { get; set; }
    public long BlockedNotifications { get; set; }
    public long RateLimitViolations { get; set; }
    public long ContentFilterViolations { get; set; }
    public Dictionary<string, long> ThreatBreakdown { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public long TotalEvents24h { get; set; }
    public Dictionary<string, int> EventTypeBreakdown { get; set; } = new();
    public Dictionary<string, int> SeverityBreakdown { get; set; } = new();
    public int CurrentlyBlockedUsers { get; set; }
    public long TotalViolations { get; set; }
}

/// <summary>
/// Configuration for enhancement features
/// </summary>
public class EnhancementConfiguration
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnableAuditing { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public bool EnableContentFiltering { get; set; } = true;
    public bool EnableCompression { get; set; } = true;
    public Dictionary<string, object> FeatureSettings { get; set; } = new();
}

/// <summary>
/// Enhancement health report
/// </summary>
public class EnhancementHealthReport
{
    public bool IsHealthy { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public Dictionary<string, bool> FeaturesEnabled { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object>? MetricsStats { get; set; }
    public Dictionary<string, object>? RateLimitStats { get; set; }
    public Dictionary<string, object>? ContentFilterStats { get; set; }
    public Dictionary<string, object>? CompressionStats { get; set; }
}


