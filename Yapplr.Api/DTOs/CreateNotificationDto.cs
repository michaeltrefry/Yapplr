using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class CreateNotificationDto
{
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int? ActorUserId { get; set; }
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
}
