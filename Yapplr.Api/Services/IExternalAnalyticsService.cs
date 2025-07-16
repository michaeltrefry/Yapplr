using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for sending analytics data to external systems (InfluxDB, etc.)
/// This allows disconnecting analytics from the main application database
/// </summary>
public interface IExternalAnalyticsService
{
    /// <summary>
    /// Track a generic event with custom properties
    /// </summary>
    Task TrackEventAsync(string eventType, Dictionary<string, object> properties, CancellationToken cancellationToken = default);

    /// <summary>
    /// Track a numeric metric with tags
    /// </summary>
    Task TrackMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Track user activity (equivalent to current UserActivity tracking)
    /// </summary>
    Task TrackUserActivityAsync(int userId, ActivityType activityType, 
        string? targetEntityType = null, int? targetEntityId = null, 
        string? metadata = null, string? sessionId = null, 
        int? durationMs = null, bool? success = null, string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Track content engagement (equivalent to current ContentEngagement tracking)
    /// </summary>
    Task TrackContentEngagementAsync(int userId, ContentType contentType, int contentId, 
        EngagementType engagementType, int? contentOwnerId = null, 
        string? source = null, string? metadata = null, string? sessionId = null, 
        int? durationMs = null, int? position = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Track tag actions (equivalent to current TagAnalytics tracking)
    /// </summary>
    Task TrackTagActionAsync(int tagId, TagAction action, int? userId = null,
        string? relatedContentType = null, int? relatedContentId = null,
        string? source = null, string? metadata = null, string? sessionId = null,
        int? position = null, bool? wasSuggested = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record performance metrics (equivalent to current PerformanceMetric tracking)
    /// </summary>
    Task RecordPerformanceMetricAsync(MetricType metricType, double value, string unit,
        string source, string? operation = null, string? tags = null,
        string? instanceId = null, string? environment = null, string? version = null,
        bool? success = null, string? errorMessage = null, int? userId = null,
        string? sessionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the external analytics service is available
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Flush any pending analytics data
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
