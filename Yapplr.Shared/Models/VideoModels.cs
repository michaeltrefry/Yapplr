using System.ComponentModel.DataAnnotations;

namespace Yapplr.Shared.Models;

/// <summary>
/// Represents the processing status of a video
/// </summary>
public enum VideoProcessingStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

/// <summary>
/// Video upload response DTO
/// </summary>
public record VideoUploadResponse
{
    [Required]
    public string FileName { get; init; } = string.Empty;
    
    [Required]
    public string VideoUrl { get; init; } = string.Empty;
    
    public long FileSizeBytes { get; init; }
    
    public TimeSpan? Duration { get; init; }
    
    public int? Width { get; init; }
    
    public int? Height { get; init; }
}

/// <summary>
/// Video processing configuration
/// </summary>
public record VideoProcessingConfig
{
    public int MaxWidth { get; init; } = 1920;
    public int MaxHeight { get; init; } = 1080;
    public int TargetBitrate { get; init; } = 2000; // kbps
    public string OutputFormat { get; init; } = "mp4";
    public string VideoCodec { get; init; } = "libx264";
    public string AudioCodec { get; init; } = "aac";
    public int ThumbnailWidth { get; init; } = 320;
    public int ThumbnailHeight { get; init; } = 240;
    public double ThumbnailTimeSeconds { get; init; } = 1.0; // Extract thumbnail at 1 second
    public string InputPath { get; init; } = "/app/uploads/videos";
    public string OutputPath { get; init; } = "/app/uploads/processed";
    public string ThumbnailPath { get; init; } = "/app/uploads/thumbnails";
    public long MaxFileSizeBytes { get; init; } = 104857600; // 100MB
    public int MaxDurationSeconds { get; init; } = 300; // 5 minutes
    public bool DeleteOriginalAfterProcessing { get; init; } = true;
}

/// <summary>
/// Video processing result
/// </summary>
public record VideoProcessingResult
{
    [Required]
    public string ProcessedVideoFileName { get; init; } = string.Empty;
    
    [Required]
    public string ThumbnailFileName { get; init; } = string.Empty;
    
    public VideoMetadata? Metadata { get; init; }
    
    public bool Success { get; init; }
    
    public string? ErrorMessage { get; init; }
    
    public TimeSpan ProcessingDuration { get; init; }
}

/// <summary>
/// Video metadata from processing
/// </summary>
public record VideoMetadata
{
    public int OriginalWidth { get; init; }
    public int OriginalHeight { get; init; }
    public int ProcessedWidth { get; init; }
    public int ProcessedHeight { get; init; }
    public TimeSpan OriginalDuration { get; init; }
    public TimeSpan ProcessedDuration { get; init; }
    public long OriginalFileSizeBytes { get; init; }
    public long ProcessedFileSizeBytes { get; init; }
    public string OriginalFormat { get; init; } = string.Empty;
    public string ProcessedFormat { get; init; } = string.Empty;
    public double OriginalBitrate { get; init; }
    public double ProcessedBitrate { get; init; }
    public double CompressionRatio { get; init; }
}
