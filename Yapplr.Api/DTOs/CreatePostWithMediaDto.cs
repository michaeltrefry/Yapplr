using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

/// <summary>
/// DTO for creating posts with multiple media files
/// </summary>
public record CreatePostWithMediaDto(
    [StringLength(256)] string? Content,
    PostPrivacy Privacy = PostPrivacy.Public,
    [MaxLength(10, ErrorMessage = "Maximum 10 media files allowed")]
    List<MediaFileDto>? MediaFiles = null,
    int? GroupId = null // Optional - for creating posts in groups
);

/// <summary>
/// DTO for individual media file information
/// </summary>
public record MediaFileDto(
    [Required] string FileName,
    [Required] MediaType MediaType,
    int? Width = null,
    int? Height = null,
    long? FileSizeBytes = null,
    TimeSpan? Duration = null
);
