namespace Yapplr.Api.Services;

/// <summary>
/// Service for migrating analytics data from PostgreSQL to InfluxDB
/// </summary>
public interface IAnalyticsMigrationService
{
    /// <summary>
    /// Migrate all analytics data from PostgreSQL to InfluxDB
    /// </summary>
    Task<MigrationResult> MigrateAllAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrate user activities from PostgreSQL to InfluxDB
    /// </summary>
    Task<MigrationResult> MigrateUserActivitiesAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrate content engagements from PostgreSQL to InfluxDB
    /// </summary>
    Task<MigrationResult> MigrateContentEngagementsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrate tag analytics from PostgreSQL to InfluxDB
    /// </summary>
    Task<MigrationResult> MigrateTagAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrate performance metrics from PostgreSQL to InfluxDB
    /// </summary>
    Task<MigrationResult> MigratePerformanceMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get migration status and progress
    /// </summary>
    Task<MigrationStatus> GetMigrationStatusAsync();

    /// <summary>
    /// Validate migrated data by comparing counts between PostgreSQL and InfluxDB
    /// </summary>
    Task<ValidationResult> ValidateMigrationAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Check if InfluxDB is available for migration
    /// </summary>
    Task<bool> IsInfluxDbAvailableAsync();
}

/// <summary>
/// Result of a migration operation
/// </summary>
public class MigrationResult
{
    public bool Success { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int MigratedRecords { get; set; }
    public int FailedRecords { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Overall migration status
/// </summary>
public class MigrationStatus
{
    public bool IsInProgress { get; set; }
    public string? CurrentTable { get; set; }
    public int TotalTables { get; set; }
    public int CompletedTables { get; set; }
    public DateTime? StartTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public List<MigrationResult> CompletedMigrations { get; set; } = new();
}

/// <summary>
/// Result of migration validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<TableValidation> TableValidations { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Validation result for a specific table
/// </summary>
public class TableValidation
{
    public string TableName { get; set; } = string.Empty;
    public int PostgreSqlCount { get; set; }
    public int InfluxDbCount { get; set; }
    public bool IsValid => PostgreSqlCount == InfluxDbCount;
    public int Difference => PostgreSqlCount - InfluxDbCount;
}
