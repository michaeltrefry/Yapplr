using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Yapplr.Shared.Models;

namespace Yapplr.VideoProcessor.Services;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly IConfiguration _configuration;

    public VideoProcessingService(ILogger<VideoProcessingService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Configure FFMpeg binary path
        var ffmpegPath = _configuration["FFmpeg:BinaryPath"];
        if (!string.IsNullOrEmpty(ffmpegPath) && File.Exists(ffmpegPath))
        {
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = Path.GetDirectoryName(ffmpegPath) });
        }
    }

    public async Task<VideoProcessingResult> ProcessVideoAsync(
        string inputPath, 
        string outputPath, 
        string thumbnailPath, 
        VideoProcessingConfig config)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Starting video processing: {InputPath} -> {OutputPath}", inputPath, outputPath);

            // Validate input file exists
            if (!File.Exists(inputPath))
            {
                return new VideoProcessingResult
                {
                    Success = false,
                    ErrorMessage = $"Input file not found: {inputPath}",
                    ProcessingDuration = DateTime.UtcNow - startTime
                };
            }

            // Get original video metadata
            var originalMetadata = await GetVideoMetadataAsync(inputPath);
            if (originalMetadata == null)
            {
                return new VideoProcessingResult
                {
                    Success = false,
                    ErrorMessage = "Failed to read video metadata",
                    ProcessingDuration = DateTime.UtcNow - startTime
                };
            }

            // Ensure output directories exist
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

            // Process video
            var processedFileName = Path.GetFileName(outputPath);
            var thumbnailFileName = Path.GetFileName(thumbnailPath);

            await ProcessVideoFileAsync(inputPath, outputPath, config);
            await GenerateThumbnailAsync(inputPath, thumbnailPath, config);

            // Get processed video metadata
            var processedMetadata = await GetVideoMetadataAsync(outputPath);
            if (processedMetadata == null)
            {
                return new VideoProcessingResult
                {
                    Success = false,
                    ErrorMessage = "Failed to read processed video metadata",
                    ProcessingDuration = DateTime.UtcNow - startTime
                };
            }

            // Create combined metadata
            var metadata = new VideoMetadata
            {
                OriginalWidth = originalMetadata.OriginalWidth,
                OriginalHeight = originalMetadata.OriginalHeight,
                ProcessedWidth = processedMetadata.ProcessedWidth,
                ProcessedHeight = processedMetadata.ProcessedHeight,
                OriginalDuration = originalMetadata.OriginalDuration,
                ProcessedDuration = processedMetadata.ProcessedDuration,
                OriginalFileSizeBytes = originalMetadata.OriginalFileSizeBytes,
                ProcessedFileSizeBytes = processedMetadata.ProcessedFileSizeBytes,
                OriginalFormat = originalMetadata.OriginalFormat,
                ProcessedFormat = processedMetadata.ProcessedFormat,
                OriginalBitrate = originalMetadata.OriginalBitrate,
                ProcessedBitrate = processedMetadata.ProcessedBitrate,
                CompressionRatio = originalMetadata.OriginalFileSizeBytes > 0 
                    ? (double)processedMetadata.ProcessedFileSizeBytes / originalMetadata.OriginalFileSizeBytes 
                    : 1.0
            };

            _logger.LogInformation("Video processing completed successfully: {OutputPath}", outputPath);

            return new VideoProcessingResult
            {
                ProcessedVideoFileName = processedFileName,
                ThumbnailFileName = thumbnailFileName,
                Metadata = metadata,
                Success = true,
                ProcessingDuration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video: {InputPath}", inputPath);
            
            return new VideoProcessingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingDuration = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<VideoMetadata?> GetVideoMetadataAsync(string videoPath)
    {
        try
        {
            var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
            var videoStream = mediaInfo.PrimaryVideoStream;
            var fileInfo = new FileInfo(videoPath);

            if (videoStream == null)
            {
                return null;
            }

            return new VideoMetadata
            {
                OriginalWidth = videoStream.Width,
                OriginalHeight = videoStream.Height,
                ProcessedWidth = videoStream.Width,
                ProcessedHeight = videoStream.Height,
                OriginalDuration = mediaInfo.Duration,
                ProcessedDuration = mediaInfo.Duration,
                OriginalFileSizeBytes = fileInfo.Length,
                ProcessedFileSizeBytes = fileInfo.Length,
                OriginalFormat = Path.GetExtension(videoPath).TrimStart('.'),
                ProcessedFormat = Path.GetExtension(videoPath).TrimStart('.'),
                OriginalBitrate = videoStream.BitRate,
                ProcessedBitrate = videoStream.BitRate,
                CompressionRatio = 1.0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video metadata: {VideoPath}", videoPath);
            return null;
        }
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateVideoAsync(
        string videoPath, 
        long maxFileSizeBytes, 
        int maxDurationSeconds)
    {
        try
        {
            if (!File.Exists(videoPath))
            {
                return (false, "Video file not found");
            }

            var fileInfo = new FileInfo(videoPath);
            if (fileInfo.Length > maxFileSizeBytes)
            {
                return (false, $"Video file too large: {fileInfo.Length} bytes (max: {maxFileSizeBytes} bytes)");
            }

            var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
            if (mediaInfo.Duration.TotalSeconds > maxDurationSeconds)
            {
                return (false, $"Video too long: {mediaInfo.Duration.TotalSeconds:F1} seconds (max: {maxDurationSeconds} seconds)");
            }

            if (mediaInfo.PrimaryVideoStream == null)
            {
                return (false, "No video stream found in file");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating video: {VideoPath}", videoPath);
            return (false, $"Validation error: {ex.Message}");
        }
    }

    private async Task ProcessVideoFileAsync(string inputPath, string outputPath, VideoProcessingConfig config)
    {
        // Get original video metadata to calculate proper scaling
        var mediaInfo = await FFProbe.AnalyseAsync(inputPath);
        var videoStream = mediaInfo.PrimaryVideoStream;

        if (videoStream == null)
        {
            throw new InvalidOperationException("No video stream found in input file");
        }

        // Calculate scaling dimensions while maintaining aspect ratio
        var originalWidth = videoStream.Width;
        var originalHeight = videoStream.Height;
        var aspectRatio = (double)originalWidth / originalHeight;

        int targetWidth, targetHeight;

        if (originalWidth > config.MaxWidth || originalHeight > config.MaxHeight)
        {
            // Scale down while maintaining aspect ratio
            if (aspectRatio > (double)config.MaxWidth / config.MaxHeight)
            {
                // Width is the limiting factor
                targetWidth = config.MaxWidth;
                targetHeight = (int)(config.MaxWidth / aspectRatio);
            }
            else
            {
                // Height is the limiting factor
                targetHeight = config.MaxHeight;
                targetWidth = (int)(config.MaxHeight * aspectRatio);
            }
        }
        else
        {
            // Keep original dimensions if smaller than max
            targetWidth = originalWidth;
            targetHeight = originalHeight;
        }

        await FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, true, options => options
                .WithVideoCodec(config.VideoCodec)
                .WithAudioCodec(config.AudioCodec)
                .WithVideoBitrate(config.TargetBitrate)
                .WithVideoFilters(filterOptions => filterOptions
                    .Scale(targetWidth, targetHeight))
                .WithFastStart())
            .ProcessAsynchronously();
    }

    private async Task GenerateThumbnailAsync(string inputPath, string thumbnailPath, VideoProcessingConfig config)
    {
        // Get original video metadata to calculate proper thumbnail scaling
        var mediaInfo = await FFProbe.AnalyseAsync(inputPath);
        var videoStream = mediaInfo.PrimaryVideoStream;

        if (videoStream == null)
        {
            throw new InvalidOperationException("No video stream found in input file");
        }

        // Calculate thumbnail dimensions while maintaining aspect ratio
        var originalWidth = videoStream.Width;
        var originalHeight = videoStream.Height;
        var aspectRatio = (double)originalWidth / originalHeight;

        int thumbnailWidth, thumbnailHeight;

        if (aspectRatio > (double)config.ThumbnailWidth / config.ThumbnailHeight)
        {
            // Width is the limiting factor
            thumbnailWidth = config.ThumbnailWidth;
            thumbnailHeight = (int)(config.ThumbnailWidth / aspectRatio);
        }
        else
        {
            // Height is the limiting factor
            thumbnailHeight = config.ThumbnailHeight;
            thumbnailWidth = (int)(config.ThumbnailHeight * aspectRatio);
        }

        await FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(thumbnailPath, true, options => options
                .WithVideoFilters(filterOptions => filterOptions
                    .Scale(thumbnailWidth, thumbnailHeight))
                .WithFrameOutputCount(1)
                .Seek(TimeSpan.FromSeconds(config.ThumbnailTimeSeconds)))
            .ProcessAsynchronously();
    }
}
