namespace Yapplr.Api.DTOs;

public class MigrationStatsDto
{
    public int TotalMigrations { get; set; }
    public int SuccessfulMigrations { get; set; }
    public int FailedMigrations { get; set; }
    public int TotalRecordsMigrated { get; set; }
    public DateTime? LastMigrationTime { get; set; }
    public TimeSpan TotalMigrationTime { get; set; }
    public double AverageRecordsPerSecond { get; set; }
    public Dictionary<string, MigrationTableStats> TableStats { get; set; } = new();
    public List<string> RecentErrors { get; set; } = new();
}

public class MigrationTableStats
{
    public string TableName { get; set; } = string.Empty;
    public int RecordsMigrated { get; set; }
    public DateTime? LastMigration { get; set; }
    public bool IsUpToDate { get; set; }
    public TimeSpan MigrationDuration { get; set; }
}
