using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? Status { get; set; }

    // Actor information (user who triggered the notification)
    public UserDto? ActorUser { get; set; }
    
    // Related content
    public PostDto? Post { get; set; }
    public CommentDto? Comment { get; set; }
    
    // Mention-specific data
    public MentionDto? Mention { get; set; }
}
