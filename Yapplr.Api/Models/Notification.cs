using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class Notification
{
    public int Id { get; set; }
    
    public NotificationType Type { get; set; }
    
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;
    
    public bool IsRead { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ReadAt { get; set; }
    
    // Foreign keys
    public int UserId { get; set; } // User who receives the notification
    public int? ActorUserId { get; set; } // User who triggered the notification (can be null for system notifications)
    public int? PostId { get; set; } // Related post (for post mentions, likes, reposts)
    public int? CommentId { get; set; } // Related comment (for comment mentions)
    
    // Navigation properties
    public User User { get; set; } = null!; // Recipient
    public User? ActorUser { get; set; } // Actor who triggered the notification
    public Post? Post { get; set; }
    public Comment? Comment { get; set; }
}
