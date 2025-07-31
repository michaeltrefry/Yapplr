using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services.Analytics;

/// <summary>
/// Service for migrating analytics data from database to InfluxDB
/// </summary>
public interface IAnalyticsMigrationService
{
    /// <summary>
    /// Check if InfluxDB is available for migration
    /// </summary>
    Task<bool> IsInfluxDbAvailableAsync();

    /// <summary>
    /// Migrate all analytics data to InfluxDB
    /// </summary>
    Task<MigrationResult> MigrateAllAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrate user activities to InfluxDB
    /// </summary>
    Task<MigrationResult> MigrateUserActivitiesAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrate content engagements to InfluxDB
    /// </summary>
    Task<MigrationResult> MigrateContentEngagementsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrate tag analytics to InfluxDB
    /// </summary>
    Task<MigrationResult> MigrateTagAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrate performance metrics to InfluxDB
    /// </summary>
    Task<MigrationResult> MigratePerformanceMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get migration status and progress
    /// </summary>
    Task<MigrationStatusDto> GetMigrationStatusAsync();

    /// <summary>
    /// Validate migrated data integrity
    /// </summary>
    Task<DataValidationResult> ValidateMigratedDataAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Get migration statistics
    /// </summary>
    Task<MigrationStatsDto> GetMigrationStatsAsync();
}
