using System.ComponentModel.DataAnnotations;

namespace Yapplr.Shared.Models;

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