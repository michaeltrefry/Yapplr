using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class AiSuggestedTag
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int? PostId { get; set; }
    public Post? Post { get; set; }

    public int? CommentId { get; set; } // Now references Posts table with PostType.Comment
    public Post? Comment { get; set; } // Now references Posts table with PostType.Comment

    [Required]
    [MaxLength(100)]
    public string TagName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Range(0.0, 1.0)]
    public double Confidence { get; set; }

    [MaxLength(20)]
    public string RiskLevel { get; set; } = string.Empty;

    public bool RequiresReview { get; set; }

    public DateTime SuggestedAt { get; set; } = DateTime.UtcNow;

    public bool IsApproved { get; set; } = false;

    public bool IsRejected { get; set; } = false;

    public int? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string? ApprovalReason { get; set; }

    // Additional metadata from AI analysis
    [MaxLength(1000)]
    public string? AnalysisDetails { get; set; }

    public double? SentimentScore { get; set; }

    [MaxLength(20)]
    public string? SentimentLabel { get; set; }
}
