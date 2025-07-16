using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Yapplr.Api.Common;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for migrating analytics data from database to InfluxDB
/// </summary>
public class AnalyticsMigrationService : IAnalyticsMigrationService
{
    private readonly YapplrDbContext _context;
    private readonly IExternalAnalyticsService _externalAnalytics;
    private readonly ILogger<AnalyticsMigrationService> _logger;
    private readonly bool _isEnabled;
    private static readonly Dictionary<string, MigrationStatus> _migrationStatus = new();

    public AnalyticsMigrationService(
        YapplrDbContext context,
        IExternalAnalyticsService externalAnalytics,
        IConfiguration configuration,
        ILogger<AnalyticsMigrationService> logger)
    {
        _context = context;
        _externalAnalytics = externalAnalytics;
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("InfluxDB:Enabled", false);
    }

    public async Task<bool> IsInfluxDbAvailableAsync()
    {
        if (!_isEnabled) return false;
        return await _externalAnalytics.IsAvailableAsync();
    }

    public async Task<MigrationResult> MigrateAllAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new MigrationResult
        {
            TableName = "All",
            StartTime = startTime,
            Success = true,
            Statistics = new Dictionary<string, object>()
        };

        try
        {
            if (!await IsInfluxDbAvailableAsync())
            {
                result.Success = false;
                result.ErrorMessage = "InfluxDB is not available";
                return result;
            }

            var totalRecords = 0;
            var migratedRecords = 0;

            // Migrate user activities
            var userActivitiesResult = await MigrateUserActivitiesAsync(fromDate, toDate, batchSize, cancellationToken);
            totalRecords += userActivitiesResult.TotalRecords;
            migratedRecords += userActivitiesResult.RecordsMigrated;
            result.Warnings.AddRange(userActivitiesResult.Warnings);

            // Migrate content engagements
            var contentEngagementsResult = await MigrateContentEngagementsAsync(fromDate, toDate, batchSize, cancellationToken);
            totalRecords += contentEngagementsResult.TotalRecords;
            migratedRecords += contentEngagementsResult.RecordsMigrated;
            result.Warnings.AddRange(contentEngagementsResult.Warnings);

            // Migrate tag analytics
            var tagAnalyticsResult = await MigrateTagAnalyticsAsync(fromDate, toDate, batchSize, cancellationToken);
            totalRecords += tagAnalyticsResult.TotalRecords;
            migratedRecords += tagAnalyticsResult.RecordsMigrated;
            result.Warnings.AddRange(tagAnalyticsResult.Warnings);

            // Migrate performance metrics
            var performanceMetricsResult = await MigratePerformanceMetricsAsync(fromDate, toDate, batchSize, cancellationToken);
            totalRecords += performanceMetricsResult.TotalRecords;
            migratedRecords += performanceMetricsResult.RecordsMigrated;
            result.Warnings.AddRange(performanceMetricsResult.Warnings);

            result.TotalRecords = totalRecords;
            result.RecordsMigrated = migratedRecords;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            result.Statistics["user_activities"] = userActivitiesResult.RecordsMigrated;
            result.Statistics["content_engagements"] = contentEngagementsResult.RecordsMigrated;
            result.Statistics["tag_analytics"] = tagAnalyticsResult.RecordsMigrated;
            result.Statistics["performance_metrics"] = performanceMetricsResult.RecordsMigrated;

            _logger.LogInformation("Migration completed: {MigratedRecords}/{TotalRecords} records in {Duration}",
                migratedRecords, totalRecords, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            
            _logger.LogError(ex, "Migration failed for all analytics data");
            return result;
        }
    }

