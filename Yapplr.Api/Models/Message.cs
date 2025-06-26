using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class Message
{
    public int Id { get; set; }
    
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;
    
    public string? ImageFileName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsEdited { get; set; } = false;
    
    public bool IsDeleted { get; set; } = false;
    
    // Foreign keys
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    
    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public ICollection<MessageStatus> MessageStatuses { get; set; } = new List<MessageStatus>();
}
