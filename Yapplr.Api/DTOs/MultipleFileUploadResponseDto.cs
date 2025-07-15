using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

/// <summary>
/// Response DTO for multiple file uploads
/// </summary>
public record MultipleFileUploadResponseDto(
    List<UploadedFileDto> UploadedFiles,
    List<FileUploadErrorDto> Errors,
    int TotalFiles,
    int SuccessfulUploads,
    int FailedUploads
);

/// <summary>
/// DTO for individual uploaded file information
/// </summary>
public record UploadedFileDto(
    string FileName,
    string FileUrl,
    MediaType MediaType,
    long FileSizeBytes,
    int? Width = null,
    int? Height = null,
    TimeSpan? Duration = null
);

/// <summary>
/// DTO for file upload errors
/// </summary>
public record FileUploadErrorDto(
    string OriginalFileName,
    string ErrorMessage,
    string ErrorCode
);
