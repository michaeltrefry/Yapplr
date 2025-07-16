namespace Yapplr.Api.DTOs;

public class MigrationResult
{
    public bool Success { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int RecordsMigrated { get; set; }
    public int TotalRecords { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Statistics { get; set; } = new();
}
