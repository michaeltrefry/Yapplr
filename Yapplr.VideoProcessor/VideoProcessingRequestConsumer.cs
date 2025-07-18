using MassTransit;
using Yapplr.Shared.Messages;
using Yapplr.Shared.Models;
using Yapplr.VideoProcessor.Services;
using Serilog.Context;

namespace Yapplr.VideoProcessor;

public class VideoProcessingRequestConsumer : IConsumer<VideoProcessingRequest>
{
    private readonly ILogger<VideoProcessingRequestConsumer> _logger;
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IConfiguration _configuration;

    public VideoProcessingRequestConsumer(
        ILogger<VideoProcessingRequestConsumer> logger,
        IVideoProcessingService videoProcessingService,
        IConfiguration configuration)
    {
        _logger = logger;
        _videoProcessingService = videoProcessingService;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<VideoProcessingRequest> context)
    {
        var request = context.Message;
        var startTime = DateTime.UtcNow;

        using var operationScope = Serilog.Context.LogContext.PushProperty("Operation", "ProcessVideoRequest");
        using var postScope = Serilog.Context.LogContext.PushProperty("PostId", request.PostId);
        using var userScope = Serilog.Context.LogContext.PushProperty("UserId", request.UserId);
        using var fileScope = Serilog.Context.LogContext.PushProperty("OriginalFileName", request.OriginalVideoFileName);
        using var correlationScope = Serilog.Context.LogContext.PushProperty("CorrelationId", context.CorrelationId?.ToString() ?? "unknown");

        _logger.LogInformation("Received video processing request for Post {PostId}, User {UserId}, File: {OriginalFileName}",
            request.PostId, request.UserId, request.OriginalVideoFileName);

        try
        {
            // Get configuration
            var config = GetVideoProcessingConfig();

            // Construct input path using our own configuration
            var inputPath = Path.Combine(config.InputPath, request.OriginalVideoFileName);
            
            // Generate output paths
            var processedFileName = GenerateProcessedFileName(request.OriginalVideoFileName);
            var thumbnailFileName = GenerateThumbnailFileName(request.OriginalVideoFileName);
            
            var outputPath = Path.Combine(config.OutputPath, processedFileName);
            var thumbnailPath = Path.Combine(config.ThumbnailPath, thumbnailFileName);

            // Process the video (validation should have been done by the API before sending this request)
            var result = await _videoProcessingService.ProcessVideoAsync(
                inputPath, 
                outputPath, 
                thumbnailPath, 
                config);

            if (result.Success)
            {
                _logger.LogInformation("Video processing completed successfully for Post {PostId} in {Duration}ms", 
                    request.PostId, result.ProcessingDuration.TotalMilliseconds);

                // Delete original file if configured to do so
                if (config.DeleteOriginalAfterProcessing && File.Exists(inputPath))
                {
                    try
                    {
                        File.Delete(inputPath);
                        _logger.LogInformation("Deleted original video file: {InputPath}", inputPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete original video file: {InputPath}", inputPath);
                    }
                }

                // Publish success message
                await context.Publish(new VideoProcessingCompleted
                {
                    PostId = request.PostId,
                    UserId = request.UserId,
                    OriginalVideoFileName = request.OriginalVideoFileName,
                    ProcessedVideoFileName = result.ProcessedVideoFileName,
                    ThumbnailFileName = result.ThumbnailFileName,
                    CompletedAt = DateTime.UtcNow,
                    ProcessingDuration = result.ProcessingDuration,
                    Metadata = result.Metadata
                });
            }
            else
            {
                _logger.LogError("Video processing failed for Post {PostId}: {Error}", request.PostId, result.ErrorMessage);
                
                await context.Publish(new VideoProcessingFailed
                {
                    PostId = request.PostId,
                    UserId = request.UserId,
                    OriginalVideoFileName = request.OriginalVideoFileName,
                    ErrorMessage = result.ErrorMessage ?? "Unknown processing error",
                    FailedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing video for Post {PostId}", request.PostId);
            
            await context.Publish(new VideoProcessingFailed
            {
                PostId = request.PostId,
                UserId = request.UserId,
                OriginalVideoFileName = request.OriginalVideoFileName,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                FailedAt = DateTime.UtcNow,
                StackTrace = ex.StackTrace
            });
        }
    }

    private VideoProcessingConfig GetVideoProcessingConfig()
    {
        var section = _configuration.GetSection("VideoProcessing");
        
        return new VideoProcessingConfig
        {
            MaxWidth = section.GetValue<int>("MaxWidth", 1920),
            MaxHeight = section.GetValue<int>("MaxHeight", 1080),
            TargetBitrate = section.GetValue<int>("TargetBitrate", 2000),
            OutputFormat = section.GetValue<string>("OutputFormat", "mp4")!,
            VideoCodec = section.GetValue<string>("VideoCodec", "libx264")!,
            AudioCodec = section.GetValue<string>("AudioCodec", "aac")!,
            ThumbnailWidth = section.GetValue<int>("ThumbnailWidth", 320),
            ThumbnailHeight = section.GetValue<int>("ThumbnailHeight", 240),
            ThumbnailTimeSeconds = section.GetValue<double>("ThumbnailTimeSeconds", 1.0),
            InputPath = section.GetValue<string>("InputPath", "/app/uploads/videos")!,
            OutputPath = section.GetValue<string>("OutputPath", "/app/uploads/processed")!,
            ThumbnailPath = section.GetValue<string>("ThumbnailPath", "/app/uploads/thumbnails")!,
            DeleteOriginalAfterProcessing = section.GetValue<bool>("DeleteOriginalAfterProcessing", true)
        };
    }

    private static string GenerateProcessedFileName(string originalFileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        return $"{nameWithoutExtension}_processed.mp4";
    }

    private static string GenerateThumbnailFileName(string originalFileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        return $"{nameWithoutExtension}_thumb.jpg";
    }
}
