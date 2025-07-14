using System.ComponentModel.DataAnnotations;

namespace Yapplr.Shared.Messages;

/// <summary>
/// Message sent when video processing fails
/// </summary>
public record VideoProcessingFailed
{
    [Required]
    public int PostId { get; init; }
    
    [Required]
    public int UserId { get; init; }
    
    [Required]
    public string OriginalVideoFileName { get; init; } = string.Empty;
    
    [Required]
    public string ErrorMessage { get; init; } = string.Empty;
    
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
    
    public string? StackTrace { get; init; }
}