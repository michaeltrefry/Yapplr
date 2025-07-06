using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IImageService
{
    Task<string> SaveImageAsync(IFormFile file);
    bool DeleteImage(string fileName);
    bool IsValidImageFile(IFormFile file);
}

// Storage abstraction interfaces
public interface IStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string directory, string? fileName = null);
    Task<string> SaveFileAsync(Stream stream, string directory, string fileName);
    Task<bool> DeleteFileAsync(string directory, string fileName);
    Task<bool> FileExistsAsync(string directory, string fileName);
    Task<Stream> GetFileStreamAsync(string directory, string fileName);
    string GetFileUrl(string directory, string fileName);
    Task<long> GetFileSizeAsync(string directory, string fileName);
}

public interface IVideoService
{
    Task<VideoUploadResult> SaveVideoAsync(IFormFile file);
    bool IsValidVideoFile(IFormFile file);
    Task<VideoProcessingJobRequest> QueueVideoProcessingAsync(string fileName, int userId, int? postId = null, int? messageId = null);
}

public class VideoUploadResult
{
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class VideoProcessingJobRequest
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int? PostId { get; set; }
    public int? MessageId { get; set; }
    public VideoProcessingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

// Note: VideoProcessingStatus enum is defined in Models/Post.cs to avoid conflicts
