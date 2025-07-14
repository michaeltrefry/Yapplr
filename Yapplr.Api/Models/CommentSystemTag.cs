using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class CommentSystemTag
{
    public int Id { get; set; }
    
    public int CommentId { get; set; }
    public Comment Comment { get; set; } = null!;
    
    public int SystemTagId { get; set; }
    public SystemTag SystemTag { get; set; } = null!;
    
    public int AppliedByUserId { get; set; }
    public User AppliedByUser { get; set; } = null!;
    
    [StringLength(500)]
    public string? Reason { get; set; }
    
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}
