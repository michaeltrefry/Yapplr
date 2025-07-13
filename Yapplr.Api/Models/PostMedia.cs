using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;
using Yapplr.Shared.Models;

namespace Yapplr.Api.Models;

public enum MediaType
{
    Image = 0,
    Video = 1
}

/// <summary>
/// Stores media-related information for posts (images and videos)
/// </summary>
public class PostMedia : IEntity
{
    public int Id { get; set; }
    
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    
    public MediaType MediaType { get; set; }
    
    // Original file information
    [StringLength(256)]
    public string? OriginalFileName { get; set; }
    
    // Image fields
    [StringLength(256)]
    public string? ImageFileName { get; set; }
    
    // Video fields
    [StringLength(256)]
    public string? VideoFileName { get; set; }
    [StringLength(256)]
    public string? ProcessedVideoFileName { get; set; }
    [StringLength(256)]
    public string? VideoThumbnailFileName { get; set; }
    
    // Video processing status
    public VideoProcessingStatus VideoProcessingStatus { get; set; } = VideoProcessingStatus.Pending;
    public DateTime? VideoProcessingStartedAt { get; set; }
    public DateTime? VideoProcessingCompletedAt { get; set; }
    [StringLength(500)]
    public string? VideoProcessingError { get; set; }
    
    // Video metadata (populated after processing)
    public int? VideoWidth { get; set; }
    public int? VideoHeight { get; set; }
    public TimeSpan? VideoDuration { get; set; }
    public long? VideoFileSizeBytes { get; set; }
    [StringLength(50)]
    public string? VideoFormat { get; set; }
    public double? VideoBitrate { get; set; }
    public double? VideoCompressionRatio { get; set; }
    
    // Original video metadata (if available)
    public int? OriginalVideoWidth { get; set; }
    public int? OriginalVideoHeight { get; set; }
    public TimeSpan? OriginalVideoDuration { get; set; }
    public long? OriginalVideoFileSizeBytes { get; set; }
    [StringLength(50)]
    public string? OriginalVideoFormat { get; set; }
    public double? OriginalVideoBitrate { get; set; }
    
    // Image metadata
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public long? ImageFileSizeBytes { get; set; }
    [StringLength(50)]
    public string? ImageFormat { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
