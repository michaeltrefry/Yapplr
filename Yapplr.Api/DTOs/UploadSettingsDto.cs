namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for upload settings configuration
/// </summary>
public class UploadSettingsDto
{
    /// <summary>
    /// Maximum file size for images in bytes
    /// </summary>
    public long MaxImageSizeBytes { get; set; }
    
    /// <summary>
    /// Maximum file size for videos in bytes
    /// </summary>
    public long MaxVideoSizeBytes { get; set; }
    
    /// <summary>
    /// Maximum video duration in seconds
    /// </summary>
    public int MaxVideoDurationSeconds { get; set; }
    
    /// <summary>
    /// Maximum number of media files per post
    /// </summary>
    public int MaxMediaFilesPerPost { get; set; }
    
    /// <summary>
    /// Allowed image file extensions (comma-separated)
    /// </summary>
    public string AllowedImageExtensions { get; set; } = string.Empty;
    
    /// <summary>
    /// Allowed video file extensions (comma-separated)
    /// </summary>
    public string AllowedVideoExtensions { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to delete original videos after processing
    /// </summary>
    public bool DeleteOriginalAfterProcessing { get; set; }
    
    /// <summary>
    /// Video processing target bitrate in kbps
    /// </summary>
    public int VideoTargetBitrate { get; set; }
    
    /// <summary>
    /// Video processing max width
    /// </summary>
    public int VideoMaxWidth { get; set; }
    
    /// <summary>
    /// Video processing max height
    /// </summary>
    public int VideoMaxHeight { get; set; }
    
    /// <summary>
    /// When these settings were last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// User who last updated these settings
    /// </summary>
    public string? UpdatedByUsername { get; set; }
    
    /// <summary>
    /// Reason for the last update
    /// </summary>
    public string? UpdateReason { get; set; }
}

/// <summary>
/// DTO for updating upload settings
/// </summary>
public class UpdateUploadSettingsDto
{
    /// <summary>
    /// Maximum file size for images in bytes
    /// </summary>
    public long MaxImageSizeBytes { get; set; }
    
    /// <summary>
    /// Maximum file size for videos in bytes
    /// </summary>
    public long MaxVideoSizeBytes { get; set; }
    
    /// <summary>
    /// Maximum video duration in seconds
    /// </summary>
    public int MaxVideoDurationSeconds { get; set; }
    
    /// <summary>
    /// Maximum number of media files per post
    /// </summary>
    public int MaxMediaFilesPerPost { get; set; }
    
    /// <summary>
    /// Allowed image file extensions (comma-separated)
    /// </summary>
    public string AllowedImageExtensions { get; set; } = string.Empty;
    
    /// <summary>
    /// Allowed video file extensions (comma-separated)
    /// </summary>
    public string AllowedVideoExtensions { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to delete original videos after processing
    /// </summary>
    public bool DeleteOriginalAfterProcessing { get; set; }
    
    /// <summary>
    /// Video processing target bitrate in kbps
    /// </summary>
    public int VideoTargetBitrate { get; set; }
    
    /// <summary>
    /// Video processing max width
    /// </summary>
    public int VideoMaxWidth { get; set; }
    
    /// <summary>
    /// Video processing max height
    /// </summary>
    public int VideoMaxHeight { get; set; }
    
    /// <summary>
    /// Reason for this update
    /// </summary>
    public string? UpdateReason { get; set; }
}
