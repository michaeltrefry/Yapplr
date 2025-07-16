using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Yapplr.Api.Models.Analytics;
using System.Text.Json;

namespace Yapplr.Api.Services;

/// <summary>
/// InfluxDB implementation of external analytics service
/// Sends analytics data to InfluxDB instead of the main application database
/// </summary>
public class InfluxAnalyticsService : IExternalAnalyticsService
{
    private readonly IInfluxDBClient _influxClient;
    private readonly ILogger<InfluxAnalyticsService> _logger;
    private readonly string _bucket;
    private readonly string _organization;
    private readonly bool _isEnabled;

    public InfluxAnalyticsService(
        IInfluxDBClient influxClient,
        ILogger<InfluxAnalyticsService> logger,
        IConfiguration configuration)
    {
        _influxClient = influxClient;
        _logger = logger;
        _bucket = configuration.GetValue<string>("InfluxDB:Bucket", "analytics")!;
        _organization = configuration.GetValue<string>("InfluxDB:Organization", "yapplr")!;
        _isEnabled = configuration.GetValue<bool>("InfluxDB:Enabled", true);

        _logger.LogInformation("InfluxDB Analytics Service initialized. Enabled: {IsEnabled}, Bucket: {Bucket}, Org: {Organization}", 
            _isEnabled, _bucket, _organization);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (!_isEnabled) return false;

        try
        {
            var health = await _influxClient.HealthAsync(cancellationToken);
            return health.Status == HealthCheck.StatusEnum.Pass;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InfluxDB health check failed");
            return false;
        }
    }

    public async Task TrackEventAsync(string eventType, Dictionary<string, object> properties, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled) return;

        try
        {
            var point = PointData
                .Measurement("events")
                .Tag("event_type", eventType)
                .Field("count", 1)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            // Add properties as tags (strings) or fields (numbers/booleans)
            foreach (var prop in properties)
            {
                switch (prop.Value)
                {
                    case string stringValue:
                        point = point.Tag(prop.Key, stringValue);
                        break;
                    case int intValue:
                        point = point.Field(prop.Key, intValue);
                        break;
                    case double doubleValue:
                        point = point.Field(prop.Key, doubleValue);
                        break;
                    case bool boolValue:
                        point = point.Field(prop.Key, boolValue);
                        break;
                    case DateTime dateValue:
                        point = point.Field(prop.Key, dateValue.ToString("O"));
                        break;
                    default:
                        point = point.Tag(prop.Key, prop.Value?.ToString() ?? "null");
                        break;
                }
            }

            await WritePointAsync(point, cancellationToken);
            _logger.LogDebug("Tracked event: {EventType} with {PropertyCount} properties", eventType, properties.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track event: {EventType}", eventType);
        }
    }

