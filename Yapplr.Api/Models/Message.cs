using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models;

public class Message : IUserOwnedEntity
{
    public int Id { get; set; }
    
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;
    [StringLength(256)]
    public string? ImageFileName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsEdited { get; set; } = false;
    
    public bool IsDeleted { get; set; } = false;
    
    // Foreign keys
    public int ConversationId { get; set; }
    public int SenderId { get; set; }

    // IUserOwnedEntity implementation
    public int UserId
    {
        get => SenderId;
        set => SenderId = value;
    }

    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public ICollection<MessageStatus> MessageStatuses { get; set; } = new List<MessageStatus>();
}
