using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Yapplr.Api.Models;

/// <summary>
/// Database entity for queued notifications that need to be delivered when users come online
/// </summary>
public class QueuedNotification
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON data for the notification
    /// </summary>
    public string? Data { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? DeliveredAt { get; set; }
    
    [Required]
    public int RetryCount { get; set; } = 0;
    
    [Required]
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Retry delay in minutes
    /// </summary>
    [Required]
    public int RetryDelayMinutes { get; set; } = 1;
    
    public DateTime? NextRetryAt { get; set; }
    
    [StringLength(1000)]
    public string? LastError { get; set; }
    
    // Navigation property
    public User? User { get; set; }
}
