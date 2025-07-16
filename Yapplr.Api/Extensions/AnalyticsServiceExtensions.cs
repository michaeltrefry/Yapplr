using InfluxDB.Client;
using Prometheus;
using Yapplr.Api.Services;

namespace Yapplr.Api.Extensions;

/// <summary>
/// Extension methods for configuring analytics services
/// </summary>
public static class AnalyticsServiceExtensions
{
    /// <summary>
    /// Add external analytics services to the DI container
    /// </summary>
    public static IServiceCollection AddExternalAnalytics(this IServiceCollection services, IConfiguration configuration)
    {
        // InfluxDB configuration
        var influxUrl = configuration.GetValue<string>("InfluxDB:Url", "http://influxdb:8086");
        var influxToken = configuration.GetValue<string>("InfluxDB:Token", "yapplr-analytics-token-local-dev-only");
        var influxEnabled = configuration.GetValue<bool>("InfluxDB:Enabled", true);

        if (influxEnabled && !string.IsNullOrEmpty(influxUrl) && !string.IsNullOrEmpty(influxToken))
        {
            // Register InfluxDB client
            services.AddSingleton<IInfluxDBClient>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<InfluxDBClient>>();
                try
                {
                    var client = new InfluxDBClient(influxUrl, influxToken);
                    logger.LogInformation("InfluxDB client configured for URL: {Url}", influxUrl);
                    return client;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create InfluxDB client for URL: {Url}", influxUrl);
                    throw;
                }
            });

            // Register InfluxDB analytics service
            services.AddScoped<IExternalAnalyticsService, InfluxAnalyticsService>();
        }
        else
        {
            // Register a no-op analytics service if InfluxDB is disabled
            services.AddScoped<IExternalAnalyticsService, NoOpAnalyticsService>();
        }

        return services;
    }

    /// <summary>
    /// Add Prometheus metrics collection to the DI container
    /// </summary>
    public static IServiceCollection AddPrometheusMetrics(this IServiceCollection services)
    {
        // Configure Prometheus metrics
        services.AddSingleton(provider =>
        {
            // Create custom metrics for Yapplr
            var userActivityCounter = Metrics.CreateCounter(
                "yapplr_user_activities_total",
                "Total number of user activities",
                new[] { "activity_type", "user_id" });

            var contentEngagementCounter = Metrics.CreateCounter(
                "yapplr_content_engagements_total",
                "Total number of content engagements",
                new[] { "content_type", "engagement_type" });

            var performanceHistogram = Metrics.CreateHistogram(
                "yapplr_request_duration_seconds",
                "Request duration in seconds",
                new[] { "method", "endpoint" });

            var errorCounter = Metrics.CreateCounter(
                "yapplr_errors_total",
                "Total number of errors",
                new[] { "error_type", "endpoint" });

            return new PrometheusMetrics
            {
                UserActivityCounter = userActivityCounter,
                ContentEngagementCounter = contentEngagementCounter,
                PerformanceHistogram = performanceHistogram,
                ErrorCounter = errorCounter
            };
        });

        return services;
    }
}

/// <summary>
/// Container for Prometheus metrics
/// </summary>
public class PrometheusMetrics
{
    public Counter UserActivityCounter { get; set; } = null!;
    public Counter ContentEngagementCounter { get; set; } = null!;
    public Histogram PerformanceHistogram { get; set; } = null!;
    public Counter ErrorCounter { get; set; } = null!;
}

/// <summary>
/// No-operation analytics service for when external analytics is disabled
/// </summary>
public class NoOpAnalyticsService : IExternalAnalyticsService
{
    private readonly ILogger<NoOpAnalyticsService> _logger;

    public NoOpAnalyticsService(ILogger<NoOpAnalyticsService> logger)
    {
        _logger = logger;
        _logger.LogInformation("No-op analytics service initialized - external analytics disabled");
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task TrackEventAsync(string eventType, Dictionary<string, object> properties, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would track event {EventType} with {PropertyCount} properties", eventType, properties.Count);
        return Task.CompletedTask;
    }

    public Task TrackMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would track metric {MetricName} = {Value}", metricName, value);
        return Task.CompletedTask;
    }

    public Task TrackUserActivityAsync(int userId, Models.Analytics.ActivityType activityType, string? targetEntityType = null, int? targetEntityId = null, string? metadata = null, string? sessionId = null, int? durationMs = null, bool? success = null, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would track user activity for user {UserId}: {ActivityType}", userId, activityType);
        return Task.CompletedTask;
    }

    public Task TrackContentEngagementAsync(int userId, Models.Analytics.ContentType contentType, int contentId, Models.Analytics.EngagementType engagementType, int? contentOwnerId = null, string? source = null, string? metadata = null, string? sessionId = null, int? durationMs = null, int? position = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would track content engagement for user {UserId}: {EngagementType} on {ContentType} {ContentId}", userId, engagementType, contentType, contentId);
        return Task.CompletedTask;
    }

    public Task TrackTagActionAsync(int tagId, Models.Analytics.TagAction action, int? userId = null, string? relatedContentType = null, int? relatedContentId = null, string? source = null, string? metadata = null, string? sessionId = null, int? position = null, bool? wasSuggested = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would track tag action for tag {TagId}: {Action}", tagId, action);
        return Task.CompletedTask;
    }

    public Task RecordPerformanceMetricAsync(Models.Analytics.MetricType metricType, double value, string unit, string source, string? operation = null, string? tags = null, string? instanceId = null, string? environment = null, string? version = null, bool? success = null, string? errorMessage = null, int? userId = null, string? sessionId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would record performance metric {MetricType} = {Value} {Unit} from {Source}", metricType, value, unit, source);
        return Task.CompletedTask;
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would flush analytics data");
        return Task.CompletedTask;
    }
}
