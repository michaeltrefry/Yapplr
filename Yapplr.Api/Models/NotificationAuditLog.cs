using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

/// <summary>
/// Audit log entry for notification security events
/// </summary>
public class NotificationAuditLog
{
    public int Id { get; set; }
    
    [StringLength(64)]
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty;
    
    public int? UserId { get; set; }
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string Severity { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? AdditionalData { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    // Navigation property
    public User? User { get; set; }
}
