using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class UserReport
{
    public int Id { get; set; }
    
    public int ReportedByUserId { get; set; }
    public User ReportedByUser { get; set; } = null!;
    
    // Content being reported (either post or comment)
    public int? PostId { get; set; }
    public Post? Post { get; set; }

    public int? CommentId { get; set; } // Now references Posts table with PostType.Comment
    public Post? Comment { get; set; } // Now references Posts table with PostType.Comment
    
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
    
    public UserReportStatus Status { get; set; } = UserReportStatus.Pending;
    
    // Review information
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    
    [StringLength(1000)]
    public string? ReviewNotes { get; set; }
    
    public DateTime? ReviewedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties for selected system tags
    public ICollection<UserReportSystemTag> UserReportSystemTags { get; set; } = new List<UserReportSystemTag>();
}
