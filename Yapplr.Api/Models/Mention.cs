using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class Mention
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int MentionedUserId { get; set; } // User who was mentioned
    public int MentioningUserId { get; set; } // User who made the mention
    public int PostId { get; set; } // Post/Comment where the mention occurred (always populated since comments are posts)
    public int NotificationId { get; set; } // Associated notification

    // Navigation properties
    public User MentionedUser { get; set; } = null!;
    public User MentioningUser { get; set; } = null!;
    public Post Post { get; set; } = null!; // The post or comment where mention occurred
    public Notification Notification { get; set; } = null!;
}
