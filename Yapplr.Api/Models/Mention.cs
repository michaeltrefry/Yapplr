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
    public int? PostId { get; set; } // Post where the mention occurred (null if in comment)
    public int? CommentId { get; set; } // Comment where the mention occurred (null if in post) - now references Posts table with PostType.Comment
    public int NotificationId { get; set; } // Associated notification

    // Navigation properties
    public User MentionedUser { get; set; } = null!;
    public User MentioningUser { get; set; } = null!;
    public Post? Post { get; set; }
    public Post? Comment { get; set; } // Now references Posts table with PostType.Comment
    public Notification Notification { get; set; } = null!;
}
