using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for handling multiple file uploads
/// </summary>
public class MultipleFileUploadService : IMultipleFileUploadService
{
    private readonly IImageService _imageService;
    private readonly IVideoService _videoService;
    private readonly ILogger<MultipleFileUploadService> _logger;
    private readonly UploadsConfiguration _uploadsConfig;

    public int MaxFilesAllowed => 10;
    public long MaxImageSizeBytes => 5 * 1024 * 1024; // 5MB
    public long MaxVideoSizeBytes => 100 * 1024 * 1024; // 100MB

    private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly string[] _allowedVideoExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
    
    private readonly string[] _allowedImageMimeTypes = {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
    };
    
    private readonly string[] _allowedVideoMimeTypes = {
        "video/mp4", "video/avi", "video/x-msvideo", "video/quicktime", "video/x-ms-wmv",
        "video/x-flv", "video/webm", "video/x-matroska"
    };

    public MultipleFileUploadService(
        IImageService imageService,
        IVideoService videoService,
        ILogger<MultipleFileUploadService> logger,
        IOptions<UploadsConfiguration> uploadsConfig)
    {
        _imageService = imageService;
        _videoService = videoService;
        _logger = logger;
        _uploadsConfig = uploadsConfig.Value;
    }

    public async Task<MultipleFileUploadResponseDto> UploadMultipleMediaFilesAsync(IFormFileCollection files)
    {
        var uploadedFiles = new List<UploadedFileDto>();
        var errors = new List<FileUploadErrorDto>();

        // Validate all files first
        var validation = ValidateMultipleFiles(files);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                errors.Add(new FileUploadErrorDto("", error, "VALIDATION_ERROR"));
            }
            return new MultipleFileUploadResponseDto(uploadedFiles, errors, files.Count, 0, files.Count);
        }

        // Process each file
        foreach (var file in files)
        {
            try
            {
                var isImage = IsImageFile(file);
                var isVideo = IsVideoFile(file);

                if (isImage)
                {
                    var fileName = await _imageService.SaveImageAsync(file);
                    var imageUrl = GenerateImageUrl(fileName);
                    
                    uploadedFiles.Add(new UploadedFileDto(
                        fileName,
                        imageUrl,
                        MediaType.Image,
                        file.Length
                    ));
                }
                else if (isVideo)
                {
                    var fileName = await _videoService.SaveVideoAsync(file);
                    var videoUrl = GenerateVideoUrl(fileName);
                    
                    uploadedFiles.Add(new UploadedFileDto(
                        fileName,
                        videoUrl,
                        MediaType.Video,
                        file.Length
                    ));
                }
                else
                {
                    errors.Add(new FileUploadErrorDto(
                        file.FileName ?? "unknown",
                        "Unsupported file type",
                        "UNSUPPORTED_TYPE"
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                errors.Add(new FileUploadErrorDto(
                    file.FileName ?? "unknown",
                    ex.Message,
                    "UPLOAD_ERROR"
                ));
            }
        }

        return new MultipleFileUploadResponseDto(
            uploadedFiles,
            errors,
            files.Count,
            uploadedFiles.Count,
            errors.Count
        );
    }

    public ValidationResult ValidateMultipleFiles(IFormFileCollection files)
    {
        var errors = new List<string>();

        if (files == null || files.Count == 0)
        {
            errors.Add("No files provided");
            return new ValidationResult(false, errors);
        }

        if (files.Count > MaxFilesAllowed)
        {
            errors.Add($"Maximum {MaxFilesAllowed} files allowed, but {files.Count} files provided");
        }

        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                errors.Add($"File '{file.FileName}' is empty");
                continue;
            }

            var isImage = IsImageFile(file);
            var isVideo = IsVideoFile(file);

            if (!isImage && !isVideo)
            {
                errors.Add($"File '{file.FileName}' has unsupported type. Supported: images (JPG, PNG, GIF, WebP) and videos (MP4, AVI, MOV, WMV, FLV, WebM, MKV)");
                continue;
            }

            if (isImage && file.Length > MaxImageSizeBytes)
            {
                errors.Add($"Image '{file.FileName}' exceeds maximum size of {MaxImageSizeBytes / (1024 * 1024)}MB");
            }

            if (isVideo && file.Length > MaxVideoSizeBytes)
            {
                errors.Add($"Video '{file.FileName}' exceeds maximum size of {MaxVideoSizeBytes / (1024 * 1024)}MB");
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    private bool IsImageFile(IFormFile file)
    {
        if (file == null) return false;

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var mimeType = file.ContentType?.ToLowerInvariant();

        return !string.IsNullOrEmpty(extension) && 
               _allowedImageExtensions.Contains(extension) &&
               !string.IsNullOrEmpty(mimeType) &&
               _allowedImageMimeTypes.Contains(mimeType);
    }

    private bool IsVideoFile(IFormFile file)
    {
        if (file == null) return false;

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var mimeType = file.ContentType?.ToLowerInvariant();

        return !string.IsNullOrEmpty(extension) && 
               _allowedVideoExtensions.Contains(extension) &&
               !string.IsNullOrEmpty(mimeType) &&
               _allowedVideoMimeTypes.Contains(mimeType);
    }

    private string GenerateImageUrl(string fileName)
    {
        // This should be updated to use proper URL generation with HttpContext
        return $"/api/images/{fileName}";
    }

    private string GenerateVideoUrl(string fileName)
    {
        // This should be updated to use proper URL generation with HttpContext
        return $"/api/videos/{fileName}";
    }
}
