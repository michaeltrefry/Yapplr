using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Yapplr.Api.Models;

/// <summary>
/// Notification history entry for replay functionality
/// </summary>
public class NotificationHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [StringLength(64)]
    public string NotificationId { get; set; } = string.Empty;
    [StringLength(100)]
    public string NotificationType { get; set; } = string.Empty;
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;
    [StringLength(1000)]
    public string Body { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? DataJson { get; set; } // JSON serialized data
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public bool WasDelivered { get; set; } = false;
    public bool WasReplayed { get; set; } = false;
    public DateTime? ReplayedAt { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
    
    // Helper property to deserialize data
    [NotMapped]
    public Dictionary<string, string>? Data
    {
        get => string.IsNullOrEmpty(DataJson) 
            ? null 
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(DataJson);
        set => DataJson = value == null 
            ? null 
            : System.Text.Json.JsonSerializer.Serialize(value);
    }
}
