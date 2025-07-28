using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Serilog.Context;
using Yapplr.Shared.Models;

namespace Yapplr.VideoProcessor.Services;

/// <summary>
/// Simple video processing service using direct FFmpeg process execution
/// This replaces the FFMpegCore-based implementation to fix rotation and scaling issues
/// </summary>
public class SimpleVideoProcessingService : IVideoProcessingService
{
    private readonly ILogger<SimpleVideoProcessingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public SimpleVideoProcessingService(
        ILogger<SimpleVideoProcessingService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Get FFmpeg paths from configuration
        _ffmpegPath = _configuration["FFmpeg:BinaryPath"] ?? "ffmpeg";
        _ffprobePath = _configuration["FFmpeg:ProbePath"] ?? "ffprobe";
    }

    public async Task<VideoProcessingResult> ProcessVideoAsync(
        string inputPath,
        string outputPath,
        string thumbnailPath,
        VideoProcessingConfig config)
    {
        var startTime = DateTime.UtcNow;
        var inputFileName = Path.GetFileName(inputPath);
        var outputFileName = Path.GetFileName(outputPath);

        using var operationScope = LogContext.PushProperty("Operation", "ProcessVideo");
        using var inputScope = LogContext.PushProperty("InputFile", inputFileName);
        using var outputScope = LogContext.PushProperty("OutputFile", outputFileName);
        using var codecScope = LogContext.PushProperty("VideoCodec", config.VideoCodec);
        using var bitrateScope = LogContext.PushProperty("TargetBitrate", config.TargetBitrate);

        try
        {
            _logger.LogInformation("Starting video processing: {InputFile} -> {OutputFile} with codec {VideoCodec}",
                inputFileName, outputFileName, config.VideoCodec);

            // Validate input file exists
            if (!File.Exists(inputPath))
            {
                _logger.LogError("Video processing failed: Input file not found {InputFile}", inputFileName);
                return new VideoProcessingResult
                {
                    Success = false,
                    ErrorMessage = $"Input file not found: {inputPath}",
                    ProcessingDuration = DateTime.UtcNow - startTime
                };
            }

            var inputFileSize = new FileInfo(inputPath).Length;
            using var fileSizeScope = LogContext.PushProperty("InputFileSize", inputFileSize);
            _logger.LogInformation("Processing video file {InputFile} ({InputFileSize} bytes)",
                inputFileName, inputFileSize);

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

            await ProcessVideoFileAsync(inputPath, outputPath, config, originalMetadata);
            await GenerateThumbnailAsync(inputPath, thumbnailPath, config, originalMetadata);

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
                CompressionRatio = originalMetadata.OriginalFileSizeBytes > 0
                    ? (double)processedMetadata.ProcessedFileSizeBytes / originalMetadata.OriginalFileSizeBytes
                    : 1.0,
                OriginalRotation = originalMetadata.OriginalRotation,
                ProcessedRotation = 0, // Always 0 after processing (rotation is corrected)
                DisplayWidth = processedMetadata.ProcessedWidth,
                DisplayHeight = processedMetadata.ProcessedHeight
            };

            _logger.LogInformation("Video processing completed successfully: {OutputPath}\n{MetaData}", outputPath, metadata);

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
            _logger.LogError(ex, "Video processing failed for {InputFile}: {ErrorMessage}", 
                inputFileName, ex.Message);
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
            var videoInfo = await GetVideoInfoAsync(videoPath);
            if (videoInfo == null)
            {
                return null;
            }

            var fileInfo = new FileInfo(videoPath);
            
            return new VideoMetadata
            {
                OriginalWidth = videoInfo.Width,
                OriginalHeight = videoInfo.Height,
                ProcessedWidth = videoInfo.Width,
                ProcessedHeight = videoInfo.Height,
                OriginalDuration = TimeSpan.FromSeconds(videoInfo.Duration),
                ProcessedDuration = TimeSpan.FromSeconds(videoInfo.Duration),
                OriginalFileSizeBytes = fileInfo.Length,
                ProcessedFileSizeBytes = fileInfo.Length,
                CompressionRatio = 1.0,
                OriginalRotation = videoInfo.Rotation,
                ProcessedRotation = videoInfo.Rotation,
                DisplayWidth = videoInfo.DisplayWidth,
                DisplayHeight = videoInfo.DisplayHeight
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video metadata for {VideoPath}", videoPath);
            return null;
        }
    }

