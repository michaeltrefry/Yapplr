using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

/// <summary>
/// Database model for storing upload configuration settings
/// </summary>
public class UploadSettings
{
    public int Id { get; set; }
    
    /// <summary>
    /// Maximum file size for images in bytes
    /// </summary>
    public long MaxImageSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB default
    
    /// <summary>
    /// Maximum file size for videos in bytes
    /// </summary>
    public long MaxVideoSizeBytes { get; set; } = 1024 * 1024 * 1024; // 1GB default
    
    /// <summary>
    /// Maximum video duration in seconds
    /// </summary>
    public int MaxVideoDurationSeconds { get; set; } = 1800; //30 minutes default
    
    /// <summary>
    /// Maximum number of media files per post
    /// </summary>
    public int MaxMediaFilesPerPost { get; set; } = 10;
    
    /// <summary>
    /// Allowed image file extensions (comma-separated)
    /// </summary>
    [StringLength(500)]
    public string AllowedImageExtensions { get; set; } = ".jpg,.jpeg,.png,.gif,.webp";
    
    /// <summary>
    /// Allowed video file extensions (comma-separated)
    /// </summary>
    [StringLength(500)]
    public string AllowedVideoExtensions { get; set; } = ".mp4,.avi,.mov,.wmv,.flv,.webm,.mkv,.3gp";
    
    /// <summary>
    /// Whether to delete original videos after processing
    /// </summary>
    public bool DeleteOriginalAfterProcessing { get; set; } = true;
    
    /// <summary>
    /// Video processing target bitrate in kbps
    /// </summary>
    public int VideoTargetBitrate { get; set; } = 2000;
    
    /// <summary>
    /// Video processing max width
    /// </summary>
    public int VideoMaxWidth { get; set; } = 1920;
    
    /// <summary>
    /// Video processing max height
    /// </summary>
    public int VideoMaxHeight { get; set; } = 1080;
    
    /// <summary>
    /// When these settings were created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When these settings were last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User who last updated these settings
    /// </summary>
    public int? UpdatedByUserId { get; set; }
    
    /// <summary>
    /// Navigation property for the user who updated these settings
    /// </summary>
    public User? UpdatedByUser { get; set; }
    
    /// <summary>
    /// Reason for the last update
    /// </summary>
    [StringLength(500)]
    public string? UpdateReason { get; set; }
}
