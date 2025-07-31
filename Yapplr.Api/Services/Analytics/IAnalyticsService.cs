using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Services.Analytics;

/// <summary>
/// Service for recording and analyzing user activities and content engagement
/// </summary>
public interface IAnalyticsService
{
    // User Activity Tracking
    Task TrackUserActivityAsync(int userId, ActivityType activityType, 
        string? targetEntityType = null, int? targetEntityId = null, 
        string? metadata = null, string? sessionId = null, 
        int? durationMs = null, bool? success = null, string? errorMessage = null);
    
    Task<IEnumerable<UserActivity>> GetUserActivitiesAsync(int userId, 
        DateTime? fromDate = null, DateTime? toDate = null, 
        ActivityType? activityType = null, int limit = 100);
    
    Task<Dictionary<ActivityType, int>> GetUserActivitySummaryAsync(int userId, 
        DateTime? fromDate = null, DateTime? toDate = null);

    // Content Engagement Tracking
    Task TrackContentEngagementAsync(int userId, ContentType contentType, int contentId, 
        EngagementType engagementType, int? contentOwnerId = null, 
        string? source = null, string? metadata = null, string? sessionId = null, 
        int? durationMs = null, int? position = null);
    
    Task<IEnumerable<ContentEngagement>> GetContentEngagementsAsync(
        ContentType? contentType = null, int? contentId = null, 
        EngagementType? engagementType = null, DateTime? fromDate = null, 
        DateTime? toDate = null, int limit = 100);
    
    Task<Dictionary<EngagementType, int>> GetContentEngagementSummaryAsync(
        ContentType contentType, int contentId, DateTime? fromDate = null, DateTime? toDate = null);

    // Trust Score Management
    Task UpdateUserTrustScoreAsync(int userId, float scoreChange, 
        TrustScoreChangeReason reason, string? details = null, 
        string? relatedEntityType = null, int? relatedEntityId = null, 
        int? triggeredByUserId = null, string? calculatedBy = null, 
        bool isAutomatic = true, float? confidence = null);
    
    Task<IEnumerable<UserTrustScoreHistory>> GetUserTrustScoreHistoryAsync(int userId, 
        DateTime? fromDate = null, DateTime? toDate = null, int limit = 50);
    
    Task<float> GetCurrentUserTrustScoreAsync(int userId);

    // Tag Analytics
    Task TrackTagActionAsync(int tagId, TagAction action, int? userId = null, 
        string? relatedContentType = null, int? relatedContentId = null, 
        string? source = null, string? metadata = null, string? sessionId = null, 
        int? position = null, bool? wasSuggested = null);
    
    Task<IEnumerable<TagAnalytics>> GetTagAnalyticsAsync(int tagId, 
        DateTime? fromDate = null, DateTime? toDate = null, 
        TagAction? action = null, int limit = 100);
    
    Task<Dictionary<TagAction, int>> GetTagActionSummaryAsync(int tagId, 
        DateTime? fromDate = null, DateTime? toDate = null);

    // Performance Metrics
    Task RecordPerformanceMetricAsync(MetricType metricType, double value, string unit, 
        string source, string? operation = null, string? tags = null, 
        string? instanceId = null, string? environment = null, string? version = null, 
        bool? success = null, string? errorMessage = null, int? userId = null, 
        string? sessionId = null);
    
    Task<IEnumerable<PerformanceMetric>> GetPerformanceMetricsAsync(
        MetricType? metricType = null, string? source = null, 
        DateTime? fromDate = null, DateTime? toDate = null, int limit = 100);
    
    Task<Dictionary<string, double>> GetPerformanceMetricSummaryAsync(
        MetricType metricType, string? source = null, 
        DateTime? fromDate = null, DateTime? toDate = null);

    // Analytics Aggregations
    Task<Dictionary<string, object>> GetUserEngagementStatsAsync(
        DateTime? fromDate = null, DateTime? toDate = null);
    
    Task<Dictionary<string, object>> GetContentPerformanceStatsAsync(
        ContentType? contentType = null, DateTime? fromDate = null, DateTime? toDate = null);
    
    Task<Dictionary<string, object>> GetSystemPerformanceStatsAsync(
        DateTime? fromDate = null, DateTime? toDate = null);

    // Cleanup and Maintenance
    Task CleanupOldAnalyticsDataAsync(TimeSpan maxAge);
    Task<Dictionary<string, long>> GetAnalyticsStorageStatsAsync();
}
