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
    private readonly IUploadSettingsService _uploadSettingsService;
    private readonly ILogger<MultipleFileUploadService> _logger;
    private readonly UploadsConfiguration _uploadsConfig;

    public async Task<int> GetMaxFilesAllowedAsync() => await _uploadSettingsService.GetMaxMediaFilesPerPostAsync();
    public async Task<long> GetMaxImageSizeBytesAsync() => await _uploadSettingsService.GetMaxImageSizeBytesAsync();
    public async Task<long> GetMaxVideoSizeBytesAsync() => await _uploadSettingsService.GetMaxVideoSizeBytesAsync();

    // Keep synchronous properties for backward compatibility
    public int MaxFilesAllowed => GetMaxFilesAllowedAsync().GetAwaiter().GetResult();
    public long MaxImageSizeBytes => GetMaxImageSizeBytesAsync().GetAwaiter().GetResult();
    public long MaxVideoSizeBytes => GetMaxVideoSizeBytesAsync().GetAwaiter().GetResult();
    
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
        IUploadSettingsService uploadSettingsService,
        ILogger<MultipleFileUploadService> logger,
        IOptions<UploadsConfiguration> uploadsConfig)
    {
        _imageService = imageService;
        _videoService = videoService;
        _uploadSettingsService = uploadSettingsService;
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

    public async Task<ValidationResult> ValidateMultipleFilesAsync(IFormFileCollection files)
    {
        var errors = new List<string>();

        if (files == null || files.Count == 0)
        {
            errors.Add("No files provided");
            return new ValidationResult(false, errors);
        }

        var maxFilesAllowed = await GetMaxFilesAllowedAsync();
        if (files.Count > maxFilesAllowed)
        {
            errors.Add($"Maximum {maxFilesAllowed} files allowed, but {files.Count} files provided");
        }

        var maxImageSize = await GetMaxImageSizeBytesAsync();
        var maxVideoSize = await GetMaxVideoSizeBytesAsync();
        var allowedImageExtensions = await _uploadSettingsService.GetAllowedImageExtensionsAsync();
        var allowedVideoExtensions = await _uploadSettingsService.GetAllowedVideoExtensionsAsync();

        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                errors.Add($"File '{file.FileName}' is empty");
                continue;
            }

            var isImage = await IsImageFileAsync(file, allowedImageExtensions);
            var isVideo = await IsVideoFileAsync(file, allowedVideoExtensions);

            if (!isImage && !isVideo)
            {
                var imageExts = string.Join(", ", allowedImageExtensions);
                var videoExts = string.Join(", ", allowedVideoExtensions);
                errors.Add($"File '{file.FileName}' has unsupported type. Supported images: {imageExts}. Supported videos: {videoExts}");
                continue;
            }

            if (isImage && file.Length > maxImageSize)
            {
                errors.Add($"Image '{file.FileName}' exceeds maximum size of {maxImageSize / (1024 * 1024)}MB");
            }

            if (isVideo && file.Length > maxVideoSize)
            {
                errors.Add($"Video '{file.FileName}' exceeds maximum size of {maxVideoSize / (1024 * 1024 * 1024)}GB");
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    // Keep synchronous version for backward compatibility
    [Obsolete("Use ValidateMultipleFilesAsync instead")]
    public ValidationResult ValidateMultipleFiles(IFormFileCollection files)
    {
        return ValidateMultipleFilesAsync(files).GetAwaiter().GetResult();
    }

    private async Task<bool> IsImageFileAsync(IFormFile file, string[] allowedExtensions)
    {
        if (file == null) return false;

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var mimeType = file.ContentType?.ToLowerInvariant();

        return !string.IsNullOrEmpty(extension) &&
               allowedExtensions.Contains(extension) &&
               !string.IsNullOrEmpty(mimeType) &&
               _allowedImageMimeTypes.Contains(mimeType);
    }

    private async Task<bool> IsVideoFileAsync(IFormFile file, string[] allowedExtensions)
    {
        if (file == null) return false;

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var mimeType = file.ContentType?.ToLowerInvariant();

        return !string.IsNullOrEmpty(extension) &&
               allowedExtensions.Contains(extension) &&
               !string.IsNullOrEmpty(mimeType) &&
               _allowedVideoMimeTypes.Contains(mimeType);
    }

    // Keep synchronous versions for backward compatibility
    private bool IsImageFile(IFormFile file)
    {
        if (file == null) return false;

        var allowedExtensions = _uploadSettingsService.GetAllowedImageExtensionsAsync().GetAwaiter().GetResult();
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var mimeType = file.ContentType?.ToLowerInvariant();

        return !string.IsNullOrEmpty(extension) &&
               allowedExtensions.Contains(extension) &&
               !string.IsNullOrEmpty(mimeType) &&
               _allowedImageMimeTypes.Contains(mimeType);
    }

    private bool IsVideoFile(IFormFile file)
    {
        if (file == null) return false;

        var allowedExtensions = _uploadSettingsService.GetAllowedVideoExtensionsAsync().GetAwaiter().GetResult();
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var mimeType = file.ContentType?.ToLowerInvariant();

        return !string.IsNullOrEmpty(extension) &&
               allowedExtensions.Contains(extension) &&
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