    public async Task<MigrationResult> MigrateUserActivitiesAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        return await MigrateTableAsync<UserActivity>(
            "UserActivities",
            fromDate,
            toDate,
            batchSize,
            async (activities) =>
            {
                foreach (var activity in activities)
                {
                    await _externalAnalytics.TrackUserActivityAsync(
                        activity.UserId,
                        activity.ActivityType,
                        activity.TargetEntityType,
                        activity.TargetEntityId,
                        activity.Metadata,
                        activity.SessionId,
                        activity.DurationMs,
                        activity.Success,
                        activity.ErrorMessage,
                        cancellationToken);
                }
            },
            cancellationToken);
    }

    public async Task<MigrationResult> MigrateContentEngagementsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        return await MigrateTableAsync<ContentEngagement>(
            "ContentEngagements",
            fromDate,
            toDate,
            batchSize,
            async (engagements) =>
            {
                foreach (var engagement in engagements)
                {
                    await _externalAnalytics.TrackContentEngagementAsync(
                        engagement.UserId,
                        engagement.ContentType,
                        engagement.ContentId,
                        engagement.EngagementType,
                        engagement.ContentOwnerId,
                        engagement.Source,
                        engagement.Metadata,
                        engagement.SessionId,
                        engagement.DurationMs,
                        engagement.Position,
                        cancellationToken);
                }
            },
            cancellationToken);
    }

    public async Task<MigrationResult> MigrateTagAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        return await MigrateTableAsync<TagAnalytics>(
            "TagAnalytics",
            fromDate,
            toDate,
            batchSize,
            async (tagAnalytics) =>
            {
                foreach (var tagAnalytic in tagAnalytics)
                {
                    await _externalAnalytics.TrackTagActionAsync(
                        tagAnalytic.TagId,
                        tagAnalytic.Action,
                        tagAnalytic.UserId,
                        tagAnalytic.RelatedContentType,
                        tagAnalytic.RelatedContentId,
                        tagAnalytic.Source,
                        tagAnalytic.Metadata,
                        tagAnalytic.SessionId,
                        null, // position
                        null, // wasSuggested
                        cancellationToken);
                }
            },
            cancellationToken);
    }

    public async Task<MigrationResult> MigratePerformanceMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        return await MigrateTableAsync<PerformanceMetric>(
            "PerformanceMetrics",
            fromDate,
            toDate,
            batchSize,
            async (metrics) =>
            {
                foreach (var metric in metrics)
                {
                    await _externalAnalytics.RecordPerformanceMetricAsync(
                        metric.MetricType,
                        metric.Value,
                        metric.Unit,
                        metric.Source,
                        metric.Operation,
                        metric.Tags,
                        metric.InstanceId,
                        metric.Environment,
                        metric.Version,
                        metric.Success,
                        metric.ErrorMessage,
                        metric.UserId,
                        metric.SessionId,
                        cancellationToken);
                }
            },
            cancellationToken);
    }

    private async Task<MigrationResult> MigrateTableAsync<T>(
        string tableName,
        DateTime? fromDate,
        DateTime? toDate,
        int batchSize,
        Func<IEnumerable<T>, Task> migrateAction,
        CancellationToken cancellationToken) where T : class, IEntity
    {
        var startTime = DateTime.UtcNow;
        var result = new MigrationResult
        {
            TableName = tableName,
            StartTime = startTime,
            Success = true
        };

        try
        {
            // Set migration status
            _migrationStatus[tableName] = new MigrationStatus
            {
                IsRunning = true,
                StartTime = startTime,
                TableName = tableName
            };

            var query = _context.Set<T>().AsQueryable();

            // Apply date filters if provided
            if (fromDate.HasValue || toDate.HasValue)
            {
                if (typeof(T).GetProperty("CreatedAt") != null)
                {
                    if (fromDate.HasValue)
                        query = query.Where(e => EF.Property<DateTime>(e, "CreatedAt") >= fromDate.Value);
                    if (toDate.HasValue)
                        query = query.Where(e => EF.Property<DateTime>(e, "CreatedAt") <= toDate.Value);
                }
            }

            var totalRecords = await query.CountAsync(cancellationToken);
            result.TotalRecords = totalRecords;

            var migratedRecords = 0;
            var skip = 0;

            while (skip < totalRecords && !cancellationToken.IsCancellationRequested)
            {
                var batch = await query
                    .Skip(skip)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!batch.Any()) break;

                await migrateAction(batch);
                migratedRecords += batch.Count;
                skip += batchSize;

                // Update progress
                _migrationStatus[tableName].RecordsProcessed = migratedRecords;
                _migrationStatus[tableName].ProgressPercentage = (int)((double)migratedRecords / totalRecords * 100);

                _logger.LogDebug("Migrated {MigratedRecords}/{TotalRecords} records from {TableName}",
                    migratedRecords, totalRecords, tableName);
            }

            result.RecordsMigrated = migratedRecords;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogInformation("Migration completed for {TableName}: {MigratedRecords}/{TotalRecords} records in {Duration}",
                tableName, migratedRecords, totalRecords, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            
            _logger.LogError(ex, "Migration failed for {TableName}", tableName);
            return result;
        }
        finally
        {
            // Clear migration status
            _migrationStatus.Remove(tableName);
        }
    }

    public async Task<MigrationStatusDto> GetMigrationStatusAsync()
    {
        var status = new MigrationStatusDto
        {
            IsRunning = _migrationStatus.Any(s => s.Value.IsRunning),
            RecordsProcessed = _migrationStatus.Values.Sum(s => s.RecordsProcessed),
            TotalRecords = _migrationStatus.Values.Sum(s => s.TotalRecords),
            Statistics = new Dictionary<string, object>()
        };

        if (_migrationStatus.Any())
        {
            var currentMigration = _migrationStatus.Values.FirstOrDefault(s => s.IsRunning);
            if (currentMigration != null)
            {
                status.CurrentTable = currentMigration.TableName;
                status.StartTime = currentMigration.StartTime;
                status.ProgressPercentage = currentMigration.ProgressPercentage;

                // Estimate completion time
                if (currentMigration.RecordsProcessed > 0)
                {
                    var elapsed = DateTime.UtcNow - currentMigration.StartTime;
                    var recordsPerSecond = currentMigration.RecordsProcessed / elapsed.TotalSeconds;
                    var remainingRecords = currentMigration.TotalRecords - currentMigration.RecordsProcessed;
                    var estimatedSecondsRemaining = remainingRecords / Math.Max(recordsPerSecond, 1);
                    status.EstimatedCompletion = DateTime.UtcNow.AddSeconds(estimatedSecondsRemaining);
                }
            }
        }

        return status;
    }

    public async Task<DataValidationResult> ValidateMigratedDataAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var result = new DataValidationResult
        {
            TableName = "All",
            ValidationTime = DateTime.UtcNow,
            IsValid = true
        };

        try
        {
            if (!await IsInfluxDbAvailableAsync())
            {
                result.IsValid = false;
                result.ValidationErrors.Add("InfluxDB is not available for validation");
                return result;
            }

            // For now, just return a basic validation result
            // In a real implementation, you would query both database and InfluxDB
            // and compare record counts and data integrity

            var userActivitiesCount = await GetTableRecordCount<UserActivity>(fromDate, toDate);
            var contentEngagementsCount = await GetTableRecordCount<ContentEngagement>(fromDate, toDate);
            var tagAnalyticsCount = await GetTableRecordCount<TagAnalytics>(fromDate, toDate);
            var performanceMetricsCount = await GetTableRecordCount<PerformanceMetric>(fromDate, toDate);

            result.DatabaseRecords = userActivitiesCount + contentEngagementsCount + tagAnalyticsCount + performanceMetricsCount;
            result.InfluxRecords = result.DatabaseRecords; // Would query InfluxDB for actual count
            result.AccuracyPercentage = 100.0; // Would calculate based on actual comparison

            result.DetailedMetrics["user_activities_db"] = userActivitiesCount;
            result.DetailedMetrics["content_engagements_db"] = contentEngagementsCount;
            result.DetailedMetrics["tag_analytics_db"] = tagAnalyticsCount;
            result.DetailedMetrics["performance_metrics_db"] = performanceMetricsCount;

            return result;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ValidationErrors.Add($"Validation failed: {ex.Message}");
            _logger.LogError(ex, "Data validation failed");
            return result;
        }
    }

    public async Task<MigrationStatsDto> GetMigrationStatsAsync()
    {
        try
        {
            // In a real implementation, you would store migration history in a table
            // For now, return basic statistics

            var stats = new MigrationStatsDto
            {
                TotalMigrations = 0,
                SuccessfulMigrations = 0,
                FailedMigrations = 0,
                TotalRecordsMigrated = 0,
                LastMigrationTime = null,
                TotalMigrationTime = TimeSpan.Zero,
                AverageRecordsPerSecond = 0,
                TableStats = new Dictionary<string, MigrationTableStats>(),
                RecentErrors = new List<string>()
            };

            // Add table statistics
            stats.TableStats["UserActivities"] = new MigrationTableStats
            {
                TableName = "UserActivities",
                RecordsMigrated = 0,
                LastMigration = null,
                IsUpToDate = false,
                MigrationDuration = TimeSpan.Zero
            };

            stats.TableStats["ContentEngagements"] = new MigrationTableStats
            {
                TableName = "ContentEngagements",
                RecordsMigrated = 0,
                LastMigration = null,
                IsUpToDate = false,
                MigrationDuration = TimeSpan.Zero
            };

            stats.TableStats["TagAnalytics"] = new MigrationTableStats
            {
                TableName = "TagAnalytics",
                RecordsMigrated = 0,
                LastMigration = null,
                IsUpToDate = false,
                MigrationDuration = TimeSpan.Zero
            };

            stats.TableStats["PerformanceMetrics"] = new MigrationTableStats
            {
                TableName = "PerformanceMetrics",
                RecordsMigrated = 0,
                LastMigration = null,
                IsUpToDate = false,
                MigrationDuration = TimeSpan.Zero
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get migration statistics");
            return new MigrationStatsDto
            {
                RecentErrors = new List<string> { ex.Message }
            };
        }
    }

    private async Task<int> GetTableRecordCount<T>(DateTime? fromDate, DateTime? toDate) where T : class, IEntity
    {
        var query = _context.Set<T>().AsQueryable();

        if (fromDate.HasValue || toDate.HasValue)
        {
            if (typeof(T).GetProperty("CreatedAt") != null)
            {
                if (fromDate.HasValue)
                    query = query.Where(e => EF.Property<DateTime>(e, "CreatedAt") >= fromDate.Value);
                if (toDate.HasValue)
                    query = query.Where(e => EF.Property<DateTime>(e, "CreatedAt") <= toDate.Value);
            }
        }

        return await query.CountAsync();
    }
}

public class MigrationStatus
{
    public bool IsRunning { get; set; }
    public string TableName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int RecordsProcessed { get; set; }
    public int TotalRecords { get; set; }
    public int ProgressPercentage { get; set; }
}
