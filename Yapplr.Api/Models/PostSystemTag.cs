using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class PostSystemTag
{
    public int Id { get; set; }
    
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    
    public int SystemTagId { get; set; }
    public SystemTag SystemTag { get; set; } = null!;
    
    public int AppliedByUserId { get; set; }
    public User AppliedByUser { get; set; } = null!;
    
    [StringLength(500)]
    public string? Reason { get; set; }
    
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}
