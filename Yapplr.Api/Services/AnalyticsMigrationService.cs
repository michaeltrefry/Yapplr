using InfluxDB.Client;
using InfluxDB.Client.Writes;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Yapplr.Api.Data;
using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for migrating analytics data from PostgreSQL to InfluxDB
/// </summary>
public class AnalyticsMigrationService : IAnalyticsMigrationService
{
    private readonly YapplrDbContext _context;
    private readonly IInfluxDBClient _influxClient;
    private readonly ILogger<AnalyticsMigrationService> _logger;
    private readonly string _bucket;
    private readonly string _organization;
    private readonly bool _isEnabled;
    
    private static readonly object _migrationLock = new();
    private static MigrationStatus _currentStatus = new();

    public AnalyticsMigrationService(
        YapplrDbContext context,
        IInfluxDBClient influxClient,
        ILogger<AnalyticsMigrationService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _influxClient = influxClient;
        _logger = logger;
        _bucket = configuration.GetValue<string>("InfluxDB:Bucket", "analytics")!;
        _organization = configuration.GetValue<string>("InfluxDB:Organization", "yapplr")!;
        _isEnabled = configuration.GetValue<bool>("InfluxDB:Enabled", true);

        _logger.LogInformation("Analytics Migration Service initialized. Enabled: {IsEnabled}", _isEnabled);
    }

    public async Task<bool> IsInfluxDbAvailableAsync()
    {
        if (!_isEnabled) return false;

        try
        {
            var health = await _influxClient.HealthAsync();
            return health.Status == InfluxDB.Client.Api.Domain.HealthCheck.StatusEnum.Pass;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InfluxDB health check failed");
            return false;
        }
    }

    public async Task<MigrationResult> MigrateAllAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        if (!await IsInfluxDbAvailableAsync())
        {
            return new MigrationResult
            {
                Success = false,
                ErrorMessage = "InfluxDB is not available",
                TableName = "All"
            };
        }

        lock (_migrationLock)
        {
            if (_currentStatus.IsInProgress)
            {
                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = "Migration is already in progress",
                    TableName = "All"
                };
            }

