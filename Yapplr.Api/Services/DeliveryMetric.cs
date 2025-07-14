namespace Yapplr.Api.Services;

/// <summary>
/// Real-time notification delivery metrics
/// </summary>
public class DeliveryMetric
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public double LatencyMs => EndTime.HasValue ? (EndTime.Value - StartTime).TotalMilliseconds : 0;
}