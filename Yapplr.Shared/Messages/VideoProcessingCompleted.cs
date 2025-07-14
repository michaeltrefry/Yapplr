using System.ComponentModel.DataAnnotations;
using Yapplr.Shared.Models;

namespace Yapplr.Shared.Messages;

/// <summary>
/// Message sent when video processing is completed
/// </summary>
public record VideoProcessingCompleted
{
    [Required]
    public int PostId { get; init; }
    
    [Required]
    public int UserId { get; init; }
    
    [Required]
    public string OriginalVideoFileName { get; init; } = string.Empty;
    
    [Required]
    public string ProcessedVideoFileName { get; init; } = string.Empty;
    
    [Required]
    public string ThumbnailFileName { get; init; } = string.Empty;
    
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
    
    public TimeSpan ProcessingDuration { get; init; }
    
    public VideoMetadata? Metadata { get; init; }
}