namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for queued notifications that bridges between the old in-memory implementation
/// and the new database-backed implementation
/// </summary>
public class QueuedNotificationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
}
