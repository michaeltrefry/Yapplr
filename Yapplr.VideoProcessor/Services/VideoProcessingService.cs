using FFMpegCore;
using Yapplr.Shared.Models;
using Serilog.Context;

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
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = Path.GetDirectoryName(ffmpegPath) ?? throw new InvalidOperationException() });
        }
    }

    /// <summary>
    /// Validates that the specified codec is available in the current FFmpeg installation
    /// </summary>
    private async Task<bool> IsCodecAvailableAsync(string codecName, bool isVideoCodec = true)
    {
        try
        {
            var codecType = isVideoCodec ? "encoders" : "encoders";
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _configuration["FFmpeg:BinaryPath"] ?? "ffmpeg",
                    Arguments = $"-{codecType}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Contains(codecName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check codec availability for {CodecName}", codecName);
            return false;
        }
    }

    /// <summary>
    /// Gets the best available video codec from the fallback list
    /// </summary>
    private async Task<string> GetBestAvailableVideoCodecAsync(string preferredCodec)
    {
        // First try the preferred codec
        if (await IsCodecAvailableAsync(preferredCodec, true))
        {
            _logger.LogInformation("Using preferred video codec: {Codec}", preferredCodec);
            return preferredCodec;
        }

        // Try fallback codecs
        var fallbackCodecs = _configuration.GetSection("VideoProcessing:FallbackCodecs:Video").Get<string[]>()
                           ?? new[] { "libx264", "libx265", "libvpx-vp9", "libvpx" };

        foreach (var codec in fallbackCodecs)
        {
            if (await IsCodecAvailableAsync(codec, true))
            {
                _logger.LogInformation("Using fallback video codec: {Codec} (preferred {PreferredCodec} not available)",
                    codec, preferredCodec);
                return codec;
            }
        }

        _logger.LogWarning("No suitable video codec found, using default: libx264");
        return "libx264"; // Last resort
    }

    /// <summary>
    /// Gets the best available audio codec from the fallback list
    /// </summary>
    private async Task<string> GetBestAvailableAudioCodecAsync(string preferredCodec)
    {
        // First try the preferred codec
        if (await IsCodecAvailableAsync(preferredCodec, false))
        {
            _logger.LogInformation("Using preferred audio codec: {Codec}", preferredCodec);
            return preferredCodec;
        }

        // Try fallback codecs
        var fallbackCodecs = _configuration.GetSection("VideoProcessing:FallbackCodecs:Audio").Get<string[]>()
                           ?? new[] { "aac", "libmp3lame", "libvorbis", "libopus" };

        foreach (var codec in fallbackCodecs)
        {
            if (await IsCodecAvailableAsync(codec, false))
            {
                _logger.LogInformation("Using fallback audio codec: {Codec} (preferred {PreferredCodec} not available)",
                    codec, preferredCodec);
                return codec;
            }
        }

        _logger.LogWarning("No suitable audio codec found, using default: aac");
        return "aac"; // Last resort
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
                    : 1.0,
                OriginalRotation = originalMetadata.OriginalRotation,
                ProcessedRotation = 0, // Always 0 after processing (rotation is corrected)
                DisplayWidth = originalMetadata.DisplayWidth,
                DisplayHeight = originalMetadata.DisplayHeight
            };

            _logger.LogInformation("Video processing completed successfully: {OutputPath}\n{Metadata}", outputPath, metadata);

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

            // Get rotation from video stream metadata
            var rotation = await GetVideoRotationAsync(videoPath);
            var normalizedRotation = NormalizeRotation(rotation);

            // Calculate display dimensions based on rotation
            var (displayWidth, displayHeight) = GetDisplayDimensions(
                videoStream.Width, videoStream.Height, normalizedRotation);

            _logger.LogInformation("Video metadata: {Width}x{Height}, rotation: {Rotation}°, display: {DisplayWidth}x{DisplayHeight}",
                videoStream.Width, videoStream.Height, normalizedRotation, displayWidth, displayHeight);

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
                CompressionRatio = 1.0,
                OriginalRotation = normalizedRotation,
                ProcessedRotation = normalizedRotation,
                DisplayWidth = displayWidth,
                DisplayHeight = displayHeight
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video metadata: {VideoPath}", videoPath);
            return null;
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

        // Validate and get best available codecs
        var videoCodec = await GetBestAvailableVideoCodecAsync(config.VideoCodec);
        var audioCodec = await GetBestAvailableAudioCodecAsync(config.AudioCodec);

        // Get rotation and calculate display dimensions
        var rotation = await GetVideoRotationAsync(inputPath);
        var normalizedRotation = NormalizeRotation(rotation);
        var (displayWidth, displayHeight) = GetDisplayDimensions(
            videoStream.Width, videoStream.Height, normalizedRotation);

        _logger.LogInformation("Video processing analysis:");
        _logger.LogInformation("  Original stream: {Width}x{Height}", videoStream.Width, videoStream.Height);
        _logger.LogInformation("  Raw rotation: {Rotation}°", rotation);
        _logger.LogInformation("  Normalized rotation: {NormalizedRotation}°", normalizedRotation);
        _logger.LogInformation("  Display dimensions: {DisplayWidth}x{DisplayHeight}", displayWidth, displayHeight);
        _logger.LogInformation("  Max dimensions: {MaxWidth}x{MaxHeight}", config.MaxWidth, config.MaxHeight);

        // Calculate scaling dimensions using display dimensions (after rotation)
        var aspectRatio = (double)displayWidth / displayHeight;

        int targetWidth, targetHeight;

        // Use flexible max dimensions - allow either orientation
        var maxDimension = Math.Max(config.MaxWidth, config.MaxHeight); // 1920

        _logger.LogInformation("  Max dimension (flexible): {MaxDimension}", maxDimension);

        // Check if we need to scale down
        if (displayWidth > maxDimension || displayHeight > maxDimension)
        {
            // Scale down while maintaining aspect ratio
            var scaleFactor = Math.Min((double)maxDimension / displayWidth, (double)maxDimension / displayHeight);
            targetWidth = (int)(displayWidth * scaleFactor);
            targetHeight = (int)(displayHeight * scaleFactor);

            _logger.LogInformation("  Scaling down by factor {ScaleFactor}: {DisplayWidth}x{DisplayHeight} -> {TargetWidth}x{TargetHeight}",
                scaleFactor, displayWidth, displayHeight, targetWidth, targetHeight);
        }
        else
        {
            // Keep display dimensions if smaller than max
            targetWidth = displayWidth;
            targetHeight = displayHeight;

            _logger.LogInformation("  No scaling needed: keeping {DisplayWidth}x{DisplayHeight}",
                displayWidth, displayHeight);
        }

        // Ensure dimensions are even numbers (required by libx264 encoder)
        // Round down to nearest even number to maintain aspect ratio as closely as possible
        targetWidth = (targetWidth / 2) * 2;
        targetHeight = (targetHeight / 2) * 2;

        _logger.LogInformation("Video scaling: {DisplayWidth}x{DisplayHeight} -> {TargetWidth}x{TargetHeight} (rotation: {Rotation}°)",
            displayWidth, displayHeight, targetWidth, targetHeight, normalizedRotation);

        // For videos with rotation metadata, we need to:
        // 1. Apply the rotation to get the correct physical dimensions
        // 2. Scale to target size
        // 3. Remove rotation metadata so the final video displays correctly

        var filterString = "";
        if (normalizedRotation == 90)
        {
            // 90° clockwise: transpose and scale
            filterString = $"transpose=1,scale={targetWidth}:{targetHeight}";
        }
        else if (normalizedRotation == 180)
        {
            // 180°: double transpose and scale
            filterString = $"transpose=1,transpose=1,scale={targetWidth}:{targetHeight}";
        }
        else if (normalizedRotation == 270)
        {
            // 270° clockwise (or -90°): transpose counterclockwise and scale
            filterString = $"transpose=2,scale={targetWidth}:{targetHeight}";
        }
        else
        {
            // No rotation needed, just scale
            filterString = $"scale={targetWidth}:{targetHeight}";
        }

        _logger.LogInformation("FFmpeg filter string: {FilterString}", filterString);

        await FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, true, options => options
                .WithVideoCodec(videoCodec)
                .WithAudioCodec(audioCodec)
                .WithVideoBitrate(config.TargetBitrate)
                .WithCustomArgument($"-vf {filterString}")
                .WithFastStart()
                // Remove rotation metadata from output
                .WithCustomArgument("-metadata:s:v:0 rotate=0"))
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

        // Get rotation and calculate display dimensions for thumbnail
        var rotation = await GetVideoRotationAsync(inputPath);
        var normalizedRotation = NormalizeRotation(rotation);
        var (displayWidth, displayHeight) = GetDisplayDimensions(
            videoStream.Width, videoStream.Height, normalizedRotation);

        // Calculate thumbnail dimensions using display dimensions (after rotation)
        var aspectRatio = (double)displayWidth / displayHeight;

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

        // Ensure thumbnail dimensions are even numbers (required by video encoders)
        thumbnailWidth = (thumbnailWidth / 2) * 2;
        thumbnailHeight = (thumbnailHeight / 2) * 2;

        _logger.LogInformation("Thumbnail scaling: {DisplayWidth}x{DisplayHeight} -> {ThumbnailWidth}x{ThumbnailHeight} (rotation: {Rotation}°)",
            displayWidth, displayHeight, thumbnailWidth, thumbnailHeight, normalizedRotation);

        // Build filter string for thumbnail rotation and scaling
        var thumbnailFilterString = "";
        if (normalizedRotation == 90)
        {
            thumbnailFilterString = $"transpose=1,scale={thumbnailWidth}:{thumbnailHeight}";
        }
        else if (normalizedRotation == 180)
        {
            thumbnailFilterString = $"transpose=1,transpose=1,scale={thumbnailWidth}:{thumbnailHeight}";
        }
        else if (normalizedRotation == 270)
        {
            thumbnailFilterString = $"transpose=2,scale={thumbnailWidth}:{thumbnailHeight}";
        }
        else
        {
            thumbnailFilterString = $"scale={thumbnailWidth}:{thumbnailHeight}";
        }

        await FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(thumbnailPath, true, options => options
                .WithCustomArgument($"-vf {thumbnailFilterString}")
                .WithFrameOutputCount(1)
                .Seek(TimeSpan.FromSeconds(config.ThumbnailTimeSeconds)))
            .ProcessAsynchronously();
    }

    /// <summary>
    /// Extract rotation value from video stream metadata
    /// </summary>
    private async Task<int> GetVideoRotationAsync(string videoPath)
    {
        try
        {
            // Use FFProbe directly to get rotation metadata
            var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
            var videoStream = mediaInfo.PrimaryVideoStream;

            if (videoStream == null)
            {
                _logger.LogWarning("No video stream found for rotation detection");
                return 0;
            }

            // Try to get rotation from the video stream
            var rotation = videoStream.Rotation;

            _logger.LogInformation("Video rotation detection: Rotation property = {Rotation}, Tags count = {TagsCount}",
                rotation, videoStream.Tags?.Count ?? 0);

            // Log all available tags for debugging
            if (videoStream.Tags != null)
            {
                foreach (var tag in videoStream.Tags)
                {
                    _logger.LogInformation("Video tag: {Key} = {Value}", tag.Key, tag.Value);
                }
            }

            // Try alternative rotation detection methods
            if (rotation == 0 && videoStream.Tags != null)
            {
                // Check for rotation in tags
                if (videoStream.Tags.TryGetValue("rotate", out var rotateTag))
                {
                    if (int.TryParse(rotateTag, out var tagRotation))
                    {
                        _logger.LogInformation("Found rotation in tags: {Rotation}°", tagRotation);
                        return tagRotation;
                    }
                }

                // Check for display matrix rotation
                if (videoStream.Tags.TryGetValue("displaymatrix", out var displayMatrix))
                {
                    _logger.LogInformation("Found display matrix: {DisplayMatrix}", displayMatrix);
                    // Display matrix parsing would be complex, but we can detect common patterns
                }
            }

            // If still no rotation found, try direct FFProbe command
            if (rotation == 0)
            {
                var directRotation = await GetRotationFromDirectFFProbeAsync(videoPath);
                if (directRotation != 0)
                {
                    _logger.LogInformation("Found rotation via direct FFProbe: {Rotation}°", directRotation);
                    return directRotation;
                }
            }

            _logger.LogInformation("Final detected rotation: {Rotation}°", rotation);
            return rotation;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract rotation metadata from video stream, assuming 0°");
            return 0;
        }
    }

    private async Task<int> GetRotationFromDirectFFProbeAsync(string videoPath)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _configuration["FFmpeg:BinaryPath"]?.Replace("ffmpeg", "ffprobe") ?? "ffprobe",
                    Arguments = $"-v quiet -select_streams v:0 -show_entries stream_tags=rotate -of csv=p=0 \"{videoPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            _logger.LogInformation("Direct FFProbe output: '{Output}', Error: '{Error}'", output?.Trim(), error?.Trim());

            if (!string.IsNullOrWhiteSpace(output) && int.TryParse(output.Trim(), out var rotation))
            {
                return rotation;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get rotation via direct FFProbe");
            return 0;
        }
    }

    private int GetVideoRotation(FFMpegCore.VideoStream videoStream)
    {
        try
        {
            // Fallback method for when we already have the video stream
            var rotation = videoStream.Rotation;
            _logger.LogInformation("Video rotation from stream: {Rotation}°", rotation);
            return rotation;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract rotation metadata from video stream, assuming 0°");
            return 0;
        }
    }

    /// <summary>
    /// Normalize rotation to 0, 90, 180, or 270 degrees
    /// </summary>
    private int NormalizeRotation(int rotation)
    {
        // Normalize rotation to 0-359 range
        var normalized = ((rotation % 360) + 360) % 360;

        // Round to nearest 90-degree increment
        if (normalized >= 315 || normalized < 45) return 0;
        if (normalized >= 45 && normalized < 135) return 90;
        if (normalized >= 135 && normalized < 225) return 180;
        if (normalized >= 225 && normalized < 315) return 270;

        return 0; // Default fallback
    }

    /// <summary>
    /// Calculate display dimensions based on rotation
    /// </summary>
    private (int width, int height) GetDisplayDimensions(int originalWidth, int originalHeight, int rotation)
    {
        // For 90° and 270° rotations, swap width and height
        if (rotation == 90 || rotation == 270)
        {
            return (originalHeight, originalWidth);
        }

        // For 0° and 180° rotations, keep original dimensions
        return (originalWidth, originalHeight);
    }
}