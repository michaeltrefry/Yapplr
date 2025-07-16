namespace Yapplr.Api.DTOs;

public class MigrationStatusDto
{
    public bool IsRunning { get; set; }
    public string? CurrentTable { get; set; }
    public int ProgressPercentage { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EstimatedCompletion { get; set; }
    public int RecordsProcessed { get; set; }
    public int TotalRecords { get; set; }
    public List<MigrationResult> CompletedMigrations { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Statistics { get; set; } = new();
}
