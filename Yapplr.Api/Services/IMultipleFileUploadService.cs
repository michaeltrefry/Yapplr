using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for handling multiple file uploads
/// </summary>
public interface IMultipleFileUploadService
{
    /// <summary>
    /// Upload multiple media files (images and videos)
    /// </summary>
    /// <param name="files">Collection of files to upload</param>
    /// <returns>Upload response with successful uploads and errors</returns>
    Task<MultipleFileUploadResponseDto> UploadMultipleMediaFilesAsync(IFormFileCollection files);

    /// <summary>
    /// Validate multiple file uploads
    /// </summary>
    /// <param name="files">Collection of files to validate</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateMultipleFilesAsync(IFormFileCollection files);
    /// <summary>
    /// Get maximum allowed files count
    /// </summary>
    int MaxFilesAllowed { get; }

    /// <summary>
    /// Get maximum file size for images in bytes
    /// </summary>
    long MaxImageSizeBytes { get; }

    /// <summary>
    /// Get maximum file size for videos in bytes
    /// </summary>
    long MaxVideoSizeBytes { get; }
}

/// <summary>
/// Validation result for file uploads
/// </summary>
public record ValidationResult(
    bool IsValid,
    List<string> Errors
);
