using Yapplr.Api.Models;
using Yapplr.Shared.Models;

namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for post media items
/// </summary>
public record PostMediaDto(
    int Id,
    MediaType MediaType,
    string? ImageUrl,
    string? VideoUrl,
    string? VideoThumbnailUrl,
    VideoProcessingStatus? VideoProcessingStatus,
    string? GifUrl,
    string? GifPreviewUrl,
    int? Width,
    int? Height,
    TimeSpan? Duration,
    long? FileSizeBytes,
    string? Format,
    DateTime CreatedAt,
    VideoMetadata? VideoMetadata = null
);
