using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record VideoUploadResponseDto(
    string FileName,
    string VideoUrl,
    long SizeBytes,
    string OriginalFileName,
    string ContentType,
    int JobId
);

public record VideoProcessingJobDto(
    int Id,
    string FileName,
    string? OriginalFileName,
    string? ContentType,
    long SizeBytes,
    VideoProcessingStatus Status,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ProcessedFileName,
    string? ThumbnailFileName,
    int? DurationSeconds,
    long? ProcessedSizeBytes,
    string? ProcessedFormat,
    string? Resolution,
    int? Bitrate,
    double? FrameRate,
    int UserId,
    int? PostId,
    int? MessageId
);

public record VideoProcessingStatusDto(
    int JobId,
    VideoProcessingStatus Status,
    string? ErrorMessage = null,
    int? ProgressPercentage = null,
    string? CurrentStep = null
);

public record VideoMetadataDto(
    int DurationSeconds,
    string Format,
    string Resolution,
    int Bitrate,
    double FrameRate,
    long SizeBytes,
    string? ThumbnailUrl = null
);

public record VideoStreamRequestDto(
    string FileName,
    long? RangeStart = null,
    long? RangeEnd = null
);

public record VideoQualityOption(
    string Label,
    string Resolution,
    int Bitrate,
    string Format
);

public record VideoProcessingOptionsDto(
    IEnumerable<VideoQualityOption> QualityOptions,
    bool GenerateThumbnail = true,
    int ThumbnailTimeSeconds = 1,
    bool OptimizeForWeb = true,
    int MaxDurationSeconds = 300 // 5 minutes
);
