using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class VideoProcessingJob
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string? OriginalFileName { get; set; }
    
    [StringLength(100)]
    public string? ContentType { get; set; }
    
    public long SizeBytes { get; set; }
    
    public VideoProcessingStatus Status { get; set; } = VideoProcessingStatus.Pending;
    
    [StringLength(500)]
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    // Processing results
    [StringLength(255)]
    public string? ProcessedFileName { get; set; }
    
    [StringLength(255)]
    public string? ThumbnailFileName { get; set; }
    
    public int? DurationSeconds { get; set; }
    
    public long? ProcessedSizeBytes { get; set; }
    
    [StringLength(50)]
    public string? ProcessedFormat { get; set; }
    
    [StringLength(100)]
    public string? Resolution { get; set; }
    
    public int? Bitrate { get; set; }
    
    public double? FrameRate { get; set; }
    
    // Foreign keys
    public int UserId { get; set; }
    public int? PostId { get; set; }
    public int? MessageId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Post? Post { get; set; }
    public Message? Message { get; set; }
}

public enum VideoProcessingJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
