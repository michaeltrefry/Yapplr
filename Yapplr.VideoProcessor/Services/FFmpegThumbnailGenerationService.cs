using System.Diagnostics;
using Yapplr.Shared.Models;
using Serilog.Context;

namespace Yapplr.VideoProcessor.Services;

/// <summary>
/// FFmpeg-based thumbnail generation service
/// </summary>
public class FFmpegThumbnailGenerationService : IThumbnailGenerationService
{
    private readonly ILogger<FFmpegThumbnailGenerationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _ffmpegPath;

    public FFmpegThumbnailGenerationService(
        ILogger<FFmpegThumbnailGenerationService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Get FFmpeg path from configuration
        _ffmpegPath = _configuration["FFmpeg:BinaryPath"] ?? "ffmpeg";
    }

    public async Task GenerateThumbnailAsync(string videoPath, string thumbnailPath, VideoProcessingConfig config, VideoMetadata videoMetadata)
    {
        var thumbnailFileName = Path.GetFileName(thumbnailPath);
        
        using var operationScope = LogContext.PushProperty("Operation", "GenerateThumbnail");
        using var videoScope = LogContext.PushProperty("VideoFile", Path.GetFileName(videoPath));
        using var thumbnailScope = LogContext.PushProperty("ThumbnailFile", thumbnailFileName);

        try
        {
            // Validate input video file exists
            if (!File.Exists(videoPath))
            {
                throw new FileNotFoundException($"Video file not found: {videoPath}");
            }

            // Ensure thumbnail output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

            // Calculate thumbnail dimensions (no rotation needed since processed video is correctly oriented)
            var (thumbnailWidth, thumbnailHeight) = CalculateTargetDimensions(
                videoMetadata.DisplayWidth,
                videoMetadata.DisplayHeight,
                config.ThumbnailWidth,
                config.ThumbnailHeight);

            _logger.LogInformation("Generating thumbnail from video: {VideoPath} -> {ThumbnailWidth}x{ThumbnailHeight}",
                videoPath, thumbnailWidth, thumbnailHeight);

            // Build simple scale filter (no rotation needed)
            var filterString = $"scale={thumbnailWidth}:{thumbnailHeight}";

            // Build FFmpeg arguments for thumbnail
            var arguments = BuildThumbnailArguments(videoPath, thumbnailPath, config, filterString);

            _logger.LogInformation("FFmpeg thumbnail command: {FFmpegPath} {Arguments}", _ffmpegPath, arguments);

            // Execute FFmpeg for thumbnail generation
            await ExecuteFFmpegAsync(arguments, "thumbnail generation");

            _logger.LogInformation("Thumbnail generated successfully: {ThumbnailPath}", thumbnailPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for video {VideoPath}", videoPath);
            throw;
        }
    }

    /// <summary>
    /// Calculate target dimensions while maintaining aspect ratio
    /// </summary>
    private (int width, int height) CalculateTargetDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
        {
            return (originalWidth, originalHeight);
        }

        var aspectRatio = (double)originalWidth / originalHeight;

        var targetWidth = maxWidth;
        var targetHeight = (int)(maxWidth / aspectRatio);

        if (targetHeight > maxHeight)
        {
            targetHeight = maxHeight;
            targetWidth = (int)(maxHeight * aspectRatio);
        }

        // Ensure dimensions are even (required for most video codecs)
        targetWidth = targetWidth % 2 == 0 ? targetWidth : targetWidth - 1;
        targetHeight = targetHeight % 2 == 0 ? targetHeight : targetHeight - 1;

        return (targetWidth, targetHeight);
    }

    /// <summary>
    /// Build FFmpeg arguments for thumbnail generation
    /// </summary>
    private string BuildThumbnailArguments(string inputPath, string thumbnailPath, VideoProcessingConfig config, string filterString)
    {
        var args = new List<string>
        {
            "-i", $"\"{inputPath}\"",
            "-vf", filterString,
            "-vframes", "1", // Extract only one frame
            "-ss", config.ThumbnailTimeSeconds.ToString("F1"), // Seek to specified time
            "-y", // Overwrite output file
            $"\"{thumbnailPath}\""
        };

        return string.Join(" ", args);
    }

    /// <summary>
    /// Execute FFmpeg with the given arguments
    /// </summary>
    private async Task ExecuteFFmpegAsync(string arguments, string operation)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogError("FFmpeg {Operation} failed with exit code {ExitCode}: {Error}",
                operation, process.ExitCode, error);
            throw new InvalidOperationException($"FFmpeg {operation} failed: {error}");
        }

        _logger.LogDebug("FFmpeg {Operation} completed successfully", operation);
    }
}
