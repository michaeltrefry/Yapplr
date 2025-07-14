using System.ComponentModel.DataAnnotations;

namespace Yapplr.Shared.Messages;

/// <summary>
/// Message sent when a video needs to be processed
/// </summary>
public record VideoProcessingRequest
{
    [Required]
    public int PostId { get; init; }
    
    [Required]
    public int UserId { get; init; }
    
    [Required]
    public string OriginalVideoFileName { get; init; } = string.Empty;
    
    [Required]
    public string OriginalVideoPath { get; init; } = string.Empty;
    
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
    
    public string? PostContent { get; init; }
}