namespace Yapplr.Api.DTOs;

public class DataValidationResult
{
    public bool IsValid { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int DatabaseRecords { get; set; }
    public int InfluxRecords { get; set; }
    public int MissingRecords { get; set; }
    public int ExtraRecords { get; set; }
    public double AccuracyPercentage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime ValidationTime { get; set; }
    public Dictionary<string, object> DetailedMetrics { get; set; } = new();
}
