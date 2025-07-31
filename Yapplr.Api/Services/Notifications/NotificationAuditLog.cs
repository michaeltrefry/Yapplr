using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Audit log entry for notification events
/// </summary>
public class NotificationAuditLog
{
    public int Id { get; set; }
    [StringLength(64)]
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [StringLength(30)]
    public string EventType { get; set; } = string.Empty; // "sent", "delivered", "failed", "blocked", "rate_limited"
    public int? UserId { get; set; }
    [StringLength(100)]
    public string? Username { get; set; }
    [StringLength(256)]
    public string NotificationType { get; set; } = string.Empty;
    [StringLength(256)]
    public string? Title { get; set; }
    [StringLength(1000)]
    public string? Body { get; set; }
    [StringLength(20)]
    public string? DeliveryMethod { get; set; }
    public bool Success { get; set; }
    [StringLength(500)]
    public string? ErrorMessage { get; set; }
    [StringLength(64)]
    public string? IpAddress { get; set; }
    [StringLength(256)]
    public string? UserAgent { get; set; }
    [StringLength(1000)]
    public string? AdditionalDataJson { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
    [StringLength(1000)]
    public string? SecurityFlags { get; set; } // JSON array of security-related flags
}