    /// <summary>
    /// Video information extracted from ffprobe
    /// </summary>
    private class VideoInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public double Duration { get; set; }
        public int Rotation { get; set; }
        public int DisplayWidth { get; set; }
        public int DisplayHeight { get; set; }
        public string? PixelFormat { get; set; }
        public string? Codec { get; set; }
    }

    /// <summary>
    /// Get video information using ffprobe
    /// </summary>
    private async Task<VideoInfo?> GetVideoInfoAsync(string videoPath)
    {
        try
        {
            var arguments = $"-v quiet -print_format json -show_format -show_streams \"{videoPath}\"";
            
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
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
                _logger.LogError("ffprobe failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                return null;
            }

            _logger.LogDebug("ffprobe output for {VideoPath}: {Output}", videoPath, output);
            return ParseVideoInfo(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video info for {VideoPath}", videoPath);
            return null;
        }
    }

    /// <summary>
    /// Parse ffprobe JSON output to extract video information
    /// </summary>
    private VideoInfo? ParseVideoInfo(string jsonOutput)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonOutput);
            var root = document.RootElement;

            if (!root.TryGetProperty("streams", out var streams))
            {
                return null;
            }

            // Find the first video stream
            JsonElement? videoStream = null;
            foreach (var streamElement in streams.EnumerateArray())
            {
                if (streamElement.TryGetProperty("codec_type", out var codecType) &&
                    codecType.GetString() == "video")
                {
                    videoStream = streamElement;
                    break;
                }
            }

            if (videoStream == null)
            {
                return null;
            }

            var stream = videoStream.Value;
            
            // Get basic dimensions
            var width = stream.TryGetProperty("width", out var w) ? w.GetInt32() : 0;
            var height = stream.TryGetProperty("height", out var h) ? h.GetInt32() : 0;
            
            // Get duration
            var duration = 0.0;
            if (stream.TryGetProperty("duration", out var d))
            {
                if (d.ValueKind == JsonValueKind.String && double.TryParse(d.GetString(), out var parsedDuration))
                {
                    duration = parsedDuration;
                }
                else if (d.ValueKind == JsonValueKind.Number)
                {
                    duration = d.GetDouble();
                }
            }

            // Get rotation from metadata
            var rotation = GetRotationFromMetadata(stream);
            
            // Calculate display dimensions based on rotation
            var (displayWidth, displayHeight) = CalculateDisplayDimensions(width, height, rotation);

            return new VideoInfo
            {
                Width = width,
                Height = height,
                Duration = duration,
                Rotation = rotation,
                DisplayWidth = displayWidth,
                DisplayHeight = displayHeight,
                PixelFormat = stream.TryGetProperty("pix_fmt", out var pf) ? pf.GetString() : null,
                Codec = stream.TryGetProperty("codec_name", out var cn) ? cn.GetString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse video info JSON");
            return null;
        }
    }

    /// <summary>
    /// Extract rotation information from video stream metadata
    /// Handles conflicts between display matrix and rotate tag (common in iPhone videos)
    /// </summary>
    private int GetRotationFromMetadata(JsonElement stream)
    {
        var rotation = 0;
        var hasDisplayMatrix = false;
        var hasRotateTag = false;
        var displayMatrixRotation = 0;
        var rotateTagRotation = 0;

        // Check for rotation in tags (legacy method)
        if (stream.TryGetProperty("tags", out var tags))
        {
            if (tags.TryGetProperty("rotate", out var rotateTag))
            {
                rotateTagRotation = ParseRotationValue(rotateTag);
                hasRotateTag = true;
                _logger.LogDebug("Found rotate tag: {Rotation}°", rotateTagRotation);
            }
        }

        // Check for side_data_list (newer FFmpeg versions, more reliable)
        if (stream.TryGetProperty("side_data_list", out var sideDataList))
        {
            foreach (var sideData in sideDataList.EnumerateArray())
            {
                if (sideData.TryGetProperty("side_data_type", out var sideDataType) &&
                    sideDataType.GetString() == "Display Matrix")
                {
                    if (sideData.TryGetProperty("rotation", out var rotationValue))
                    {
                        displayMatrixRotation = ParseRotationValue(rotationValue);
                        hasDisplayMatrix = true;
                        _logger.LogDebug("Found display matrix rotation: {Rotation}°", displayMatrixRotation);
                    }
                    break;
                }
            }
        }

        // Resolve conflicts between display matrix and rotate tag
        if (hasDisplayMatrix && hasRotateTag)
        {
            // Both are present - this is the iPhone conflict case
            _logger.LogWarning("Video has conflicting rotation metadata - Display Matrix: {DisplayMatrix}°, Rotate Tag: {RotateTag}°",
                displayMatrixRotation, rotateTagRotation);

            // For iPhone videos, prioritize display matrix but handle negative values
            rotation = displayMatrixRotation;

            // Convert negative display matrix values to positive equivalents
            if (displayMatrixRotation == -90)
            {
                rotation = 270; // -90° = 270° clockwise
            }
            else if (displayMatrixRotation == -180)
            {
                rotation = 180;
            }
            else if (displayMatrixRotation == -270)
            {
                rotation = 90; // -270° = 90° clockwise
            }

            _logger.LogInformation("Resolved rotation conflict: using {Rotation}° (converted from display matrix {DisplayMatrix}°)",
                rotation, displayMatrixRotation);
        }
        else if (hasDisplayMatrix)
        {
            // Only display matrix - use it, handling negative values
            rotation = displayMatrixRotation;
            if (displayMatrixRotation < 0)
            {
                rotation = 360 + displayMatrixRotation; // Convert negative to positive
            }
            _logger.LogDebug("Using display matrix rotation: {Rotation}°", rotation);
        }
        else if (hasRotateTag)
        {
            // Only rotate tag - use it
            rotation = rotateTagRotation;
            _logger.LogDebug("Using rotate tag: {Rotation}°", rotation);
        }
        else
        {
            // No rotation metadata found
            rotation = 0;
            _logger.LogDebug("No rotation metadata found, assuming 0°");
        }

        return NormalizeRotation(rotation);
    }

    /// <summary>
    /// Parse rotation value from JSON element (handles both string and number types)
    /// </summary>
    private int ParseRotationValue(JsonElement rotationElement)
    {
        try
        {
            // Handle numeric rotation values
            if (rotationElement.ValueKind == JsonValueKind.Number)
            {
                return rotationElement.GetInt32();
            }

            // Handle string rotation values
            if (rotationElement.ValueKind == JsonValueKind.String)
            {
                var rotationString = rotationElement.GetString();
                if (int.TryParse(rotationString, out var rotation))
                {
                    return rotation;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse rotation value from JSON element: {ValueKind}",
                rotationElement.ValueKind);
        }

        return 0; // Default to no rotation if parsing fails
    }

    /// <summary>
    /// Calculate display dimensions based on rotation
    /// </summary>
    private (int displayWidth, int displayHeight) CalculateDisplayDimensions(int width, int height, int rotation)
    {
        var normalizedRotation = NormalizeRotation(rotation);

        // For 90° and 270° rotations, swap width and height
        if (normalizedRotation == 90 || normalizedRotation == 270)
        {
            return (height, width);
        }

        return (width, height);
    }

    /// <summary>
    /// Normalize rotation to 0, 90, 180, or 270 degrees
    /// </summary>
    private int NormalizeRotation(int rotation)
    {
        // Handle negative rotations
        while (rotation < 0)
        {
            rotation += 360;
        }

        // Normalize to 0-359 range
        rotation = rotation % 360;

        // Round to nearest 90-degree increment
        if (rotation >= 315 || rotation < 45)
            return 0;
        else if (rotation >= 45 && rotation < 135)
            return 90;
        else if (rotation >= 135 && rotation < 225)
            return 180;
        else
            return 270;
    }

    /// <summary>
    /// Process video file using direct FFmpeg execution
    /// </summary>
    private async Task ProcessVideoFileAsync(string inputPath, string outputPath, VideoProcessingConfig config, VideoMetadata originalMetadata)
    {
        var rotation = originalMetadata.OriginalRotation;
        var normalizedRotation = NormalizeRotation(rotation);

        // Calculate target dimensions based on rotation and scaling
        var (targetWidth, targetHeight) = CalculateTargetDimensions(
            originalMetadata.DisplayWidth,
            originalMetadata.DisplayHeight,
            config.MaxWidth,
            config.MaxHeight);

        _logger.LogInformation("Processing video: {Width}x{Height} rotation={Rotation}° -> {TargetWidth}x{TargetHeight}",
            originalMetadata.OriginalWidth, originalMetadata.OriginalHeight, rotation, targetWidth, targetHeight);

        // Build FFmpeg filter string for rotation and scaling
        var filterString = BuildVideoFilter(normalizedRotation, targetWidth, targetHeight);

        // Build FFmpeg arguments
        var arguments = BuildFFmpegArguments(inputPath, outputPath, config, filterString);

        _logger.LogInformation("FFmpeg command: {FFmpegPath} {Arguments}", _ffmpegPath, arguments);

        // Execute FFmpeg
        await ExecuteFFmpegAsync(arguments, "video processing");
    }

    /// <summary>
    /// Calculate target dimensions for scaling while maintaining aspect ratio
    /// </summary>
    private (int width, int height) CalculateTargetDimensions(int sourceWidth, int sourceHeight, int maxWidth, int maxHeight)
    {
        if (sourceWidth <= maxWidth && sourceHeight <= maxHeight)
        {
            return (sourceWidth, sourceHeight);
        }

        var aspectRatio = (double)sourceWidth / sourceHeight;

        var targetWidth = maxWidth;
        var targetHeight = (int)(maxWidth / aspectRatio);

        if (targetHeight > maxHeight)
        {
            targetHeight = maxHeight;
            targetWidth = (int)(maxHeight * aspectRatio);
        }

        // Ensure even dimensions for video encoding
        targetWidth = targetWidth % 2 == 0 ? targetWidth : targetWidth - 1;
        targetHeight = targetHeight % 2 == 0 ? targetHeight : targetHeight - 1;

        return (targetWidth, targetHeight);
    }

    /// <summary>
    /// Build FFmpeg video filter string for rotation and scaling
    /// </summary>
    private string BuildVideoFilter(int normalizedRotation, int targetWidth, int targetHeight)
    {
        var filters = new List<string>();

        // Check if we should apply physical rotation or just metadata removal
        var applyPhysicalRotation = _configuration["VideoProcessing:ApplyPhysicalRotation"] != "false";

        if (applyPhysicalRotation)
        {
            // Apply rotation first
            switch (normalizedRotation)
            {
                case 90:
                    filters.Add("transpose=2"); // 90° counterclockwise (inverted)
                    break;
                case 180:
                    filters.Add("transpose=1,transpose=1"); // 180°
                    break;
                case 270:
                    filters.Add("transpose=1"); // 90° clockwise (inverted)
                    break;
                // case 0: no rotation needed
            }
        }

        // Add scaling (dimensions should already account for rotation if applied)
        filters.Add($"scale={targetWidth}:{targetHeight}");

        return string.Join(",", filters);
    }

    /// <summary>
    /// Build complete FFmpeg arguments string
    /// </summary>
    private string BuildFFmpegArguments(string inputPath, string outputPath, VideoProcessingConfig config, string filterString)
    {
        var args = new List<string>
        {
            "-i", $"\"{inputPath}\"",
            "-c:v", config.VideoCodec,
            "-c:a", config.AudioCodec,
            "-b:v", $"{config.TargetBitrate}k",
            "-vf", filterString,

            // Mobile compatibility parameters
            "-pix_fmt", "yuv420p",           // Ensure compatible pixel format
            "-profile:v", "baseline",        // H.264 baseline profile for maximum compatibility
            "-level", "3.1",                 // H.264 level 3.1 (supports up to 1920x1080)
            "-maxrate", $"{config.TargetBitrate * 2}k",  // Maximum bitrate (2x target)
            "-bufsize", $"{config.TargetBitrate * 4}k",  // Buffer size (4x target)

            // Container optimization
            "-movflags", "+faststart",       // Enable fast start for web streaming
            "-avoid_negative_ts", "make_zero", // Fix timestamp issues

            // Completely strip rotation metadata - most reliable approach
            "-map_metadata", "-1",           // Remove all metadata first
            "-metadata:s:v:0", "rotate=0",   // Explicitly set video stream rotation to 0
            "-metadata", "rotate=0",         // Set global rotation to 0
            "-disposition:v:0", "default",   // Reset video disposition

            "-y", // Overwrite output file
            $"\"{outputPath}\""
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

    /// <summary>
    /// Generate thumbnail using direct FFmpeg execution
    /// </summary>
    private async Task GenerateThumbnailAsync(string inputPath, string thumbnailPath, VideoProcessingConfig config, VideoMetadata originalMetadata)
    {
        var rotation = originalMetadata.OriginalRotation;
        var normalizedRotation = NormalizeRotation(rotation);

        // Calculate thumbnail dimensions based on rotation
        var (thumbnailWidth, thumbnailHeight) = CalculateTargetDimensions(
            originalMetadata.DisplayWidth,
            originalMetadata.DisplayHeight,
            config.ThumbnailWidth,
            config.ThumbnailHeight);

        _logger.LogInformation("Generating thumbnail: {Width}x{Height} rotation={Rotation}° -> {ThumbnailWidth}x{ThumbnailHeight}",
            originalMetadata.OriginalWidth, originalMetadata.OriginalHeight, rotation, thumbnailWidth, thumbnailHeight);

        // Build thumbnail filter string
        var filterString = BuildVideoFilter(normalizedRotation, thumbnailWidth, thumbnailHeight);

        // Build FFmpeg arguments for thumbnail
        var arguments = BuildThumbnailArguments(inputPath, thumbnailPath, config, filterString);

        _logger.LogInformation("FFmpeg thumbnail command: {FFmpegPath} {Arguments}", _ffmpegPath, arguments);

        // Execute FFmpeg
        await ExecuteFFmpegAsync(arguments, "thumbnail generation");
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
}
