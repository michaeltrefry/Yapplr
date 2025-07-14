using System.ComponentModel.DataAnnotations;

namespace Yapplr.Shared.Models;

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