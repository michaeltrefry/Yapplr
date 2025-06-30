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
    
    // Actor information (user who triggered the notification)
    public UserDto? ActorUser { get; set; }
    
    // Related content
    public PostDto? Post { get; set; }
    public CommentDto? Comment { get; set; }
    
    // Mention-specific data
    public MentionDto? Mention { get; set; }
}

public class MentionDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MentionedUserId { get; set; }
    public int MentioningUserId { get; set; }
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
}

public class CreateNotificationDto
{
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int? ActorUserId { get; set; }
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
}

public class NotificationListDto
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public bool HasMore { get; set; }
}
