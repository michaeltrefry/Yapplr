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

            // Register InfluxDB admin analytics service
            services.AddScoped<IInfluxAdminAnalyticsService, InfluxAdminAnalyticsService>();

            // Register analytics migration service
            services.AddScoped<IAnalyticsMigrationService, AnalyticsMigrationService>();
        }
        else
        {
            // Register a no-op analytics service if InfluxDB is disabled
            services.AddScoped<IExternalAnalyticsService, NoOpAnalyticsService>();

            // Register a no-op admin analytics service
            services.AddScoped<IInfluxAdminAnalyticsService, NoOpInfluxAdminAnalyticsService>();

            // Register a no-op migration service
            services.AddScoped<IAnalyticsMigrationService, NoOpAnalyticsMigrationService>();
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

/// <summary>
/// No-operation InfluxDB admin analytics service for when InfluxDB is disabled
/// </summary>
public class NoOpInfluxAdminAnalyticsService : IInfluxAdminAnalyticsService
{
    private readonly ILogger<NoOpInfluxAdminAnalyticsService> _logger;

    public NoOpInfluxAdminAnalyticsService(ILogger<NoOpInfluxAdminAnalyticsService> logger)
    {
        _logger = logger;
        _logger.LogInformation("No-op InfluxDB admin analytics service initialized - InfluxDB disabled");
    }

    public Task<bool> IsAvailableAsync() => Task.FromResult(false);

    public Task<UserGrowthStatsDto> GetUserGrowthStatsAsync(int days = 30)
    {
        _logger.LogDebug("No-op: Would get user growth stats from InfluxDB");
        return Task.FromResult(new UserGrowthStatsDto
        {
            TotalNewUsers = 0,
            TotalActiveUsers = 0,
            GrowthRate = 0,
            DailyStats = new List<DailyStatsDto>()
        });
    }

    public Task<ContentStatsDto> GetContentStatsAsync(int days = 30)
    {
        _logger.LogDebug("No-op: Would get content stats from InfluxDB");
        return Task.FromResult(new ContentStatsDto
        {
            TotalPosts = 0,
            TotalComments = 0,
            AveragePostsPerDay = 0,
            AverageCommentsPerDay = 0,
            DailyPosts = new List<DailyStatsDto>(),
            DailyComments = new List<DailyStatsDto>()
        });
    }

    public Task<ModerationTrendsDto> GetModerationTrendsAsync(int days = 30)
    {
        _logger.LogDebug("No-op: Would get moderation trends from InfluxDB");
        return Task.FromResult(new ModerationTrendsDto
        {
            TotalActions = 0,
            DailyActions = new List<DailyStatsDto>(),
            ActionBreakdown = new List<ActionBreakdownDto>()
        });
    }

    public Task<SystemHealthDto> GetSystemHealthAsync()
    {
        _logger.LogDebug("No-op: Would get system health from InfluxDB");
        return Task.FromResult(new SystemHealthDto
        {
            IsHealthy = false,
            AverageResponseTime = 0,
            ActiveConnections = 0,
            QueueDepth = 0,
            ErrorRate = 0,
            LastChecked = DateTime.UtcNow
        });
    }

    public Task<TopModeratorsDto> GetTopModeratorsAsync(int days = 30, int limit = 10)
    {
        _logger.LogDebug("No-op: Would get top moderators from InfluxDB");
        return Task.FromResult(new TopModeratorsDto
        {
            Moderators = new List<ModeratorStatsDto>()
        });
    }

    public Task<ContentTrendsDto> GetContentTrendsAsync(int days = 30)
    {
        _logger.LogDebug("No-op: Would get content trends from InfluxDB");
        return Task.FromResult(new ContentTrendsDto
        {
            Trends = new List<ContentTrendDto>()
        });
    }

    public Task<UserEngagementStatsDto> GetUserEngagementStatsAsync(int days = 30)
    {
        _logger.LogDebug("No-op: Would get user engagement stats from InfluxDB");
        return Task.FromResult(new UserEngagementStatsDto
        {
            TotalEngagements = 0,
            AverageEngagementsPerDay = 0,
            DailyEngagement = new List<DailyStatsDto>()
        });
    }
}

/// <summary>
/// No-operation analytics migration service for when InfluxDB is disabled
/// </summary>
public class NoOpAnalyticsMigrationService : IAnalyticsMigrationService
{
    private readonly ILogger<NoOpAnalyticsMigrationService> _logger;

    public NoOpAnalyticsMigrationService(ILogger<NoOpAnalyticsMigrationService> logger)
    {
        _logger = logger;
        _logger.LogInformation("No-op analytics migration service initialized - InfluxDB disabled");
    }

    public Task<bool> IsInfluxDbAvailableAsync() => Task.FromResult(false);

    public Task<MigrationResult> MigrateAllAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would migrate all analytics data");
        return Task.FromResult(new MigrationResult
        {
            Success = false,
            TableName = "All",
            ErrorMessage = "InfluxDB is not enabled"
        });
    }

    public Task<MigrationResult> MigrateUserActivitiesAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would migrate user activities");
        return Task.FromResult(new MigrationResult
        {
            Success = false,
            TableName = "UserActivities",
            ErrorMessage = "InfluxDB is not enabled"
        });
    }

    public Task<MigrationResult> MigrateContentEngagementsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would migrate content engagements");
        return Task.FromResult(new MigrationResult
        {
            Success = false,
            TableName = "ContentEngagements",
            ErrorMessage = "InfluxDB is not enabled"
        });
    }

    public Task<MigrationResult> MigrateTagAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would migrate tag analytics");
        return Task.FromResult(new MigrationResult
        {
            Success = false,
            TableName = "TagAnalytics",
            ErrorMessage = "InfluxDB is not enabled"
        });
    }

    public Task<MigrationResult> MigratePerformanceMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("No-op: Would migrate performance metrics");
        return Task.FromResult(new MigrationResult
        {
            Success = false,
            TableName = "PerformanceMetrics",
            ErrorMessage = "InfluxDB is not enabled"
        });
    }

    public Task<MigrationStatus> GetMigrationStatusAsync()
    {
        return Task.FromResult(new MigrationStatus
        {
            IsInProgress = false
        });
    }

    public Task<ValidationResult> ValidateMigrationAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        return Task.FromResult(new ValidationResult
        {
            IsValid = false,
            ErrorMessage = "InfluxDB is not enabled"
        });
    }
}
