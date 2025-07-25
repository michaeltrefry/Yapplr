using Microsoft.Extensions.Options;
using Yapplr.Api.Configuration;
using Serilog.Context;

namespace Yapplr.Api.Services;

public class ImageService : IImageService
{
    private readonly string _uploadPath;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
    private readonly ILogger<ImageService> _logger;

    public ImageService(IOptions<UploadsConfiguration> uploadsConfig, ILogger<ImageService> logger)
    {
        _logger = logger;
        var config = uploadsConfig.Value;
        _uploadPath = Path.GetFullPath(Path.Combine(config.BasePath, "images"));

        // Create uploads directory if it doesn't exist
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
            _logger.LogInformation("Created image upload directory: {UploadPath}", _uploadPath);
        }
    }

    public async Task<string> SaveImageAsync(IFormFile file)
    {
        using var operationScope = LogContext.PushProperty("Operation", "SaveImage");
        using var originalNameScope = LogContext.PushProperty("OriginalFileName", file.FileName);
        using var fileSizeScope = LogContext.PushProperty("FileSize", file.Length);
        using var contentTypeScope = LogContext.PushProperty("ContentType", file.ContentType);

        _logger.LogInformation("Saving image file {OriginalFileName} ({FileSize} bytes, {ContentType})",
            file.FileName, file.Length, file.ContentType);

        if (!IsValidImageFile(file))
        {
            _logger.LogWarning("Image upload rejected: Invalid file {OriginalFileName}", file.FileName);
            throw new ArgumentException("Invalid image file");
        }

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using var fileNameScope = LogContext.PushProperty("GeneratedFileName", fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("Image saved successfully as {GeneratedFileName}", fileName);
        return fileName;
    }

    public bool DeleteImage(string fileName)
    {
        using var operationScope = LogContext.PushProperty("Operation", "DeleteImage");
        using var fileNameScope = LogContext.PushProperty("FileName", fileName);

        if (string.IsNullOrEmpty(fileName))
        {
            _logger.LogWarning("Image deletion failed: Empty filename provided");
            return false;
        }

        var filePath = Path.Combine(_uploadPath, fileName);

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _logger.LogInformation("Image deleted successfully: {FileName}", fileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image file: {FileName}", fileName);
                return false;
            }
        }

        _logger.LogWarning("Image deletion failed: File not found {FileName}", fileName);
        return false;
    }

    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > _maxFileSize)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return false;

        // Basic MIME type check
        var allowedMimeTypes = new[]
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "image/webp"
        };

        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        // Additional security check - verify file signature
        return IsValidImageSignature(file);
    }

    private bool IsValidImageSignature(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[8];
            stream.ReadExactly(buffer, 0, 8);
            stream.Position = 0;

            // Check for common image file signatures
            // JPEG: FF D8 FF
            if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
                return true;

            // GIF: 47 49 46 38
            if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x38)
                return true;

            // WebP: 52 49 46 46 (RIFF) + WEBP at offset 8
            if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46)
            {
                // Need to check for WEBP signature at offset 8
                stream.Position = 8;
                var webpBuffer = new byte[4];
                stream.ReadExactly(webpBuffer, 0, 4);
                return webpBuffer[0] == 0x57 && webpBuffer[1] == 0x45 && webpBuffer[2] == 0x42 && webpBuffer[3] == 0x50;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