            _currentStatus = new MigrationStatus
            {
                IsInProgress = true,
                TotalTables = 4,
                CompletedTables = 0,
                StartTime = DateTime.UtcNow
            };
        }

        var overallResult = new MigrationResult
        {
            Success = true,
            TableName = "All",
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting migration of all analytics data from {FromDate} to {ToDate}", 
                fromDate?.ToString("yyyy-MM-dd") ?? "beginning", 
                toDate?.ToString("yyyy-MM-dd") ?? "now");

            // Migrate each table
            var migrations = new[]
            {
                ("UserActivities", () => MigrateUserActivitiesAsync(fromDate, toDate, batchSize, cancellationToken)),
                ("ContentEngagements", () => MigrateContentEngagementsAsync(fromDate, toDate, batchSize, cancellationToken)),
                ("TagAnalytics", () => MigrateTagAnalyticsAsync(fromDate, toDate, batchSize, cancellationToken)),
                ("PerformanceMetrics", () => MigratePerformanceMetricsAsync(fromDate, toDate, batchSize, cancellationToken))
            };

            foreach (var (tableName, migrationFunc) in migrations)
            {
                _currentStatus.CurrentTable = tableName;
                
                var result = await migrationFunc();
                _currentStatus.CompletedMigrations.Add(result);
                _currentStatus.CompletedTables++;

                overallResult.TotalRecords += result.TotalRecords;
                overallResult.MigratedRecords += result.MigratedRecords;
                overallResult.FailedRecords += result.FailedRecords;
                overallResult.Errors.AddRange(result.Errors);

                if (!result.Success)
                {
                    overallResult.Success = false;
                    _logger.LogError("Migration failed for table {TableName}: {Error}", tableName, result.ErrorMessage);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Migration cancelled by user");
                    overallResult.Success = false;
                    overallResult.ErrorMessage = "Migration was cancelled";
                    break;
                }
            }

            overallResult.EndTime = DateTime.UtcNow;
            overallResult.Duration = overallResult.EndTime - overallResult.StartTime;

            _logger.LogInformation("Migration completed. Success: {Success}, Total: {Total}, Migrated: {Migrated}, Failed: {Failed}, Duration: {Duration}",
                overallResult.Success, overallResult.TotalRecords, overallResult.MigratedRecords, 
                overallResult.FailedRecords, overallResult.Duration);

            return overallResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed with exception");
            overallResult.Success = false;
            overallResult.ErrorMessage = ex.Message;
            overallResult.EndTime = DateTime.UtcNow;
            overallResult.Duration = overallResult.EndTime - overallResult.StartTime;
            return overallResult;
        }
        finally
        {
            lock (_migrationLock)
            {
                _currentStatus.IsInProgress = false;
                _currentStatus.CurrentTable = null;
            }
        }
    }

    public async Task<MigrationResult> MigrateUserActivitiesAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        return await MigrateTableAsync<UserActivity>(
            "UserActivities",
            query => ApplyDateFilter(query, fromDate, toDate, ua => ua.CreatedAt),
            ConvertUserActivityToInfluxPoint,
            batchSize,
            cancellationToken);
    }

    public async Task<MigrationResult> MigrateContentEngagementsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        return await MigrateTableAsync<ContentEngagement>(
            "ContentEngagements",
            query => ApplyDateFilter(query, fromDate, toDate, ce => ce.CreatedAt),
            ConvertContentEngagementToInfluxPoint,
            batchSize,
            cancellationToken);
    }

    public async Task<MigrationResult> MigrateTagAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        return await MigrateTableAsync<TagAnalytics>(
            "TagAnalytics",
            query => ApplyDateFilter(query, fromDate, toDate, ta => ta.CreatedAt),
            ConvertTagAnalyticsToInfluxPoint,
            batchSize,
            cancellationToken);
    }

    public async Task<MigrationResult> MigratePerformanceMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        return await MigrateTableAsync<PerformanceMetric>(
            "PerformanceMetrics",
            query => ApplyDateFilter(query, fromDate, toDate, pm => pm.CreatedAt),
            ConvertPerformanceMetricToInfluxPoint,
            batchSize,
            cancellationToken);
    }

    public Task<MigrationStatus> GetMigrationStatusAsync()
    {
        lock (_migrationLock)
        {
            return Task.FromResult(_currentStatus);
        }
    }

    public async Task<ValidationResult> ValidateMigrationAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var validations = new List<TableValidation>();

            // Validate UserActivities
            var userActivitiesCount = await GetPostgreSqlCountAsync<UserActivity>(fromDate, toDate, ua => ua.CreatedAt);
            var userActivitiesInfluxCount = await GetInfluxDbCountAsync("user_activities", fromDate, toDate);
            validations.Add(new TableValidation
            {
                TableName = "UserActivities",
                PostgreSqlCount = userActivitiesCount,
                InfluxDbCount = userActivitiesInfluxCount
            });

            // Validate ContentEngagements
            var contentEngagementsCount = await GetPostgreSqlCountAsync<ContentEngagement>(fromDate, toDate, ce => ce.CreatedAt);
            var contentEngagementsInfluxCount = await GetInfluxDbCountAsync("content_engagement", fromDate, toDate);
            validations.Add(new TableValidation
            {
                TableName = "ContentEngagements",
                PostgreSqlCount = contentEngagementsCount,
                InfluxDbCount = contentEngagementsInfluxCount
            });

            // Validate TagAnalytics
            var tagAnalyticsCount = await GetPostgreSqlCountAsync<TagAnalytics>(fromDate, toDate, ta => ta.CreatedAt);
            var tagAnalyticsInfluxCount = await GetInfluxDbCountAsync("tag_actions", fromDate, toDate);
            validations.Add(new TableValidation
            {
                TableName = "TagAnalytics",
                PostgreSqlCount = tagAnalyticsCount,
                InfluxDbCount = tagAnalyticsInfluxCount
            });

            // Validate PerformanceMetrics
            var performanceMetricsCount = await GetPostgreSqlCountAsync<PerformanceMetric>(fromDate, toDate, pm => pm.CreatedAt);
            var performanceMetricsInfluxCount = await GetInfluxDbCountAsync("performance_metrics", fromDate, toDate);
            validations.Add(new TableValidation
            {
                TableName = "PerformanceMetrics",
                PostgreSqlCount = performanceMetricsCount,
                InfluxDbCount = performanceMetricsInfluxCount
            });

            var isValid = validations.All(v => v.IsValid);

            return new ValidationResult
            {
                IsValid = isValid,
                TableValidations = validations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<MigrationResult> MigrateTableAsync<T>(
        string tableName,
        Func<IQueryable<T>, IQueryable<T>> queryFilter,
        Func<T, PointData> converter,
        int batchSize,
        CancellationToken cancellationToken) where T : class
    {
        var result = new MigrationResult
        {
            TableName = tableName,
            StartTime = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting migration of {TableName}", tableName);

            var query = _context.Set<T>().AsNoTracking();
            query = queryFilter(query);

            var totalCount = await query.CountAsync(cancellationToken);
            result.TotalRecords = totalCount;

            _logger.LogInformation("Found {TotalCount} records to migrate from {TableName}", totalCount, tableName);

            if (totalCount == 0)
            {
                result.Success = true;
                result.EndTime = DateTime.UtcNow;
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            var writeApi = _influxClient.GetWriteApiAsync();
            var processed = 0;

            while (processed < totalCount && !cancellationToken.IsCancellationRequested)
            {
                var batch = await query
                    .Skip(processed)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!batch.Any()) break;

                try
                {
                    var points = batch.Select(converter).ToList();
                    await writeApi.WritePointsAsync(points, _bucket, _organization, cancellationToken);
                    
                    result.MigratedRecords += batch.Count;
                    processed += batch.Count;

                    _logger.LogDebug("Migrated batch of {BatchSize} records from {TableName}. Progress: {Processed}/{Total}",
                        batch.Count, tableName, processed, totalCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to migrate batch from {TableName} at offset {Offset}", tableName, processed);
                    result.FailedRecords += batch.Count;
                    result.Errors.Add($"Batch at offset {processed}: {ex.Message}");
                    processed += batch.Count; // Skip this batch and continue
                }
            }

            result.Success = result.FailedRecords == 0 && !cancellationToken.IsCancellationRequested;
            
            if (cancellationToken.IsCancellationRequested)
            {
                result.ErrorMessage = "Migration was cancelled";
            }

            _logger.LogInformation("Completed migration of {TableName}. Success: {Success}, Migrated: {Migrated}, Failed: {Failed}",
                tableName, result.Success, result.MigratedRecords, result.FailedRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration of {TableName} failed", tableName);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.EndTime = DateTime.UtcNow;
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    private static IQueryable<T> ApplyDateFilter<T>(IQueryable<T> query, DateTime? fromDate, DateTime? toDate, System.Linq.Expressions.Expression<Func<T, DateTime>> dateSelector)
    {
        if (fromDate.HasValue)
        {
            var fromDateUtc = fromDate.Value.ToUniversalTime();
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.GreaterThanOrEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(fromDateUtc)),
                dateSelector.Parameters));
        }

        if (toDate.HasValue)
        {
            var toDateUtc = toDate.Value.ToUniversalTime();
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.LessThanOrEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(toDateUtc)),
                dateSelector.Parameters));
        }

        return query;
    }

    private async Task<int> GetPostgreSqlCountAsync<T>(DateTime? fromDate, DateTime? toDate, System.Linq.Expressions.Expression<Func<T, DateTime>> dateSelector) where T : class
    {
        var query = _context.Set<T>().AsNoTracking();
        query = ApplyDateFilter(query, fromDate, toDate, dateSelector);
        return await query.CountAsync();
    }

    private async Task<int> GetInfluxDbCountAsync(string measurement, DateTime? fromDate, DateTime? toDate)
    {
        try
        {
            var startTime = fromDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "1970-01-01T00:00:00Z";
            var endTime = toDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "now()";

            var query = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: {startTime}, stop: {endTime})
                  |> filter(fn: (r) => r._measurement == ""{measurement}"")
                  |> filter(fn: (r) => r._field == ""count"")
                  |> count()";

            var queryApi = _influxClient.GetQueryApi();
            var tables = await queryApi.QueryAsync(query, _organization);

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    return Convert.ToInt32(record.GetValue());
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get InfluxDB count for measurement {Measurement}", measurement);
            return 0;
        }
    }

    private static PointData ConvertUserActivityToInfluxPoint(UserActivity activity)
    {
        var point = PointData
            .Measurement("user_activities")
            .Tag("user_id", activity.UserId.ToString())
            .Tag("activity_type", activity.ActivityType.ToString())
            .Field("count", 1)
            .Timestamp(activity.CreatedAt, InfluxDB.Client.Api.Domain.WritePrecision.Ns);

        if (!string.IsNullOrEmpty(activity.TargetEntityType))
            point = point.Tag("target_entity_type", activity.TargetEntityType);

        if (activity.TargetEntityId.HasValue)
            point = point.Field("target_entity_id", activity.TargetEntityId.Value);

        if (!string.IsNullOrEmpty(activity.SessionId))
            point = point.Tag("session_id", activity.SessionId);

        if (activity.DurationMs.HasValue)
            point = point.Field("duration_ms", activity.DurationMs.Value);

        if (activity.Success.HasValue)
            point = point.Field("success", activity.Success.Value);

        if (!string.IsNullOrEmpty(activity.ErrorMessage))
            point = point.Field("error_message", activity.ErrorMessage);

        if (!string.IsNullOrEmpty(activity.Metadata))
            point = point.Field("metadata", activity.Metadata);

        if (!string.IsNullOrEmpty(activity.IpAddress))
            point = point.Tag("ip_address", activity.IpAddress);

        if (!string.IsNullOrEmpty(activity.UserAgent))
            point = point.Field("user_agent", activity.UserAgent);

        return point;
    }

    private static PointData ConvertContentEngagementToInfluxPoint(ContentEngagement engagement)
    {
        var point = PointData
            .Measurement("content_engagement")
            .Tag("user_id", engagement.UserId.ToString())
            .Tag("content_type", engagement.ContentType.ToString())
            .Tag("engagement_type", engagement.EngagementType.ToString())
            .Field("content_id", engagement.ContentId)
            .Field("count", 1)
            .Timestamp(engagement.CreatedAt, InfluxDB.Client.Api.Domain.WritePrecision.Ns);

        if (engagement.ContentOwnerId.HasValue)
            point = point.Field("content_owner_id", engagement.ContentOwnerId.Value);

        if (!string.IsNullOrEmpty(engagement.Source))
            point = point.Tag("source", engagement.Source);

        if (!string.IsNullOrEmpty(engagement.SessionId))
            point = point.Tag("session_id", engagement.SessionId);

        if (engagement.DurationMs.HasValue)
            point = point.Field("duration_ms", engagement.DurationMs.Value);

        if (engagement.Position.HasValue)
            point = point.Field("position", engagement.Position.Value);

        if (!string.IsNullOrEmpty(engagement.Metadata))
            point = point.Field("metadata", engagement.Metadata);

        if (!string.IsNullOrEmpty(engagement.DeviceType))
            point = point.Tag("device_type", engagement.DeviceType);

        if (!string.IsNullOrEmpty(engagement.Platform))
            point = point.Tag("platform", engagement.Platform);

        return point;
    }

    private static PointData ConvertTagAnalyticsToInfluxPoint(TagAnalytics tagAnalytics)
    {
        var point = PointData
            .Measurement("tag_actions")
            .Tag("tag_id", tagAnalytics.TagId.ToString())
            .Tag("action", tagAnalytics.Action.ToString())
            .Field("count", 1)
            .Timestamp(tagAnalytics.CreatedAt, InfluxDB.Client.Api.Domain.WritePrecision.Ns);

        if (tagAnalytics.UserId.HasValue)
            point = point.Tag("user_id", tagAnalytics.UserId.Value.ToString());

        if (!string.IsNullOrEmpty(tagAnalytics.RelatedContentType))
            point = point.Tag("related_content_type", tagAnalytics.RelatedContentType);

        if (tagAnalytics.RelatedContentId.HasValue)
            point = point.Field("related_content_id", tagAnalytics.RelatedContentId.Value);

        if (!string.IsNullOrEmpty(tagAnalytics.Source))
            point = point.Tag("source", tagAnalytics.Source);

        if (!string.IsNullOrEmpty(tagAnalytics.SessionId))
            point = point.Tag("session_id", tagAnalytics.SessionId);

        if (tagAnalytics.Position.HasValue)
            point = point.Field("position", tagAnalytics.Position.Value);

        if (tagAnalytics.WasSuggested.HasValue)
            point = point.Field("was_suggested", tagAnalytics.WasSuggested.Value);

        if (!string.IsNullOrEmpty(tagAnalytics.Metadata))
            point = point.Field("metadata", tagAnalytics.Metadata);

        if (!string.IsNullOrEmpty(tagAnalytics.DeviceType))
            point = point.Tag("device_type", tagAnalytics.DeviceType);

        if (!string.IsNullOrEmpty(tagAnalytics.Platform))
            point = point.Tag("platform", tagAnalytics.Platform);

        return point;
    }

    private static PointData ConvertPerformanceMetricToInfluxPoint(PerformanceMetric metric)
    {
        var point = PointData
            .Measurement("performance_metrics")
            .Tag("metric_type", metric.MetricType.ToString())
            .Tag("source", metric.Source)
            .Tag("unit", metric.Unit)
            .Field("value", metric.Value)
            .Timestamp(metric.CreatedAt, InfluxDB.Client.Api.Domain.WritePrecision.Ns);

        if (!string.IsNullOrEmpty(metric.Operation))
            point = point.Tag("operation", metric.Operation);

        if (!string.IsNullOrEmpty(metric.InstanceId))
            point = point.Tag("instance_id", metric.InstanceId);

        if (!string.IsNullOrEmpty(metric.Environment))
            point = point.Tag("environment", metric.Environment);

        if (!string.IsNullOrEmpty(metric.Version))
            point = point.Tag("version", metric.Version);

        if (metric.Success.HasValue)
            point = point.Field("success", metric.Success.Value);

        if (!string.IsNullOrEmpty(metric.ErrorMessage))
            point = point.Field("error_message", metric.ErrorMessage);

        if (metric.UserId.HasValue)
            point = point.Tag("user_id", metric.UserId.Value.ToString());

        if (!string.IsNullOrEmpty(metric.SessionId))
            point = point.Tag("session_id", metric.SessionId);

        if (!string.IsNullOrEmpty(metric.Tags))
            point = point.Field("tags", metric.Tags);

        return point;
    }
}
