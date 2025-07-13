namespace Yapplr.Api.Models;

public class ConversationParticipant
{
    public int Id { get; set; }
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastReadAt { get; set; }
    
    // Foreign keys
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    
    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}
