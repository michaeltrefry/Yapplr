using System.Diagnostics;
using System.Text.Json;
using Yapplr.Shared.Models;
using Serilog.Context;

namespace Yapplr.VideoProcessor.Services;

/// <summary>
/// Video processing service using HandBrake CLI for transcoding and FFmpeg for metadata/thumbnails
/// This hybrid approach leverages HandBrake's superior transcoding quality while maintaining
/// compatibility with existing metadata extraction and thumbnail generation functionality.
/// </summary>
public class HandBrakeVideoProcessingService : IVideoProcessingService
{
    private readonly ILogger<HandBrakeVideoProcessingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IThumbnailGenerationService _thumbnailService;
    private readonly string _handBrakePath;
    private readonly string _ffprobePath;

    public HandBrakeVideoProcessingService(
        ILogger<HandBrakeVideoProcessingService> logger,
        IConfiguration configuration,
        IThumbnailGenerationService thumbnailService)
    {
        _logger = logger;
        _configuration = configuration;
        _thumbnailService = thumbnailService;

        // Get tool paths from configuration
        _handBrakePath = _configuration["HandBrake:BinaryPath"] ?? "HandBrakeCLI";
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

        _logger.LogInformation("Starting HandBrake video processing: {InputPath} -> {OutputPath}", 
            inputPath, outputPath);

        try
        {
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

            // Get original video metadata using FFprobe (HandBrake doesn't provide detailed metadata extraction)
            var originalMetadata = await GetVideoMetadataAsync(inputPath);
            if (originalMetadata == null)
            {
                return new VideoProcessingResult
                {
                    Success = false,
                    ErrorMessage = "Failed to read original video metadata",
                    ProcessingDuration = DateTime.UtcNow - startTime
                };
            }

            _logger.LogInformation("Original video metadata: {Width}x{Height}, Duration: {Duration}, Size: {Size} bytes, Rotation: {Rotation}°",
                originalMetadata.OriginalWidth, originalMetadata.OriginalHeight, 
                originalMetadata.OriginalDuration, originalMetadata.OriginalFileSizeBytes, originalMetadata.OriginalRotation);
            
            // Ensure output directories exist
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

            // Process video using HandBrake
            var processedFileName = Path.GetFileName(outputPath);
            var thumbnailFileName = Path.GetFileName(thumbnailPath);

            await ProcessVideoFileAsync(inputPath, outputPath, config, originalMetadata);
            await _thumbnailService.GenerateThumbnailAsync(outputPath, thumbnailPath, config, originalMetadata);

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
                    : 1.0,
                OriginalRotation = originalMetadata.OriginalRotation,
                ProcessedRotation = originalMetadata.OriginalRotation, // Preserve rotation for mobile player
                DisplayWidth = originalMetadata.DisplayWidth,
                DisplayHeight = originalMetadata.DisplayHeight
            };

            _logger.LogInformation("HandBrake video processing completed successfully: {OutputPath}\n{Metadata}", outputPath, metadata);

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
            _logger.LogError(ex, "HandBrake video processing failed for {InputPath}", inputPath);
            return new VideoProcessingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingDuration = DateTime.UtcNow - startTime
            };
        }
    }

    /// <summary>
    /// Get video metadata using FFprobe (HandBrake doesn't provide metadata extraction)
    /// </summary>
    public async Task<VideoMetadata?> GetVideoMetadataAsync(string videoPath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{videoPath}\"",
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
    /// Parse FFprobe JSON output to extract video metadata
    /// </summary>
    private VideoMetadata? ParseVideoInfo(string ffprobeOutput)
    {
        try
        {
            using var document = JsonDocument.Parse(ffprobeOutput);
            var root = document.RootElement;

            if (!root.TryGetProperty("streams", out var streams))
                return null;

            JsonElement? videoStream = null;
            foreach (var stream in streams.EnumerateArray())
            {
                if (stream.TryGetProperty("codec_type", out var codecType) && 
                    codecType.GetString() == "video")
                {
                    videoStream = stream;
                    break;
                }
            }

            if (videoStream == null)
                return null;

            var format = root.GetProperty("format");
            var fileInfo = new FileInfo(format.GetProperty("filename").GetString()!);

            // Extract basic properties
            var width = videoStream.Value.GetProperty("width").GetInt32();
            var height = videoStream.Value.GetProperty("height").GetInt32();
            var durationStr = format.GetProperty("duration").GetString();
            var duration = TimeSpan.FromSeconds(double.Parse(durationStr ?? "0"));
            var fileSizeBytes = fileInfo.Length;
            var formatName = format.GetProperty("format_name").GetString() ?? "";
            var bitrate = format.TryGetProperty("bit_rate", out var bitrateElement) 
                ? double.Parse(bitrateElement.GetString() ?? "0") 
                : 0;

            // Extract rotation from metadata - handle both display matrix and rotate tag
            var rotation = ExtractRotationFromMetadata(videoStream.Value);

            // Calculate display dimensions based on rotation
            var (displayWidth, displayHeight) = GetDisplayDimensions(width, height, rotation);

            return new VideoMetadata
            {
                OriginalWidth = width,
                OriginalHeight = height,
                ProcessedWidth = width,
                ProcessedHeight = height,
                OriginalDuration = duration,
                ProcessedDuration = duration,
                OriginalFileSizeBytes = fileSizeBytes,
                ProcessedFileSizeBytes = fileSizeBytes,
                OriginalFormat = formatName,
                ProcessedFormat = formatName,
                OriginalBitrate = bitrate,
                ProcessedBitrate = bitrate,
                CompressionRatio = 1.0,
                OriginalRotation = rotation,
                ProcessedRotation = rotation,
                DisplayWidth = displayWidth,
                DisplayHeight = displayHeight
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse video metadata");
            return null;
        }
    }

    /// <summary>
    /// Calculate display dimensions based on rotation
    /// </summary>
    private (int width, int height) GetDisplayDimensions(int width, int height, int rotation)
    {
        var normalizedRotation = NormalizeRotation(rotation);
        return normalizedRotation == 90 || normalizedRotation == 270 
            ? (height, width) 
            : (width, height);
    }

    /// <summary>
    /// Normalize rotation to 0, 90, 180, or 270 degrees
    /// </summary>
    private int NormalizeRotation(int rotation)
    {
        rotation = rotation % 360;
        if (rotation < 0) rotation += 360;

        return rotation switch
        {
            >= 315 or < 45 => 0,
            >= 45 and < 135 => 90,
            >= 135 and < 225 => 180,
            _ => 270  // >= 225 and < 315
        };
    }

    /// <summary>
    /// Process video file using HandBrake CLI
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

        _logger.LogInformation("Processing video with HandBrake: {Width}x{Height} rotation={Rotation}° -> {TargetWidth}x{TargetHeight}",
            originalMetadata.OriginalWidth, originalMetadata.OriginalHeight, rotation, targetWidth, targetHeight);

        // Build HandBrake arguments
        var arguments = BuildHandBrakeArguments(inputPath, outputPath, config, normalizedRotation, targetWidth, targetHeight);

        _logger.LogInformation("HandBrake command: {HandBrakePath} {Arguments}", _handBrakePath, arguments);

        // Execute HandBrake
        await ExecuteHandBrakeAsync(arguments, "video processing");
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
    /// Build HandBrake command line arguments
    /// </summary>
    private string BuildHandBrakeArguments(string inputPath, string outputPath, VideoProcessingConfig config,
        int rotation, int targetWidth, int targetHeight)
    {
        var args = new List<string>
        {
            "-i", $"\"{inputPath}\"",
            "-o", $"\"{outputPath}\"",

            // Video encoder settings
            "-e", MapVideoCodec(config.VideoCodec),
            "-q", "22.0", // Use constant quality instead of bitrate for better quality

            // Audio settings
            "-E", MapAudioCodec(config.AudioCodec),

            // Dimensions
            "-w", targetWidth.ToString(),
            "-l", targetHeight.ToString(),

            // Container format
            "-f", "av_mp4",

            // Optimize for web streaming
            "-O",

            // Mobile compatibility
            "--encoder-preset", "medium",
            "--encoder-profile", "baseline",
            "--encoder-level", "3.1"
        };

        // Note: Disabling HandBrake rotation as it's not working correctly
        // Instead, we'll let the mobile video player handle rotation using metadata
        if (rotation != 0)
        {
            _logger.LogInformation("Detected rotation {Rotation}° - skipping HandBrake rotation, will preserve in metadata", rotation);
            // Don't add --rotate parameter to HandBrake
        }

        // Add pixel format for compatibility
        args.AddRange(new[] { "-x", "pix_fmt=yuv420p" });

        return string.Join(" ", args);
    }

    /// <summary>
    /// Map FFmpeg codec names to HandBrake codec names
    /// </summary>
    private string MapVideoCodec(string ffmpegCodec)
    {
        return ffmpegCodec.ToLowerInvariant() switch
        {
            "libx264" => "x264",
            "libx265" => "x265",
            "libvpx" => "VP8",
            "libvpx-vp9" => "VP9",
            _ => "x264" // Default fallback
        };
    }

    /// <summary>
    /// Map FFmpeg audio codec names to HandBrake codec names
    /// </summary>
    private string MapAudioCodec(string ffmpegCodec)
    {
        return ffmpegCodec.ToLowerInvariant() switch
        {
            "aac" => "av_aac",
            "libmp3lame" => "mp3",
            "libvorbis" => "vorbis",
            "libopus" => "opus",
            _ => "av_aac" // Default fallback
        };
    }

    /// <summary>
    /// Execute HandBrake with the given arguments
    /// </summary>
    private async Task ExecuteHandBrakeAsync(string arguments, string operation)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = _handBrakePath,
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
            _logger.LogError("HandBrake {Operation} failed with exit code {ExitCode}: {Error}",
                operation, process.ExitCode, error);
            throw new InvalidOperationException($"HandBrake {operation} failed: {error}");
        }

        _logger.LogDebug("HandBrake {Operation} completed successfully", operation);
    }






    /// <summary>
    /// Extract rotation from video metadata, handling both display matrix and rotate tag
    /// This addresses the common iPhone video rotation conflict where display matrix and rotate tag disagree
    /// </summary>
    private int ExtractRotationFromMetadata(JsonElement videoStream)
    {
        var rotation = 0;
        var hasDisplayMatrix = false;
        var hasRotateTag = false;
        var displayMatrixRotation = 0;
        var rotateTagRotation = 0;

        // First, check for display matrix (more reliable for modern videos)
        if (videoStream.TryGetProperty("side_data_list", out var sideDataList))
        {
            foreach (var sideData in sideDataList.EnumerateArray())
            {
                if (sideData.TryGetProperty("side_data_type", out var sideDataType) &&
                    sideDataType.GetString() == "Display Matrix")
                {
                    if (sideData.TryGetProperty("rotation", out var rotationElement))
                    {
                        double rotationValue = 0;
                        bool parsed = false;

                        // Handle both number and string types
                        if (rotationElement.ValueKind == JsonValueKind.Number)
                        {
                            rotationValue = rotationElement.GetDouble();
                            parsed = true;
                        }
                        else if (rotationElement.ValueKind == JsonValueKind.String)
                        {
                            parsed = double.TryParse(rotationElement.GetString(), out rotationValue);
                        }

                        if (parsed)
                        {
                            displayMatrixRotation = (int)Math.Round(rotationValue);
                            hasDisplayMatrix = true;
                            _logger.LogDebug("Found display matrix rotation: {Rotation}°", displayMatrixRotation);
                        }
                    }
                    break;
                }
            }
        }

        // Second, check for rotate tag (legacy method)
        if (videoStream.TryGetProperty("tags", out var tags))
        {
            if (tags.TryGetProperty("rotate", out var rotateElement))
            {
                bool parsed = false;

                // Handle both number and string types
                if (rotateElement.ValueKind == JsonValueKind.Number)
                {
                    rotateTagRotation = rotateElement.GetInt32();
                    parsed = true;
                }
                else if (rotateElement.ValueKind == JsonValueKind.String)
                {
                    parsed = int.TryParse(rotateElement.GetString(), out rotateTagRotation);
                }

                if (parsed)
                {
                    hasRotateTag = true;
                    _logger.LogDebug("Found rotate tag: {Rotation}°", rotateTagRotation);
                }
            }
        }

        // Determine the correct rotation value
        if (hasDisplayMatrix && hasRotateTag)
        {
            // Both are present - this is the iPhone conflict case
            _logger.LogWarning("Video has conflicting rotation metadata - Display Matrix: {DisplayMatrix}°, Rotate Tag: {RotateTag}°",
                displayMatrixRotation, rotateTagRotation);

            // For iPhone videos with conflicting metadata, use the rotate tag
            // The rotate tag typically represents the intended display orientation
            rotation = NormalizeRotation(rotateTagRotation);

            _logger.LogInformation("Resolved rotation conflict: using rotate tag {Rotation}° (ignoring display matrix {DisplayMatrix}°)",
                rotation, displayMatrixRotation);
        }
        else if (hasDisplayMatrix)
        {
            // Only display matrix - use it
            rotation = NormalizeRotation(displayMatrixRotation);
            if (displayMatrixRotation < 0)
            {
                rotation = NormalizeRotation(360 + displayMatrixRotation);
            }
            _logger.LogDebug("Using display matrix rotation: {Rotation}°", rotation);
        }
        else if (hasRotateTag)
        {
            // Only rotate tag - use it
            rotation = NormalizeRotation(rotateTagRotation);
            _logger.LogDebug("Using rotate tag: {Rotation}°", rotation);
        }
        else
        {
            // No rotation metadata found
            rotation = 0;
            _logger.LogDebug("No rotation metadata found, assuming 0°");
        }

        return rotation;
    }
}