    public async Task TrackMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled) return;

        try
        {
            var point = PointData
                .Measurement("metrics")
                .Tag("metric_name", metricName)
                .Field("value", value)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    point = point.Tag(tag.Key, tag.Value);
                }
            }

            await WritePointAsync(point, cancellationToken);
            _logger.LogDebug("Tracked metric: {MetricName} = {Value}", metricName, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track metric: {MetricName}", metricName);
        }
    }

    public async Task TrackUserActivityAsync(int userId, ActivityType activityType, 
        string? targetEntityType = null, int? targetEntityId = null, 
        string? metadata = null, string? sessionId = null, 
        int? durationMs = null, bool? success = null, string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled) return;

        try
        {
            var point = PointData
                .Measurement("user_activities")
                .Tag("user_id", userId.ToString())
                .Tag("activity_type", activityType.ToString())
                .Field("count", 1)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            if (!string.IsNullOrEmpty(targetEntityType))
                point = point.Tag("target_entity_type", targetEntityType);
            
            if (targetEntityId.HasValue)
                point = point.Field("target_entity_id", targetEntityId.Value);
            
            if (!string.IsNullOrEmpty(sessionId))
                point = point.Tag("session_id", sessionId);
            
            if (durationMs.HasValue)
                point = point.Field("duration_ms", durationMs.Value);
            
            if (success.HasValue)
                point = point.Field("success", success.Value);
            
            if (!string.IsNullOrEmpty(errorMessage))
                point = point.Field("error_message", errorMessage);
            
            if (!string.IsNullOrEmpty(metadata))
                point = point.Field("metadata", metadata);

            await WritePointAsync(point, cancellationToken);
            _logger.LogDebug("Tracked user activity: User {UserId} performed {ActivityType}", userId, activityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track user activity: User {UserId}, Activity {ActivityType}", userId, activityType);
        }
    }

    public async Task TrackContentEngagementAsync(int userId, ContentType contentType, int contentId, 
        EngagementType engagementType, int? contentOwnerId = null, 
        string? source = null, string? metadata = null, string? sessionId = null, 
        int? durationMs = null, int? position = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled) return;

        try
        {
            var point = PointData
                .Measurement("content_engagement")
                .Tag("user_id", userId.ToString())
                .Tag("content_type", contentType.ToString())
                .Tag("engagement_type", engagementType.ToString())
                .Field("content_id", contentId)
                .Field("count", 1)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            if (contentOwnerId.HasValue)
                point = point.Field("content_owner_id", contentOwnerId.Value);
            
            if (!string.IsNullOrEmpty(source))
                point = point.Tag("source", source);
            
            if (!string.IsNullOrEmpty(sessionId))
                point = point.Tag("session_id", sessionId);
            
            if (durationMs.HasValue)
                point = point.Field("duration_ms", durationMs.Value);
            
            if (position.HasValue)
                point = point.Field("position", position.Value);
            
            if (!string.IsNullOrEmpty(metadata))
                point = point.Field("metadata", metadata);

            await WritePointAsync(point, cancellationToken);
            _logger.LogDebug("Tracked content engagement: User {UserId} {EngagementType} {ContentType} {ContentId}", 
                userId, engagementType, contentType, contentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track content engagement: User {UserId}, Content {ContentId}", userId, contentId);
        }
    }

    public async Task TrackTagActionAsync(int tagId, TagAction action, int? userId = null,
        string? relatedContentType = null, int? relatedContentId = null,
        string? source = null, string? metadata = null, string? sessionId = null,
        int? position = null, bool? wasSuggested = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled) return;

        try
        {
            var point = PointData
                .Measurement("tag_actions")
                .Tag("tag_id", tagId.ToString())
                .Tag("action", action.ToString())
                .Field("count", 1)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            if (userId.HasValue)
                point = point.Tag("user_id", userId.Value.ToString());
            
            if (!string.IsNullOrEmpty(relatedContentType))
                point = point.Tag("related_content_type", relatedContentType);
            
            if (relatedContentId.HasValue)
                point = point.Field("related_content_id", relatedContentId.Value);
            
            if (!string.IsNullOrEmpty(source))
                point = point.Tag("source", source);
            
            if (!string.IsNullOrEmpty(sessionId))
                point = point.Tag("session_id", sessionId);
            
            if (position.HasValue)
                point = point.Field("position", position.Value);
            
            if (wasSuggested.HasValue)
                point = point.Field("was_suggested", wasSuggested.Value);
            
            if (!string.IsNullOrEmpty(metadata))
                point = point.Field("metadata", metadata);

            await WritePointAsync(point, cancellationToken);
            _logger.LogDebug("Tracked tag action: Tag {TagId} {Action}", tagId, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track tag action: Tag {TagId}, Action {Action}", tagId, action);
        }
    }

    public async Task RecordPerformanceMetricAsync(MetricType metricType, double value, string unit,
        string source, string? operation = null, string? tags = null,
        string? instanceId = null, string? environment = null, string? version = null,
        bool? success = null, string? errorMessage = null, int? userId = null,
        string? sessionId = null, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled) return;

        try
        {
            var point = PointData
                .Measurement("performance_metrics")
                .Tag("metric_type", metricType.ToString())
                .Tag("source", source)
                .Tag("unit", unit)
                .Field("value", value)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            if (!string.IsNullOrEmpty(operation))
                point = point.Tag("operation", operation);
            
            if (!string.IsNullOrEmpty(instanceId))
                point = point.Tag("instance_id", instanceId);
            
            if (!string.IsNullOrEmpty(environment))
                point = point.Tag("environment", environment);
            
            if (!string.IsNullOrEmpty(version))
                point = point.Tag("version", version);
            
            if (success.HasValue)
                point = point.Field("success", success.Value);
            
            if (!string.IsNullOrEmpty(errorMessage))
                point = point.Field("error_message", errorMessage);
            
            if (userId.HasValue)
                point = point.Tag("user_id", userId.Value.ToString());
            
            if (!string.IsNullOrEmpty(sessionId))
                point = point.Tag("session_id", sessionId);
            
            if (!string.IsNullOrEmpty(tags))
                point = point.Field("tags", tags);

            await WritePointAsync(point, cancellationToken);
            _logger.LogDebug("Recorded performance metric: {MetricType} = {Value} {Unit} from {Source}", 
                metricType, value, unit, source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record performance metric: {MetricType} from {Source}", metricType, source);
        }
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (!_isEnabled) return;

        try
        {
            // InfluxDB client handles batching automatically, but we can force a flush
            await Task.CompletedTask; // No explicit flush needed for InfluxDB client
            _logger.LogDebug("Flushed analytics data to InfluxDB");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush analytics data");
        }
    }

    private async Task WritePointAsync(PointData point, CancellationToken cancellationToken = default)
    {
        var writeApi = _influxClient.GetWriteApiAsync();
        await writeApi.WritePointAsync(point, _bucket, _organization, cancellationToken);
    }
}
