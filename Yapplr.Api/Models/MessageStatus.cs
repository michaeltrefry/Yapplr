namespace Yapplr.Api.Models;

public class MessageStatus
{
    public int Id { get; set; }
    
    public MessageStatusType Status { get; set; } = MessageStatusType.Sent;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int MessageId { get; set; }
    public int UserId { get; set; }
    
    // Navigation properties
    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}
