using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class VideoService : IVideoService
{
    private readonly IStorageService _storageService;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ILogger<VideoService> _logger;
    private readonly string[] _allowedExtensions = { ".mp4", ".webm", ".mov", ".avi", ".mkv" };
    private readonly string[] _allowedMimeTypes = { 
        "video/mp4", 
        "video/webm", 
        "video/quicktime", 
        "video/x-msvideo", 
        "video/x-matroska" 
    };
    private readonly long _maxFileSize = 100 * 1024 * 1024; // 100MB
    private const string VideoDirectory = "videos";

    public VideoService(
        IStorageService storageService, 
        IMessageQueueService messageQueueService,
        ILogger<VideoService> logger)
    {
        _storageService = storageService;
        _messageQueueService = messageQueueService;
        _logger = logger;
    }

    public async Task<VideoUploadResult> SaveVideoAsync(IFormFile file)
    {
        if (!IsValidVideoFile(file))
        {
            throw new ArgumentException("Invalid video file");
        }

        try
        {
            var fileName = await _storageService.SaveFileAsync(file, VideoDirectory);
            
            var result = new VideoUploadResult
            {
                FileName = fileName,
                SizeBytes = file.Length,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType
            };

            _logger.LogInformation("Video uploaded successfully: {FileName}, Size: {Size} bytes", 
                fileName, file.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save video file: {FileName}", file.FileName);
            throw;
        }
    }

    public bool IsValidVideoFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Video file is null or empty");
            return false;
        }

        if (file.Length > _maxFileSize)
        {
            _logger.LogWarning("Video file too large: {Size} bytes (max: {MaxSize})", 
                file.Length, _maxFileSize);
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Video file extension not allowed: {Extension}", extension);
            return false;
        }

        if (!_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            _logger.LogWarning("Video MIME type not allowed: {MimeType}", file.ContentType);
            return false;
        }

        // Additional validation: Check file signature
        return IsValidVideoSignature(file);
    }

    public async Task<VideoProcessingJobRequest> QueueVideoProcessingAsync(string fileName, int userId, int? postId = null, int? messageId = null)
    {
        var job = new VideoProcessingJobRequest
        {
            FileName = fileName,
            UserId = userId,
            PostId = postId,
            MessageId = messageId,
            Status = VideoProcessingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Queue the job for processing
            await _messageQueueService.QueueVideoProcessingAsync(job);
            
            _logger.LogInformation("Video processing job queued: {FileName} for user {UserId}", 
                fileName, userId);

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue video processing job: {FileName}", fileName);
            throw;
        }
    }

    private bool IsValidVideoSignature(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[12];
            var bytesRead = stream.Read(buffer, 0, 12);
            stream.Position = 0;

            if (bytesRead < 4) return false;

            // MP4: Check for ftyp box
            if (bytesRead >= 8 && 
                buffer[4] == 0x66 && buffer[5] == 0x74 && 
                buffer[6] == 0x79 && buffer[7] == 0x70)
            {
                return true;
            }

            // WebM: Check for EBML signature
            if (buffer[0] == 0x1A && buffer[1] == 0x45 && 
                buffer[2] == 0xDF && buffer[3] == 0xA3)
            {
                return true;
            }

            // AVI: Check for RIFF + AVI
            if (bytesRead >= 12 &&
                buffer[0] == 0x52 && buffer[1] == 0x49 && 
                buffer[2] == 0x46 && buffer[3] == 0x46 &&
                buffer[8] == 0x41 && buffer[9] == 0x56 && 
                buffer[10] == 0x49 && buffer[11] == 0x20)
            {
                return true;
            }

            // QuickTime: Check for various QuickTime signatures
            if (bytesRead >= 8)
            {
                var qtSignatures = new byte[][]
                {
                    new byte[] { 0x66, 0x74, 0x79, 0x70, 0x71, 0x74 }, // ftyp qt
                    new byte[] { 0x6D, 0x6F, 0x6F, 0x76 }, // moov
                    new byte[] { 0x6D, 0x64, 0x61, 0x74 }  // mdat
                };

                foreach (var signature in qtSignatures)
                {
                    bool matches = true;
                    for (int i = 0; i < signature.Length && i + 4 < bytesRead; i++)
                    {
                        if (buffer[i + 4] != signature[i])
                        {
                            matches = false;
                            break;
                        }
                    }
                    if (matches) return true;
                }
            }

            _logger.LogWarning("Video file signature validation failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating video file signature");
            return false;
        }
    }
}
