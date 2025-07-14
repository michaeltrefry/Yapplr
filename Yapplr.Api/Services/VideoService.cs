using Yapplr.Shared.Models;
using Yapplr.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Yapplr.Api.Services;

public class VideoService : IVideoService
{
    private readonly string _uploadPath;
    private readonly string _processedPath;
    private readonly string _thumbnailPath;
    private readonly string[] _allowedExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
    private readonly string[] _allowedMimeTypes = { 
        "video/mp4", "video/avi", "video/quicktime", "video/x-ms-wmv", 
        "video/x-flv", "video/webm", "video/x-matroska" 
    };
    private readonly long _maxFileSize = 100 * 1024 * 1024; // 100MB
    private readonly ILogger<VideoService> _logger;

    public VideoService(IOptions<UploadsConfiguration> uploadsConfig, ILogger<VideoService> logger)
    {
        _logger = logger;
        var config = uploadsConfig.Value;

        _uploadPath = Path.GetFullPath(config.GetVideosFullPath());
        _processedPath = Path.GetFullPath(config.GetProcessedFullPath());
        _thumbnailPath = Path.GetFullPath(config.GetThumbnailsFullPath());

        // Create directories if they don't exist
        Directory.CreateDirectory(_uploadPath);
        Directory.CreateDirectory(_processedPath);
        Directory.CreateDirectory(_thumbnailPath);

        _logger.LogInformation("VideoService initialized with paths - Videos: {VideosPath}, Processed: {ProcessedPath}, Thumbnails: {ThumbnailsPath}",
            _uploadPath, _processedPath, _thumbnailPath);
    }

    public async Task<string> SaveVideoAsync(IFormFile? file)
    {
        if (file == null || !IsValidVideoFile(file))
        {
            throw new ArgumentException("Invalid video file");
        }

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(_uploadPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("Video saved: {FileName} ({Size} bytes)", fileName, file.Length);
        return fileName;
    }

    public bool DeleteVideo(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var filePath = Path.Combine(_uploadPath, fileName);
        return DeleteFileIfExists(filePath);
    }

    public bool DeleteProcessedVideo(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var filePath = Path.Combine(_processedPath, fileName);
        return DeleteFileIfExists(filePath);
    }

    public bool DeleteVideoThumbnail(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var filePath = Path.Combine(_thumbnailPath, fileName);
        return DeleteFileIfExists(filePath);
    }

    public bool IsValidVideoFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > _maxFileSize)
        {
            _logger.LogWarning("Video file too large: {Size} bytes (max: {MaxSize})", file.Length, _maxFileSize);
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Invalid video extension: {Extension}", extension);
            return false;
        }

        if (!_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            _logger.LogWarning("Invalid video MIME type: {MimeType}", file.ContentType);
            return false;
        }

        return true;
    }

    public Task<VideoUploadResponse> GetVideoUploadResponseAsync(string fileName, HttpContext httpContext)
    {
        var videoUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/videos/{fileName}";
        var filePath = Path.Combine(_uploadPath, fileName);

        var fileInfo = new FileInfo(filePath);

        // Return basic file info immediately after upload
        // The VideoProcessor will extract detailed metadata during processing
        // and store it in the Post model via the VideoProcessingCompleted message
        return Task.FromResult(new VideoUploadResponse
        {
            FileName = fileName,
            VideoUrl = videoUrl,
            FileSizeBytes = fileInfo.Length,
            // Duration, Width, Height will be populated after video processing completes
            Duration = null,
            Width = null,
            Height = null
        });
    }

    private bool DeleteFileIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                return false;
            }
        }

        return false;
    }
}
