namespace Yapplr.Api.DTOs;

public class NotificationListDto
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public bool HasMore { get; set; }
}
